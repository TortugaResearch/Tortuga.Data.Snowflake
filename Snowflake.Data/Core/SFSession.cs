/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using Microsoft.AspNetCore.WebUtilities;
using System.Net;
using System.Security;
using System.Text.RegularExpressions;
using Tortuga.Data.Snowflake.Core.Authenticator;

namespace Tortuga.Data.Snowflake.Core;

class SFSession
{
	private static readonly Regex APPLICATION_REGEX = new Regex(@"^[A-Za-z]([A-Za-z0-9.\-_]){1,50}$");

	private const string SF_AUTHORIZATION_BASIC = "Basic";

	private const string SF_AUTHORIZATION_SNOWFLAKE_FMT = "Snowflake Token=\"{0}\"";

	internal string sessionToken;

	internal string masterToken;

	internal IRestRequester restRequester { get; private set; }

	private IAuthenticator authenticator;

	internal SFSessionProperties properties;

	internal string database;

	internal string schema;

	internal string serverVersion;

	internal TimeSpan connectionTimeout;

	internal bool InsecureMode;

	private HttpClient _HttpClient;

	internal void ProcessLoginResponse(LoginResponse authnResponse)
	{
		if (authnResponse.success)
		{
			sessionToken = authnResponse.data.token;
			masterToken = authnResponse.data.masterToken;
			database = authnResponse.data.authResponseSessionInfo.databaseName;
			schema = authnResponse.data.authResponseSessionInfo.schemaName;
			serverVersion = authnResponse.data.serverVersion;

			UpdateSessionParameterMap(authnResponse.data.nameValueParameter);
		}
		else
		{
			throw new SnowflakeDbException(SnowflakeDbException.CONNECTION_FAILURE_SSTATE, authnResponse.code, authnResponse.message, "");
		}
	}

	internal readonly Dictionary<SFSessionParameter, Object> ParameterMap;

	internal Uri BuildLoginUrl()
	{
		var queryParams = new Dictionary<string, string>();
		string warehouseValue;
		string dbValue;
		string schemaValue;
		string roleName;
		queryParams[RestParams.SF_QUERY_WAREHOUSE] = properties.TryGetValue(SFSessionProperty.WAREHOUSE, out warehouseValue) ? warehouseValue : "";
		queryParams[RestParams.SF_QUERY_DB] = properties.TryGetValue(SFSessionProperty.DB, out dbValue) ? dbValue : "";
		queryParams[RestParams.SF_QUERY_SCHEMA] = properties.TryGetValue(SFSessionProperty.SCHEMA, out schemaValue) ? schemaValue : "";
		queryParams[RestParams.SF_QUERY_ROLE] = properties.TryGetValue(SFSessionProperty.ROLE, out roleName) ? roleName : "";
		queryParams[RestParams.SF_QUERY_REQUEST_ID] = Guid.NewGuid().ToString();
		queryParams[RestParams.SF_QUERY_REQUEST_GUID] = Guid.NewGuid().ToString();

		var loginUrl = BuildUri(RestPath.SF_LOGIN_PATH, queryParams);
		return loginUrl;
	}

	/// <summary>
	///     Constructor
	/// </summary>
	/// <param name="connectionString">A string in the form of "key1=value1;key2=value2"</param>
	internal SFSession(String connectionString, SecureString password)
	{
		properties = SFSessionProperties.parseConnectionString(connectionString, password);

		// If there is an "application" setting, verify that it matches the expect pattern
		properties.TryGetValue(SFSessionProperty.APPLICATION, out string applicationNameSetting);
		if (!String.IsNullOrEmpty(applicationNameSetting) && !APPLICATION_REGEX.IsMatch(applicationNameSetting))
		{
			throw new SnowflakeDbException(
				SnowflakeDbException.CONNECTION_FAILURE_SSTATE,
				SFError.INVALID_CONNECTION_PARAMETER_VALUE,
				applicationNameSetting,
				SFSessionProperty.APPLICATION.ToString()
				);
		}

		ParameterMap = new Dictionary<SFSessionParameter, object>();
		int recommendedMinTimeoutSec = BaseRestRequest.DEFAULT_REST_RETRY_SECONDS_TIMEOUT;
		int timeoutInSec = recommendedMinTimeoutSec;
		try
		{
			ParameterMap[SFSessionParameter.CLIENT_VALIDATE_DEFAULT_PARAMETERS] =
				Boolean.Parse(properties[SFSessionProperty.VALIDATE_DEFAULT_PARAMETERS]);
			timeoutInSec = int.Parse(properties[SFSessionProperty.CONNECTION_TIMEOUT]);
			InsecureMode = Boolean.Parse(properties[SFSessionProperty.INSECUREMODE]);
			string proxyHost = null;
			string proxyPort = null;
			string noProxyHosts = null;
			string proxyPwd = null;
			string proxyUser = null;
			if (Boolean.Parse(properties[SFSessionProperty.USEPROXY]))
			{
				// Let's try to get the associated RestRequester
				properties.TryGetValue(SFSessionProperty.PROXYHOST, out proxyHost);
				properties.TryGetValue(SFSessionProperty.PROXYPORT, out proxyPort);
				properties.TryGetValue(SFSessionProperty.NONPROXYHOSTS, out noProxyHosts);
				properties.TryGetValue(SFSessionProperty.PROXYPASSWORD, out proxyPwd);
				properties.TryGetValue(SFSessionProperty.PROXYUSER, out proxyUser);

				if (!String.IsNullOrEmpty(noProxyHosts))
				{
					// The list is url-encoded
					// Host names are separated with a URL-escaped pipe symbol (%7C).
					noProxyHosts = WebUtility.UrlDecode(noProxyHosts);
				}
			}

			// HttpClient config based on the setting in the connection string
			HttpClientConfig httpClientConfig =
				new HttpClientConfig(
					!InsecureMode,
					proxyHost,
					proxyPort,
					proxyUser,
					proxyPwd,
					noProxyHosts);

			// Get the http client for the config
			_HttpClient = HttpUtil.Instance.GetHttpClient(httpClientConfig);
			restRequester = new RestRequester(_HttpClient);
		}
		catch (Exception e)
		{
			throw new SnowflakeDbException(e.InnerException,
						SnowflakeDbException.CONNECTION_FAILURE_SSTATE,
						SFError.INVALID_CONNECTION_STRING,
						"Unable to connect");
		}

		connectionTimeout = timeoutInSec > 0 ? TimeSpan.FromSeconds(timeoutInSec) : Timeout.InfiniteTimeSpan;
	}

	internal SFSession(String connectionString, SecureString password, IMockRestRequester restRequester) : this(connectionString, password)
	{
		// Inject the HttpClient to use with the Mock requester
		restRequester.setHttpClient(_HttpClient);
		// Override the Rest requester with the mock for testing
		this.restRequester = restRequester;
	}

	internal Uri BuildUri(string path, Dictionary<string, string> queryParams = null)
	{
		UriBuilder uriBuilder = new UriBuilder();
		uriBuilder.Scheme = properties[SFSessionProperty.SCHEME];
		uriBuilder.Host = properties[SFSessionProperty.HOST];
		uriBuilder.Port = int.Parse(properties[SFSessionProperty.PORT]);
		uriBuilder.Path = path;

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
		if (authenticator == null)
		{
			authenticator = AuthenticatorFactory.GetAuthenticator(this);
		}

		authenticator.Authenticate();
	}

	internal async Task OpenAsync(CancellationToken cancellationToken)
	{
		if (authenticator == null)
		{
			authenticator = AuthenticatorFactory.GetAuthenticator(this);
		}

		await authenticator.AuthenticateAsync(cancellationToken).ConfigureAwait(false);
	}

	internal void close()
	{
		// Nothing to do if the session is not open
		if (null == sessionToken) return;

		// Send a close session request
		var queryParams = new Dictionary<string, string>();
		queryParams[RestParams.SF_QUERY_SESSION_DELETE] = "true";
		queryParams[RestParams.SF_QUERY_REQUEST_ID] = Guid.NewGuid().ToString();
		queryParams[RestParams.SF_QUERY_REQUEST_GUID] = Guid.NewGuid().ToString();

		SFRestRequest closeSessionRequest = new SFRestRequest
		{
			Url = BuildUri(RestPath.SF_SESSION_PATH, queryParams),
			authorizationToken = string.Format(SF_AUTHORIZATION_SNOWFLAKE_FMT, sessionToken)
		};

		restRequester.Post<CloseResponse>(closeSessionRequest);
	}

	internal async Task CloseAsync(CancellationToken cancellationToken)
	{
		// Nothing to do if the session is not open
		if (null == sessionToken) return;

		// Send a close session request
		var queryParams = new Dictionary<string, string>();
		queryParams[RestParams.SF_QUERY_SESSION_DELETE] = "true";
		queryParams[RestParams.SF_QUERY_REQUEST_ID] = Guid.NewGuid().ToString();
		queryParams[RestParams.SF_QUERY_REQUEST_GUID] = Guid.NewGuid().ToString();

		SFRestRequest closeSessionRequest = new SFRestRequest()
		{
			Url = BuildUri(RestPath.SF_SESSION_PATH, queryParams),
			authorizationToken = string.Format(SF_AUTHORIZATION_SNOWFLAKE_FMT, sessionToken)
		};

		await restRequester.PostAsync<CloseResponse>(closeSessionRequest, cancellationToken).ConfigureAwait(false);
	}

	internal void renewSession()
	{
		RenewSessionRequest postBody = new RenewSessionRequest()
		{
			oldSessionToken = this.sessionToken,
			requestType = "RENEW"
		};

		var parameters = new Dictionary<string, string>
				{
					{ RestParams.SF_QUERY_REQUEST_ID, Guid.NewGuid().ToString() },
					{ RestParams.SF_QUERY_REQUEST_GUID, Guid.NewGuid().ToString() },
				};

		SFRestRequest renewSessionRequest = new SFRestRequest
		{
			jsonBody = postBody,
			Url = BuildUri(RestPath.SF_TOKEN_REQUEST_PATH, parameters),
			authorizationToken = string.Format(SF_AUTHORIZATION_SNOWFLAKE_FMT, masterToken),
			RestTimeout = Timeout.InfiniteTimeSpan
		};

		var response = restRequester.Post<RenewSessionResponse>(renewSessionRequest);
		if (!response.success)
		{
			throw new SnowflakeDbException("", response.code, response.message, "");
		}
		else
		{
			sessionToken = response.data.sessionToken;
			masterToken = response.data.masterToken;
		}
	}

	internal SFRestRequest BuildTimeoutRestRequest(Uri uri, Object body)
	{
		return new SFRestRequest()
		{
			jsonBody = body,
			Url = uri,
			authorizationToken = SF_AUTHORIZATION_BASIC,
			RestTimeout = connectionTimeout,
		};
	}

	internal void UpdateSessionParameterMap(List<NameValueParameter> parameterList)
	{
		foreach (NameValueParameter parameter in parameterList)
		{
			if (Enum.TryParse(parameter.name, out SFSessionParameter parameterName))
			{
				ParameterMap[parameterName] = parameter.value;
			}
		}
	}
}
