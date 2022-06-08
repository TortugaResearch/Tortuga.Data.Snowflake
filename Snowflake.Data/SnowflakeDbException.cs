/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using System.Data.Common;

#nullable enable

namespace Tortuga.Data.Snowflake;

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

	readonly SFError _errorCode;

	readonly string? _sqlState;

	public SnowflakeDbException(string sqlState, int vendorCode, string? errorMessage, string queryId) :
				base(errorMessage)
	{
		_sqlState = sqlState;
		_errorCode = (SFError)vendorCode;
		QueryId = queryId;
	}

	public SnowflakeDbException(SFError error, params object?[] args) :
		base(string.Format(GetFormatString(error), args))
	{
		_errorCode = error;
	}

	public SnowflakeDbException(string sqlState, SFError error, params object[] args) :
		base(string.Format(GetFormatString(error), args))
	{
		_errorCode = error;
		_sqlState = sqlState;
	}

	/// <remarks>This is used to re-throw an exception without losing the stack trace when crossing a thread boundary.</remarks>
	internal SnowflakeDbException(SnowflakeDbException innerException) : this(innerException.Message, innerException, innerException.SFErrorCode)
	{
	}

	public SnowflakeDbException(string message, Exception innerException, SFError error)
		: base(message, innerException)
	{
		_errorCode = error;
	}

	public SnowflakeDbException(Exception innerException, SFError error, params object[] args)
		: base(string.Format(GetFormatString(error), args), innerException)
	{
		_errorCode = error;
	}

	public SnowflakeDbException(Exception innerException, string sqlState, SFError error, params object[] args)
		: base(string.Format(GetFormatString(error), args), innerException)
	{
		_errorCode = error;
		_sqlState = sqlState;
	}

	public override int ErrorCode => (int)_errorCode;
	public string? QueryId { get; }
	public SFError SFErrorCode => _errorCode;

#if NET5_0_OR_GREATER
	public override string? SqlState { get => _sqlState; }
#else
	public string? SqlState { get => _sqlState; }
#endif

	public override string ToString()
	{
		return string.Format("Error: {0} SqlState: {1}, VendorCode: {2}, QueryId: {3}",
			Message, SqlState, _errorCode, QueryId);
	}

	static string GetFormatString(SFError error)
	{
		return error switch
		{
			SFError.INTERNAL_ERROR => "Snowflake Internal Error: {0}",
			SFError.COLUMN_INDEX_OUT_OF_BOUND => "Column index {0} is out of bound of valid index.",
			SFError.INVALID_DATA_CONVERSION => "Failed to convert data {0} from type {1} to type {2}.",
			SFError.STATEMENT_ALREADY_RUNNING_QUERY => "Another query is already running against this statement.",
			SFError.QUERY_CANCELLED => "Query has been cancelled.",
			SFError.MISSING_CONNECTION_PROPERTY => "Required property {0} is not provided.",
			SFError.REQUEST_TIMEOUT => "Request reach its timeout.",
			SFError.INVALID_CONNECTION_STRING => "Connection string is invalid: {0}",
			SFError.UNSUPPORTED_FEATURE => "Feature is not supported. ",
			SFError.DATA_READER_ALREADY_CLOSED => "Data reader has already been closed.",
			SFError.UNKNOWN_AUTHENTICATOR => "Unknown authenticator: {0}",
			SFError.UNSUPPORTED_PLATFORM => "OS platform is not supported.",
			SFError.IDP_SSO_TOKEN_URL_MISMATCH => "Scheme/Hostname mismatch: token/sso url 1: {0}  session url: {1}",
			SFError.IDP_SAML_POSTBACK_NOTFOUND => "Cannot found the postback url from the SAML response",
			SFError.IDP_SAML_POSTBACK_INVALID => "Scheme/Hostname mismatch: postback lrl: {0}  session url: {1}",
			SFError.BROWSER_RESPONSE_WRONG_METHOD => "Expect GET, but got {0}",
			SFError.BROWSER_RESPONSE_INVALID_PREFIX => "Expect ?token=, but got {0}",
			SFError.JWT_ERROR_READING_PK => "Could not read private key {0}. \n Error : {1}",
			SFError.UNSUPPORTED_DOTNET_TYPE => "No corresponding Snowflake type for type {0}.",
			SFError.UNSUPPORTED_SNOWFLAKE_TYPE_FOR_PARAM => "Snowflake type {0} is not supported for parameters.",
			SFError.INVALID_CONNECTION_PARAMETER_VALUE => "Invalid parameter value {0} for {1}",
			_ => error.ToString(),
		};
	}
}
