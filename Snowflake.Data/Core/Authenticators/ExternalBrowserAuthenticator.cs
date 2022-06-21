/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Tortuga.Data.Snowflake.Core.Messages;
using Tortuga.Data.Snowflake.Core.RequestProcessing;
using Tortuga.Data.Snowflake.Core.Sessions;
using Tortuga.Data.Snowflake.Legacy;

#if !NETFRAMEWORK
using System.Runtime.InteropServices;
using System;
#endif

namespace Tortuga.Data.Snowflake.Core.Authenticators;

/// <summary>
/// ExternalBrowserAuthenticator would start a new browser to perform authentication
/// </summary>
class ExternalBrowserAuthenticator : Authenticator
{
	public const string AUTH_NAME = "externalbrowser";
	const string TokenRequestPrefix = "?token=";

	static readonly byte[] s_successResponse = System.Text.Encoding.UTF8.GetBytes(
		"<!DOCTYPE html><html><head><meta charset=\"UTF-8\"/>" +
		"<title> SAML Response for Snowflake </title></head>" +
		"<body>Your identity was confirmed and propagated to Snowflake .NET driver. You can close this window now and go back where you started from." +
		"</body></html>;"
		);

	protected override string AuthName => AUTH_NAME;

	// The saml token to send in the login request.
	string? _samlResponseToken;

	// The proof key to send in the login request.
	string? _proofKey;

	/// <summary>
	/// Constructor of the External authenticator
	/// </summary>
	/// <param name="session"></param>
	internal ExternalBrowserAuthenticator(SFSession session) : base(session)
	{
	}

	/// <see cref="IAuthenticator"/>
	async public override Task LoginAsync(CancellationToken cancellationToken)
	{
		var localPort = GetRandomUnusedPort();
		using (var httpListener = GetHttpListener(localPort))
		{
			httpListener.Start();

			var authenticatorRestRequest = BuildAuthenticatorRestRequest(localPort);
			var authenticatorRestResponse =
				await Session.RestRequester.PostAsync<AuthenticatorResponse>(
					authenticatorRestRequest,
					cancellationToken
				).ConfigureAwait(false);
			authenticatorRestResponse.FilterFailedResponse();

			var idpUrl = authenticatorRestResponse.Data!.ssoUrl!;
			_proofKey = authenticatorRestResponse.Data.proofKey;

			StartBrowser(idpUrl);

			var context = await httpListener.GetContextAsync().ConfigureAwait(false);
			var request = context.Request;
			_samlResponseToken = ValidateAndExtractToken(request);
			var response = context.Response;
			try
			{
				using (var output = response.OutputStream)
				{
#if NETCOREAPP3_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
					await output.WriteAsync(s_successResponse, cancellationToken).ConfigureAwait(false);
#else
					await output.WriteAsync(s_successResponse, 0, s_successResponse.Length, cancellationToken).ConfigureAwait(false);
#endif
				}
			}
			catch
			{
				// Ignore the exception as it does not affect the overall authentication flow
			}

			httpListener.Stop();
		}

		await base.LoginAsync(cancellationToken).ConfigureAwait(false);
	}

	/// <see cref="IAuthenticator"/>
	public override void Login()
	{
		var localPort = GetRandomUnusedPort();
		using (var httpListener = GetHttpListener(localPort))
		{
			httpListener.Prefixes.Add("http://localhost:" + localPort + "/");
			httpListener.Start();

			var authenticatorRestRequest = BuildAuthenticatorRestRequest(localPort);
			var authenticatorRestResponse = Session.RestRequester.Post<AuthenticatorResponse>(authenticatorRestRequest);
			authenticatorRestResponse.FilterFailedResponse();

			var idpUrl = authenticatorRestResponse.Data!.ssoUrl!;
			_proofKey = authenticatorRestResponse.Data.proofKey;

			StartBrowser(idpUrl);

			var context = httpListener.GetContext();
			var request = context.Request;
			_samlResponseToken = ValidateAndExtractToken(request);
			var response = context.Response;
			try
			{
				using (var output = response.OutputStream)
				{
					output.Write(s_successResponse, 0, s_successResponse.Length);
				}
			}
			catch
			{
				// Ignore the exception as it does not affect the overall authentication flow
			}

			httpListener.Stop();
		}

		base.Login();
	}

	static int GetRandomUnusedPort()
	{
		var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		var port = ((IPEndPoint)listener.LocalEndpoint).Port;
		listener.Stop();
		return port;
	}

	static HttpListener GetHttpListener(int port)
	{
		var redirectURI = $"http://{IPAddress.Loopback}:{port}/";
		var listener = new HttpListener();
		listener.Prefixes.Add(redirectURI);
		return listener;
	}

	static void StartBrowser(string url)
	{
		// The following code is learnt from https://brockallen.com/2016/09/24/process-start-for-urls-on-net-core/
#if NETFRAMEWORK
		// .net standard would pass here
		Process.Start(url);
#else
		// hack because of this: https://github.com/dotnet/corefx/issues/10361
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			url = url.Replace("&", "^&", StringComparison.Ordinal);
			Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			Process.Start("xdg-open", url);
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			Process.Start("open", url);
		}
		else
		{
			throw new SnowflakeDbException(SnowflakeDbError.UnsupportedPlatform);
		}
#endif
	}

	static string ValidateAndExtractToken(HttpListenerRequest request)
	{
		if (request.HttpMethod != "GET")
		{
			throw new SnowflakeDbException(SnowflakeDbError.BrowserResponseWrongMethod, request.HttpMethod);
		}

		if (request.Url?.Query == null || !request.Url.Query.StartsWith(TokenRequestPrefix, StringComparison.Ordinal))
		{
			throw new SnowflakeDbException(SnowflakeDbError.BrowserResponseInvalidPrefix, request.Url?.Query);
		}

		return Uri.UnescapeDataString(request.Url.Query.Substring(TokenRequestPrefix.Length));
	}

	SFRestRequest BuildAuthenticatorRestRequest(int port)
	{
		var fedUrl = Session.BuildUri(RestPath.SF_AUTHENTICATOR_REQUEST_PATH);
		var data = new AuthenticatorRequestData()
		{
			AccountName = Session.m_Properties[SFSessionProperty.ACCOUNT],
			Authenticator = AUTH_NAME,
			BrowserModeRedirectPort = port.ToString(CultureInfo.InvariantCulture),
		};

		return Session.BuildTimeoutRestRequest(fedUrl, new AuthenticatorRequest() { Data = data });
	}

	/// <see cref="BaseAuthenticator.SetSpecializedAuthenticatorData(LoginRequestData)"/>
	protected override void SetSpecializedAuthenticatorData(LoginRequestData data)
	{
		// Add the token and proof key to the Data
		data.Token = _samlResponseToken;
		data.ProofKey = _proofKey;
	}
}
