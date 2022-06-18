/*
 * Copyright (c) 2021 Snowflake Computing Inc. All rights reserved.
 */

using System.Globalization;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Tortuga.Data.Snowflake.Core.FileTransfer.StorageClient;
using Tortuga.Data.Snowflake.Core.Messages;
using Tortuga.Data.Snowflake.Core.RequestProcessing;
using Tortuga.Data.Snowflake.Core.ResponseProcessing;
using Tortuga.Data.Snowflake.Core.Sessions;
using Tortuga.Data.Snowflake.Legacy;

namespace Tortuga.Data.Snowflake.Core.FileTransfer;

/// <summary>
/// Class responsible for uploading and downloading files to the remote client.
/// </summary>
class SFFileTransferAgent
{
	/// <summary>
	/// Auto-detect keyword for source compression type auto detection.
	/// </summary>
	const string COMPRESSION_AUTO_DETECT = "auto_detect";

	/// <summary>
	/// The Snowflake query
	/// </summary>
	readonly string m_Query;

	/// <summary>
	/// The Snowflake session
	/// </summary>
	readonly SFSession m_Session;

	/// <summary>
	/// External cancellation token, used to stop the transfer
	/// </summary>
	readonly CancellationToken m_ExternalCancellationToken;

	/// <summary>
	/// The type of transfer either UPLOAD or DOWNLOAD.
	/// </summary>
	readonly CommandTypes m_CommandType;

	/// <summary>
	/// The file metadata. Applies to all files being uploaded/downloaded
	/// </summary>
	readonly PutGetResponseData m_TransferMetadata;

	/// <summary>
	/// List of metadata for small and large files.
	/// </summary>
	readonly List<SFFileMetadata> m_FilesMetas = new();

	readonly List<SFFileMetadata> m_SmallFilesMetas = new();
	readonly List<SFFileMetadata> m_LargeFilesMetas = new();

	/// <summary>
	/// List of metadata for the resulting file after upload/download.
	/// </summary>
	readonly List<SFFileMetadata> m_ResultsMetas = new();

	/// <summary>
	/// List of encryption materials of the files to be uploaded/downloaded.
	/// </summary>
	readonly List<PutGetEncryptionMaterial> m_EncryptionMaterials = new();

	/// <summary>
	/// String indicating local storage type.
	/// </summary>
	const string LOCAL_FS = "LOCAL_FS";

	/// <summary>
	/// Constructor.
	/// </summary>
	public SFFileTransferAgent(
		string query,
		SFSession session,
		PutGetResponseData responseData,
		CancellationToken cancellationToken)
	{
		m_Query = query;
		m_Session = session;
		m_TransferMetadata = responseData;
		m_CommandType = (CommandTypes)Enum.Parse(typeof(CommandTypes), m_TransferMetadata.Command!, true);
		m_ExternalCancellationToken = cancellationToken;
	}

	/// <summary>
	/// Execute the PUT/GET command.
	/// </summary>
	public void Execute()
	{
		// Initialize the encryption metadata
		InitEncryptionMaterial();

		if (CommandTypes.UPLOAD == m_CommandType)
		{
			// Initialize the list of actual files to upload
			var expandedSrcLocations = new List<string>();
			foreach (var location in m_TransferMetadata.SourceLocations!)
			{
				expandedSrcLocations.AddRange(ExpandFileNames(location));
			}

			// Initialize each file specific metadata (for example, file path, name and size) and
			// put it in1 of the 2 lists : Small files and large files based on a threshold
			// extracted from the command response
			InitFileMetadata(expandedSrcLocations);

			if (expandedSrcLocations.Count == 0)
			{
				throw new ArgumentException("No file found for: " + m_TransferMetadata.SourceLocations[0].ToString());
			}
		}
		else if (CommandTypes.DOWNLOAD == m_CommandType)
		{
			InitFileMetadata(m_TransferMetadata.SourceLocations!);

			Directory.CreateDirectory(m_TransferMetadata.LocalLocation!);
		}

		// Update the file metadata with GCS presigned URL
		UpdatePresignedUrl();

		foreach (var fileMetadata in m_FilesMetas)
		{
			// If the file is larger than the threshold, add it to the large files list
			// Otherwise add it to the small files list
			if (fileMetadata.SrcFileSize > m_TransferMetadata.Threshold)
				m_LargeFilesMetas.Add(fileMetadata);
			else
				m_SmallFilesMetas.Add(fileMetadata);
		}

		// Check command type
		if (CommandTypes.UPLOAD == m_CommandType)
			Upload();
		else if (CommandTypes.DOWNLOAD == m_CommandType)
			Download();
	}

	/// <summary>
	/// Generate the result set based on the file metadata.
	/// </summary>
	/// <returns>The result set containing file status and info</returns>
	public SFBaseResultSet Result()
	{
		// Set the row count using the number of metadata in the result metas
#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
		m_TransferMetadata.RowSet = new string[m_ResultsMetas.Count, 8];
#pragma warning restore CA1814 // Prefer jagged arrays over multidimensional

		// For each file metadata, set the result set variables
		for (var index = 0; index < m_ResultsMetas.Count; index++)
		{
			m_TransferMetadata.RowSet[index, 0] = m_ResultsMetas[index].SrcFileName;
			m_TransferMetadata.RowSet[index, 1] = m_ResultsMetas[index].DestFileName;
			m_TransferMetadata.RowSet[index, 2] = m_ResultsMetas[index].SrcFileSize.ToString(CultureInfo.InvariantCulture);
			m_TransferMetadata.RowSet[index, 3] = m_ResultsMetas[index].DestFileSize.ToString(CultureInfo.InvariantCulture);
			m_TransferMetadata.RowSet[index, 4] = m_ResultsMetas[index].ResultStatus;

			if (m_ResultsMetas[index].LastError != null)
				m_TransferMetadata.RowSet[index, 5] = m_ResultsMetas[index].LastError!.ToString();
			else
				m_TransferMetadata.RowSet[index, 5] = null;

			if (m_ResultsMetas[index].SourceCompression.Name != null)
				m_TransferMetadata.RowSet[index, 6] = m_ResultsMetas[index].SourceCompression.Name;
			else
				m_TransferMetadata.RowSet[index, 6] = null;

			if (m_ResultsMetas[index].TargetCompression.Name != null)
				m_TransferMetadata.RowSet[index, 7] = m_ResultsMetas[index].TargetCompression.Name;
			else
				m_TransferMetadata.RowSet[index, 7] = null;
		}

		return new SFResultSet(m_TransferMetadata, new SFStatement(m_Session), m_ExternalCancellationToken);
	}

	/// <summary>
	/// Upload files sequentially or in parallel.
	/// </summary>
	void Upload()
	{
		//Start the upload tasks(for small files upload in parallel using the given parallelism
		//factor, for large file updload sequentially)
		//For each file, using the remote client
		if (0 < m_LargeFilesMetas.Count)
		{
			foreach (var fileMetadata in m_LargeFilesMetas)
				UploadFilesInSequential(fileMetadata);
		}

		if (0 < m_SmallFilesMetas.Count)
			UploadFilesInParallel(m_SmallFilesMetas, m_TransferMetadata.Parallel);
	}

	/// <summary>
	/// Download files sequentially or in parallel.
	/// </summary>
	void Download()
	{
		//Start the download tasks(for small files download in parallel using the given parallelism
		//factor, for large file download sequentially)
		//For each file, using the remote client
		if (0 < m_LargeFilesMetas.Count)
		{
			foreach (var fileMetadata in m_LargeFilesMetas)
				DownloadFilesInSequential(fileMetadata);
		}
		if (0 < m_SmallFilesMetas.Count)
			DownloadFilesInParallel(m_SmallFilesMetas, m_TransferMetadata.Parallel);
	}

	/// <summary>
	/// Get the pre-signed URL and update the file metadata.
	/// </summary>
	void UpdatePresignedUrl()
	{
		// Pre-signed url only applies to GCS
		if (m_TransferMetadata.StageInfo!.LocationType == "GCS")
		{
			if (CommandTypes.UPLOAD == m_CommandType)
			{
				foreach (var fileMeta in m_FilesMetas)
				{
					var filePathToReplace = GetFilePathFromPutCommand(m_Query);
					var fileNameToReplaceWith = fileMeta.DestFileName;
					var queryWithSingleFile = m_Query;
					queryWithSingleFile = queryWithSingleFile.Replace(filePathToReplace, fileNameToReplaceWith, StringComparison.Ordinal);

					var sfStatement = new SFStatement(m_Session) { m_IsPutGetQuery = true };

					var response = sfStatement.ExecuteHelper<PutGetExecResponse, PutGetResponseData>(0, queryWithSingleFile, null, false);

					fileMeta.StageInfo = response.Data!.StageInfo!;
					fileMeta.PresignedUrl = response.Data!.StageInfo!.PresignedUrl;
				}
			}
			else if (CommandTypes.DOWNLOAD == m_CommandType)
			{
				for (var index = 0; index < m_FilesMetas.Count; index++)
					m_FilesMetas[index].PresignedUrl = m_TransferMetadata.PresignedUrls![index];
			}
		}
	}

	/// <summary>
	/// Obtain the file path from the PUT query.
	/// </summary>
	/// <param name="query">The query containing the file path</param>
	/// <returns>The file path contained by the query</returns>
	static string GetFilePathFromPutCommand(string query)
	{
		// Extract file path from PUT command:
		// E.g. "PUT file://C:<path-to-file> @DB.SCHEMA.%TABLE;"
		var startIndex = query.IndexOf("file://", StringComparison.Ordinal) + "file://".Length;
		var endIndex = query.Substring(startIndex).IndexOf(' ', StringComparison.Ordinal);
		var filePath = query.Substring(startIndex, endIndex);
		return filePath;
	}

	/// <summary>
	/// Initialize the encryption materials for file encryption.
	/// </summary>
	void InitEncryptionMaterial()
	{
		if (CommandTypes.UPLOAD == m_CommandType)
			m_EncryptionMaterials.Add(m_TransferMetadata.EncryptionMaterial![0]);
	}

	/// <summary>
	/// Initialize the file metadata of each file to be uploaded/downloaded.
	/// </summary>
	/// <param name="files">List of files to obtain metadata from</param>
	void InitFileMetadata(List<string> files)
	{
		if (CommandTypes.UPLOAD == m_CommandType)
		{
			foreach (var file in files)
			{
				var fileInfo = new FileInfo(file);

				//  Retrieve / Compute the file actual compression type for each file in the list(most work is for auto - detect)
				var fileName = fileInfo.Name;
				SFFileCompressionTypes.SFFileCompressionType compressionType;

				if (m_TransferMetadata.AutoCompress && m_TransferMetadata.SourceCompression!.Equals(COMPRESSION_AUTO_DETECT, StringComparison.Ordinal))
				{
					// Auto-detect source compression type
					// Will return NONE if no matching type is found
					compressionType = SFFileCompressionTypes.GuessCompressionType(file);
				}
				else
				{
					// User defined source compression type
					compressionType = SFFileCompressionTypes.LookUpByName(m_TransferMetadata.SourceCompression!);
				}

				// Verify that the compression type is supported
				if (!compressionType.IsSupported)
				{
					//   SqlState.FEATURE_NOT_SUPPORTED = 0A000
					throw new SFException("0A000", SFError.InternalError, compressionType.Name);
				}

				var fileMetadata = new SFFileMetadata()
				{
					SrcFilePath = file,
					SrcFileName = fileName,
					SrcFileSize = fileInfo.Length,
					StageInfo = m_TransferMetadata.StageInfo,
					Overwrite = m_TransferMetadata.Overwrite,
					// Need to compress before sending only if autoCompress is On and the file is
					// not compressed yet
					RequireCompress = (m_TransferMetadata.AutoCompress && (SFFileCompressionTypes.NONE.Equals(compressionType))),
					SourceCompression = compressionType,
					PresignedUrl = m_TransferMetadata.StageInfo!.PresignedUrl,
					// If the file is under the threshold, don't upload in chunks, set parallel to 1
					Parallel = (fileInfo.Length > m_TransferMetadata.Threshold) ? m_TransferMetadata.Parallel : 1,
				};

				if (!fileMetadata.RequireCompress)
				{
					// The file is already compressed
					fileMetadata.TargetCompression = fileMetadata.SourceCompression;
					fileMetadata.DestFileName = fileName;
				}
				else
				{
					// The file will need to be compressed using gzip
					fileMetadata.TargetCompression = SFFileCompressionTypes.GZIP;
					fileMetadata.DestFileName = fileName + SFFileCompressionTypes.GZIP.FileExtension;
				}

				if (m_EncryptionMaterials.Count > 0)
					fileMetadata.EncryptionMaterial = m_EncryptionMaterials[0];

				m_FilesMetas.Add(fileMetadata);
			}
		}
		else if (CommandTypes.DOWNLOAD == m_CommandType)
		{
			for (var index = 0; index < files.Count; index++)
			{
				var file = files[index];
				var fileMetadata = new SFFileMetadata()
				{
					SrcFileName = file,
					DestFileName = file,
					LocalLocation = m_TransferMetadata.LocalLocation,
					StageInfo = m_TransferMetadata.StageInfo,
					Overwrite = m_TransferMetadata.Overwrite,
					PresignedUrl = m_TransferMetadata.StageInfo!.PresignedUrl,
					Parallel = m_TransferMetadata.Parallel,
					EncryptionMaterial = m_TransferMetadata.EncryptionMaterial![index]
				};

				m_FilesMetas.Add(fileMetadata);
			}
		}
	}

	/// <summary>
	/// Expand the expand the wildcards if any to generate the list of paths for all files
	/// matched by the wildcards. Also replace
	/// Get the absolute path for the file.
	/// </summary>
	/// <param name="location">The path to expand</param>
	/// <returns>The list of file matching the input location</returns>
	/// <exception cref="DirectoryNotFoundException">Directory not found. Could not find a part of the pat </exception>
	/// <exception cref="FileNotFoundException">File not found or the path is pointing to a Directory</exception>
	static List<string> ExpandFileNames(string location)
	{
		// Replace ~ with the user home directory path
		if (location.Contains('~', StringComparison.Ordinal))
		{
			var homePath = (Environment.OSVersion.Platform == PlatformID.Unix ||
			Environment.OSVersion.Platform == PlatformID.MacOSX)
			? Environment.GetEnvironmentVariable("HOME")
			: Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");

			location = location.Replace("~", homePath, StringComparison.Ordinal);
		}

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			location = Path.GetFullPath(location);

		var fileName = Path.GetFileName(location);
		var directoryName = Path.GetDirectoryName(location)!;

		var filePaths = new List<string>();
		//filePaths.Add(""); //Start with an empty string to build upon
		if (directoryName.Contains('?', StringComparison.Ordinal) || directoryName.Contains('*', StringComparison.Ordinal))
		{
			// If there is a wildcard in at least one of the directory name in the file path
			var pathParts = location.Split(Path.DirectorySeparatorChar);

			string currPart;
			for (var i = 0; i < pathParts.Length; i++)
			{
				var tempPaths = new List<string>();
				foreach (var filePath in filePaths)
				{
					currPart = pathParts[i];

					if (currPart.Contains('?', StringComparison.Ordinal) || currPart.Contains('*', StringComparison.Ordinal))
					{
						if (i < pathParts.Length - 1)
						{
							// Expand the directories names
							tempPaths.AddRange(Directory.GetDirectories(filePath, currPart, SearchOption.TopDirectoryOnly));
						}
						else
						{
							// Expand the files names
							tempPaths.AddRange(Directory.GetFiles(filePath, currPart, SearchOption.TopDirectoryOnly));
						}
					}
					else
					{
						if (0 < i) // Keep building the paths
							tempPaths.Add(filePath + Path.DirectorySeparatorChar + currPart);
						else // First part
							tempPaths.Add(currPart);
					}
				}
				filePaths = tempPaths;
			}
		}
		else if (fileName.Contains('?', StringComparison.Ordinal) || fileName.Contains('*', StringComparison.Ordinal))
		{
			var ext = Path.GetExtension(fileName);
			if ((4 == ext.Length) && fileName.Contains('*', StringComparison.Ordinal))
			{
				/*
					* When you use the asterisk wildcard character in a searchPattern such as
					* "*.txt", the number of characters in the specified extension affects the
					* search as follows:
					* - If the specified extension is exactly three characters long, the method
					* returns files with extensions that begin with the specified extension.
					* For example, "*.xls" returns both "book.xls" and "book.xlsx".
					* - In all other cases, the method returns files that exactly match the
					* specified extension. For example, "*.ai" returns "file.ai" but not "file.aif".
					*/
				var potentialMatches = Directory.GetFiles(directoryName, fileName, SearchOption.TopDirectoryOnly);
				foreach (var potentialMatch in potentialMatches)
				{
					if (potentialMatch.EndsWith(ext, StringComparison.Ordinal))
						filePaths.Add(potentialMatch);
				}
			}
			else
			{
				// If there is a wildcard in the file name in the file path
				filePaths.AddRange(
					Directory.GetFiles(
						directoryName,
						fileName,
						SearchOption.TopDirectoryOnly));
			}
		}
		else
		{
			// No wild card, just make sure it's a file
			var attr = File.GetAttributes(location);
			if (!attr.HasFlag(FileAttributes.Directory))
				filePaths.Add(location);
			else
				throw new FileNotFoundException("Directories not supported, you need to provide a file path", location);
		}

		return filePaths;
	}

	/// <summary>
	/// Compress a file using the given file metadata (file path, compression type, etc...) and
	/// update the metadata accordingly after the compression is finished.
	/// </summary>
	/// <param name="fileMetadata">The metadata for the file to compress.</param>
	static void CompressFileWithGzip(SFFileMetadata fileMetadata)
	{
		if (fileMetadata.SrcFilePath == null)
			throw new ArgumentException("fileMetadata.srcFilePath is null", nameof(fileMetadata));
		if (fileMetadata.TmpDir == null)
			throw new ArgumentException("fileMetadata.tmpDir is null", nameof(fileMetadata));

		var fileToCompress = new FileInfo(fileMetadata.SrcFilePath);
		fileMetadata.RealSrcFilePath = Path.Combine(fileMetadata.TmpDir, fileMetadata.SrcFileName + "_c.gz");

		using (var originalFileStream = fileToCompress.OpenRead())
		{
			if ((File.GetAttributes(fileToCompress.FullName) &
			   FileAttributes.Hidden) != FileAttributes.Hidden)
			{
				using (var compressedFileStream = File.Create(fileMetadata.RealSrcFilePath))
				{
					using (var compressionStream = new GZipStream(compressedFileStream, CompressionMode.Compress))
					{
						originalFileStream.CopyTo(compressionStream);
					}
				}

				var destInfo = new FileInfo(fileMetadata.RealSrcFilePath);
				fileMetadata.DestFileSize = destInfo.Length;
			}
		}
	}

	/// <summary>
	/// Get digest and size of file to be uploaded.
	/// </summary>
	/// <param name="fileMetadata">The metadata for the file to get digest.</param>
	static void GetDigestAndSizeForFile(SFFileMetadata fileMetadata)
	{
		if (fileMetadata.RealSrcFilePath == null)
			throw new ArgumentException("fileMetadata.realSrcFilePath is null", nameof(fileMetadata));

		using (SHA256 SHA256 = SHA256.Create())
		{
			using (var fileStream = File.OpenRead(fileMetadata.RealSrcFilePath))
			{
				fileMetadata.Sha256Digest = Convert.ToBase64String(SHA256.ComputeHash(fileStream));
				fileMetadata.UploadSize = fileStream.Length;
			}
		}
	}

	/// <summary>
	/// Renew expired client.
	/// </summary>
	/// <returns>The renewed storage client.</returns>
	ISFRemoteStorageClient? RenewExpiredClient()
	{
		var sfStatement = new SFStatement(m_Session);

		var response = sfStatement.ExecuteHelper<PutGetExecResponse, PutGetResponseData>(0, m_TransferMetadata.Command!, null, false);

		return SFRemoteStorage.GetRemoteStorageType(response.Data!);
	}

	/// <summary>
	/// Upload a list of files in parallel using the given parallelization factor.
	/// </summary>
	/// <param name="fileMetadata">The metadata of the file to upload.</param>
	/// <returns>The result outcome for each file.</returns>
	void UploadFilesInSequential(SFFileMetadata fileMetadata)
	{
		/// The storage client used to upload/download data from files or streams
		fileMetadata.Client = SFRemoteStorage.GetRemoteStorageType(m_TransferMetadata);
		var resultMetadata = UploadSingleFile(fileMetadata);

		if (resultMetadata.ResultStatus == ResultStatus.RENEW_TOKEN.ToString())
			fileMetadata.Client = RenewExpiredClient();
		else if (resultMetadata.ResultStatus == ResultStatus.RENEW_PRESIGNED_URL.ToString())
			UpdatePresignedUrl();

		m_ResultsMetas.Add(resultMetadata);
	}

	/// <summary>
	/// Download a list of files in parallel using the given parallelization factor.
	/// </summary>
	/// <param name="fileMetadata">The metadata of the file to download.</param>
	/// <returns>The result outcome for each file.</returns>
	void DownloadFilesInSequential(SFFileMetadata fileMetadata)
	{
		/// The storage client used to upload/download data from files or streams
		fileMetadata.Client = SFRemoteStorage.GetRemoteStorageType(m_TransferMetadata);
		var resultMetadata = DownloadSingleFile(fileMetadata);

		if (resultMetadata.ResultStatus == ResultStatus.RENEW_TOKEN.ToString())
			fileMetadata.Client = RenewExpiredClient();
		else if (resultMetadata.ResultStatus == ResultStatus.RENEW_PRESIGNED_URL.ToString())
			UpdatePresignedUrl();

		m_ResultsMetas.Add(resultMetadata);
	}

	/// <summary>
	/// Upload a list of files in parallel using the given parallelization factor.
	/// </summary>
	/// <param name="filesMetadata">The list of files to upload in parallel.</param>
	/// <param name="parallel">The number of files to upload in parallel.</param>
	/// <returns>The result outcome for each file.</returns>
	void UploadFilesInParallel(List<SFFileMetadata> filesMetadata, int parallel)
	{
		var listOfActions = new List<Action>();
		foreach (var fileMetadata in filesMetadata)
			listOfActions.Add(() => UploadFilesInSequential(fileMetadata));

		var options = new ParallelOptions { MaxDegreeOfParallelism = parallel };
		Parallel.Invoke(options, listOfActions.ToArray());
	}

	/// <summary>
	/// Download a list of files in parallel using the given parallelization factor.
	/// </summary>
	/// <param name="filesMetadata">The list of files to download in parallel.</param>
	/// <param name="parallel">The number of files to download in parallel.</param>
	/// <returns>The result outcome for each file.</returns>
	void DownloadFilesInParallel(List<SFFileMetadata> filesMetadata, int parallel)
	{
		var listOfActions = new List<Action>();
		foreach (var fileMetadata in filesMetadata)
			listOfActions.Add(() => DownloadFilesInSequential(fileMetadata));

		var options = new ParallelOptions { MaxDegreeOfParallelism = parallel };
		Parallel.Invoke(options, listOfActions.ToArray());
	}

	/// <summary>
	/// Upload a single file.
	/// </summary>
	/// <param name="storageClient">Storage client to upload the file with.</param>
	/// <param name="fileMetadata">The metadata of the file to upload.</param>
	/// <returns>The result outcome.</returns>
	SFFileMetadata UploadSingleFile(SFFileMetadata fileMetadata)
	{
		fileMetadata.RealSrcFilePath = fileMetadata.SrcFilePath;

		// Create tmp folder to store compressed files
		fileMetadata.TmpDir = GetTemporaryDirectory();

		try
		{
			// Compress the file if needed
			if (fileMetadata.RequireCompress)
				CompressFileWithGzip(fileMetadata);

			// Calculate the digest
			GetDigestAndSizeForFile(fileMetadata);

			GetStorageClientType(m_TransferMetadata.StageInfo!).UploadOneFileWithRetry(fileMetadata);
		}
		finally
		{
			Directory.Delete(fileMetadata.TmpDir, true);
		}

		return fileMetadata;
	}

	/// <summary>
	/// Download a single file.
	/// </summary>
	/// <param name="storageClient">Storage client to download the file with.</param>
	/// <param name="fileMetadata">The metadata of the file to download.</param>
	/// <returns>The result outcome.</returns>
	SFFileMetadata DownloadSingleFile(SFFileMetadata fileMetadata)
	{
		// Create tmp folder to store compressed files
		fileMetadata.TmpDir = GetTemporaryDirectory();

		try
		{
			GetStorageClientType(m_TransferMetadata.StageInfo!).DownloadOneFile(fileMetadata);
		}
		finally
		{
			Directory.Delete(fileMetadata.TmpDir, true);
		}

		return fileMetadata;
	}

	/// <summary>
	/// Create a temporary directory.
	/// </summary>
	/// <returns>The temporary directory name.</returns>
	/// Referenced from: https://stackoverflow.com/a/278457
	static string GetTemporaryDirectory()
	{
		var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
		Directory.CreateDirectory(tempDirectory);
		return tempDirectory;
	}

	/// <summary>
	/// Get the storage client type.
	/// </summary>
	/// <param name="stageInfo">The stage info used to get the stage location type.</param>
	/// <returns>The storage client type.</returns>
	public static SFStorage GetStorageClientType(PutGetStageInfo stageInfo)
	{
		if (stageInfo.LocationType == LOCAL_FS)
			return SFLocalStorage.Instance;
		else
			return SFRemoteStorage.Instance;
	}
}
