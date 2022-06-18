/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing;

/// <summary>
///     Used to concat multiple streams without copying. Since we need to preappend '[' and append ']'
/// </summary>
class ConcatenatedStream : Stream
{
	readonly Queue<Stream> m_Streams;

	public ConcatenatedStream(IEnumerable<Stream> streams)
	{
		m_Streams = new Queue<Stream>(streams);
	}

	public override bool CanRead => true;

	public override int Read(byte[] buffer, int offset, int count)
	{
		if (m_Streams.Count == 0)
			return 0;

		var bytesRead = m_Streams.Peek().Read(buffer, offset, count);
		if (bytesRead == 0)
		{
			m_Streams.Dequeue().Dispose();
			bytesRead += Read(buffer, offset + bytesRead, count - bytesRead);
		}
		return bytesRead;
	}

	public override bool CanSeek => false;

	public override bool CanWrite => false;

	public override void Flush() => throw new NotImplementedException();

	public override long Length => throw new NotImplementedException();

	public override long Position
	{
		get => throw new NotImplementedException();
		set => throw new NotImplementedException();
	}

	public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

	public override void SetLength(long value) => throw new NotImplementedException();

	public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
}
