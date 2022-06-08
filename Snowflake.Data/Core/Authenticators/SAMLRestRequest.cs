/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

#nullable enable

using System.Net.Http.Headers;
using Tortuga.Data.Snowflake.Core.RequestProcessing;

namespace Tortuga.Data.Snowflake.Core.Authenticators;

class SAMLRestRequest : RestRequest
{
	internal string? OnetimeToken { set; get; }

	internal override HttpRequestMessage ToRequestMessage(HttpMethod method)
	{
		if (Url == null)
			throw new InvalidOperationException($"{nameof(Url)} is null");

		var builder = new UriBuilder(Url);
		builder.Query = "RelayState=%2Fsome%2Fdeep%2Flink&onetimetoken=" + OnetimeToken;
		var message = newMessage(method, builder.Uri);

		message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

		return message;
	}
}
