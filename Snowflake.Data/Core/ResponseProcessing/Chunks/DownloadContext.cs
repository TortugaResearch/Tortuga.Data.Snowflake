/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing.Chunks;

class DownloadContext
{
    public SFResultChunk? chunk { get; set; }

    public int chunkIndex { get; set; }

    public string? qrmk { get; set; }

    public Dictionary<string, string>? chunkHeaders { get; set; }

    public CancellationToken cancellationToken { get; set; }
}
