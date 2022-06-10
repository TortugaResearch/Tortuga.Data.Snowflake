/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core.FileTransfer.StorageClient;

internal class EncryptionData
{
    public string? EncryptionMode { get; set; }
    public WrappedContentInfo? WrappedContentKey { get; set; }
    public EncryptionAgentInfo? EncryptionAgent { get; set; }
    public string? ContentEncryptionIV { get; set; }
    public KeyWrappingMetadataInfo? KeyWrappingMetadata { get; set; }
}
