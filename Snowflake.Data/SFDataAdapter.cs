/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using System.Data.Common;

namespace Tortuga.Data.Snowflake;

public class SFDataAdapter : DbDataAdapter
{
	public SFDataAdapter()
	{
	}

	public SFDataAdapter(SFCommand selectCommand) : this()
	{
		SelectCommand = selectCommand;
	}

	public SFDataAdapter(string selectCommandText, SFConnection selectConnection) : this()
	{
		SelectCommand = new SFCommand(selectConnection) { CommandText = selectCommandText };
	}

	new public SFCommand? DeleteCommand
	{
		get { return (SFCommand?)base.DeleteCommand; }
		set { base.DeleteCommand = value; }
	}

	new public SFCommand? InsertCommand
	{
		get { return (SFCommand?)base.InsertCommand; }
		set { base.InsertCommand = value; }
	}

	new public SFCommand? SelectCommand
	{
		get { return (SFCommand?)base.SelectCommand; }
		set { base.SelectCommand = value; }
	}

	new public SFCommand? UpdateCommand
	{
		get { return (SFCommand?)base.UpdateCommand; }
		set { base.UpdateCommand = value; }
	}
}
