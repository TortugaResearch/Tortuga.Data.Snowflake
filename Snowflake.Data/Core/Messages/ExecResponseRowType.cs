/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Messages;

class ExecResponseRowType
{
	[JsonProperty(PropertyName = "name")]
	internal string? name { get; set; }

	[JsonProperty(PropertyName = "byteLength", NullValueHandling = NullValueHandling.Ignore)]
	internal long byteLength { get; set; }

	[JsonProperty(PropertyName = "length", NullValueHandling = NullValueHandling.Ignore)]
	internal long length { get; set; }

	[JsonProperty(PropertyName = "type")]
	internal string? type { get; set; }

	[JsonProperty(PropertyName = "scale", NullValueHandling = NullValueHandling.Ignore)]
	internal long scale { get; set; }

	[JsonProperty(PropertyName = "precision", NullValueHandling = NullValueHandling.Ignore)]
	internal long precision { get; set; }

	[JsonProperty(PropertyName = "nullable")]
	internal bool nullable { get; set; }
}
