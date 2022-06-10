/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing.Chunks;

class ChunkDeserializer : IChunkParser
{
	static JsonSerializer JsonSerializer = new() { DateParseHandling = DateParseHandling.None };

	readonly Stream m_Stream;

	internal ChunkDeserializer(Stream stream)
	{
		m_Stream = stream;
	}

	public void ParseChunk(IResultChunk chunk)
	{
		// parse results row by row
		using (var sr = new StreamReader(m_Stream))
		using (var jr = new JsonTextReader(sr))
		{
			((SFResultChunk)chunk).RowSet = JsonSerializer.Deserialize<string[,]>(jr)!;
		}
	}

	public async Task ParseChunkAsync(IResultChunk chunk)
	{
		await Task.Run(() => ParseChunk(chunk));
	}
}
