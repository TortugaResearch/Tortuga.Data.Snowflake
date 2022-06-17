/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing;

class FastStreamWrapper
{
	readonly Stream m_WrappedStream;
	readonly byte[] m_Buffer = new byte[32768];
	int m_Count;
	int m_Next;

	public FastStreamWrapper(Stream s)
	{
		m_WrappedStream = s;
	}

	// Small method to encourage inlining
	public int ReadByte()
	{
		// fast path first
		if (m_Next < m_Count)
			return m_Buffer[m_Next++];
		else
			return ReadByteSlow();
	}

	int ReadByteSlow()
	{
		// fast path first
		if (m_Next < m_Count)
			return m_Buffer[m_Next++];

		if (m_Count >= 0)
		{
			m_Next = 0;
			m_Count = m_WrappedStream.Read(m_Buffer, 0, m_Buffer.Length);
		}

		if (m_Count <= 0)
		{
			m_Count = -1;
			return -1;
		}

		return m_Buffer[m_Next++];
	}
}
