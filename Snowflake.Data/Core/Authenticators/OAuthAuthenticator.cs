using Tortuga.Data.Snowflake.Core.Messages;
using Tortuga.Data.Snowflake.Core.Sessions;
using static Tortuga.Data.Snowflake.Core.Sessions.SFSessionProperty;
using static Tortuga.Data.Snowflake.SFError;

namespace Tortuga.Data.Snowflake.Core.Authenticators;

/// <summary>
/// OAuthenticator is used when using  an OAuth token for authentication.
/// See <see cref="https://docs.snowflake.com/en/user-guide/oauth.html"/> for more information.
/// </summary>
class OAuthAuthenticator : Authenticator
{
	// The authenticator setting value to use to authenticate using key pair authentication.
	public const string AUTH_NAME = "oauth";

	/// <summary>
	/// Constructor for the oauth authenticator.
	/// </summary>
	/// <param name="session">Session which created this authenticator</param>
	internal OAuthAuthenticator(SFSession session) : base(session)
	{
		// Get private key path or private key from connection settings
		if (!session.m_Properties.ContainsKey(TOKEN))
		{
			// There is no TOKEN defined, can't authenticate with oauth
			throw new SnowflakeDbException(INVALID_CONNECTION_STRING, "Missing required TOKEN for Oauth authentication");
		}
	}

	protected override string AuthName => AUTH_NAME;

	/// <see cref="BaseAuthenticator.SetSpecializedAuthenticatorData(LoginRequestData)"/>
	protected override void SetSpecializedAuthenticatorData(LoginRequestData data)
	{
		// Add the token to the Data attribute
		data.Token = Session.m_Properties[TOKEN];
		// Remove the login name for an OAuth session
		data.loginName = "";
	}
}
