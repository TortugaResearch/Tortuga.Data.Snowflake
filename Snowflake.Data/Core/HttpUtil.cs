/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using System.Net;
using System.Security.Authentication;

namespace Tortuga.Data.Snowflake.Core;

public sealed class HttpUtil
{
	private static readonly HttpUtil instance = new HttpUtil();

	private HttpUtil()
	{
	}

	static internal HttpUtil Instance
	{
		get { return instance; }
	}

	private readonly object httpClientProviderLock = new object();

	private Dictionary<string, HttpClient> _HttpClients = new Dictionary<string, HttpClient>();

	internal HttpClient GetHttpClient(HttpClientConfig config)
	{
		lock (httpClientProviderLock)
		{
			return RegisterNewHttpClientIfNecessary(config);
		}
	}

	private HttpClient RegisterNewHttpClientIfNecessary(HttpClientConfig config)
	{
		string name = config.ConfKey;
		if (!_HttpClients.ContainsKey(name))
		{
			var httpClient = new HttpClient(new RetryHandler(setupCustomHttpHandler(config)))
			{
				Timeout = Timeout.InfiniteTimeSpan
			};

			// Add the new client key to the list
			_HttpClients.Add(name, httpClient);
		}

		return _HttpClients[name];
	}

	private HttpClientHandler setupCustomHttpHandler(HttpClientConfig config)
	{
		HttpClientHandler httpHandler = new HttpClientHandler()
		{
			// Verify no certificates have been revoked
			CheckCertificateRevocationList = config.CrlCheckEnabled,
			// Enforce tls v1.2
			SslProtocols = SslProtocols.Tls12,
			AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
			UseCookies = false // Disable cookies
		};
		// Add a proxy if necessary
		if (null != config.ProxyHost)
		{
			// Proxy needed
			WebProxy proxy = new WebProxy(config.ProxyHost, int.Parse(config.ProxyPort));

			// Add credential if provided
			if (!String.IsNullOrEmpty(config.ProxyUser))
			{
				ICredentials credentials = new NetworkCredential(config.ProxyUser, config.ProxyPassword);
				proxy.Credentials = credentials;
			}

			// Add bypasslist if provided
			if (!String.IsNullOrEmpty(config.NoProxyList))
			{
				string[] bypassList = config.NoProxyList.Split(
					new char[] { '|' },
					StringSplitOptions.RemoveEmptyEntries);
				// Convert simplified syntax to standard regular expression syntax
				string entry = null;
				for (int i = 0; i < bypassList.Length; i++)
				{
					// Get the original entry
					entry = bypassList[i].Trim();
					// . -> [.] because . means any char
					entry = entry.Replace(".", "[.]");
					// * -> .*  because * is a quantifier and need a char or group to apply to
					entry = entry.Replace("*", ".*");

					// Replace with the valid entry syntax
					bypassList[i] = entry;
				}
				proxy.BypassList = bypassList;
			}

			httpHandler.Proxy = proxy;
		}
		return httpHandler;
	}
}
