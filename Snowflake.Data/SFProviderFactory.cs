/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using System.Data.Common;

namespace Tortuga.Data.Snowflake;

public sealed class SFProviderFactory : DbProviderFactory
{
	public static readonly SFProviderFactory Instance = new();

	/// <summary>
	/// Returns a strongly typed <see cref="DbCommand"/> instance.
	/// </summary>
	public override DbCommand CreateCommand() => new SFCommand();

	/// <summary>
	/// Returns a strongly typed <see cref="DbConnection"/> instance.
	/// </summary>
	public override DbConnection CreateConnection() => new SFConnection();

	/// <summary>
	/// Returns a strongly typed <see cref="DbParameter"/> instance.
	/// </summary>
	public override DbParameter CreateParameter() => new SFParameter();

	/// <summary>
	/// Returns a strongly typed <see cref="DbConnectionStringBuilder"/> instance.
	/// </summary>
	public override DbConnectionStringBuilder CreateConnectionStringBuilder() => new SFConnectionStringBuilder();

	/// <summary>
	/// Returns a strongly typed <see cref="DbCommandBuilder"/> instance.
	/// </summary>
	public override DbCommandBuilder CreateCommandBuilder() => new SFCommandBuilder();

	/// <summary>
	/// Returns a strongly typed <see cref="DbDataAdapter"/> instance.
	/// </summary>
	public override DbDataAdapter CreateDataAdapter() => new SFDataAdapter();
}
