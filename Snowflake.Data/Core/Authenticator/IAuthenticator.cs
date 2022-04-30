/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core.Authenticator;

/// <summary>
/// Interface for Authenticator
/// For simplicity, only the Asynchronous function call is created
/// </summary>
internal interface IAuthenticator
{
	/// <summary>
	/// Process the authentication asynchronouly
	/// </summary>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	/// <exception cref="SnowflakeDbException"></exception>
	Task AuthenticateAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Process the authentication synchronously
	/// </summary>
	/// <exception cref="SnowflakeDbException"></exception>
	void Authenticate();
}
