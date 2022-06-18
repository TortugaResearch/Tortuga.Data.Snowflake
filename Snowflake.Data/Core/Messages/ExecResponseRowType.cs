/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Messages;

class ExecResponseRowType
{
	[JsonProperty(PropertyName = "name")]
	internal string? Name { get; set; }

	[JsonProperty(PropertyName = "byteLength", NullValueHandling = NullValueHandling.Ignore)]
	internal long ByteLength { get; set; }

	[JsonProperty(PropertyName = "length", NullValueHandling = NullValueHandling.Ignore)]
	internal long Length { get; set; }

	[JsonProperty(PropertyName = "type")]
	internal string? Type { get; set; }

	[JsonProperty(PropertyName = "scale", NullValueHandling = NullValueHandling.Ignore)]
	internal long Scale { get; set; }

	[JsonProperty(PropertyName = "precision", NullValueHandling = NullValueHandling.Ignore)]
	internal long Precision { get; set; }

	[JsonProperty(PropertyName = "nullable")]
	internal bool Nullable { get; set; }
}
