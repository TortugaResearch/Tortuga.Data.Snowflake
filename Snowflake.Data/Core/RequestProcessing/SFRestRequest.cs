﻿/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

#nullable enable

using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace Tortuga.Data.Snowflake.Core.RequestProcessing;

internal class SFRestRequest : RestRequest
{
	private static MediaTypeWithQualityHeaderValue applicationSnowflake = new MediaTypeWithQualityHeaderValue("application/snowflake");
	private static MediaTypeWithQualityHeaderValue applicationJson = new MediaTypeWithQualityHeaderValue("application/json");

	private const string SF_AUTHORIZATION_HEADER = "Authorization";
	private const string SF_SERVICE_NAME_HEADER = "X-Snowflake-Service";

	internal SFRestRequest() : base()
	{
		RestTimeout = TimeSpan.FromSeconds(DEFAULT_REST_RETRY_SECONDS_TIMEOUT);

		// default each http request timeout to 16 seconds
		HttpTimeout = TimeSpan.FromSeconds(16);
	}

	internal object? jsonBody { get; set; }

	internal string? authorizationToken { get; set; }

	internal string? serviceName { get; set; }

	internal bool isPutGet { get; set; }

	public override string ToString() => $"SFRestRequest {{url: {Url}, request body: {jsonBody} }}";

	internal override HttpRequestMessage ToRequestMessage(HttpMethod method)
	{
		if (Url == null)
			throw new InvalidOperationException($"{Url} is null");

		var message = newMessage(method, Url);
		if (method != HttpMethod.Get && jsonBody != null)
		{
			var json = JsonConvert.SerializeObject(jsonBody, JsonUtils.JsonSettings);
			//TODO: Check if we should use other encodings...
			message.Content = new StringContent(json, Encoding.UTF8, "application/json");
		}

		message.Headers.Add(SF_AUTHORIZATION_HEADER, authorizationToken);
		if (serviceName != null)
			message.Headers.Add(SF_SERVICE_NAME_HEADER, serviceName);

		// add quote otherwise it would be reported as error format
		var osInfo = $"({SFEnvironment.ClientEnv.osVersion})";

		if (isPutGet)
			message.Headers.Accept.Add(applicationJson);
		else
			message.Headers.Accept.Add(applicationSnowflake);

		message.Headers.UserAgent.Add(new ProductInfoHeaderValue(SFEnvironment.DriverName, SFEnvironment.DriverVersion));
		message.Headers.UserAgent.Add(new ProductInfoHeaderValue(osInfo));
		message.Headers.UserAgent.Add(new ProductInfoHeaderValue(SFEnvironment.ClientEnv.netRuntime!, SFEnvironment.ClientEnv.netVersion));

		return message;
	}
}