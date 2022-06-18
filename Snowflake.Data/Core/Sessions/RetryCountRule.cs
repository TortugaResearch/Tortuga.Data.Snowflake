/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using Microsoft.Extensions.Primitives;
using System.Globalization;

namespace Tortuga.Data.Snowflake.Core.Sessions;

/// <summary>
/// RetryCoundRule would update the retryCount parameter
/// </summary>
class RetryCountRule : IRule
{
	int m_RetryCount;

	internal RetryCountRule()
	{
		m_RetryCount = 1;
	}

	public void Apply(Dictionary<string, StringValues> queryParams)
	{
		if (m_RetryCount == 1)
			queryParams.Add(RestParams.SF_QUERY_RETRY_COUNT, m_RetryCount.ToString(CultureInfo.InvariantCulture));
		else
			queryParams[RestParams.SF_QUERY_RETRY_COUNT] = m_RetryCount.ToString(CultureInfo.InvariantCulture);

		m_RetryCount++;
	}
}
