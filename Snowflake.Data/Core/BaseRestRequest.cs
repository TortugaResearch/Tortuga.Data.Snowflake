/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core;

/// <summary>
/// A base rest request implementation with timeout defined
/// </summary>
internal abstract class BaseRestRequest : IRestRequest
{
	internal static string HTTP_REQUEST_TIMEOUT_KEY = "TIMEOUT_PER_HTTP_REQUEST";

	internal static string REST_REQUEST_TIMEOUT_KEY = "TIMEOUT_PER_REST_REQUEST";

	// The default Rest timeout. Set to 120 seconds.
	public static int DEFAULT_REST_RETRY_SECONDS_TIMEOUT = 120;

	internal Uri Url { get; set; }

	/// <summary>
	/// Timeout of the overall rest request
	/// </summary>
	internal TimeSpan RestTimeout { get; set; }

	/// <summary>
	/// Timeout for every single HTTP request
	/// </summary>
	internal TimeSpan HttpTimeout { get; set; }

	HttpRequestMessage IRestRequest.ToRequestMessage(HttpMethod method)
	{
		throw new NotImplementedException();
	}

	protected HttpRequestMessage newMessage(HttpMethod method, Uri url)
	{
		HttpRequestMessage message = new HttpRequestMessage(method, url);
		message.Properties[HTTP_REQUEST_TIMEOUT_KEY] = HttpTimeout;
		message.Properties[REST_REQUEST_TIMEOUT_KEY] = RestTimeout;
		return message;
	}

	TimeSpan IRestRequest.GetRestTimeout()
	{
		return RestTimeout;
	}
}
