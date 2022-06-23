/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake;

public enum SnowflakeDbDataType
{
	None = 0,
	Fixed = 1,
	Real = 2,
	Text = 3,
	Date = 4,
	Variant = 5,
	TimestampLtz = 6,
	TimestampNtz = 7,
	TimestampTz = 8,
#pragma warning disable CA1720 // Identifier contains type name
	Object = 9,
#pragma warning restore CA1720 // Identifier contains type name
	Binary = 10,
	Time = 11,
	Boolean = 12,
	Array = 13
}

static class SnowflakeDbDataTypeExtensions
{
	/// <summary>
	/// Converts a SFDataType to its SQL representation.
	/// </summary>
	/// <param name="dataType">Type of the data.</param>
	public static string ToSql(this SnowflakeDbDataType dataType)
	{
		return dataType switch
		{
			SnowflakeDbDataType.None => "",
			SnowflakeDbDataType.Fixed => "FIXED",
			SnowflakeDbDataType.Real => "REAL",
			SnowflakeDbDataType.Text => "TEXT",
			SnowflakeDbDataType.Date => "DATE",
			SnowflakeDbDataType.Variant => "VARIANT",
			SnowflakeDbDataType.TimestampLtz => "TIMESTAMP_LTZ",
			SnowflakeDbDataType.TimestampNtz => "TIMESTAMP_NTZ",
			SnowflakeDbDataType.TimestampTz => "TIMESTAMP_TZ",
			SnowflakeDbDataType.Object => "OBJECT",
			SnowflakeDbDataType.Binary => "BINARY",
			SnowflakeDbDataType.Time => "TIME",
			SnowflakeDbDataType.Boolean => "BOOLEAN",
			SnowflakeDbDataType.Array => "ARRAY",
			_ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, "Unknown data type")
		};
	}

	/// <summary>
	/// Froms the SQL.
	/// </summary>
	/// <param name="type">The type.</param>
	/// <returns>SFDataType.</returns>
	/// <exception cref="System.ArgumentException">type</exception>
	/// <exception cref="Tortuga.Data.Snowflake.SnowflakeDbException">Unknow column type: {type}</exception>
	public static SnowflakeDbDataType FromSql(string type)
	{
		if (string.IsNullOrEmpty(type))
			throw new ArgumentException($"{nameof(type)} is null or empty.", nameof(type));

		bool CheckName(SnowflakeDbDataType candidate)
		{
			return string.Equals(candidate.ToString(), type, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(candidate.ToSql(), type, StringComparison.OrdinalIgnoreCase);
		}

		if (CheckName(SnowflakeDbDataType.None)) return SnowflakeDbDataType.None;
		if (CheckName(SnowflakeDbDataType.Fixed)) return SnowflakeDbDataType.Fixed;
		if (CheckName(SnowflakeDbDataType.Real)) return SnowflakeDbDataType.Real;
		if (CheckName(SnowflakeDbDataType.Text)) return SnowflakeDbDataType.Text;
		if (CheckName(SnowflakeDbDataType.Date)) return SnowflakeDbDataType.Date;
		if (CheckName(SnowflakeDbDataType.Variant)) return SnowflakeDbDataType.Variant;
		if (CheckName(SnowflakeDbDataType.TimestampLtz)) return SnowflakeDbDataType.TimestampLtz;
		if (CheckName(SnowflakeDbDataType.TimestampNtz)) return SnowflakeDbDataType.TimestampNtz;
		if (CheckName(SnowflakeDbDataType.TimestampTz)) return SnowflakeDbDataType.TimestampTz;
		if (CheckName(SnowflakeDbDataType.Object)) return SnowflakeDbDataType.Object;
		if (CheckName(SnowflakeDbDataType.Binary)) return SnowflakeDbDataType.Binary;
		if (CheckName(SnowflakeDbDataType.Time)) return SnowflakeDbDataType.Time;
		if (CheckName(SnowflakeDbDataType.Boolean)) return SnowflakeDbDataType.Boolean;
		if (CheckName(SnowflakeDbDataType.Array)) return SnowflakeDbDataType.Array;

		throw new SnowflakeDbException(SnowflakeDbError.InternalError, $"Unknow column type: {type}");
	}
}
