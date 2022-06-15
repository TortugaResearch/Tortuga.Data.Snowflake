/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using System.Net;
using System.Security.Authentication;

namespace Tortuga.Data.Snowflake.Core.Sessions;

static class HttpUtil
{
	static readonly object s_HttpClientProviderLock = new();

	static readonly Dictionary<string, HttpClient> s_HttpClients = new();

	internal static HttpClient GetHttpClient(HttpClientConfig config)
	{
		lock (s_HttpClientProviderLock)
		{
			return RegisterNewHttpClientIfNecessary(config);
		}
	}

	static HttpClient RegisterNewHttpClientIfNecessary(HttpClientConfig config)
	{
		var name = config.ConfKey;
		if (!s_HttpClients.ContainsKey(name))
		{
			var httpClient = new HttpClient(new RetryHandler(setupCustomHttpHandler(config)))
			{
				Timeout = Timeout.InfiniteTimeSpan
			};

			// Add the new client key to the list
			s_HttpClients.Add(name, httpClient);
		}

		return s_HttpClients[name];
	}

	static HttpClientHandler setupCustomHttpHandler(HttpClientConfig config)
	{
		var httpHandler = new HttpClientHandler()
		{
			// Verify no certificates have been revoked
			CheckCertificateRevocationList = config.CrlCheckEnabled,
			// Enforce tls v1.2
			SslProtocols = SslProtocols.Tls12,
			AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
			UseCookies = false // Disable cookies
		};
		// Add a proxy if necessary
		if (config.ProxyHost != null)
		{
			// Proxy needed
			var proxy = new WebProxy(config.ProxyHost, int.Parse(config.ProxyPort!));

			// Add credential if provided
			if (!string.IsNullOrEmpty(config.ProxyUser))
			{
				ICredentials credentials = new NetworkCredential(config.ProxyUser, config.ProxyPassword);
				proxy.Credentials = credentials;
			}

			// Add bypasslist if provided
			if (!string.IsNullOrEmpty(config.NoProxyList))
			{
				var bypassList = config.NoProxyList!.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
				// Convert simplified syntax to standard regular expression syntax
				for (var i = 0; i < bypassList.Length; i++)
				{
					string? entry;

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

	public static T? GetOptionOrDefault<T>(this HttpRequestMessage message, string key)
	{
#if NET5_0_OR_GREATER
		if (message.Options.TryGetValue<T>(new(key), out var value))
			return value;
		else
			return default;
#else
		if (message.Properties.TryGetValue(key, out var value))
			return (T)value;
		else
			return default;
#endif
	}

	public static void SetOption<T>(this HttpRequestMessage message, string key, T value)
	{
#if NET5_0_OR_GREATER
		message.Options.Set(new(key), value);
#else
		message.Properties[key] = value;
#endif
	}
}
