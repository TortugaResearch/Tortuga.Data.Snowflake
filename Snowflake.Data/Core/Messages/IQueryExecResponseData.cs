/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core.Messages;

interface IQueryExecResponseData
{
	string? queryId { get; }

	string? sqlState { get; }
}
