/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

#nullable enable

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing.Chunks;

static class ChunkParserFactory
{
	public static IChunkParser GetParser(SnowflakeDbConfiguration configuration, Stream stream)
	{
		if (!configuration.UseV2JsonParser)
			return new ChunkDeserializer(stream);
		else
			return new ChunkStreamingParser(stream);
	}
}
