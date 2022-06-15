using Tortuga.Data.Snowflake.Core.ResponseProcessing;

namespace Tortuga.Data.Snowflake.Core;

static class FastParser
{
	public static long FastParseInt64(byte[] s, int offset, int len)
	{
		long result = 0;
		var i = offset;
		var isMinus = false;
		if (len > 0 && s[i] == '-')
		{
			isMinus = true;
			i++;
		}
		var end = len + offset;
		for (; i < end; i++)
		{
			if ((ulong)result > (0x7fffffffffffffff / 10))
				throw new OverflowException();
			var c = s[i] - '0';
			if (c < 0 || c > 9)
				throw new FormatException();
			result = result * 10 + c;
		}
		if (isMinus)
		{
			result = -result;
			if (result > 0)
				throw new OverflowException();
		}
		else
		{
			if (result < 0)
				throw new OverflowException();
		}
		return result;
	}

	public static int FastParseInt32(byte[] s, int offset, int len)
	{
		var result = 0;
		var i = offset;
		var isMinus = false;
		if (len > 0 && s[i] == '-')
		{
			isMinus = true;
			i++;
		}
		var end = len + offset;
		for (; i < end; i++)
		{
			if ((uint)result > (0x7fffffff / 10))
				throw new OverflowException();
			var c = s[i] - '0';
			if (c < 0 || c > 9)
				throw new FormatException();
			result = result * 10 + c;
		}
		if (isMinus)
		{
			result = -result;
			if (result > 0)
				throw new OverflowException();
		}
		else
		{
			if (result < 0)
				throw new OverflowException();
		}
		return result;
	}

	public static decimal FastParseDecimal(byte[] s, int offset, int len)
	{
		// Find any decimal point
		// Parse integer part and decimal part as 64-bit numbers
		// Calculate decimal number to return
		var decimalPos = Array.IndexOf<byte>(s, (byte)'.', offset, len);

		// No decimal point found, just parse as integer
		if (decimalPos < 0)
		{
			// If len > 19 (the number of digits in int64.MaxValue), the value is likely bigger
			// than max int64. Potentially, if it is a negative number it could be ok, but it
			// is better to not to find out during the call to FastParseInt64.
			// Fallback to regular decimal constructor from string instead.
			if (len > 19)
				return decimal.Parse(UTF8Buffer.UTF8.GetString(s, offset, len));

			try
			{
				var i1 = FastParseInt64(s, offset, len);
				return (decimal)i1;
			}
			catch (OverflowException)
			{
				// Fallback to regular decimal constructor from string instead.
				return decimal.Parse(UTF8Buffer.UTF8.GetString(s, offset, len));
			}
		}
		else
		{
			decimalPos -= offset;
			var decimalLen = len - decimalPos - 1;
			long intPart;
			long decimalPart;
			try
			{
				intPart = FastParseInt64(s, offset, decimalPos);
				decimalPart = FastParseInt64(s, offset + decimalPos + 1, decimalLen);
			}
			catch (OverflowException)
			{
				// Fallback to regular decimal constructor from string instead.
				return decimal.Parse(UTF8Buffer.UTF8.GetString(s, offset, len));
			}

			var isMinus = false;
			if (decimalPart < 0)
				throw new FormatException();
			if (intPart < 0)
			{
				isMinus = true;
				intPart = -intPart;
				if (intPart < 0)
					throw new OverflowException();
			}
			else if (intPart == 0)
			{
				// Sign is stripped from the Int64 for value of "-0"
				if (s[offset] == '-')
					isMinus = true;
			}
			var d1 = new decimal(intPart);
			var d2 = new decimal((int)(decimalPart & 0xffffffff), (int)((decimalPart >> 32) & 0xffffffff), 0, false, (byte)decimalLen);
			var result = d1 + d2;
			if (isMinus)
				result = -result;
			return result;
		}
	}
}
