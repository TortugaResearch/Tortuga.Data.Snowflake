/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake;

public enum SFDataType
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

static class SFDataTypeExtensions
{
	/// <summary>
	/// Converts a SFDataType to its SQL representation.
	/// </summary>
	/// <param name="dataType">Type of the data.</param>
	public static string ToSql(this SFDataType dataType)
	{
		return dataType switch
		{
			SFDataType.None => "",
			SFDataType.Fixed => "FIXED",
			SFDataType.Real => "REAL",
			SFDataType.Text => "TEXT",
			SFDataType.Date => "DATE",
			SFDataType.Variant => "VARIANT",
			SFDataType.TimestampLtz => "TIMESTAMP_LTZ",
			SFDataType.TimestampNtz => "TIMESTAMP_NTZ",
			SFDataType.TimestampTz => "TIMESTAMP_TZ",
			SFDataType.Object => "OBJECT",
			SFDataType.Binary => "BINARY",
			SFDataType.Time => "TIME",
			SFDataType.Boolean => "BOOLEAN",
			SFDataType.Array => "ARRAY",
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
	public static SFDataType FromSql(string type)
	{
		if (string.IsNullOrEmpty(type))
			throw new ArgumentException($"{nameof(type)} is null or empty.", nameof(type));

		bool CheckName(SFDataType candidate)
		{
			return string.Equals(candidate.ToString(), type, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(candidate.ToSql(), type, StringComparison.OrdinalIgnoreCase);
		}

		if (CheckName(SFDataType.None)) return SFDataType.None;
		if (CheckName(SFDataType.Fixed)) return SFDataType.Fixed;
		if (CheckName(SFDataType.Real)) return SFDataType.Real;
		if (CheckName(SFDataType.Text)) return SFDataType.Text;
		if (CheckName(SFDataType.Date)) return SFDataType.Date;
		if (CheckName(SFDataType.Variant)) return SFDataType.Variant;
		if (CheckName(SFDataType.TimestampLtz)) return SFDataType.TimestampLtz;
		if (CheckName(SFDataType.TimestampNtz)) return SFDataType.TimestampNtz;
		if (CheckName(SFDataType.TimestampTz)) return SFDataType.TimestampTz;
		if (CheckName(SFDataType.Object)) return SFDataType.Object;
		if (CheckName(SFDataType.Binary)) return SFDataType.Binary;
		if (CheckName(SFDataType.Time)) return SFDataType.Time;
		if (CheckName(SFDataType.Boolean)) return SFDataType.Boolean;
		if (CheckName(SFDataType.Array)) return SFDataType.Array;

		throw new SnowflakeDbException(SnowflakeError.InternalError, $"Unknow column type: {type}");
	}
}
