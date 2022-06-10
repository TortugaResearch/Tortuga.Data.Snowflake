/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core.Sessions;

internal enum SFSessionProperty
{
	[SFSessionProperty(Required = true)]
	ACCOUNT,

	[SFSessionProperty]
	DB,

	[SFSessionProperty]
	HOST,

	[SFSessionProperty(Required = true)]
	PASSWORD,

	[SFSessionProperty(DefaultValue = "443")]
	PORT,

	[SFSessionProperty]
	ROLE,

	[SFSessionProperty]
	SCHEMA,

	[SFSessionProperty(DefaultValue = "https")]
	SCHEME,

	[SFSessionProperty(Required = true, DefaultValue = "")]
	USER,

	[SFSessionProperty]
	WAREHOUSE,

	[SFSessionProperty(DefaultValue = "120")]
	CONNECTION_TIMEOUT,

	[SFSessionProperty(DefaultValue = "snowflake")]
	AUTHENTICATOR,

	[SFSessionProperty(DefaultValue = "true")]
	VALIDATE_DEFAULT_PARAMETERS,

	[SFSessionProperty]
	PRIVATE_KEY_FILE,

	[SFSessionProperty]
	PRIVATE_KEY_PWD,

	[SFSessionProperty]
	PRIVATE_KEY,

	[SFSessionProperty]
	TOKEN,

	[SFSessionProperty(DefaultValue = "false")]
	INSECUREMODE,

	[SFSessionProperty(DefaultValue = "false")]
	USEPROXY,

	[SFSessionProperty]
	PROXYHOST,

	[SFSessionProperty]
	PROXYPORT,

	[SFSessionProperty]
	PROXYUSER,

	[SFSessionProperty]
	PROXYPASSWORD,

	[SFSessionProperty]
	NONPROXYHOSTS,

	[SFSessionProperty]
	APPLICATION,
}
