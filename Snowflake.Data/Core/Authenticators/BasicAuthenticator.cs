/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

#nullable enable

using Tortuga.Data.Snowflake.Core.Messages;
using Tortuga.Data.Snowflake.Core.Sessions;

namespace Tortuga.Data.Snowflake.Core.Authenticators;

class BasicAuthenticator : Authenticator
{
	public const string AUTH_NAME = "snowflake";

	internal BasicAuthenticator(SFSession session) : base(session)
	{
	}

	protected override string AuthName => AUTH_NAME;

	/// <see cref="BaseAuthenticator.SetSpecializedAuthenticatorData(LoginRequestData)"/>
	protected override void SetSpecializedAuthenticatorData(LoginRequestData data)
	{
		// Only need to add the password to Data for basic authentication
		data.password = Session.m_Properties[SFSessionProperty.PASSWORD];
	}
}
