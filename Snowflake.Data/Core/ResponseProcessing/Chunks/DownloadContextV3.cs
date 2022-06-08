/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

#nullable enable

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing.Chunks;

class DownloadContextV3
{
	public SFReusableChunk? chunk { get; set; }

	public string? qrmk { get; set; }

	public Dictionary<string, string>? chunkHeaders { get; set; }

	public CancellationToken cancellationToken { get; set; }
}
