/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core;

internal enum SFStatementType
{
	[SFStatementTypeAttr(typeId = 0x0000)]
	UNKNOWN,

	[SFStatementTypeAttr(typeId = 0x1000)]
	SELECT,

	/// <remark>
	///     Data Manipulation Language
	/// </remark>
	[SFStatementTypeAttr(typeId = 0x3000)]
	DML,

	[SFStatementTypeAttr(typeId = 0x3000 + 0x100)]
	INSERT,

	[SFStatementTypeAttr(typeId = 0x3000 + 0x200)]
	UPDATE,

	[SFStatementTypeAttr(typeId = 0x3000 + 0x300)]
	DELETE,

	[SFStatementTypeAttr(typeId = 0x3000 + 0x400)]
	MERGE,

	[SFStatementTypeAttr(typeId = 0x3000 + 0x500)]
	MULTI_INSERT,

	[SFStatementTypeAttr(typeId = 0x3000 + 0x600)]
	COPY,

	[SFStatementTypeAttr(typeId = 0x3000 + 0x700)]
	COPY_UNLOAD,

	/// <remark>
	///     System Command Language
	/// </remark>
	[SFStatementTypeAttr(typeId = 0x4000)]
	SCL,

	[SFStatementTypeAttr(typeId = 0x4000 + 0x100)]
	ALTER_SESSION,

	[SFStatementTypeAttr(typeId = 0x4000 + 0x300)]
	USE,

	[SFStatementTypeAttr(typeId = 0x4000 + 0x300 + 0x10)]
	USE_DATABASE,

	[SFStatementTypeAttr(typeId = 0x4000 + 0x300 + 0x20)]
	USE_SCHEMA,

	[SFStatementTypeAttr(typeId = 0x4000 + 0x300 + 0x30)]
	USE_WAREHOUSE,

	[SFStatementTypeAttr(typeId = 0x4000 + 0x400)]
	SHOW,

	[SFStatementTypeAttr(typeId = 0x4000 + 0x500)]
	DESCRIBE,

	/// <remark>
	///     Transaction Command Language
	/// </remark>
	[SFStatementTypeAttr(typeId = 0x5000)]
	TCL,

	/// <remark>
	///     Data Definition Language
	/// </remark>
	[SFStatementTypeAttr(typeId = 0x6000)]
	DDL,
}
