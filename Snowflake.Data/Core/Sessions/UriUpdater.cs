/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using Microsoft.AspNetCore.WebUtilities;
using Tortuga.Data.Snowflake.Core.RequestProcessing;
using Tortuga.Data.Snowflake.Legacy;

namespace Tortuga.Data.Snowflake.Core.Sessions;

/// <summary>
/// UriUpdater would update the uri in each retry. During construction, it would take in an uri that would later
/// be updated in each retry and figure out the rules to apply when updating.
/// </summary>
class UriUpdater
{
	UriBuilder m_UriBuilder;
	readonly List<IRule> rules;

	internal UriUpdater(Uri uri)
	{
		m_UriBuilder = new UriBuilder(uri);
		rules = new List<IRule>();

		if (uri.AbsolutePath.StartsWith(RestPath.SF_QUERY_PATH, StringComparison.Ordinal))
			rules.Add(new RetryCountRule());

		if (uri.Query != null && uri.Query.Contains(RestParams.SF_QUERY_REQUEST_GUID, StringComparison.Ordinal))
			rules.Add(new RequestUUIDRule());
	}

	internal Uri Update()
	{
		// Optimization to bypass parsing if there is no rules at all.
		if (rules.Count == 0)
			return m_UriBuilder.Uri;

		var queryParams = QueryHelpers.ParseQuery(m_UriBuilder.Query);

		foreach (IRule rule in rules)
		{
			rule.Apply(queryParams);
		}

		//Clear the query and apply the new query parameters
		m_UriBuilder.Query = "";

		var uri = m_UriBuilder.Uri.ToString();
		foreach (var keyPair in queryParams)
			foreach (var value in keyPair.Value)
				uri = QueryHelpers.AddQueryString(uri, keyPair.Key, value);

		m_UriBuilder = new UriBuilder(uri);

		return m_UriBuilder.Uri;
	}
}
