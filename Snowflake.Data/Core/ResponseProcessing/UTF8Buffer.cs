#nullable enable

using System.Text;

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing;

public class UTF8Buffer
{
	// Cache for maximum performance
	public static Encoding UTF8 = Encoding.UTF8;

	public byte[] Buffer;
	public int offset;
	public int length;

	public UTF8Buffer(byte[] Buffer, int Offset, int Length)
	{
		this.Buffer = Buffer;
		this.offset = Offset;
		this.length = Length;
	}

	public UTF8Buffer(byte[] Buffer)
	{
		this.Buffer = Buffer;
		this.offset = 0;
		this.length = Buffer.Length;
	}

	public override string ToString() => UTF8.GetString(Buffer, offset, length);

	public byte[] GetBytes()
	{
		// Return a new byte array containing only the relevant part of the buffer
		var result = new byte[length];
		Array.Copy(Buffer, offset, result, 0, length);
		return result;
	}
}
