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

	readonly SnowflakeDbError _errorCode;

	readonly string? _sqlState;

	public SnowflakeDbException(string sqlState, int vendorCode, string? errorMessage, string queryId) :
				base(errorMessage)
	{
		_sqlState = sqlState;
		_errorCode = (SnowflakeDbError)vendorCode;
		QueryId = queryId;
	}

	public SnowflakeDbException(SnowflakeDbError error, params object?[] args) :
		base(string.Format(CultureInfo.InvariantCulture, GetFormatString(error), args))
	{
		_errorCode = error;
	}

	public SnowflakeDbException(string sqlState, SnowflakeDbError error, params object[] args) :
		base(string.Format(CultureInfo.InvariantCulture, GetFormatString(error), args))
	{
		_errorCode = error;
		_sqlState = sqlState;
	}

	/// <remarks>This is used to re-throw an exception without losing the stack trace when crossing a thread boundary.</remarks>
	internal SnowflakeDbException(SnowflakeDbException innerException) : this(innerException.Message, innerException, innerException.SnowflakeError)
	{
	}

	public SnowflakeDbException(string message, Exception innerException, SnowflakeDbError error)
		: base(message, innerException)
	{
		_errorCode = error;
	}

	public SnowflakeDbException(Exception innerException, SnowflakeDbError error, params object[] args)
		: base(string.Format(CultureInfo.InvariantCulture, GetFormatString(error), args), innerException)
	{
		_errorCode = error;
	}

	public SnowflakeDbException(Exception innerException, string sqlState, SnowflakeDbError error, params object[] args)
		: base(string.Format(CultureInfo.InvariantCulture, GetFormatString(error), args), innerException)
	{
		_errorCode = error;
		_sqlState = sqlState;
	}

	public override int ErrorCode => (int)_errorCode;
	public string? QueryId { get; }
	public SnowflakeDbError SnowflakeError => _errorCode;

#if NET5_0_OR_GREATER
	public override string? SqlState { get => _sqlState; }
#else
	public string? SqlState { get => _sqlState; }
#endif

	public override string ToString()
	{
		return $"Error: {Message} SqlState: {SqlState}, VendorCode: {_errorCode}, QueryId: {QueryId}";
	}

	static string GetFormatString(SnowflakeDbError error)
	{
		return error switch
		{
			SnowflakeDbError.InternalError => "Snowflake Internal Error: {0}",
			SnowflakeDbError.ColumnIndexOutOfBound => "Column index {0} is out of bound of valid index.",
			SnowflakeDbError.InvalidDataConversion => "Failed to convert data {0} from type {1} to type {2}.",
			SnowflakeDbError.StatementAlreadyRunningQuery => "Another query is already running against this statement.",
			SnowflakeDbError.QueryCancelled => "Query has been cancelled.",
			SnowflakeDbError.MissingConnectionProperty => "Required property {0} is not provided.",
			SnowflakeDbError.RequestTimeout => "Request reach its timeout.",
			SnowflakeDbError.InvalidConnectionString => "Connection string is invalid: {0}",
			SnowflakeDbError.UnsupportedFeature => "Feature is not supported. ",
			SnowflakeDbError.DataReaderAlreadyClosed => "Data reader has already been closed.",
			SnowflakeDbError.UnknownAuthenticator => "Unknown authenticator: {0}",
			SnowflakeDbError.UnsupportedPlatform => "OS platform is not supported.",
			SnowflakeDbError.IdpSsoTokenUrlMismatch => "Scheme/Hostname mismatch: token/sso url 1: {0}  session url: {1}",
			SnowflakeDbError.IdpSamlPostbackNotFound => "Cannot found the postback url from the SAML response",
			SnowflakeDbError.IdpSamlPostbackInvalid => "Scheme/Hostname mismatch: postback lrl: {0}  session url: {1}",
			SnowflakeDbError.BrowserResponseWrongMethod => "Expect GET, but got {0}",
			SnowflakeDbError.BrowserResponseInvalidPrefix => "Expect ?token=, but got {0}",
			SnowflakeDbError.JwtErrorReadingPk => "Could not read private key {0}. \n Error : {1}",
			SnowflakeDbError.UnsupportedDotnetType => "No corresponding Snowflake type for type {0}.",
			SnowflakeDbError.UnsupportedSnowflakeTypeForParam => "Snowflake type {0} is not supported for parameters.",
			SnowflakeDbError.InvalidConnectionParameterValue => "Invalid parameter value {0} for {1}",
			_ => error.ToString(),
		};
	}
}
