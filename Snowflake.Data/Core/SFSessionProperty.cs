/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core;

internal enum SFSessionProperty
{
	[SFSessionPropertyAttr(required = true)]
	ACCOUNT,

	[SFSessionPropertyAttr(required = false)]
	DB,

	[SFSessionPropertyAttr(required = false)]
	HOST,

	[SFSessionPropertyAttr(required = true)]
	PASSWORD,

	[SFSessionPropertyAttr(required = false, defaultValue = "443")]
	PORT,

	[SFSessionPropertyAttr(required = false)]
	ROLE,

	[SFSessionPropertyAttr(required = false)]
	SCHEMA,

	[SFSessionPropertyAttr(required = false, defaultValue = "https")]
	SCHEME,

	[SFSessionPropertyAttr(required = true, defaultValue = "")]
	USER,

	[SFSessionPropertyAttr(required = false)]
	WAREHOUSE,

	[SFSessionPropertyAttr(required = false, defaultValue = "120")]
	CONNECTION_TIMEOUT,

	[SFSessionPropertyAttr(required = false, defaultValue = "snowflake")]
	AUTHENTICATOR,

	[SFSessionPropertyAttr(required = false, defaultValue = "true")]
	VALIDATE_DEFAULT_PARAMETERS,

	[SFSessionPropertyAttr(required = false)]
	PRIVATE_KEY_FILE,

	[SFSessionPropertyAttr(required = false)]
	PRIVATE_KEY_PWD,

	[SFSessionPropertyAttr(required = false)]
	PRIVATE_KEY,

	[SFSessionPropertyAttr(required = false)]
	TOKEN,

	[SFSessionPropertyAttr(required = false, defaultValue = "false")]
	INSECUREMODE,

	[SFSessionPropertyAttr(required = false, defaultValue = "false")]
	USEPROXY,

	[SFSessionPropertyAttr(required = false)]
	PROXYHOST,

	[SFSessionPropertyAttr(required = false)]
	PROXYPORT,

	[SFSessionPropertyAttr(required = false)]
	PROXYUSER,

	[SFSessionPropertyAttr(required = false)]
	PROXYPASSWORD,

	[SFSessionPropertyAttr(required = false)]
	NONPROXYHOSTS,

	[SFSessionPropertyAttr(required = false)]
	APPLICATION,
}
