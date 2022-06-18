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

public sealed class SFException : DbException
{
	// Sql states not coming directly from the server.
	internal const string CONNECTION_FAILURE_SSTATE = "08006";

	readonly SFError _errorCode;

	readonly string? _sqlState;

	public SFException(string sqlState, int vendorCode, string? errorMessage, string queryId) :
				base(errorMessage)
	{
		_sqlState = sqlState;
		_errorCode = (SFError)vendorCode;
		QueryId = queryId;
	}

	public SFException(SFError error, params object?[] args) :
		base(string.Format(CultureInfo.InvariantCulture, GetFormatString(error), args))
	{
		_errorCode = error;
	}

	public SFException(string sqlState, SFError error, params object[] args) :
		base(string.Format(CultureInfo.InvariantCulture, GetFormatString(error), args))
	{
		_errorCode = error;
		_sqlState = sqlState;
	}

	/// <remarks>This is used to re-throw an exception without losing the stack trace when crossing a thread boundary.</remarks>
	internal SFException(SFException innerException) : this(innerException.Message, innerException, innerException.SnowflakeError)
	{
	}

	public SFException(string message, Exception innerException, SFError error)
		: base(message, innerException)
	{
		_errorCode = error;
	}

	public SFException(Exception innerException, SFError error, params object[] args)
		: base(string.Format(CultureInfo.InvariantCulture, GetFormatString(error), args), innerException)
	{
		_errorCode = error;
	}

	public SFException(Exception innerException, string sqlState, SFError error, params object[] args)
		: base(string.Format(CultureInfo.InvariantCulture, GetFormatString(error), args), innerException)
	{
		_errorCode = error;
		_sqlState = sqlState;
	}

	public override int ErrorCode => (int)_errorCode;
	public string? QueryId { get; }
	public SFError SnowflakeError => _errorCode;

#if NET5_0_OR_GREATER
	public override string? SqlState { get => _sqlState; }
#else
	public string? SqlState { get => _sqlState; }
#endif

	public override string ToString()
	{
		return $"Error: {Message} SqlState: {SqlState}, VendorCode: {_errorCode}, QueryId: {QueryId}";
	}

	static string GetFormatString(SFError error)
	{
		return error switch
		{
			SFError.InternalError => "Snowflake Internal Error: {0}",
			SFError.ColumnIndexOutOfBound => "Column index {0} is out of bound of valid index.",
			SFError.InvalidDataConversion => "Failed to convert data {0} from type {1} to type {2}.",
			SFError.StatementAlreadyRunningQuery => "Another query is already running against this statement.",
			SFError.QueryCancelled => "Query has been cancelled.",
			SFError.MissingConnectionProperty => "Required property {0} is not provided.",
			SFError.RequestTimeout => "Request reach its timeout.",
			SFError.InvalidConnectionString => "Connection string is invalid: {0}",
			SFError.UnsupportedFeature => "Feature is not supported. ",
			SFError.DataReaderAlreadyClosed => "Data reader has already been closed.",
			SFError.UnknownAuthenticator => "Unknown authenticator: {0}",
			SFError.UnsupportedPlatform => "OS platform is not supported.",
			SFError.IdpSsoTokenUrlMismatch => "Scheme/Hostname mismatch: token/sso url 1: {0}  session url: {1}",
			SFError.IdpSamlPostbackNotFound => "Cannot found the postback url from the SAML response",
			SFError.IdpSamlPostbackInvalid => "Scheme/Hostname mismatch: postback lrl: {0}  session url: {1}",
			SFError.BrowserResponseWrongMethod => "Expect GET, but got {0}",
			SFError.BrowserResponseInvalidPrefix => "Expect ?token=, but got {0}",
			SFError.JwtErrorReadingPk => "Could not read private key {0}. \n Error : {1}",
			SFError.UnsupportedDotnetType => "No corresponding Snowflake type for type {0}.",
			SFError.UnsupportedSnowflakeTypeForParam => "Snowflake type {0} is not supported for parameters.",
			SFError.InvalidConnectionParameterValue => "Invalid parameter value {0} for {1}",
			_ => error.ToString(),
		};
	}
}
