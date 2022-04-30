/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core;

public class FastStreamWrapper
{
	Stream wrappedStream;
	byte[] buffer = new byte[32768];
	int count = 0;
	int next = 0;

	public FastStreamWrapper(Stream s)
	{
		wrappedStream = s;
	}

	// Small method to encourage inlining
	public int ReadByte()
	{
		// fast path first
		if (next < count)
			return buffer[next++];
		else
			return ReadByteSlow();
	}

	private int ReadByteSlow()
	{
		// fast path first
		if (next < count)
			return buffer[next++];

		if (count >= 0)
		{
			next = 0;
			count = wrappedStream.Read(buffer, 0, buffer.Length);
		}

		if (count <= 0)
		{
			count = -1;
			return -1;
		}

		return buffer[next++];
	}
}
