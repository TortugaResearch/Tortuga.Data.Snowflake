/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core.FileTransfer.StorageClient;

/// <summary>
/// The class containing file header information.
/// </summary>
internal class FileHeader
{
    public string? digest { get; set; }
    public long contentLength { get; set; }
    public SFEncryptionMetadata? encryptionMetadata { get; set; }
}
