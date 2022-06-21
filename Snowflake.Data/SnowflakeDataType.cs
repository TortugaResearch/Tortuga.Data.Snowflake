/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake;

public enum SnowflakeDataType
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

static class SnowflakeDataTypeExtensions
{
	/// <summary>
	/// Converts a SFDataType to its SQL representation.
	/// </summary>
	/// <param name="dataType">Type of the data.</param>
	public static string ToSql(this SnowflakeDataType dataType)
	{
		return dataType switch
		{
			SnowflakeDataType.None => "",
			SnowflakeDataType.Fixed => "FIXED",
			SnowflakeDataType.Real => "REAL",
			SnowflakeDataType.Text => "TEXT",
			SnowflakeDataType.Date => "DATE",
			SnowflakeDataType.Variant => "VARIANT",
			SnowflakeDataType.TimestampLtz => "TIMESTAMP_LTZ",
			SnowflakeDataType.TimestampNtz => "TIMESTAMP_NTZ",
			SnowflakeDataType.TimestampTz => "TIMESTAMP_TZ",
			SnowflakeDataType.Object => "OBJECT",
			SnowflakeDataType.Binary => "BINARY",
			SnowflakeDataType.Time => "TIME",
			SnowflakeDataType.Boolean => "BOOLEAN",
			SnowflakeDataType.Array => "ARRAY",
			_ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, "Unknown data type")
		};
	}

	/// <summary>
	/// Froms the SQL.
	/// </summary>
	/// <param name="type">The type.</param>
	/// <returns>SFDataType.</returns>
	/// <exception cref="System.ArgumentException">type</exception>
	/// <exception cref="Tortuga.Data.Snowflake.SnowflakeException">Unknow column type: {type}</exception>
	public static SnowflakeDataType FromSql(string type)
	{
		if (string.IsNullOrEmpty(type))
			throw new ArgumentException($"{nameof(type)} is null or empty.", nameof(type));

		bool CheckName(SnowflakeDataType candidate)
		{
			return string.Equals(candidate.ToString(), type, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(candidate.ToSql(), type, StringComparison.OrdinalIgnoreCase);
		}

		if (CheckName(SnowflakeDataType.None)) return SnowflakeDataType.None;
		if (CheckName(SnowflakeDataType.Fixed)) return SnowflakeDataType.Fixed;
		if (CheckName(SnowflakeDataType.Real)) return SnowflakeDataType.Real;
		if (CheckName(SnowflakeDataType.Text)) return SnowflakeDataType.Text;
		if (CheckName(SnowflakeDataType.Date)) return SnowflakeDataType.Date;
		if (CheckName(SnowflakeDataType.Variant)) return SnowflakeDataType.Variant;
		if (CheckName(SnowflakeDataType.TimestampLtz)) return SnowflakeDataType.TimestampLtz;
		if (CheckName(SnowflakeDataType.TimestampNtz)) return SnowflakeDataType.TimestampNtz;
		if (CheckName(SnowflakeDataType.TimestampTz)) return SnowflakeDataType.TimestampTz;
		if (CheckName(SnowflakeDataType.Object)) return SnowflakeDataType.Object;
		if (CheckName(SnowflakeDataType.Binary)) return SnowflakeDataType.Binary;
		if (CheckName(SnowflakeDataType.Time)) return SnowflakeDataType.Time;
		if (CheckName(SnowflakeDataType.Boolean)) return SnowflakeDataType.Boolean;
		if (CheckName(SnowflakeDataType.Array)) return SnowflakeDataType.Array;

		throw new SnowflakeException(SnowflakeError.InternalError, $"Unknow column type: {type}");
	}
}
