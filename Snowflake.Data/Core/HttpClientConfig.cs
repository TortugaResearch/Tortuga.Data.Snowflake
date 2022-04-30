/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core;

public class HttpClientConfig
{
	public HttpClientConfig(
		bool crlCheckEnabled,
		string proxyHost,
		string proxyPort,
		string proxyUser,
		string proxyPassword,
		string noProxyList)
	{
		CrlCheckEnabled = crlCheckEnabled;
		ProxyHost = proxyHost;
		ProxyPort = proxyPort;
		ProxyUser = proxyUser;
		ProxyPassword = proxyPassword;
		NoProxyList = noProxyList;

		ConfKey = string.Join(";",
			new string[] {
				crlCheckEnabled.ToString(),
				proxyHost,
				proxyPort,
				proxyUser,
				proxyPassword,
				noProxyList });
	}

	public readonly bool CrlCheckEnabled;
	public readonly string ProxyHost;
	public readonly string ProxyPort;
	public readonly string ProxyUser;
	public readonly string ProxyPassword;
	public readonly string NoProxyList;

	// Key used to identify the HttpClient with the configuration matching the settings
	public readonly string ConfKey;
}
