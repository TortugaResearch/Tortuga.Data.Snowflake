/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing.Chunks;

interface IChunkParser
{
    /// <summary>
    ///     Parse source data stream, result will be store into SFResultChunk.rowset
    /// </summary>
    /// <param name="chunk"></param>
    Task ParseChunkAsync(IResultChunk chunk);

    /// <summary>
    ///     Parse source data stream, result will be store into SFResultChunk.rowset
    /// </summary>
    /// <param name="chunk"></param>
    void ParseChunk(IResultChunk chunk);
}
