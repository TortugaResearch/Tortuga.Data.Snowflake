/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

#nullable enable

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing.Chunks;

internal class ExecResponseChunk
{
	[JsonProperty(PropertyName = "url")]
	internal string? url { get; set; }

	[JsonProperty(PropertyName = "rowCount")]
	internal int rowCount { get; set; }

	[JsonProperty(PropertyName = "uncompressedSize")]
	internal int uncompressedSize { get; set; }
}
