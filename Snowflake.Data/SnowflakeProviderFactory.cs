/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using System.Data.Common;

namespace Tortuga.Data.Snowflake;

public sealed class SnowflakeProviderFactory : DbProviderFactory
{
	public static readonly SnowflakeProviderFactory Instance = new();

	/// <summary>
	/// Returns a strongly typed <see cref="DbCommand"/> instance.
	/// </summary>
	public override DbCommand CreateCommand() => new SnowflakeCommand();

	/// <summary>
	/// Returns a strongly typed <see cref="DbConnection"/> instance.
	/// </summary>
	public override DbConnection CreateConnection() => new SnowflakeConnection();

	/// <summary>
	/// Returns a strongly typed <see cref="DbParameter"/> instance.
	/// </summary>
	public override DbParameter CreateParameter() => new SnowflakeParameter();

	/// <summary>
	/// Returns a strongly typed <see cref="DbConnectionStringBuilder"/> instance.
	/// </summary>
	public override DbConnectionStringBuilder CreateConnectionStringBuilder() => new SnowflakeConnectionStringBuilder();

	/// <summary>
	/// Returns a strongly typed <see cref="DbCommandBuilder"/> instance.
	/// </summary>
	public override DbCommandBuilder CreateCommandBuilder() => new SnowflakeCommandBuilder();

	/// <summary>
	/// Returns a strongly typed <see cref="DbDataAdapter"/> instance.
	/// </summary>
	public override DbDataAdapter CreateDataAdapter() => new SnowlfakeDataAdapter();
}
