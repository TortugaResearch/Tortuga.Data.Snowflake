/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using Microsoft.Extensions.Primitives;

namespace Tortuga.Data.Snowflake.Core.Sessions;

/// <summary>
/// RequestUUIDRule would update the request_guid query with a new RequestGUID
/// </summary>
class RequestUUIDRule : IRule
{
	public void Apply(Dictionary<string, StringValues> queryParams)
	{
		queryParams[RestParams.SF_QUERY_REQUEST_GUID] = Guid.NewGuid().ToString();
	}
}
