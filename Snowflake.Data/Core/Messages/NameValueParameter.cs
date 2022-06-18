/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Messages;

class NameValueParameter
{
	[JsonProperty(PropertyName = "name")]
	internal string? Name { get; set; }

	[JsonProperty(PropertyName = "value")]
	internal string? Value { get; set; }
}
