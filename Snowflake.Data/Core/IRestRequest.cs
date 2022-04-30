/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core;

internal interface IRestRequest
{
	HttpRequestMessage ToRequestMessage(HttpMethod method);

	TimeSpan GetRestTimeout();
}
