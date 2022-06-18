/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core.FileTransfer.StorageClient;

/// <summary>
/// The interface for the storage clients.
/// </summary>
interface ISFRemoteStorageClient
{
	/// <summary>
	/// Get the bucket name and path.
	/// </summary>
	RemoteLocation ExtractBucketNameAndPath(string stageLocation);

	/// <summary>
	/// Encrypt then upload one file.
	/// </summary>
	FileHeader? GetFileHeader(SFFileMetadata fileMetadata);

	/// <summary>
	/// Attempt upload of a file and retry if fails.
	/// </summary>
	void UploadFile(SFFileMetadata fileMetadata, byte[] fileBytes, SFEncryptionMetadata encryptionMetadata);

	/// <summary>
	/// Attempt download of a file and retry if fails.
	/// </summary>
	void DownloadFile(SFFileMetadata fileMetadata, string fullDstPath, int maxConcurrency);
}
