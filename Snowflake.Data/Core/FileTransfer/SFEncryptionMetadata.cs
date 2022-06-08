/*
 * Copyright (c) 2021 Snowflake Computing Inc. All rights reserved.
 */

#nullable enable

namespace Tortuga.Data.Snowflake.Core.FileTransfer;

public class SFEncryptionMetadata
{
	/// Initialization vector
	public string? iv { set; get; }

	/// File key
	public string? key { set; get; }

	/// Encryption material descriptor
	public string? matDesc { set; get; }
}
