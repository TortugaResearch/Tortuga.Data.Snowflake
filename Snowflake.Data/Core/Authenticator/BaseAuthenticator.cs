/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

#nullable enable

using Tortuga.Data.Snowflake.Core.Messages;
using Tortuga.Data.Snowflake.Core.RequestProcessing;
using Tortuga.Data.Snowflake.Core.Sessions;

namespace Tortuga.Data.Snowflake.Core.Authenticator;

/// <summary>
/// A base implementation for all authenticators to create and send a login request.
/// </summary>
internal abstract class BaseAuthenticator : IAuthenticator
{
	// The client environment properties
	readonly LoginRequestClientEnv m_ClientEnv = SFEnvironment.ClientEnv;

	/// <summary>
	/// The abstract base for all authenticators.
	/// </summary>
	/// <param name="session">The session which created the authenticator.</param>
	public BaseAuthenticator(SFSession session)
	{
		Session = session;

		// Update the value for insecureMode because it can be different for each session
		m_ClientEnv.insecureMode = session.properties[SFSessionProperty.INSECUREMODE];
		if (session.properties.TryGetValue(SFSessionProperty.APPLICATION, out var applicationName))
		{
			// If an application name has been specified in the connection setting, use it
			// Otherwise, it will default to the running process name
			m_ClientEnv.application = applicationName;
		}
	}

	/// <summary>
	/// The name of the authenticator.
	/// </summary>
	protected abstract string AuthName { get; }

	/// <summary>
	/// The session which created this authenticator.
	/// </summary>
	protected SFSession Session { get; }

	public virtual void Login()
	{
		var loginRequest = BuildLoginRequest();

		var response = Session.restRequester.Post<LoginResponse>(loginRequest);

		Session.ProcessLoginResponse(response);
	}

	public virtual async Task LoginAsync(CancellationToken cancellationToken)
	{
		var loginRequest = BuildLoginRequest();

		var response = await Session.restRequester.PostAsync<LoginResponse>(loginRequest, cancellationToken).ConfigureAwait(false);

		Session.ProcessLoginResponse(response);
	}

	/// <summary>
	/// Specialized authenticator data to add to the login request.
	/// </summary>
	/// <param name="data">The login request data to update.</param>
	protected abstract void SetSpecializedAuthenticatorData(LoginRequestData data);

	/// <summary>
	/// Builds a simple login request. Each authenticator will fill the Data part with their
	/// specialized information. The common Data attributes are already filled (clientAppId,
	/// ClienAppVersion...).
	/// </summary>
	/// <returns>A login request to send to the server.</returns>
	SFRestRequest BuildLoginRequest()
	{
		// build uri
		var loginUrl = Session.BuildLoginUrl();

		LoginRequestData data = new LoginRequestData()
		{
			loginName = Session.properties[SFSessionProperty.USER],
			accountName = Session.properties[SFSessionProperty.ACCOUNT],
			clientAppId = SFEnvironment.DriverName,
			clientAppVersion = SFEnvironment.DriverVersion,
			clientEnv = m_ClientEnv,
			SessionParameters = Session.ParameterMap,
			Authenticator = AuthName,
		};

		SetSpecializedAuthenticatorData(data);

		return Session.BuildTimeoutRestRequest(loginUrl, new LoginRequest() { data = data });
	}
}
