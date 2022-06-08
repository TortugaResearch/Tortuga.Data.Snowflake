/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

#nullable enable

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing.Chunks;

interface IChunkDownloader
{
	Task<IResultChunk?> GetNextChunkAsync();
}
