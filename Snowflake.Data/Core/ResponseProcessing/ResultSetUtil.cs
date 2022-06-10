/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing;

static class ResultSetUtil
{
	internal static int CalculateUpdateCount(this SFBaseResultSet resultSet)
	{
		if (resultSet.SFResultSetMetaData == null)
			throw new ArgumentException($"resultSet.SFResultSetMetaData is null", nameof(resultSet));

		var statementType = resultSet.SFResultSetMetaData.statementType;

		long updateCount = 0;
		switch (statementType)
		{
			case SFStatementType.INSERT:
			case SFStatementType.UPDATE:
			case SFStatementType.DELETE:
			case SFStatementType.MERGE:
			case SFStatementType.MULTI_INSERT:
				resultSet.Next();
				for (var i = 0; i < resultSet.m_ColumnCount; i++)
				{
					updateCount += resultSet.GetValue<long>(i);
				}

				break;

			case SFStatementType.COPY:
				var index = resultSet.SFResultSetMetaData.getColumnIndexByName("rows_loaded");
				if (index >= 0)
				{
					resultSet.Next();
					updateCount = resultSet.GetValue<long>(index);
					resultSet.Rewind();
				}
				break;

			case SFStatementType.COPY_UNLOAD:
				var rowIndex = resultSet.SFResultSetMetaData.getColumnIndexByName("rows_unloaded");
				if (rowIndex >= 0)
				{
					resultSet.Next();
					updateCount = resultSet.GetValue<long>(rowIndex);
					resultSet.Rewind();
				}
				break;

			case SFStatementType.SELECT:
				updateCount = -1;
				break;

			default:
				updateCount = 0;
				break;
		}

		if (updateCount > int.MaxValue)
			return -1;

		return (int)updateCount;
	}
}
