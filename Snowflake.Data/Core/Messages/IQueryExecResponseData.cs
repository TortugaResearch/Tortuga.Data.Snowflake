/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

#nullable enable

namespace Tortuga.Data.Snowflake.Core.Messages;

public interface IQueryExecResponseData
{
	string? queryId { get; }

	string? sqlState { get; }
}
