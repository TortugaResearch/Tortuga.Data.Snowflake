/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

#nullable enable

using Microsoft.Extensions.Primitives;

namespace Tortuga.Data.Snowflake.Core.Sessions;

/// <summary>
/// RetryCoundRule would update the retryCount parameter
/// </summary>
class RetryCountRule : IRule
{
	int _retryCount;

	internal RetryCountRule()
	{
		_retryCount = 1;
	}

	public void Apply(Dictionary<string, StringValues> queryParams)
	{
		if (_retryCount == 1)
			queryParams.Add(RestParams.SF_QUERY_RETRY_COUNT, _retryCount.ToString());
		else
			queryParams[RestParams.SF_QUERY_RETRY_COUNT] = _retryCount.ToString();

		_retryCount++;
	}
}
