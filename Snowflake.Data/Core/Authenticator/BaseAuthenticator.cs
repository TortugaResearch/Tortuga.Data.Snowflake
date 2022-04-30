/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using Tortuga.Data.Snowflake.Log;

namespace Tortuga.Data.Snowflake.Core.Authenticator;

/// <summary>
/// A base implementation for all authenticators to create and send a login request.
/// </summary>
internal abstract class BaseAuthenticator
{
	// The logger.
	private static readonly SFLogger logger =
		SFLoggerFactory.GetLogger<BaseAuthenticator>();

	// The name of the authenticator.
	protected string authName;

	// The session which created this authenticator.
	protected SFSession session;

	// The client environment properties
	protected LoginRequestClientEnv ClientEnv = SFEnvironment.ClientEnv;

	/// <summary>
	/// The abstract base for all authenticators.
	/// </summary>
	/// <param name="session">The session which created the authenticator.</param>
	public BaseAuthenticator(SFSession session, string authName)
	{
		this.session = session;
		this.authName = authName;
		// Update the value for insecureMode because it can be different for each session
		ClientEnv.insecureMode = session.properties[SFSessionProperty.INSECUREMODE];
		if (session.properties.TryGetValue(SFSessionProperty.APPLICATION, out var applicationName))
		{
			// If an application name has been specified in the connection setting, use it
			// Otherwise, it will default to the running process name
			ClientEnv.application = applicationName;
		}
	}

	//// <see cref="IAuthenticator.AuthenticateAsync"/>
	protected async Task LoginAsync(CancellationToken cancellationToken)
	{
		var loginRequest = BuildLoginRequest();

		var response = await session.restRequester.PostAsync<LoginResponse>(loginRequest, cancellationToken).ConfigureAwait(false);

		session.ProcessLoginResponse(response);
	}

	/// <see cref="IAuthenticator.Authenticate"/>
	protected void Login()
	{
		var loginRequest = BuildLoginRequest();

		var response = session.restRequester.Post<LoginResponse>(loginRequest);

		session.ProcessLoginResponse(response);
	}

	/// <summary>
	/// Specialized authenticator data to add to the login request.
	/// </summary>
	/// <param name="data">The login request data to update.</param>
	protected abstract void SetSpecializedAuthenticatorData(ref LoginRequestData data);

	/// <summary>
	/// Builds a simple login request. Each authenticator will fill the Data part with their
	/// specialized information. The common Data attributes are already filled (clientAppId,
	/// ClienAppVersion...).
	/// </summary>
	/// <returns>A login request to send to the server.</returns>
	private SFRestRequest BuildLoginRequest()
	{
		// build uri
		var loginUrl = session.BuildLoginUrl();

		LoginRequestData data = new LoginRequestData()
		{
			loginName = session.properties[SFSessionProperty.USER],
			accountName = session.properties[SFSessionProperty.ACCOUNT],
			clientAppId = SFEnvironment.DriverName,
			clientAppVersion = SFEnvironment.DriverVersion,
			clientEnv = ClientEnv,
			SessionParameters = session.ParameterMap,
			Authenticator = authName,
		};

		SetSpecializedAuthenticatorData(ref data);

		return session.BuildTimeoutRestRequest(loginUrl, new LoginRequest() { data = data });
	}
}
