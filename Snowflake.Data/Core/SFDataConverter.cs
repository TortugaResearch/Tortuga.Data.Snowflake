/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using System.Data;
using System.Globalization;
using System.Text;
using Tortuga.Data.Snowflake.Core.ResponseProcessing;
using Tortuga.Data.Snowflake.Legacy;

namespace Tortuga.Data.Snowflake.Core;

static class SFDataConverter
{
	static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

	// Method with the same signature as before the performance work
	// Used by unit tests only
	internal static object ConvertToCSharpVal(string srcVal, SFDataType srcType, Type destType)
	{
		// Create an UTF8Buffer with an offset to get better testing
		var b1 = Encoding.UTF8.GetBytes(srcVal);
		var b2 = new byte[b1.Length + 100];
		Array.Copy(b1, 0, b2, 100, b1.Length);
		var v = new UTF8Buffer(b2, 100, b1.Length);
		return ConvertToCSharpVal(v, srcType, destType);
	}

	internal static object ConvertToCSharpVal(UTF8Buffer? srcVal, SFDataType srcType, Type destType)
	{
		if (srcVal == null)
			return DBNull.Value;

		try
		{
			// The most common conversions are checked first for maximum performance
			if (destType == typeof(long))
			{
				return FastParser.FastParseInt64(srcVal.Buffer, srcVal.offset, srcVal.length);
			}
			else if (destType == typeof(int))
			{
				return FastParser.FastParseInt32(srcVal.Buffer, srcVal.offset, srcVal.length);
			}
			else if (destType == typeof(decimal))
			{
				return FastParser.FastParseDecimal(srcVal.Buffer, srcVal.offset, srcVal.length);
			}
			else if (destType == typeof(string))
			{
				return srcVal.ToString();
			}
			else if (destType == typeof(DateTime))
			{
				return ConvertToDateTime(srcVal, srcType);
			}
			else if (destType == typeof(TimeSpan))
			{
				return ConvertToTimeSpan(srcVal, srcType);
			}
			else if (destType == typeof(DateTimeOffset))
			{
				return ConvertToDateTimeOffset(srcVal, srcType);
			}
			else if (destType == typeof(bool))
			{
				var val = srcVal.Buffer[srcVal.offset];
				return val == '1' || val == 't' || val == 'T';
			}
			else if (destType == typeof(byte[]))
			{
				return srcType == SFDataType.Binary ?
					HexToBytes(srcVal.ToString()) : srcVal.GetBytes();
			}
			else if (destType == typeof(short))
			{
				// Use checked keyword to make sure we generate an OverflowException if conversion fails
				var result = FastParser.FastParseInt32(srcVal.Buffer, srcVal.offset, srcVal.length);
				return checked((short)result);
			}
			else if (destType == typeof(byte))
			{
				// Use checked keyword to make sure we generate an OverflowException if conversion fails
				var result = FastParser.FastParseInt32(srcVal.Buffer, srcVal.offset, srcVal.length);
				return checked((byte)result);
			}
			else if (destType == typeof(double))
			{
				return Convert.ToDouble(srcVal.ToString(), CultureInfo.InvariantCulture);
			}
			else if (destType == typeof(float))
			{
				return Convert.ToSingle(srcVal.ToString(), CultureInfo.InvariantCulture);
			}
			else if (destType == typeof(Guid))
			{
				return new Guid(srcVal.ToString());
			}
			else if (destType == typeof(char[]))
			{
				var data = srcType == SFDataType.Binary ? HexToBytes(srcVal.ToString()) : srcVal.GetBytes();
				return Encoding.UTF8.GetString(data).ToCharArray();
			}
			else
			{
				throw new SnowflakeDbException(SnowflakeError.InternalError, "Invalid destination type.");
			}
		}
		catch (OverflowException e)
		{
			throw new OverflowException($"Error converting '{srcVal} to {destType.Name}'. Use GetString() to handle very large values", e);
		}
	}

	static object ConvertToTimeSpan(UTF8Buffer srcVal, SFDataType srcType)
	{
		switch (srcType)
		{
			case SFDataType.Time:
				// Convert fractional seconds since midnight to TimeSpan
				//  A single tick represents one hundred nanoseconds or one ten-millionth of a second.
				// There are 10,000 ticks in a millisecond
				return TimeSpan.FromTicks(GetTicksFromSecondAndNanosecond(srcVal));

			default:
				throw new SnowflakeDbException(SnowflakeError.InvalidDataConversion, srcVal, srcType, typeof(TimeSpan));
		}
	}

	static DateTime ConvertToDateTime(UTF8Buffer srcVal, SFDataType srcType)
	{
		switch (srcType)
		{
			case SFDataType.Date:
				var srcValLong = FastParser.FastParseInt64(srcVal.Buffer, srcVal.offset, srcVal.length);
				return UnixEpoch.AddDays(srcValLong);

			case SFDataType.Time:
			case SFDataType.TimestampNtz:
				var tickDiff = GetTicksFromSecondAndNanosecond(srcVal);
				return UnixEpoch.AddTicks(tickDiff);

			default:
				throw new SnowflakeDbException(SnowflakeError.InvalidDataConversion, srcVal, srcType, typeof(DateTime));
		}
	}

	static DateTimeOffset ConvertToDateTimeOffset(UTF8Buffer srcVal, SFDataType srcType)
	{
		switch (srcType)
		{
			case SFDataType.TimestampTz:
				var spaceIndex = Array.IndexOf(srcVal.Buffer, (byte)' ', srcVal.offset, srcVal.length); ;
				if (spaceIndex == -1)
				{
					throw new SnowflakeDbException(SnowflakeError.InternalError,
						$"Invalid timestamp_tz value: {srcVal}");
				}
				else
				{
					spaceIndex -= srcVal.offset;
					var timeVal = new UTF8Buffer(srcVal.Buffer, srcVal.offset, spaceIndex);
					var offset = FastParser.FastParseInt32(srcVal.Buffer, srcVal.offset + spaceIndex + 1, srcVal.length - spaceIndex - 1);
					var offSetTimespan = new TimeSpan((offset - 1440) / 60, 0, 0);
					return new DateTimeOffset(UnixEpoch.Ticks + GetTicksFromSecondAndNanosecond(timeVal), TimeSpan.Zero).ToOffset(offSetTimespan);
				}
			case SFDataType.TimestampLtz:
				return new DateTimeOffset(UnixEpoch.Ticks + GetTicksFromSecondAndNanosecond(srcVal), TimeSpan.Zero).ToLocalTime();

			default:
				throw new SnowflakeDbException(SnowflakeError.InvalidDataConversion, srcVal, srcType, typeof(DateTimeOffset).ToString());
		}
	}

	static readonly int[] powersOf10 =  {
			1,
			10,
			100,
			1000,
			10000,
			100000,
			1000000,
			10000000,
			100000000
		};

	/// <summary>
	/// Convert the time value with the format seconds.nanoseconds to a number of
	/// Ticks. A single tick represents one hundred nanoseconds.
	/// For example, "23:59:59.123456789" is represented by 86399.123456789 and is
	/// 863991234567 ticks (precision is maximum 7).
	/// </summary>
	/// <param name="srcVal">The source data returned by the server. For example '86399.123456789'</param>
	/// <returns>The corresponding number of ticks for the given value.</returns>
	static long GetTicksFromSecondAndNanosecond(UTF8Buffer srcVal)
	{
		long intPart;
		var decimalPart = 0L;
		var dotIndex = Array.IndexOf(srcVal.Buffer, (byte)'.', srcVal.offset, srcVal.length);
		if (dotIndex == -1)
		{
			intPart = FastParser.FastParseInt64(srcVal.Buffer, srcVal.offset, srcVal.length);
		}
		else
		{
			dotIndex -= srcVal.offset;
			intPart = FastParser.FastParseInt64(srcVal.Buffer, srcVal.offset, dotIndex);
			var decimalPartLength = srcVal.length - dotIndex - 1;
			decimalPart = FastParser.FastParseInt64(srcVal.Buffer, srcVal.offset + dotIndex + 1, decimalPartLength);
			// If the decimal part contained less than nine characters, we must convert the value to nanoseconds by
			// multiplying by 10^[precision difference].
			if (decimalPartLength < 9 && decimalPartLength > 0)
			{
				decimalPart *= powersOf10[9 - decimalPartLength];
			}
		}

		return intPart * 10000000L + decimalPart / 100L;
	}

	internal static Tuple<string, string?> CSharpTypeValToSfTypeVal(DbType srcType, object? srcVal)
	{
		SFDataType destType;
		string? destVal;

		switch (srcType)
		{
			case DbType.Decimal:
			case DbType.SByte:
			case DbType.Int16:
			case DbType.Int32:
			case DbType.Int64:
			case DbType.Byte:
			case DbType.UInt16:
			case DbType.UInt32:
			case DbType.UInt64:
			case DbType.VarNumeric:
				destType = SFDataType.Fixed;
				break;

			case DbType.Boolean:
				destType = SFDataType.Boolean;
				break;

			case DbType.Double:
			case DbType.Single:
				destType = SFDataType.Real;
				break;

			case DbType.Guid:
			case DbType.String:
			case DbType.StringFixedLength:
				destType = SFDataType.Text;
				break;

			case DbType.Date:
				destType = SFDataType.Date;
				break;

			case DbType.Time:
				destType = SFDataType.Time;
				break;

			case DbType.DateTime:
			case DbType.DateTime2:
				destType = SFDataType.TimestampNtz;
				break;

			// By default map DateTimeoffset to TIMESTAMP_TZ
			case DbType.DateTimeOffset:
				destType = SFDataType.TimestampTz;
				break;

			case DbType.Binary:
				destType = SFDataType.Binary;
				break;

			default:
				throw new SnowflakeDbException(SnowflakeError.UnsupportedDotnetType, srcType);
		}
		destVal = csharpValToSfVal(destType, srcVal);
		return Tuple.Create(destType.ToSql(), destVal);
	}

	static string BytesToHex(byte[] bytes)
	{
		var hexBuilder = new StringBuilder(bytes.Length * 2);
		foreach (var b in bytes)
		{
			hexBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0:x2}", b);
		}
		return hexBuilder.ToString();
	}

	static byte[] HexToBytes(string hex)
	{
		var NumberChars = hex.Length;
		var bytes = new byte[NumberChars / 2];
		for (var i = 0; i < NumberChars; i += 2)
			bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
		return bytes;
	}

	internal static string? csharpValToSfVal(SFDataType sfDataType, object? srcVal)
	{
		if ((srcVal == DBNull.Value) || (srcVal == null))
			return null;

		string? destVal;

		switch (sfDataType)
		{
			case SFDataType.TimestampLtz:
				if (srcVal.GetType() != typeof(DateTimeOffset))
				{
					throw new SnowflakeDbException(SnowflakeError.InvalidDataConversion, srcVal,
						srcVal.GetType().ToString(), SFDataType.TimestampLtz.ToString());
				}
				else
				{
					destVal = ((long)(((DateTimeOffset)srcVal).UtcTicks - UnixEpoch.Ticks) * 100).ToString(CultureInfo.InvariantCulture);
				}
				break;

			case SFDataType.Fixed:
			case SFDataType.Boolean:
			case SFDataType.Real:
			case SFDataType.Text:
				destVal = string.Format(CultureInfo.InvariantCulture, "{0}", srcVal);
				break;

			case SFDataType.Time:
				if (srcVal.GetType() != typeof(DateTime))
				{
					throw new SnowflakeDbException(SnowflakeError.InvalidDataConversion, srcVal, srcVal.GetType().ToString(), DbType.Time.ToString());
				}
				else
				{
					var srcDt = ((DateTime)srcVal);
					var nanoSinceMidNight = (long)(srcDt.Ticks - srcDt.Date.Ticks) * 100L;

					destVal = nanoSinceMidNight.ToString(CultureInfo.InvariantCulture);
				}
				break;

			case SFDataType.Date:
				if (srcVal.GetType() != typeof(DateTime))
				{
					throw new SnowflakeDbException(SnowflakeError.InvalidDataConversion, srcVal, srcVal.GetType().ToString(), DbType.Date.ToString());
				}
				else
				{
					var dt = ((DateTime)srcVal).Date;
					var ts = dt.Subtract(UnixEpoch);
					var millis = (long)(ts.TotalMilliseconds);
					destVal = millis.ToString(CultureInfo.InvariantCulture);
				}
				break;

			case SFDataType.TimestampNtz:
				if (srcVal.GetType() != typeof(DateTime))
				{
					throw new SnowflakeDbException(SnowflakeError.InvalidDataConversion, srcVal,
						srcVal.GetType().ToString(), DbType.DateTime.ToString());
				}
				else
				{
					var srcDt = (DateTime)srcVal;
					var diff = srcDt.Subtract(UnixEpoch);
					var tickDiff = diff.Ticks;
					destVal = $"{tickDiff}00"; // Cannot multiple tickDiff by 100 because long might overflow.
				}
				break;

			case SFDataType.TimestampTz:
				if (srcVal.GetType() != typeof(DateTimeOffset))
				{
					throw new SnowflakeDbException(SnowflakeError.InvalidDataConversion, srcVal, srcVal.GetType().ToString(), DbType.DateTimeOffset.ToString());
				}
				else
				{
					var dtOffset = (DateTimeOffset)srcVal;
					destVal = String.Format(CultureInfo.InvariantCulture, "{0} {1}", (dtOffset.UtcTicks - UnixEpoch.Ticks) * 100L, dtOffset.Offset.TotalMinutes + 1440);
				}
				break;

			case SFDataType.Binary:
				if (srcVal.GetType() != typeof(byte[]))
				{
					throw new SnowflakeDbException(SnowflakeError.InvalidDataConversion, srcVal, srcVal.GetType().ToString(), DbType.Binary.ToString());
				}
				else
				{
					destVal = BytesToHex((byte[])srcVal);
				}
				break;

			default:
				throw new SnowflakeDbException(
					SnowflakeError.UnsupportedSnowflakeTypeForParam, sfDataType.ToString());
		}
		return destVal;
	}

	internal static string toDateString(DateTime date, string formatter)
	{
		// change formatter from "YYYY-MM-DD" to "yyyy-MM-dd"
		formatter = formatter.Replace("Y", "y", StringComparison.Ordinal).Replace("m", "M", StringComparison.Ordinal).Replace("D", "d", StringComparison.Ordinal);
		return date.ToString(formatter, CultureInfo.InvariantCulture);
	}
}
