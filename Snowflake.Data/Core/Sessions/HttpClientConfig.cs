/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core.Sessions;

public class HttpClientConfig
{
    public HttpClientConfig(bool crlCheckEnabled, string? proxyHost, string? proxyPort, string? proxyUser, string? proxyPassword, string? noProxyList)
    {
        CrlCheckEnabled = crlCheckEnabled;
        ProxyHost = proxyHost;
        ProxyPort = proxyPort;
        ProxyUser = proxyUser;
        ProxyPassword = proxyPassword;
        NoProxyList = noProxyList;

        ConfKey = string.Join(";", crlCheckEnabled.ToString(), proxyHost, proxyPort, proxyUser, proxyPassword, noProxyList);
    }

    public bool CrlCheckEnabled { get; }
    public string? ProxyHost { get; }
    public string? ProxyPort { get; }
    public string? ProxyUser { get; }
    public string? ProxyPassword { get; }
    public string? NoProxyList { get; }

    // Key used to identify the HttpClient with the configuration matching the settings
    public string ConfKey { get; }
}
