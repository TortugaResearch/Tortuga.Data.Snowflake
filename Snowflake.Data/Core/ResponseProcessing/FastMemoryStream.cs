namespace Tortuga.Data.Snowflake.Core.ResponseProcessing;

// Optimized for maximum speed when adding one byte at a time to short buffers
class FastMemoryStream
{
	byte[] m_Buffer;
	int m_Size;

	public FastMemoryStream()
	{
		m_Buffer = new byte[256];
		m_Size = 0;
	}

	public void WriteByte(byte b)
	{
		if (m_Size == m_Buffer.Length)
			GrowBuffer();
		m_Buffer[m_Size] = b;
		m_Size++;
	}

	public void Clear()
	{
		// We reuse the same buffer, we also do not bother to clear the buffer
		m_Size = 0;
	}

	public byte[] GetBuffer()
	{
		// Note that we return a reference to the actual buffer. No copying here
		return m_Buffer;
	}

	public int Length => m_Size;

	void GrowBuffer()
	{
		// Create a new array with double the size and copy existing elements to the new array
		var newBuffer = new byte[m_Buffer.Length * 2];
		Array.Copy(m_Buffer, newBuffer, m_Size);
		m_Buffer = newBuffer;
	}
}
