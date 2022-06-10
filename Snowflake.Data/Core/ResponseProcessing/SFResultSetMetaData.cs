/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Tortuga.Data.Snowflake.Core.Messages;

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing;

class SFResultSetMetaData
{
    int m_ColumnCount;

    internal readonly string? dateOutputFormat;

    internal readonly string? timeOutputFormat;

    internal readonly string? timestampeNTZOutputFormat;

    internal readonly string? timestampeLTZOutputFormat;

    internal readonly string? timestampeTZOutputFormat;

    internal List<ExecResponseRowType> rowTypes;

    internal readonly SFStatementType statementType;

    internal readonly List<Tuple<SFDataType, Type>> columnTypes;

    /// <summary>
    ///     This map is used to cache column name to column index. Index is 0-based.
    /// </summary>
    Dictionary<string, int> columnNameToIndexCache = new Dictionary<string, int>();

    internal SFResultSetMetaData(QueryExecResponseData queryExecResponseData)
    {
        if (queryExecResponseData.rowType == null)
            throw new ArgumentException($"queryExecResponseData.rowType is null", nameof(queryExecResponseData));
        if (queryExecResponseData.parameters == null)
            throw new ArgumentException($"queryExecResponseData.parameters is null", nameof(queryExecResponseData));

        rowTypes = queryExecResponseData.rowType;
        m_ColumnCount = rowTypes.Count;
        statementType = findStatementTypeById(queryExecResponseData.statementTypeId);
        columnTypes = InitColumnTypes();

        foreach (NameValueParameter parameter in queryExecResponseData.parameters)
        {
            switch (parameter.name)
            {
                case "DATE_OUTPUT_FORMAT":
                    dateOutputFormat = parameter.value;
                    break;

                case "TIME_OUTPUT_FORMAT":
                    timeOutputFormat = parameter.value;
                    break;
            }
        }
    }

    internal SFResultSetMetaData(PutGetResponseData putGetResponseData)
    {
        if (putGetResponseData.rowType == null)
            throw new ArgumentException($"putGetResponseData.rowType is null", nameof(putGetResponseData));

        rowTypes = putGetResponseData.rowType;
        m_ColumnCount = rowTypes.Count;
        statementType = findStatementTypeById(putGetResponseData.statementTypeId);
        columnTypes = InitColumnTypes();
    }

    List<Tuple<SFDataType, Type>> InitColumnTypes()
    {
        List<Tuple<SFDataType, Type>> types = new List<Tuple<SFDataType, Type>>();
        for (int i = 0; i < m_ColumnCount; i++)
        {
            var column = rowTypes[i];
            var dataType = GetSFDataType(column.type);
            var nativeType = GetNativeTypeForColumn(dataType, column);

            types.Add(Tuple.Create(dataType, nativeType));
        }
        return types;
    }

    /// <summary>
    /// </summary>
    /// <returns>index of column given a name, -1 if no column names are found</returns>
    internal int getColumnIndexByName(string targetColumnName)
    {
        int resultIndex;
        if (columnNameToIndexCache.TryGetValue(targetColumnName, out resultIndex))
        {
            return resultIndex;
        }
        else
        {
            int indexCounter = 0;
            foreach (ExecResponseRowType rowType in rowTypes)
            {
                if (String.Compare(rowType.name, targetColumnName, false) == 0)
                {
                    columnNameToIndexCache[targetColumnName] = indexCounter;
                    return indexCounter;
                }
                indexCounter++;
            }
        }
        return -1;
    }

    internal SFDataType getColumnTypeByIndex(int targetIndex)
    {
        if (targetIndex < 0 || targetIndex >= m_ColumnCount)
            throw new SnowflakeDbException(SFError.COLUMN_INDEX_OUT_OF_BOUND, targetIndex);

        return columnTypes[targetIndex].Item1;
    }

    internal Tuple<SFDataType, Type> GetTypesByIndex(int targetIndex)
    {
        if (targetIndex < 0 || targetIndex >= m_ColumnCount)
            throw new SnowflakeDbException(SFError.COLUMN_INDEX_OUT_OF_BOUND, targetIndex);

        return columnTypes[targetIndex];
    }

    SFDataType GetSFDataType(string? type)
    {
        SFDataType result;
        if (Enum.TryParse(type, true, out result))
            return result;

        throw new SnowflakeDbException(SFError.INTERNAL_ERROR, $"Unknow column type: {type}");
    }

    Type GetNativeTypeForColumn(SFDataType sfType, ExecResponseRowType col)
    {
        switch (sfType)
        {
            case SFDataType.FIXED:
                return col.scale == 0 ? typeof(long) : typeof(decimal);

            case SFDataType.REAL:
                return typeof(double);

            case SFDataType.TEXT:
            case SFDataType.VARIANT:
            case SFDataType.OBJECT:
            case SFDataType.ARRAY:
                return typeof(string);

            case SFDataType.DATE:
            case SFDataType.TIME:
            case SFDataType.TIMESTAMP_NTZ:
                return typeof(DateTime);

            case SFDataType.TIMESTAMP_LTZ:
            case SFDataType.TIMESTAMP_TZ:
                return typeof(DateTimeOffset);

            case SFDataType.BINARY:
                return typeof(byte[]);

            case SFDataType.BOOLEAN:
                return typeof(bool);

            default:
                throw new SnowflakeDbException(SFError.INTERNAL_ERROR,
                    $"Unknow column type: {sfType}");
        }
    }

    internal Type getCSharpTypeByIndex(int targetIndex)
    {
        if (targetIndex < 0 || targetIndex >= m_ColumnCount)
            throw new SnowflakeDbException(SFError.COLUMN_INDEX_OUT_OF_BOUND, targetIndex);

        SFDataType sfType = getColumnTypeByIndex(targetIndex);
        return GetNativeTypeForColumn(sfType, rowTypes[targetIndex]);
    }

    internal string? getColumnNameByIndex(int targetIndex)
    {
        if (targetIndex < 0 || targetIndex >= m_ColumnCount)
            throw new SnowflakeDbException(SFError.COLUMN_INDEX_OUT_OF_BOUND, targetIndex);

        return rowTypes[targetIndex].name;
    }

    SFStatementType findStatementTypeById(long id)
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
