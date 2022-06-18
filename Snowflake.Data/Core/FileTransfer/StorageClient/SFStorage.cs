/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core.FileTransfer.StorageClient;

abstract class SFStorage
{
	internal abstract void UploadOneFileWithRetry(SFFileMetadata fileMetadata);

	internal abstract void DownloadOneFile(SFFileMetadata fileMetadata);
}
