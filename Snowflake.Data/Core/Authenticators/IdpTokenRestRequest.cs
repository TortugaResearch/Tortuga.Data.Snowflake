/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using Tortuga.Data.Snowflake.Core.RequestProcessing;

namespace Tortuga.Data.Snowflake.Core.Authenticators;

class IdpTokenRestRequest : RestRequest
{
	static readonly MediaTypeWithQualityHeaderValue s_JsonHeader = new("application/json");

	internal IdpTokenRequest? JsonBody { get; set; }

	internal override HttpRequestMessage ToRequestMessage(HttpMethod method)
	{
		if (Url == null)
			throw new InvalidOperationException($"{nameof(Url)} is null");

		var message = NewMessage(method, Url);
		message.Headers.Accept.Add(s_JsonHeader);

		var json = JsonConvert.SerializeObject(JsonBody, JsonUtils.JsonSettings);
		message.Content = new StringContent(json, Encoding.UTF8, "application/json");

		return message;
	}
}
