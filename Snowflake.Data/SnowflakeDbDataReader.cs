/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

#nullable enable

using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using Tortuga.Data.Snowflake.Core.ResponseProcessing;

namespace Tortuga.Data.Snowflake;

public class SnowflakeDbDataReader : DbDataReader
{
	readonly CommandBehavior m_CommandBehavior;
	readonly SnowflakeDbConnection m_Connection;
	readonly DataTable m_SchemaTable;
	bool m_IsClosed;
	SFBaseResultSet m_ResultSet;

	internal SnowflakeDbDataReader(SFBaseResultSet resultSet, SnowflakeDbConnection connection, CommandBehavior commandBehavior)
	{
		m_ResultSet = resultSet ?? throw new ArgumentNullException(nameof(resultSet), $"{nameof(resultSet)} is null."); ;
		m_Connection = connection ?? throw new ArgumentNullException(nameof(connection), $"{nameof(connection)} is null.");
		m_CommandBehavior = commandBehavior;
		m_SchemaTable = PopulateSchemaTable(resultSet);
		RecordsAffected = resultSet.CalculateUpdateCount();
	}

	public override int Depth => 0;

	public override int FieldCount => m_ResultSet.m_ColumnCount;

	public override bool HasRows
	{
		// return true for now since every query returned from server
		// will have at least one row
		get => true;
	}

	public override bool IsClosed => m_IsClosed;

	public override int RecordsAffected { get; }

	public override object this[string name] => m_ResultSet.GetValue(GetOrdinal(name));

	public override object this[int ordinal] => m_ResultSet.GetValue(ordinal);

	public override void Close()
	{
		base.Close();
		m_ResultSet.close();
		m_IsClosed = true;
		if (m_CommandBehavior.HasFlag(CommandBehavior.CloseConnection))
			m_Connection.Close();
	}

	public override bool GetBoolean(int ordinal) => m_ResultSet.GetValue<bool>(ordinal);

	public override byte GetByte(int ordinal)
	{
		var bytes = m_ResultSet.GetValue<byte[]>(ordinal);
		return bytes[0];
	}

	public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
	{
		return ReadSubset(ordinal, dataOffset, buffer, bufferOffset, length);
	}

	public override char GetChar(int ordinal)
	{
		var val = m_ResultSet.GetString(ordinal);
		return val?[0] ?? (char)0;
	}

	public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
	{
		return ReadSubset(ordinal, dataOffset, buffer, bufferOffset, length);
	}

	public override string GetDataTypeName(int ordinal)
	{
		if (m_ResultSet.SFResultSetMetaData == null)
			throw new InvalidOperationException($"{nameof(m_ResultSet.SFResultSetMetaData)} is null.");

		return m_ResultSet.SFResultSetMetaData.getColumnTypeByIndex(ordinal).ToString();
	}

	public override DateTime GetDateTime(int ordinal) => m_ResultSet.GetValue<DateTime>(ordinal);

	public override decimal GetDecimal(int ordinal) => m_ResultSet.GetValue<decimal>(ordinal);

	public override double GetDouble(int ordinal) => m_ResultSet.GetValue<double>(ordinal);

	public override IEnumerator GetEnumerator()
	{
		while (Read())
		{
			yield return this;
		}
	}

	public override Type GetFieldType(int ordinal)
	{
		if (m_ResultSet.SFResultSetMetaData == null)
			throw new InvalidOperationException($"{nameof(m_ResultSet.SFResultSetMetaData)} is null.");

		return m_ResultSet.SFResultSetMetaData.getCSharpTypeByIndex(ordinal);
	}

	public override float GetFloat(int ordinal) => m_ResultSet.GetValue<float>(ordinal);

	public override Guid GetGuid(int ordinal) => m_ResultSet.GetValue<Guid>(ordinal);

	public override short GetInt16(int ordinal) => m_ResultSet.GetValue<short>(ordinal);

	public override int GetInt32(int ordinal) => m_ResultSet.GetValue<int>(ordinal);

	public override long GetInt64(int ordinal) => m_ResultSet.GetValue<long>(ordinal);

	public override string GetName(int ordinal)
	{
		if (m_ResultSet.SFResultSetMetaData == null)
			throw new InvalidOperationException($"{nameof(m_ResultSet.SFResultSetMetaData)} is null.");
		return m_ResultSet.SFResultSetMetaData.getColumnNameByIndex(ordinal)!;
	}

	public override int GetOrdinal(string name)
	{
		if (m_ResultSet.SFResultSetMetaData == null)
			throw new InvalidOperationException($"{nameof(m_ResultSet.SFResultSetMetaData)} is null.");
		return m_ResultSet.SFResultSetMetaData.getColumnIndexByName(name);
	}

	public string? GetQueryId() => m_ResultSet.m_QueryId;

	public override DataTable GetSchemaTable() => m_SchemaTable;

	public override string GetString(int ordinal) => m_ResultSet.GetString(ordinal) ?? throw new SqlNullValueException();

	/// <summary>
	/// Retrieves the value of the specified column as a TimeSpan object.
	/// </summary>
	/// <param name="ordinal">The zero-based column ordinal.</param>
	/// <returns>The value of the specified column as a TimeSpan.</returns>
	/// <exception cref="InvalidCastException">The specified cast is not valid.</exception>
	/// <remarks>
	/// Call IsDBNull to check for null values before calling this method, because TimeSpan
	/// objects are not nullable.
	/// </remarks>
	public TimeSpan GetTimeSpan(int ordinal) => m_ResultSet.GetValue<TimeSpan>(ordinal);

	public override object GetValue(int ordinal) => m_ResultSet.GetValue(ordinal);

	public override int GetValues(object[] values)
	{
		var count = Math.Min(FieldCount, values.Length);
		for (var i = 0; i < count; i++)
		{
			values[i] = GetValue(i);
		}
		return count;
	}

	public override bool IsDBNull(int ordinal) => m_ResultSet.IsDBNull(ordinal);

	[EditorBrowsable(EditorBrowsableState.Never)]
	public override bool NextResult() => false;

	public override bool Read() => m_ResultSet.Next();

	public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
			throw new TaskCanceledException();

		return await m_ResultSet.NextAsync().ConfigureAwait(false);
	}

	private DataTable PopulateSchemaTable(SFBaseResultSet resultSet)
	{
		var table = new DataTable("SchemaTable");

		table.Columns.Add(SchemaTableColumn.ColumnName, typeof(string));
		table.Columns.Add(SchemaTableColumn.ColumnOrdinal, typeof(int));
		table.Columns.Add(SchemaTableColumn.ColumnSize, typeof(int));
		table.Columns.Add(SchemaTableColumn.NumericPrecision, typeof(int));
		table.Columns.Add(SchemaTableColumn.NumericScale, typeof(int));
		table.Columns.Add(SchemaTableColumn.DataType, typeof(Type));
		table.Columns.Add(SchemaTableColumn.AllowDBNull, typeof(bool));
		table.Columns.Add(SchemaTableColumn.ProviderType, typeof(SFDataType));

		if (resultSet.SFResultSetMetaData == null)
			throw new ArgumentException($"{nameof(resultSet.SFResultSetMetaData)} is null.", nameof(resultSet));

		var columnOrdinal = 0;
		var sfResultSetMetaData = resultSet.SFResultSetMetaData;
		foreach (var rowType in sfResultSetMetaData.rowTypes)
		{
			var row = table.NewRow();

			row[SchemaTableColumn.ColumnName] = rowType.name;
			row[SchemaTableColumn.ColumnOrdinal] = columnOrdinal;
			row[SchemaTableColumn.ColumnSize] = (int)rowType.length;
			row[SchemaTableColumn.NumericPrecision] = (int)rowType.precision;
			row[SchemaTableColumn.NumericScale] = (int)rowType.scale;
			row[SchemaTableColumn.AllowDBNull] = rowType.nullable;

			var types = sfResultSetMetaData.GetTypesByIndex(columnOrdinal);
			row[SchemaTableColumn.ProviderType] = types.Item1;
			row[SchemaTableColumn.DataType] = types.Item2;

			table.Rows.Add(row);
			columnOrdinal++;
		}

		return table;
	}

	//
	// Summary:
	//     Reads a subset of data starting at location indicated by dataOffset into the buffer,
	//     starting at the location indicated by bufferOffset.
	//
	// Parameters:
	//   ordinal:
	//     The zero-based column ordinal.
	//
	//   dataOffset:
	//     The index within the data from which to begin the read operation.
	//
	//   buffer:
	//     The buffer into which to copy the data.
	//
	//   bufferOffset:
	//     The index with the buffer to which the data will be copied.
	//
	//   length:
	//     The maximum number of elements to read.
	//
	// Returns:
	//     The actual number of elements read.
	long ReadSubset<T>(int ordinal, long dataOffset, T[]? buffer, int bufferOffset, int length)
	{
		if (dataOffset < 0)
			throw new ArgumentOutOfRangeException(nameof(dataOffset), dataOffset, "Non negative number is required.");

		if (bufferOffset < 0)
			throw new ArgumentOutOfRangeException(nameof(bufferOffset), bufferOffset, "Non negative number is required.");

		if ((buffer != null) && (bufferOffset > buffer.Length))
			throw new ArgumentException("Destination buffer is not long enough. " +
				"Check the buffer offset, length, and the buffer's lower bounds.", nameof(buffer));

		var data = m_ResultSet.GetValue<T[]>(ordinal);

		// https://docs.microsoft.com/en-us/dotnet/api/system.data.idatarecord.getbytes?view=net-5.0#remarks
		// If you pass a buffer that is null, GetBytes returns the length of the row in bytes.
		// https://docs.microsoft.com/en-us/dotnet/api/system.data.idatarecord.getchars?view=net-5.0#remarks
		// If you pass a buffer that is null, GetChars returns the length of the field in characters.
		if (null == buffer)
		{
			return data.Length;
		}

		if (dataOffset > data.Length)
		{
			throw new ArgumentException("Source data is not long enough. " +
				"Check the data offset, length, and the data's lower bounds.", nameof(dataOffset));
		}
		else
		{
			// How much data is available after the offset
			var dataLength = data.Length - dataOffset;
			// How much data to read
			var elementsRead = Math.Min(length, dataLength);
			Array.Copy(data, dataOffset, buffer, bufferOffset, elementsRead);

			return elementsRead;
		}
	}
}
