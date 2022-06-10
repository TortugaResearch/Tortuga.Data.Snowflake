/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing.Chunks;

public interface IResultChunk
{
    UTF8Buffer? ExtractCell(int rowIndex, int columnIndex);

    int GetRowCount();

    int GetChunkIndex();
}
