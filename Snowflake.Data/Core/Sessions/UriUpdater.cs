/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using Microsoft.AspNetCore.WebUtilities;
using Tortuga.Data.Snowflake.Core.RequestProcessing;

namespace Tortuga.Data.Snowflake.Core.Sessions;

/// <summary>
/// UriUpdater would update the uri in each retry. During construction, it would take in an uri that would later
/// be updated in each retry and figure out the rules to apply when updating.
/// </summary>
internal class UriUpdater
{
	UriBuilder uriBuilder;
	List<IRule> rules;

	internal UriUpdater(Uri uri)
	{
		uriBuilder = new UriBuilder(uri);
		rules = new List<IRule>();

		if (uri.AbsolutePath.StartsWith(RestPath.SF_QUERY_PATH))
		{
			rules.Add(new RetryCountRule());
		}

		if (uri.Query != null && uri.Query.Contains(RestParams.SF_QUERY_REQUEST_GUID))
		{
			rules.Add(new RequestUUIDRule());
		}
	}

	internal Uri Update()
	{
		// Optimization to bypass parsing if there is no rules at all.
		if (rules.Count == 0)
		{
			return uriBuilder.Uri;
		}

		var queryParams = QueryHelpers.ParseQuery(uriBuilder.Query);

		foreach (IRule rule in rules)
		{
			rule.Apply(queryParams);
		}

		//Clear the query and apply the new query parameters
		uriBuilder.Query = "";

		var uri = uriBuilder.Uri.ToString();
		foreach (var keyPair in queryParams)
			foreach (var value in keyPair.Value)
				uri = QueryHelpers.AddQueryString(uri, keyPair.Key, value);

		uriBuilder = new UriBuilder(uri);

		return uriBuilder.Uri;
	}
}
