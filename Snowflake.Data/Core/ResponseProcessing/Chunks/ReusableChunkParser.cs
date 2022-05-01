/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing.Chunks;

public class ReusableChunkParser : IChunkParser
{
	// Very fast parser, only supports strings and nulls
	// Never generates parsing errors

	private readonly Stream stream;

	internal ReusableChunkParser(Stream stream)
	{
		this.stream = stream;
	}

	public async Task ParseChunk(IResultChunk chunk)
	{
		SFReusableChunk rc = (SFReusableChunk)chunk;

		bool inString = false;
		int c;
		var input = new FastStreamWrapper(stream);
		var ms = new FastMemoryStream();
		await Task.Run(() =>
		{
			while ((c = input.ReadByte()) >= 0)
			{
				if (!inString)
				{
					// n means null
					// " quote means begin string
					// all else are ignored
					if (c == '"')
					{
						inString = true;
					}
					else if (c == 'n')
					{
						rc.AddCell(null, 0);
					}
					// ignore anything else
				}
				else
				{
					// Inside a string, look for end string
					// Anything else is saved in the buffer
					if (c == '"')
					{
						rc.AddCell(ms.GetBuffer(), ms.Length);
						ms.Clear();
						inString = false;
					}
					else if (c == '\\')
					{
						// Process next character
						c = input.ReadByte();
						switch (c)
						{
							case 'n':
								c = '\n';
								break;

							case 'r':
								c = '\r';
								break;

							case 'b':
								c = '\b';
								break;

							case 't':
								c = '\t';
								break;

							case -1:
								throw new SnowflakeDbException(SFError.INTERNAL_ERROR, $"Unexpected end of stream in escape sequence");
						}
						ms.WriteByte((byte)c);
					}
					else
					{
						ms.WriteByte((byte)c);
					}
				}
			}
			if (inString)
				throw new SnowflakeDbException(SFError.INTERNAL_ERROR, $"Unexpected end of stream in string");
		});
	}
}
