/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using System.Data.Common;
using System.Globalization;

namespace Tortuga.Data.Snowflake;

#pragma warning disable CA2237 // Mark ISerializable types with serializable
#pragma warning disable CA1032 // Implement standard exception constructors

/// <summary>
///     Wraps the exception.
///     If the exception is thrown in the client side, error code from
///     270000 to 279999 will be used. Otherwise, server side error code
///     will be used.
/// </summary>

public sealed class SnowflakeDbException : DbException
{
	// Sql states not coming directly from the server.
	internal const string CONNECTION_FAILURE_SSTATE = "08006";

	readonly SnowflakeError _errorCode;

	readonly string? _sqlState;

	public SnowflakeDbException(string sqlState, int vendorCode, string? errorMessage, string queryId) :
				base(errorMessage)
	{
		_sqlState = sqlState;
		_errorCode = (SnowflakeError)vendorCode;
		QueryId = queryId;
	}

	public SnowflakeDbException(SnowflakeError error, params object?[] args) :
		base(string.Format(CultureInfo.InvariantCulture, GetFormatString(error), args))
	{
		_errorCode = error;
	}

	public SnowflakeDbException(string sqlState, SnowflakeError error, params object[] args) :
		base(string.Format(CultureInfo.InvariantCulture, GetFormatString(error), args))
	{
		_errorCode = error;
		_sqlState = sqlState;
	}

	/// <remarks>This is used to re-throw an exception without losing the stack trace when crossing a thread boundary.</remarks>
	internal SnowflakeDbException(SnowflakeDbException innerException) : this(innerException.Message, innerException, innerException.SnowflakeError)
	{
	}

	public SnowflakeDbException(string message, Exception innerException, SnowflakeError error)
		: base(message, innerException)
	{
		_errorCode = error;
	}

	public SnowflakeDbException(Exception innerException, SnowflakeError error, params object[] args)
		: base(string.Format(CultureInfo.InvariantCulture, GetFormatString(error), args), innerException)
	{
		_errorCode = error;
	}

	public SnowflakeDbException(Exception innerException, string sqlState, SnowflakeError error, params object[] args)
		: base(string.Format(CultureInfo.InvariantCulture, GetFormatString(error), args), innerException)
	{
		_errorCode = error;
		_sqlState = sqlState;
	}

	public override int ErrorCode => (int)_errorCode;
	public string? QueryId { get; }
	public SnowflakeError SnowflakeError => _errorCode;

#if NET5_0_OR_GREATER
	public override string? SqlState { get => _sqlState; }
#else
	public string? SqlState { get => _sqlState; }
#endif

	public override string ToString()
	{
		return $"Error: {Message} SqlState: {SqlState}, VendorCode: {_errorCode}, QueryId: {QueryId}";
	}

	static string GetFormatString(SnowflakeError error)
	{
		return error switch
		{
			SnowflakeError.InternalError => "Snowflake Internal Error: {0}",
			SnowflakeError.ColumnIndexOutOfBound => "Column index {0} is out of bound of valid index.",
			SnowflakeError.InvalidDataConversion => "Failed to convert data {0} from type {1} to type {2}.",
			SnowflakeError.StatementAlreadyRunningQuery => "Another query is already running against this statement.",
			SnowflakeError.QueryCancelled => "Query has been cancelled.",
			SnowflakeError.MissingConnectionProperty => "Required property {0} is not provided.",
			SnowflakeError.RequestTimeout => "Request reach its timeout.",
			SnowflakeError.InvalidConnectionString => "Connection string is invalid: {0}",
			SnowflakeError.UnsupportedFeature => "Feature is not supported. ",
			SnowflakeError.DataReaderAlreadyClosed => "Data reader has already been closed.",
			SnowflakeError.UnknownAuthenticator => "Unknown authenticator: {0}",
			SnowflakeError.UnsupportedPlatform => "OS platform is not supported.",
			SnowflakeError.IdpSsoTokenUrlMismatch => "Scheme/Hostname mismatch: token/sso url 1: {0}  session url: {1}",
			SnowflakeError.IdpSamlPostbackNotFound => "Cannot found the postback url from the SAML response",
			SnowflakeError.IdpSamlPostbackInvalid => "Scheme/Hostname mismatch: postback lrl: {0}  session url: {1}",
			SnowflakeError.BrowserResponseWrongMethod => "Expect GET, but got {0}",
			SnowflakeError.BrowserResponseInvalidPrefix => "Expect ?token=, but got {0}",
			SnowflakeError.JwtErrorReadingPk => "Could not read private key {0}. \n Error : {1}",
			SnowflakeError.UnsupportedDotnetType => "No corresponding Snowflake type for type {0}.",
			SnowflakeError.UnsupportedSnowflakeTypeForParam => "Snowflake type {0} is not supported for parameters.",
			SnowflakeError.InvalidConnectionParameterValue => "Invalid parameter value {0} for {1}",
			_ => error.ToString(),
		};
	}
}
