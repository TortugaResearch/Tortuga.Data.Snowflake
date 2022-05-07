/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing;

internal enum SFStatementType : long
{
	UNKNOWN = 0x0000,

	SELECT = 0x1000,

	/// <remark>
	///     Data Manipulation Language
	/// </remark>
	DML = 0x3000,

	INSERT = 0x3000 + 0x100,

	UPDATE = 0x3000 + 0x200,

	DELETE = 0x3000 + 0x300,

	MERGE = 0x3000 + 0x400,

	MULTI_INSERT = 0x3000 + 0x500,

	COPY = 0x3000 + 0x600,

	COPY_UNLOAD = 0x3000 + 0x700,

	/// <remark>
	///     System Command Language
	/// </remark>
	SCL = 0x4000,

	ALTER_SESSION = 0x4000 + 0x100,

	USE = 0x4000 + 0x300,

	USE_DATABASE = 0x4000 + 0x300 + 0x10,

	USE_SCHEMA = 0x4000 + 0x300 + 0x20,

	USE_WAREHOUSE = 0x4000 + 0x300 + 0x30,

	SHOW = 0x4000 + 0x400,

	DESCRIBE = 0x4000 + 0x500,

	/// <remark>
	///     Transaction Command Language
	/// </remark>
	TCL = 0x5000,

	/// <remark>
	///     Data Definition Language
	/// </remark>
	DDL = 0x6000,
}
