/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing.Chunks;

class ChunkParserFactory
{
	public static IChunkParser GetParser(SnowflakeDbConfiguration configuration, Stream stream)
	{
		if (!configuration.UseV2JsonParser)
		{
			return new ChunkDeserializer(stream);
		}
		else
		{
			return new ChunkStreamingParser(stream);
		}
	}
}
