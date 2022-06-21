/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using Microsoft.AspNetCore.WebUtilities;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Security;
using System.Security.Authentication;
using System.Text.RegularExpressions;
using Tortuga.Data.Snowflake.Core.Authenticators;
using Tortuga.Data.Snowflake.Core.Messages;
using Tortuga.Data.Snowflake.Core.RequestProcessing;
using Tortuga.Data.Snowflake.Legacy;
using static System.StringComparison;
using static Tortuga.Data.Snowflake.SnowflakeDbError;

namespace Tortuga.Data.Snowflake.Core.Sessions;

class SFSession
{
	static readonly Regex APPLICATION_REGEX = new(@"^[A-Za-z]([A-Za-z0-9.\-_]){1,50}$");

	const string SF_AUTHORIZATION_BASIC = "Basic";

	const string SF_AUTHORIZATION_SNOWFLAKE_FMT = "Snowflake Token=\"{0}\"";

	internal string? m_SessionToken;

	internal string? m_MasterToken;

	internal IRestRequester RestRequester { get; set; }

	Authenticator? m_Authenticator;

	internal SFSessionProperties m_Properties;

	internal string? m_Database;

	internal string? m_Schema;

	internal string? m_ServerVersion;

	internal TimeSpan m_ConnectionTimeout;

	internal bool m_InsecureMode;

	readonly HttpClient m_HttpClient;

	internal void ProcessLoginResponse(LoginResponse authnResponse)
	{
		if (authnResponse.Success)
		{
			m_SessionToken = authnResponse.Data!.Token!;
			m_MasterToken = authnResponse.Data!.MasterToken!;
			m_Database = authnResponse.Data!.AuthResponseSessionInfo!.DatabaseName!;
			m_Schema = authnResponse.Data!.AuthResponseSessionInfo!.SchemaName!;
			m_ServerVersion = authnResponse.Data!.ServerVersion!;

			UpdateSessionParameterMap(authnResponse.Data!.NameValueParameter!);
		}
		else
		{
			throw new SnowflakeDbException(SnowflakeDbException.CONNECTION_FAILURE_SSTATE, authnResponse.Code, authnResponse.Message, "");
		}
	}

	internal readonly Dictionary<SFSessionParameter, object?> ParameterMap;

	internal Uri BuildLoginUrl()
	{
		var queryParams = new Dictionary<string, string?>
		{
			[RestParams.SF_QUERY_WAREHOUSE] = m_Properties.TryGetValue(SFSessionProperty.WAREHOUSE, out var warehouseValue) ? warehouseValue : "",
			[RestParams.SF_QUERY_DB] = m_Properties.TryGetValue(SFSessionProperty.DB, out var dbValue) ? dbValue : "",
			[RestParams.SF_QUERY_SCHEMA] = m_Properties.TryGetValue(SFSessionProperty.SCHEMA, out var schemaValue) ? schemaValue : "",
			[RestParams.SF_QUERY_ROLE] = m_Properties.TryGetValue(SFSessionProperty.ROLE, out var roleName) ? roleName : "",
			[RestParams.SF_QUERY_REQUEST_ID] = Guid.NewGuid().ToString(),
			[RestParams.SF_QUERY_REQUEST_GUID] = Guid.NewGuid().ToString()
		};

		var loginUrl = BuildUri(RestPath.SF_LOGIN_PATH, queryParams);
		return loginUrl;
	}

	/// <summary>
	///     Constructor
	/// </summary>
	/// <param name="connectionString">A string in the form of "key1=value1;key2=value2"</param>
	internal SFSession(string connectionString, SecureString? password, SnowflakeDbConfiguration configuration)
	{
		Configuration = configuration;
		m_Properties = SFSessionProperties.parseConnectionString(connectionString, password);

		// If there is an "application" setting, verify that it matches the expect pattern
		m_Properties.TryGetValue(SFSessionProperty.APPLICATION, out var applicationNameSetting);
		if (!string.IsNullOrEmpty(applicationNameSetting) && !APPLICATION_REGEX.IsMatch(applicationNameSetting))
		{
			throw new SnowflakeDbException(SnowflakeDbException.CONNECTION_FAILURE_SSTATE, SnowflakeDbError.InvalidConnectionParameterValue, applicationNameSetting, SFSessionProperty.APPLICATION.ToString());
		}

		ParameterMap = new();
		int timeoutInSec;
		try
		{
			ParameterMap[SFSessionParameter.CLIENT_VALIDATE_DEFAULT_PARAMETERS] =
				Boolean.Parse(m_Properties[SFSessionProperty.VALIDATE_DEFAULT_PARAMETERS]);
			timeoutInSec = int.Parse(m_Properties[SFSessionProperty.CONNECTION_TIMEOUT], CultureInfo.InvariantCulture);
			m_InsecureMode = Boolean.Parse(m_Properties[SFSessionProperty.INSECUREMODE]);
			string? proxyHost = null;
			string? proxyPort = null;
			string? noProxyHosts = null;
			string? proxyPwd = null;
			string? proxyUser = null;
			if (bool.Parse(m_Properties[SFSessionProperty.USEPROXY]))
			{
				// Let's try to get the associated RestRequester
				m_Properties.TryGetValue(SFSessionProperty.PROXYHOST, out proxyHost);
				m_Properties.TryGetValue(SFSessionProperty.PROXYPORT, out proxyPort);
				m_Properties.TryGetValue(SFSessionProperty.NONPROXYHOSTS, out noProxyHosts);
				m_Properties.TryGetValue(SFSessionProperty.PROXYPASSWORD, out proxyPwd);
				m_Properties.TryGetValue(SFSessionProperty.PROXYUSER, out proxyUser);

				if (!string.IsNullOrEmpty(noProxyHosts))
				{
					// The list is url-encoded
					// Host names are separated with a URL-escaped pipe symbol (%7C).
					noProxyHosts = WebUtility.UrlDecode(noProxyHosts);
				}
			}

			// HttpClient config based on the setting in the connection string
			var httpClientConfig = new HttpClientConfig(!m_InsecureMode, proxyHost, proxyPort, proxyUser, proxyPwd, noProxyHosts);

			// Get the http client for the config
			m_HttpClient = s_HttpClients.GetOrAdd(httpClientConfig.ConfKey, _ => new HttpClient(new RetryHandler(SetupCustomHttpHandler(httpClientConfig))) { Timeout = Timeout.InfiniteTimeSpan });

			RestRequester = new RestRequester(m_HttpClient);
		}
		catch (Exception e)
		{
			throw new SnowflakeDbException(e, SnowflakeDbException.CONNECTION_FAILURE_SSTATE, SnowflakeDbError.InvalidConnectionString, "Unable to connect");
		}

		m_ConnectionTimeout = timeoutInSec > 0 ? TimeSpan.FromSeconds(timeoutInSec) : Timeout.InfiniteTimeSpan;
	}

	internal SFSession(String connectionString, SecureString password, IMockRestRequester restRequester, SnowflakeDbConfiguration configuration) : this(connectionString, password, configuration)
	{
		// Inject the HttpClient to use with the Mock requester
		restRequester.setHttpClient(m_HttpClient);
		// Override the Rest requester with the mock for testing
		RestRequester = restRequester;
	}

	internal Uri BuildUri(string path, Dictionary<string, string?>? queryParams = null)
	{
		var uriBuilder = new UriBuilder()
		{
			Scheme = m_Properties[SFSessionProperty.SCHEME],
			Host = m_Properties[SFSessionProperty.HOST],
			Port = int.Parse(m_Properties[SFSessionProperty.PORT], CultureInfo.InvariantCulture),
			Path = path
		};

		if (queryParams != null && queryParams.Any())
		{
			var queryString = QueryHelpers.ParseQuery(string.Empty);
			foreach (var kvp in queryParams)
				queryString[kvp.Key] = kvp.Value;

			//Clear the query and apply the new query parameters
			uriBuilder.Query = "";

			var uri = uriBuilder.Uri.ToString();
			foreach (var keyPair in queryParams)
				uri = QueryHelpers.AddQueryString(uri, keyPair.Key, keyPair.Value);

			uriBuilder = new UriBuilder(uri);
		}

		return uriBuilder.Uri;
	}

	internal void Open()
	{
		if (m_Authenticator == null)
			m_Authenticator = GetAuthenticator();

		m_Authenticator.Login();
	}

	internal async Task OpenAsync(CancellationToken cancellationToken)
	{
		if (m_Authenticator == null)
			m_Authenticator = GetAuthenticator();

		await m_Authenticator.LoginAsync(cancellationToken).ConfigureAwait(false);
	}

	internal void Close()
	{
		// Nothing to do if the session is not open
		if (null == m_SessionToken)
			return;

		// Send a close session request
		var queryParams = new Dictionary<string, string?>
		{
			[RestParams.SF_QUERY_SESSION_DELETE] = "true",
			[RestParams.SF_QUERY_REQUEST_ID] = Guid.NewGuid().ToString(),
			[RestParams.SF_QUERY_REQUEST_GUID] = Guid.NewGuid().ToString()
		};

		var closeSessionRequest = new SFRestRequest
		{
			Url = BuildUri(RestPath.SF_SESSION_PATH, queryParams),
			AuthorizationToken = string.Format(CultureInfo.InvariantCulture, SF_AUTHORIZATION_SNOWFLAKE_FMT, m_SessionToken)
		};

		RestRequester.Post<CloseResponse>(closeSessionRequest);
	}

	internal async Task CloseAsync(CancellationToken cancellationToken)
	{
		// Nothing to do if the session is not open
		if (null == m_SessionToken)
			return;

		// Send a close session request
		var queryParams = new Dictionary<string, string?>
		{
			[RestParams.SF_QUERY_SESSION_DELETE] = "true",
			[RestParams.SF_QUERY_REQUEST_ID] = Guid.NewGuid().ToString(),
			[RestParams.SF_QUERY_REQUEST_GUID] = Guid.NewGuid().ToString()
		};

		var closeSessionRequest = new SFRestRequest()
		{
			Url = BuildUri(RestPath.SF_SESSION_PATH, queryParams),
			AuthorizationToken = string.Format(CultureInfo.InvariantCulture, SF_AUTHORIZATION_SNOWFLAKE_FMT, m_SessionToken)
		};

		await RestRequester.PostAsync<CloseResponse>(closeSessionRequest, cancellationToken).ConfigureAwait(false);
	}

	internal void renewSession()
	{
		var postBody = new RenewSessionRequest()
		{
			oldSessionToken = this.m_SessionToken,
			requestType = "RENEW"
		};

		var parameters = new Dictionary<string, string?>
				{
					{ RestParams.SF_QUERY_REQUEST_ID, Guid.NewGuid().ToString() },
					{ RestParams.SF_QUERY_REQUEST_GUID, Guid.NewGuid().ToString() },
				};

		var renewSessionRequest = new SFRestRequest
		{
			JsonBody = postBody,
			Url = BuildUri(RestPath.SF_TOKEN_REQUEST_PATH, parameters),
			AuthorizationToken = string.Format(CultureInfo.InvariantCulture, SF_AUTHORIZATION_SNOWFLAKE_FMT, m_MasterToken),
			RestTimeout = Timeout.InfiniteTimeSpan
		};

		var response = RestRequester.Post<RenewSessionResponse>(renewSessionRequest);
		if (!response.Success)
		{
			throw new SnowflakeDbException("", response.Code, response.Message, "");
		}
		else
		{
			m_SessionToken = response.Data!.SessionToken!;
			m_MasterToken = response.Data!.MasterToken!;
		}
	}

	internal SFRestRequest BuildTimeoutRestRequest(Uri uri, Object body)
	{
		return new SFRestRequest()
		{
			JsonBody = body,
			Url = uri,
			AuthorizationToken = SF_AUTHORIZATION_BASIC,
			RestTimeout = m_ConnectionTimeout,
		};
	}

	internal void UpdateSessionParameterMap(List<NameValueParameter> parameterList)
	{
		foreach (var parameter in parameterList)
		{
			if (Enum.TryParse(parameter.Name, out SFSessionParameter parameterName))
				ParameterMap[parameterName] = parameter.Value;
		}
	}

	internal SnowflakeDbConfiguration Configuration { get; }

	/// <summary>
	/// Generate the authenticator given the session
	/// </summary>
	/// <param name="session">session that requires the authentication</param>
	/// <returns>authenticator</returns>
	/// <exception cref="SnowflakeDbException">when authenticator is unknown</exception>
	Authenticator GetAuthenticator()
	{
		var type = m_Properties[SFSessionProperty.AUTHENTICATOR];

		if (type.Equals(BasicAuthenticator.AUTH_NAME, OrdinalIgnoreCase))
		{
			return new BasicAuthenticator(this);
		}
		else if (type.Equals(ExternalBrowserAuthenticator.AUTH_NAME, OrdinalIgnoreCase))
		{
			return new ExternalBrowserAuthenticator(this);
		}
		else if (type.Equals(KeyPairAuthenticator.AUTH_NAME, OrdinalIgnoreCase))
		{
			return new KeyPairAuthenticator(this);
		}
		else if (type.Equals(OAuthAuthenticator.AUTH_NAME, OrdinalIgnoreCase))
		{
			return new OAuthAuthenticator(this);
		}
		// Okta would provide a url of form: https://xxxxxx.okta.com or https://xxxxxx.oktapreview.com or https://vanity.url/snowflake/okta
		else if (type.Contains("okta", OrdinalIgnoreCase) && type.StartsWith("https://", OrdinalIgnoreCase))
		{
			return new OktaAuthenticator(this, type);
		}

		throw new SnowflakeDbException(UnknownAuthenticator, type);
	}

	static readonly ConcurrentDictionary<string, HttpClient> s_HttpClients = new();

	static HttpClientHandler SetupCustomHttpHandler(HttpClientConfig config)
	{
#pragma warning disable CA5398 // Avoid hardcoded SslProtocols values
		var httpHandler = new HttpClientHandler()
		{
			// Verify no certificates have been revoked
			CheckCertificateRevocationList = config.CrlCheckEnabled,
			// Enforce tls v1.2
			SslProtocols = SslProtocols.Tls12,
			AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
			UseCookies = false // Disable cookies
		};
#pragma warning restore CA5398 // Avoid hardcoded SslProtocols values

		if (config.ProxyHost != null) // Add a proxy if necessary
		{
			// Proxy needed
			var proxy = new WebProxy(config.ProxyHost, int.Parse(config.ProxyPort!, CultureInfo.InvariantCulture));

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
					entry = entry.Replace(".", "[.]", StringComparison.Ordinal);
					// * -> .*  because * is a quantifier and need a char or group to apply to
					entry = entry.Replace("*", ".*", StringComparison.Ordinal);

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
