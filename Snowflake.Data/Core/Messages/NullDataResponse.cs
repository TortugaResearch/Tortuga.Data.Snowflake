/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

#nullable enable

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Messages;

class NullDataResponse : BaseRestResponse
{
	[JsonProperty(PropertyName = "data")]
	internal object? data { get; set; }
}
