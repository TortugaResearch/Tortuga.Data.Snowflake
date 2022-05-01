/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core.FileTransfer.StorageClient;

internal class EncryptionData
{
	public string EncryptionMode;
	public WrappedContentInfo WrappedContentKey;
	public EncryptionAgentInfo EncryptionAgent;
	public string ContentEncryptionIV;
	public KeyWrappingMetadataInfo KeyWrappingMetadata;
}
