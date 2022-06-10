/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing.Chunks;

class ChunkStreamingParser : IChunkParser
{
	readonly Stream m_Stream;

	internal ChunkStreamingParser(Stream stream)
	{
		m_Stream = stream;
	}

	public void ParseChunk(IResultChunk chunk)
	{
		// parse results row by row
		using (var sr = new StreamReader(m_Stream))
		using (var jr = new JsonTextReader(sr) { DateParseHandling = DateParseHandling.None })
		{
			var row = 0;
			var col = 0;

			var outputMatrix = new string?[chunk.GetRowCount(), ((SFResultChunk)chunk).ColCount];

			while (jr.Read())
			{
				switch (jr.TokenType)
				{
					case JsonToken.StartArray:
					case JsonToken.None:
						break;

					case JsonToken.EndArray:
						if (col > 0)
						{
							col = 0;
							row++;
						}

						break;

					case JsonToken.Null:
						outputMatrix[row, col++] = null;
						break;

					case JsonToken.String:
						outputMatrix[row, col++] = (string?)jr.Value;
						break;

					default:
						throw new SnowflakeDbException(SFError.INTERNAL_ERROR, $"Unexpected token type: {jr.TokenType}");
				}
			}
			((SFResultChunk)chunk).RowSet = outputMatrix;
		}
	}

	public async Task ParseChunkAsync(IResultChunk chunk)
	{
		await Task.Run(() => ParseChunk(chunk)); ;
	}
}
