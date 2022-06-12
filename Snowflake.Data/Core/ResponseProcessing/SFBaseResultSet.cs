/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Tortuga.Data.Snowflake.Core.RequestProcessing;

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing;

abstract class SFBaseResultSet
{
	internal SFStatement? SFStatement;

	internal SFResultSetMetaData? SFResultSetMetaData;

	internal int m_ColumnCount;

	internal bool m_IsClosed;

	internal string? m_QueryId;

	internal abstract bool Next();

	internal abstract Task<bool> NextAsync();

	protected abstract UTF8Buffer? getObjectInternal(int columnIndex);

	/// <summary>
	/// Move cursor back one row.
	/// </summary>
	/// <returns>True if it works, false otherwise.</returns>
	internal abstract bool Rewind();

	protected SFBaseResultSet(SnowflakeDbConfiguration configuration)
	{
		Configuration = configuration;
	}

	internal T GetValue<T>(int columnIndex)
	{
		if (SFResultSetMetaData == null)
			throw new InvalidOperationException($"{nameof(SFResultSetMetaData)} is null");

		var val = getObjectInternal(columnIndex);
		var types = SFResultSetMetaData.GetTypesByIndex(columnIndex);
		return (T)SFDataConverter.ConvertToCSharpVal(val, types.Item1, typeof(T));
	}

	internal string? GetString(int columnIndex)
	{
		if (SFResultSetMetaData == null)
			throw new InvalidOperationException($"{nameof(SFResultSetMetaData)} is null");

		var type = SFResultSetMetaData.GetColumnTypeByIndex(columnIndex);
		switch (type)
		{
			case SFDataType.DATE:
				var val = GetValue(columnIndex);
				if (val == DBNull.Value)
					return null;

				if (SFResultSetMetaData.m_DateOutputFormat == null)
					throw new InvalidOperationException("SFResultSetMetaData.dateOutputFormat is null");

				return SFDataConverter.toDateString((DateTime)val, SFResultSetMetaData.m_DateOutputFormat);
			//TODO: Implement SqlFormat for timestamp type, aka parsing format specified by user and format the value
			default:
				return getObjectInternal(columnIndex).SafeToString();
		}
	}

	internal object GetValue(int columnIndex)
	{
		if (SFResultSetMetaData == null)
			throw new InvalidOperationException($"{nameof(SFResultSetMetaData)} is null");

		var val = getObjectInternal(columnIndex);
		var types = SFResultSetMetaData.GetTypesByIndex(columnIndex);
		return SFDataConverter.ConvertToCSharpVal(val, types.Item1, types.Item2);
	}

	internal bool IsDBNull(int ordinal) => getObjectInternal(ordinal) == null;

	internal void Close() => m_IsClosed = true;

	internal SnowflakeDbConfiguration Configuration { get; }
}
