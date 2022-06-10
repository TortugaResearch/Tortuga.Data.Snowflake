/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using Tortuga.Data.Snowflake.Core.Sessions;
using static System.StringComparison;
using static Tortuga.Data.Snowflake.SFError;

namespace Tortuga.Data.Snowflake.Core.Authenticators;

/// <summary>
/// Authenticator Factory to build authenticators
/// </summary>
class AuthenticatorFactory
{
	/// <summary>
	/// Generate the authenticator given the session
	/// </summary>
	/// <param name="session">session that requires the authentication</param>
	/// <returns>authenticator</returns>
	/// <exception cref="SnowflakeDbException">when authenticator is unknown</exception>
	internal static Authenticator GetAuthenticator(SFSession session)
	{
		var type = session.m_Properties[SFSessionProperty.AUTHENTICATOR];

		if (type.Equals(BasicAuthenticator.AUTH_NAME, InvariantCultureIgnoreCase))
		{
			return new BasicAuthenticator(session);
		}
		else if (type.Equals(ExternalBrowserAuthenticator.AUTH_NAME, InvariantCultureIgnoreCase))
		{
			return new ExternalBrowserAuthenticator(session);
		}
		else if (type.Equals(KeyPairAuthenticator.AUTH_NAME, InvariantCultureIgnoreCase))
		{
			return new KeyPairAuthenticator(session);
		}
		else if (type.Equals(OAuthAuthenticator.AUTH_NAME, InvariantCultureIgnoreCase))
		{
			return new OAuthAuthenticator(session);
		}
		// Okta would provide a url of form: https://xxxxxx.okta.com or https://xxxxxx.oktapreview.com or https://vanity.url/snowflake/okta
		else if (type.Contains("okta") && type.StartsWith("https://"))
		{
			return new OktaAuthenticator(session, type);
		}

		throw new SnowflakeDbException(UNKNOWN_AUTHENTICATOR, type);
	}
}
