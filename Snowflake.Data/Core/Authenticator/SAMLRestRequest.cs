/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using System.Net.Http.Headers;

namespace Tortuga.Data.Snowflake.Core.Authenticator;

class SAMLRestRequest : BaseRestRequest, IRestRequest
{
	internal string OnetimeToken { set; get; }

	HttpRequestMessage IRestRequest.ToRequestMessage(HttpMethod method)
	{
		UriBuilder builder = new UriBuilder(Url);
		builder.Query = "RelayState=%2Fsome%2Fdeep%2Flink&onetimetoken=" + OnetimeToken;
		HttpRequestMessage message = newMessage(method, builder.Uri);

		message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

		return message;
	}
}
