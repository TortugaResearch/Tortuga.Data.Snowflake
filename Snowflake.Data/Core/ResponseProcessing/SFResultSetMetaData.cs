﻿/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Tortuga.Data.Snowflake.Core.Messages;

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing;

class SFResultSetMetaData
{
	readonly int m_ColumnCount;

	internal readonly string? m_DateOutputFormat;

	internal readonly string? m_TimeOutputFormat;

	internal readonly string? m_TimestampeNTZOutputFormat;

	internal readonly string? m_TimestampeLTZOutputFormat;

	internal readonly string? m_TimestampeTZOutputFormat;

	internal List<ExecResponseRowType> m_RowTypes;

	internal readonly SFStatementType m_StatementType;

	internal readonly List<Tuple<SnowflakeDataType, Type>> m_ColumnTypes;

	/// <summary>
	///     This map is used to cache column name to column index. Index is 0-based.
	/// </summary>
	readonly Dictionary<string, int> m_ColumnNameToIndexCache = new();

	internal SFResultSetMetaData(QueryExecResponseData queryExecResponseData)
	{
		if (queryExecResponseData.RowType == null)
			throw new ArgumentException($"queryExecResponseData.rowType is null", nameof(queryExecResponseData));
		if (queryExecResponseData.Parameters == null)
			throw new ArgumentException($"queryExecResponseData.parameters is null", nameof(queryExecResponseData));

		m_RowTypes = queryExecResponseData.RowType;
		m_ColumnCount = m_RowTypes.Count;
		m_StatementType = FindStatementTypeById(queryExecResponseData.StatementTypeId);
		m_ColumnTypes = InitColumnTypes();

		foreach (var parameter in queryExecResponseData.Parameters)
		{
			switch (parameter.Name)
			{
				case "DATE_OUTPUT_FORMAT":
					m_DateOutputFormat = parameter.Value;
					break;

				case "TIME_OUTPUT_FORMAT":
					m_TimeOutputFormat = parameter.Value;
					break;
			}
		}
	}

	internal SFResultSetMetaData(PutGetResponseData putGetResponseData)
	{
		if (putGetResponseData.RowType == null)
			throw new ArgumentException($"putGetResponseData.rowType is null", nameof(putGetResponseData));

		m_RowTypes = putGetResponseData.RowType;
		m_ColumnCount = m_RowTypes.Count;
		m_StatementType = FindStatementTypeById(putGetResponseData.StatementTypeId);
		m_ColumnTypes = InitColumnTypes();
	}

	List<Tuple<SnowflakeDataType, Type>> InitColumnTypes()
	{
		var types = new List<Tuple<SnowflakeDataType, Type>>();
		for (var i = 0; i < m_ColumnCount; i++)
		{
			var column = m_RowTypes[i];
			var dataType = SnowflakeDataTypeExtensions.FromSql(column.Type!);
			var nativeType = GetNativeTypeForColumn(dataType, column);

			types.Add(Tuple.Create(dataType, nativeType));
		}
		return types;
	}

	/// <summary>
	/// </summary>
	/// <returns>index of column given a name, -1 if no column names are found</returns>
	internal int GetColumnIndexByName(string targetColumnName)
	{
		if (m_ColumnNameToIndexCache.TryGetValue(targetColumnName, out var resultIndex))
		{
			return resultIndex;
		}
		else
		{
			var indexCounter = 0;
			foreach (var rowType in m_RowTypes)
			{
				if (string.Equals(rowType.Name, targetColumnName, StringComparison.Ordinal))
				{
					m_ColumnNameToIndexCache[targetColumnName] = indexCounter;
					return indexCounter;
				}
				indexCounter++;
			}
		}
		return -1;
	}

	internal SnowflakeDataType GetColumnTypeByIndex(int targetIndex)
	{
		if (targetIndex < 0 || targetIndex >= m_ColumnCount)
			throw new SnowflakeException(SnowflakeError.ColumnIndexOutOfBound, targetIndex);

		return m_ColumnTypes[targetIndex].Item1;
	}

	internal Tuple<SnowflakeDataType, Type> GetTypesByIndex(int targetIndex)
	{
		if (targetIndex < 0 || targetIndex >= m_ColumnCount)
			throw new SnowflakeException(SnowflakeError.ColumnIndexOutOfBound, targetIndex);

		return m_ColumnTypes[targetIndex];
	}

	static Type GetNativeTypeForColumn(SnowflakeDataType sfType, ExecResponseRowType col)
	{
		switch (sfType)
		{
			case SnowflakeDataType.Fixed:
				return col.Scale == 0 ? typeof(long) : typeof(decimal);

			case SnowflakeDataType.Real:
				return typeof(double);

			case SnowflakeDataType.Text:
			case SnowflakeDataType.Variant:
			case SnowflakeDataType.Object:
			case SnowflakeDataType.Array:
				return typeof(string);

			case SnowflakeDataType.Date:
			case SnowflakeDataType.Time:
			case SnowflakeDataType.TimestampNtz:
				return typeof(DateTime);

			case SnowflakeDataType.TimestampLtz:
			case SnowflakeDataType.TimestampTz:
				return typeof(DateTimeOffset);

			case SnowflakeDataType.Binary:
				return typeof(byte[]);

			case SnowflakeDataType.Boolean:
				return typeof(bool);

			default:
				throw new SnowflakeException(SnowflakeError.InternalError,
					$"Unknow column type: {sfType}");
		}
	}

	internal Type GetCSharpTypeByIndex(int targetIndex)
	{
		if (targetIndex < 0 || targetIndex >= m_ColumnCount)
			throw new SnowflakeException(SnowflakeError.ColumnIndexOutOfBound, targetIndex);

		var sfType = GetColumnTypeByIndex(targetIndex);
		return GetNativeTypeForColumn(sfType, m_RowTypes[targetIndex]);
	}

	internal string? getColumnNameByIndex(int targetIndex)
	{
		if (targetIndex < 0 || targetIndex >= m_ColumnCount)
			throw new SnowflakeException(SnowflakeError.ColumnIndexOutOfBound, targetIndex);

		return m_RowTypes[targetIndex].Name;
	}

	static SFStatementType FindStatementTypeById(long id)
	{
#pragma warning disable CS8605 // Unboxing a possibly null value. Workaround for .NET Core 3.1 warning. Doesn't apply to .NET 4.x or .NET 6
		foreach (SFStatementType type in Enum.GetValues(typeof(SFStatementType)))
		{
			if (id == (long)type)
				return type;
		}
#pragma warning restore CS8605 // Unboxing a possibly null value.

		// if specific type not found, we will try to find the range
		if (id >= (long)SFStatementType.SCL && id < (long)SFStatementType.SCL + 0x1000)
		{
			return SFStatementType.SCL;
		}
		else if (id >= (long)SFStatementType.TCL && id < (long)SFStatementType.TCL + 0x1000)
		{
			return SFStatementType.TCL;
		}
		else if (id >= (long)SFStatementType.DDL && id < (long)SFStatementType.DDL + 0x1000)
		{
			return SFStatementType.DDL;
		}
		else
		{
			return SFStatementType.UNKNOWN;
		}
	}
}
