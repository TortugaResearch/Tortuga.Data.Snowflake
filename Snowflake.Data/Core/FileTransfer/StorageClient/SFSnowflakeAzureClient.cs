/*
 * Copyright (c) 2021 Snowflake Computing Inc. All rights reserved.
 */

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Newtonsoft.Json;
using Tortuga.Data.Snowflake.Legacy;

namespace Tortuga.Data.Snowflake.Core.FileTransfer.StorageClient;

/// <summary>
/// The azure client used to transfer files to the remote Azure storage.
/// </summary>
class SFSnowflakeAzureClient : ISFRemoteStorageClient
{
	const string EXPIRED_TOKEN = "ExpiredToken";
	const string NO_SUCH_KEY = "NoSuchKey";

	/// <summary>
	/// The attribute in the credential map containing the shared access signature token.
	/// </summary>
	const string AZURE_SAS_TOKEN = "AZURE_SAS_TOKEN";

	/// <summary>
	/// The cloud blob client to use to upload and download data on Azure.
	/// </summary>
	readonly BlobServiceClient m_BlobServiceClient;

	/// <summary>
	/// Azure client without client-side encryption.
	/// </summary>
	/// <param name="stageInfo">The command stage info.</param>
	public SFSnowflakeAzureClient(PutGetStageInfo stageInfo)
	{
		if (stageInfo.StageCredentials == null)
			throw new ArgumentException("stageInfo.stageCredentials is null", nameof(stageInfo));

		// Get the Azure SAS token and create the client
		if (stageInfo.StageCredentials.TryGetValue(AZURE_SAS_TOKEN, out var sasToken))
		{
			var blobEndpoint = $"https://{stageInfo.StorageAccount}.blob.core.windows.net";
			m_BlobServiceClient = new BlobServiceClient(new Uri(blobEndpoint),
				new AzureSasCredential(sasToken));
		}
		else
		{
			throw new ArgumentException($"{nameof(stageInfo)}.{nameof(stageInfo.StageCredentials)} does not contain an AZURE_SAS_TOKEN", nameof(stageInfo));
		}
	}

	RemoteLocation ISFRemoteStorageClient.ExtractBucketNameAndPath(string stageLocation) => ExtractBucketNameAndPath(stageLocation);

	/// <summary>
	/// Extract the bucket name and path from the stage location.
	/// </summary>
	/// <param name="stageLocation">The command stage location.</param>
	/// <returns>The remote location of the Azure file.</returns>
	static public RemoteLocation ExtractBucketNameAndPath(string stageLocation)
	{
		var blobName = stageLocation;
		string? azurePath = null;

		// Split stage location as bucket name and path
		if (stageLocation.Contains('/', StringComparison.Ordinal))
		{
			blobName = stageLocation.Substring(0, stageLocation.IndexOf('/', StringComparison.Ordinal));

			azurePath = stageLocation.Substring(stageLocation.IndexOf('/', StringComparison.Ordinal) + 1,
				stageLocation.Length - stageLocation.IndexOf('/', StringComparison.Ordinal) - 1);

			if (!string.IsNullOrEmpty(azurePath) && !azurePath.EndsWith("/", StringComparison.Ordinal))
			{
				azurePath += "/";
			}
		}

		return new RemoteLocation()
		{
			Bucket = blobName,
			Key = azurePath
		};
	}

	/// <summary>
	/// Get the file header.
	/// </summary>
	/// <param name="fileMetadata">The Azure file metadata.</param>
	/// <returns>The file header of the Azure file.</returns>
	public FileHeader? GetFileHeader(SFFileMetadata fileMetadata)
	{
		if (fileMetadata.StageInfo == null)
			throw new ArgumentException("fileMetadata.stageInfo is null", nameof(fileMetadata));
		if (fileMetadata.StageInfo.Location == null)
			throw new ArgumentException("fileMetadata.stageInfo.location is null", nameof(fileMetadata));

		var location = ExtractBucketNameAndPath(fileMetadata.StageInfo.Location);

		// Get the Azure client
		var containerClient = m_BlobServiceClient.GetBlobContainerClient(location.Bucket);
		var blobClient = containerClient.GetBlobClient(location.Key + fileMetadata.DestFileName);

		BlobProperties response;
		try
		{
			// Issue the GET request
			response = blobClient.GetProperties();
		}
		catch (Exception ex)
		{
			if (ex.Message.Contains(EXPIRED_TOKEN, StringComparison.Ordinal) || ex.Message.Contains("Status: 400", StringComparison.Ordinal))
			{
				fileMetadata.ResultStatus = ResultStatus.RENEW_TOKEN.ToString();
			}
			else if (ex.Message.Contains(NO_SUCH_KEY, StringComparison.Ordinal) || ex.Message.Contains("Status: 404", StringComparison.Ordinal))
			{
				fileMetadata.ResultStatus = ResultStatus.NOT_FOUND_FILE.ToString();
			}
			else
			{
				fileMetadata.ResultStatus = ResultStatus.ERROR.ToString();
			}
			return null;
		}

		fileMetadata.ResultStatus = ResultStatus.UPLOADED.ToString();

		dynamic encryptionData = JsonConvert.DeserializeObject(response.Metadata["encryptiondata"])!;
		var encryptionMetadata = new SFEncryptionMetadata
		{
			iv = encryptionData["ContentEncryptionIV"],
			key = encryptionData.WrappedContentKey["EncryptedKey"],
			matDesc = response.Metadata["matdesc"]
		};

		return new FileHeader
		{
			digest = response.Metadata["sfcdigest"],
			contentLength = response.ContentLength,
			encryptionMetadata = encryptionMetadata
		};
	}

	/// <summary>
	/// Upload the file to the Azure location.
	/// </summary>
	/// <param name="fileMetadata">The Azure file metadata.</param>
	/// <param name="fileBytes">The file bytes to upload.</param>
	/// <param name="encryptionMetadata">The encryption metadata for the header.</param>
	public void UploadFile(SFFileMetadata fileMetadata, byte[] fileBytes, SFEncryptionMetadata encryptionMetadata)
	{
		if (fileMetadata.StageInfo == null)
			throw new ArgumentException("fileMetadata.stageInfo is null", nameof(fileMetadata));
		if (fileMetadata.StageInfo.Location == null)
			throw new ArgumentException("fileMetadata.stageInfo.location is null", nameof(fileMetadata));

		// Create the JSON for the encryption data header
		var encryptionData = JsonConvert.SerializeObject(new EncryptionData
		{
			EncryptionMode = "FullBlob",
			WrappedContentKey = new WrappedContentInfo
			{
				KeyId = "symmKey1",
				EncryptedKey = encryptionMetadata.key,
				Algorithm = "AES_CBC_256"
			},
			EncryptionAgent = new EncryptionAgentInfo
			{
				Protocol = "1.0",
				EncryptionAlgorithm = "AES_CBC_256"
			},
			ContentEncryptionIV = encryptionMetadata.iv,
			KeyWrappingMetadata = new KeyWrappingMetadataInfo
			{
				EncryptionLibrary = "Java 5.3.0"
			}
		});

		// Create the metadata to use for the header
		var metadata = new Dictionary<string, string?>
		{
			{ "encryptiondata", encryptionData },
			{ "matdesc", encryptionMetadata.matDesc },
			{ "sfcdigest", fileMetadata.Sha256Digest }
		};

		var location = ExtractBucketNameAndPath(fileMetadata.StageInfo.Location);

		// Get the Azure client
		var containerClient = m_BlobServiceClient.GetBlobContainerClient(location.Bucket);
		var blobClient = containerClient.GetBlobClient(location.Key + fileMetadata.DestFileName);

		try
		{
			// Issue the POST/PUT request
			using (var content = new MemoryStream(fileBytes))
			{
				blobClient.Upload(content);
				blobClient.SetMetadata(metadata);
			}
		}
		catch (Exception ex)
		{
			if (ex.Message.Contains("Status: 400", StringComparison.Ordinal))
			{
				fileMetadata.ResultStatus = ResultStatus.RENEW_PRESIGNED_URL.ToString();
			}
			else if (ex.Message.Contains("Status: 401", StringComparison.Ordinal))
			{
				fileMetadata.ResultStatus = ResultStatus.RENEW_TOKEN.ToString();
			}
			else if (ex.Message.Contains("Status: 403", StringComparison.Ordinal) ||
				ex.Message.Contains("Status: 500", StringComparison.Ordinal) ||
				ex.Message.Contains("Status: 503", StringComparison.Ordinal))
			{
				fileMetadata.ResultStatus = ResultStatus.NEED_RETRY.ToString();
			}
			return;
		}

		fileMetadata.DestFileSize = fileMetadata.UploadSize;
		fileMetadata.ResultStatus = ResultStatus.UPLOADED.ToString();
	}

	/// <summary>
	/// Download the file to the local location.
	/// </summary>
	/// <param name="fileMetadata">The S3 file metadata.</param>
	/// <param name="fullDstPath">The local location to store downloaded file into.</param>
	/// <param name="maxConcurrency">Number of max concurrency.</param>
	public void DownloadFile(SFFileMetadata fileMetadata, string fullDstPath, int maxConcurrency)
	{
		if (fileMetadata.StageInfo == null)
			throw new ArgumentException("fileMetadata.stageInfo is null", nameof(fileMetadata));
		if (fileMetadata.StageInfo.Location == null)
			throw new ArgumentException("fileMetadata.stageInfo.location is null", nameof(fileMetadata));

		var location = ExtractBucketNameAndPath(fileMetadata.StageInfo.Location);

		// Get the Azure client
		var containerClient = m_BlobServiceClient.GetBlobContainerClient(location.Bucket);
		var blobClient = containerClient.GetBlobClient(location.Key + fileMetadata.DestFileName);

		try
		{
			// Issue the GET request
			blobClient.DownloadTo(fullDstPath);
		}
		catch (Exception ex)
		{
			if (ex.Message.Contains("Status: 401", StringComparison.Ordinal))
			{
				fileMetadata.ResultStatus = ResultStatus.RENEW_TOKEN.ToString();
			}
			else if (ex.Message.Contains("Status: 403", StringComparison.Ordinal) ||
				ex.Message.Contains("Status: 500", StringComparison.Ordinal) ||
				ex.Message.Contains("Status: 503", StringComparison.Ordinal))
			{
				fileMetadata.ResultStatus = ResultStatus.NEED_RETRY.ToString();
			}
			return;
		}

		fileMetadata.ResultStatus = ResultStatus.DOWNLOADED.ToString();
	}
}
