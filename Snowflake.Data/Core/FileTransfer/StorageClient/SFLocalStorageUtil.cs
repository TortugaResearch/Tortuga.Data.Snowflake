/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

#nullable enable

namespace Tortuga.Data.Snowflake.Core.FileTransfer.StorageClient;

/// <summary>
/// The storage client for local upload/download.
/// </summary>
class SFLocalStorageUtil
{
	/// <summary>
	/// Write the file locally.
	/// <param name="fileMetadata">The metadata of the file to upload.</param>
	/// </summary>
	internal static void UploadOneFileWithRetry(SFFileMetadata fileMetadata)
	{
		if (fileMetadata.stageInfo == null)
			throw new ArgumentException("fileMetadata.stageInfo is null", nameof(fileMetadata));
		if (fileMetadata.realSrcFilePath == null)
			throw new ArgumentException("fileMetadata.realSrcFilePath is null", nameof(fileMetadata));
		if (fileMetadata.destFileName == null)
			throw new ArgumentException("fileMetadata.destFileName is null", nameof(fileMetadata));

		// Create directory if doesn't exist
		Directory.CreateDirectory(fileMetadata.stageInfo.location);

		// Create reader stream
		using (var stream = new MemoryStream(File.ReadAllBytes(fileMetadata.realSrcFilePath)))
		using (var fileStream = File.Create(Path.Combine(fileMetadata.stageInfo.location, fileMetadata.destFileName)))
		{
			stream.CopyTo(fileStream);
		}

		fileMetadata.destFileSize = fileMetadata.uploadSize;
		fileMetadata.resultStatus = ResultStatus.UPLOADED.ToString();
	}

	/// <summary>
	/// Download the file locally.
	/// <param name="fileMetadata">The metadata of the file to download.</param>
	/// </summary>
	internal static void DownloadOneFile(SFFileMetadata fileMetadata)
	{
		if (fileMetadata.stageInfo == null)
			throw new ArgumentException("fileMetadata.stageInfo is null", nameof(fileMetadata));
		if (fileMetadata.srcFileName == null)
			throw new ArgumentException("fileMetadata.srcFileName is null", nameof(fileMetadata));
		if (fileMetadata.destFileName == null)
			throw new ArgumentException("fileMetadata.destFileName is null", nameof(fileMetadata));
		if (fileMetadata.localLocation == null)
			throw new ArgumentException("fileMetadata.localLocation is null", nameof(fileMetadata));

		var srcFilePath = fileMetadata.stageInfo.location;
		var realSrcFilePath = Path.Combine(srcFilePath, fileMetadata.srcFileName);
		var output = Path.Combine(fileMetadata.localLocation, fileMetadata.destFileName);

		// Create directory if doesn't exist
		Directory.CreateDirectory(fileMetadata.localLocation);

		// Create stream object for reader and writer
		using (var stream = new MemoryStream(File.ReadAllBytes(realSrcFilePath)))
		using (var fileStream = File.Create(output))
		{
			// Write file
			stream.CopyTo(fileStream);
			fileMetadata.destFileSize = fileStream.Length;
		}

		fileMetadata.resultStatus = ResultStatus.DOWNLOADED.ToString();
	}
}
