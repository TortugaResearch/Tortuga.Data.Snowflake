/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace Tortuga.Data.Snowflake.Core.RequestProcessing;

class SFRestRequest : RestRequest
{
	static readonly MediaTypeWithQualityHeaderValue applicationSnowflake = new("application/snowflake");
	static readonly MediaTypeWithQualityHeaderValue applicationJson = new("application/json");

	const string SF_AUTHORIZATION_HEADER = "Authorization";
	const string SF_SERVICE_NAME_HEADER = "X-Snowflake-Service";

	internal SFRestRequest() : base()
	{
		RestTimeout = TimeSpan.FromSeconds(DEFAULT_REST_RETRY_SECONDS_TIMEOUT);

		// default each http request timeout to 16 seconds
		HttpTimeout = TimeSpan.FromSeconds(16);
	}

	internal object? JsonBody { get; set; }

	internal string? AuthorizationToken { get; set; }

	internal string? ServiceName { get; set; }

	internal bool IsPutGet { get; set; }

	public override string ToString() => $"SFRestRequest {{url: {Url}, request body: {JsonBody} }}";

	internal override HttpRequestMessage ToRequestMessage(HttpMethod method)
	{
		if (Url == null)
			throw new InvalidOperationException($"{Url} is null");

		var message = NewMessage(method, Url);
		if (method != HttpMethod.Get && JsonBody != null)
		{
			var json = JsonConvert.SerializeObject(JsonBody, JsonUtils.JsonSettings);
			//TODO: Check if we should use other encodings...
			message.Content = new StringContent(json, Encoding.UTF8, "application/json");
		}

		message.Headers.Add(SF_AUTHORIZATION_HEADER, AuthorizationToken);
		if (ServiceName != null)
			message.Headers.Add(SF_SERVICE_NAME_HEADER, ServiceName);

		// add quote otherwise it would be reported as error format
		var osInfo = $"({SFEnvironment.ClientEnv.OSVersion})";

		if (IsPutGet)
			message.Headers.Accept.Add(applicationJson);
		else
			message.Headers.Accept.Add(applicationSnowflake);

		message.Headers.UserAgent.Add(new ProductInfoHeaderValue(SFEnvironment.DriverName, SFEnvironment.DriverVersion));
		message.Headers.UserAgent.Add(new ProductInfoHeaderValue(osInfo));
		message.Headers.UserAgent.Add(new ProductInfoHeaderValue(SFEnvironment.ClientEnv.NetRuntime!, SFEnvironment.ClientEnv.NetVersion));

		return message;
	}
}
