/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using System.Data.Common;

namespace Tortuga.Data.Snowflake;

public class SnowflakeDbDataAdapter : DbDataAdapter
{
	public SnowflakeDbDataAdapter()
	{
	}

	public SnowflakeDbDataAdapter(SnowflakeDbCommand selectCommand) : this()
	{
		SelectCommand = selectCommand;
	}

	public SnowflakeDbDataAdapter(string selectCommandText, SnowflakeDbConnection selectConnection) : this()
	{
		SelectCommand = new SnowflakeDbCommand(selectConnection) { CommandText = selectCommandText };
	}

	new public SnowflakeDbCommand? DeleteCommand
	{
		get { return (SnowflakeDbCommand?)base.DeleteCommand; }
		set { base.DeleteCommand = value; }
	}

	new public SnowflakeDbCommand? InsertCommand
	{
		get { return (SnowflakeDbCommand?)base.InsertCommand; }
		set { base.InsertCommand = value; }
	}

	new public SnowflakeDbCommand? SelectCommand
	{
		get { return (SnowflakeDbCommand?)base.SelectCommand; }
		set { base.SelectCommand = value; }
	}

	new public SnowflakeDbCommand? UpdateCommand
	{
		get { return (SnowflakeDbCommand?)base.UpdateCommand; }
		set { base.UpdateCommand = value; }
	}
}
