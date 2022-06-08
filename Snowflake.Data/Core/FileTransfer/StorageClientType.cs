/*
 * Copyright (c) 2021 Snowflake Computing Inc. All rights reserved.
 */

#nullable enable

namespace Tortuga.Data.Snowflake.Core.FileTransfer;

/// <summary>
/// The type of the storage client.
/// </summary>
internal enum StorageClientType
{
	LOCAL = 0,
	REMOTE = 1
}
