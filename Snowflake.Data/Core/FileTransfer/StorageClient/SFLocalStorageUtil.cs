/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core.FileTransfer.StorageClient;

/// <summary>
/// The storage client for local upload/download.
/// </summary>
static class SFLocalStorageUtil
{
	/// <summary>
	/// Write the file locally.
	/// <param name="fileMetadata">The metadata of the file to upload.</param>
	/// </summary>
	internal static void UploadOneFileWithRetry(SFFileMetadata fileMetadata)
	{
		if (fileMetadata.StageInfo == null)
			throw new ArgumentException("fileMetadata.stageInfo is null", nameof(fileMetadata));
		if (fileMetadata.RealSrcFilePath == null)
			throw new ArgumentException("fileMetadata.realSrcFilePath is null", nameof(fileMetadata));
		if (fileMetadata.DestFileName == null)
			throw new ArgumentException("fileMetadata.destFileName is null", nameof(fileMetadata));
		if (fileMetadata.StageInfo.Location == null)
			throw new ArgumentException("fileMetadata.stageInfo.location is null", nameof(fileMetadata));

		// Create directory if doesn't exist
		Directory.CreateDirectory(fileMetadata.StageInfo.Location);

		// Create reader stream
		using (var stream = new MemoryStream(File.ReadAllBytes(fileMetadata.RealSrcFilePath)))
		using (var fileStream = File.Create(Path.Combine(fileMetadata.StageInfo.Location, fileMetadata.DestFileName)))
		{
			stream.CopyTo(fileStream);
		}

		fileMetadata.DestFileSize = fileMetadata.UploadSize;
		fileMetadata.ResultStatus = ResultStatus.UPLOADED.ToString();
	}

	/// <summary>
	/// Download the file locally.
	/// <param name="fileMetadata">The metadata of the file to download.</param>
	/// </summary>
	internal static void DownloadOneFile(SFFileMetadata fileMetadata)
	{
		if (fileMetadata.StageInfo == null)
			throw new ArgumentException("fileMetadata.stageInfo is null", nameof(fileMetadata));
		if (fileMetadata.SrcFileName == null)
			throw new ArgumentException("fileMetadata.srcFileName is null", nameof(fileMetadata));
		if (fileMetadata.DestFileName == null)
			throw new ArgumentException("fileMetadata.destFileName is null", nameof(fileMetadata));
		if (fileMetadata.LocalLocation == null)
			throw new ArgumentException("fileMetadata.localLocation is null", nameof(fileMetadata));
		if (fileMetadata.StageInfo.Location == null)
			throw new ArgumentException("fileMetadata.stageInfo.location is null", nameof(fileMetadata));

		var srcFilePath = fileMetadata.StageInfo.Location;
		var realSrcFilePath = Path.Combine(srcFilePath, fileMetadata.SrcFileName);
		var output = Path.Combine(fileMetadata.LocalLocation, fileMetadata.DestFileName);

		// Create directory if doesn't exist
		Directory.CreateDirectory(fileMetadata.LocalLocation);

		// Create stream object for reader and writer
		using (var stream = new MemoryStream(File.ReadAllBytes(realSrcFilePath)))
		using (var fileStream = File.Create(output))
		{
			// Write file
			stream.CopyTo(fileStream);
			fileMetadata.DestFileSize = fileStream.Length;
		}

		fileMetadata.ResultStatus = ResultStatus.DOWNLOADED.ToString();
	}
}
