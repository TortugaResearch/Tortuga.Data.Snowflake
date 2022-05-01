/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Tortuga.Data.Snowflake.Core;

namespace Tortuga.Data.Snowflake;

public enum SFError
{
	[SFErrorAttr(errorCode = 270001)]
	INTERNAL_ERROR = 270001,

	[SFErrorAttr(errorCode = 270002)]
	COLUMN_INDEX_OUT_OF_BOUND = 270002,

	[SFErrorAttr(errorCode = 270003)]
	INVALID_DATA_CONVERSION = 270003,

	[SFErrorAttr(errorCode = 270004)]
	STATEMENT_ALREADY_RUNNING_QUERY = 270004,

	[SFErrorAttr(errorCode = 270005)]
	QUERY_CANCELLED = 270005,

	[SFErrorAttr(errorCode = 270006)]
	MISSING_CONNECTION_PROPERTY = 270006,

	[SFErrorAttr(errorCode = 270007)]
	REQUEST_TIMEOUT = 270007,

	[SFErrorAttr(errorCode = 270008)]
	INVALID_CONNECTION_STRING = 270008,

	[SFErrorAttr(errorCode = 270009)]
	UNSUPPORTED_FEATURE = 270009,

	[SFErrorAttr(errorCode = 270010)]
	DATA_READER_ALREADY_CLOSED = 270010,

	[SFErrorAttr(errorCode = 270011)]
	UNKNOWN_AUTHENTICATOR = 270011,

	[SFErrorAttr(errorCode = 270012)]
	UNSUPPORTED_PLATFORM = 270012,

	// Okta related
	[SFErrorAttr(errorCode = 270040)]
	IDP_SSO_TOKEN_URL_MISMATCH = 270040,

	[SFErrorAttr(errorCode = 270041)]
	IDP_SAML_POSTBACK_NOTFOUND = 270041,

	[SFErrorAttr(errorCode = 270042)]
	IDP_SAML_POSTBACK_INVALID = 270042,

	// External browser related
	[SFErrorAttr(errorCode = 270050)]
	BROWSER_RESPONSE_WRONG_METHOD = 270050,

	[SFErrorAttr(errorCode = 270051)]
	BROWSER_RESPONSE_INVALID_PREFIX = 270051,

	[SFErrorAttr(errorCode = 270052)]
	JWT_ERROR_READING_PK = 270052,

	[SFErrorAttr(errorCode = 270053)]
	UNSUPPORTED_DOTNET_TYPE = 270053,

	[SFErrorAttr(errorCode = 270054)]
	UNSUPPORTED_SNOWFLAKE_TYPE_FOR_PARAM = 270054,

	[SFErrorAttr(errorCode = 270055)]
	INVALID_CONNECTION_PARAMETER_VALUE = 270055,
}
