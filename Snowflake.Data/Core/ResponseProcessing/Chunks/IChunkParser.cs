/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing.Chunks;

abstract class ChunkParser
{
	/// <summary>
	///     Parse source data stream, result will be store into SFResultChunk.rowset
	/// </summary>
	/// <param name="chunk"></param>
	public abstract Task ParseChunkAsync(IResultChunk chunk);

	/// <summary>
	///     Parse source data stream, result will be store into SFResultChunk.rowset
	/// </summary>
	/// <param name="chunk"></param>
	public abstract void ParseChunk(IResultChunk chunk);

	public static ChunkParser GetParser(SnowflakeDbConfiguration configuration, Stream stream)
	{
		if (!configuration.UseV2JsonParser)
			return new ChunkDeserializer(stream);
		else
			return new ChunkStreamingParser(stream);
	}
}
