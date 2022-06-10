/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core.RequestProcessing;

/// <summary>
/// A base rest request implementation with timeout defined
/// </summary>
internal abstract class RestRequest
{
    internal const string HTTP_REQUEST_TIMEOUT_KEY = "TIMEOUT_PER_HTTP_REQUEST";

    internal const string REST_REQUEST_TIMEOUT_KEY = "TIMEOUT_PER_REST_REQUEST";

    // The default Rest timeout. Set to 120 seconds.
    public static int DEFAULT_REST_RETRY_SECONDS_TIMEOUT = 120;

    internal Uri? Url { get; set; }

    /// <summary>
    /// Timeout of the overall rest request
    /// </summary>
    internal TimeSpan RestTimeout { get; set; }

    /// <summary>
    /// Timeout for every single HTTP request
    /// </summary>
    internal TimeSpan HttpTimeout { get; set; }

    internal abstract HttpRequestMessage ToRequestMessage(HttpMethod method);

    protected HttpRequestMessage newMessage(HttpMethod method, Uri url)
    {
        var message = new HttpRequestMessage(method, url);
        message.SetOption(HTTP_REQUEST_TIMEOUT_KEY, HttpTimeout);
        message.SetOption(REST_REQUEST_TIMEOUT_KEY, RestTimeout);
        return message;
    }
}
