/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Tortuga.Data.Snowflake;

public class SnowflakeParameter : DbParameter
{
	string m_ParameterName = "";
	string m_SourceColumn = "";
	readonly SnowflakeDataType m_OriginType;

	public SnowflakeParameter()
	{
	}

	public SnowflakeParameter(string parameterName, SnowflakeDataType sfDataType)
	{
		ParameterName = parameterName;
		SFDataType = sfDataType;
		m_OriginType = sfDataType;
	}

	public SnowflakeParameter(int parameterIndex, SnowflakeDataType sfDataType)
	{
		ParameterName = parameterIndex.ToString(CultureInfo.InvariantCulture);
		SFDataType = sfDataType;
	}

	public override DbType DbType { get; set; }

	public override ParameterDirection Direction
	{
		get => ParameterDirection.Input;

#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
		[Obsolete("This feature is not supprted.", true)]
		set
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
		{
			if (value != ParameterDirection.Input)
				throw new NotSupportedException();
		}
	}

	public override bool IsNullable { get; set; }

	[AllowNull]
	public override string ParameterName { get => m_ParameterName; set => m_ParameterName = value ?? ""; }

	public SnowflakeDataType SFDataType { get; set; }
	public override int Size { get; set; }

	[AllowNull]
	public override string SourceColumn { get => m_SourceColumn; set => m_SourceColumn = value ?? ""; }

	public override bool SourceColumnNullMapping { get; set; }

	public override object? Value { get; set; }

	public override void ResetDbType() => SFDataType = m_OriginType;
}
