/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing.Chunks;

class DownloadContext
{
	public SFResultChunk? Chunk { get; set; }

	public int ChunkIndex { get; set; }

	public string? Qrmk { get; set; }

	public Dictionary<string, string>? ChunkHeaders { get; set; }

	public CancellationToken CancellationToken { get; set; }
}
