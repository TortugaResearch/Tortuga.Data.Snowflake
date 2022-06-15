/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake;

public enum SnowflakeError
{
	InternalError = 270001,

	ColumnIndexOutOfBound = 270002,

	InvalidDataConversion = 270003,

	StatementAlreadyRunningQuery = 270004,

	QueryCancelled = 270005,

	MissingConnectionProperty = 270006,

	RequestTimeout = 270007,

	InvalidConnectionString = 270008,

	UnsupportedFeature = 270009,

	DataReaderAlreadyClosed = 270010,

	UnknownAuthenticator = 270011,

	UnsupportedPlatform = 270012,

	// Okta related
	IdpSsoTokenUrlMismatch = 270040,

	IdpSamlPostbackNotFound = 270041,

	IdpSamlPostbackInvalid = 270042,

	// External browser related
	BrowserResponseWrongMethod = 270050,

	BrowserResponseInvalidPrefix = 270051,

	JwtErrorReadingPk = 270052,

	UnsupportedDotnetType = 270053,

	UnsupportedSnowflakeTypeForParam = 270054,

	InvalidConnectionParameterValue = 270055,
}
