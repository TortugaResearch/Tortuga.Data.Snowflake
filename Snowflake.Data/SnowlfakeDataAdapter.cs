/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using System.Data.Common;

namespace Tortuga.Data.Snowflake;

public class SnowlfakeDataAdapter : DbDataAdapter
{
	public SnowlfakeDataAdapter()
	{
	}

	public SnowlfakeDataAdapter(SnowflakeCommand selectCommand) : this()
	{
		SelectCommand = selectCommand;
	}

	public SnowlfakeDataAdapter(string selectCommandText, SnowflakeConnection selectConnection) : this()
	{
		SelectCommand = new SnowflakeCommand(selectConnection) { CommandText = selectCommandText };
	}

	new public SnowflakeCommand? DeleteCommand
	{
		get { return (SnowflakeCommand?)base.DeleteCommand; }
		set { base.DeleteCommand = value; }
	}

	new public SnowflakeCommand? InsertCommand
	{
		get { return (SnowflakeCommand?)base.InsertCommand; }
		set { base.InsertCommand = value; }
	}

	new public SnowflakeCommand? SelectCommand
	{
		get { return (SnowflakeCommand?)base.SelectCommand; }
		set { base.SelectCommand = value; }
	}

	new public SnowflakeCommand? UpdateCommand
	{
		get { return (SnowflakeCommand?)base.UpdateCommand; }
		set { base.UpdateCommand = value; }
	}
}
