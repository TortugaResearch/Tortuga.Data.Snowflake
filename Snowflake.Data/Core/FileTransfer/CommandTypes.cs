﻿/*
 * Copyright (c) 2021 Snowflake Computing Inc. All rights reserved.
 */

#nullable enable

namespace Tortuga.Data.Snowflake.Core.FileTransfer;

/// <summary>
/// The command type of the query.
/// </summary>
internal enum CommandTypes
{
	UPLOAD = 0,
	DOWNLOAD = 1,
}