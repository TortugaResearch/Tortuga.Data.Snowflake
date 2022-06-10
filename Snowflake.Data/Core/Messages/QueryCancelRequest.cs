/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Messages;

class QueryCancelRequest
{
	[JsonProperty(PropertyName = "requestId")]
	internal string? requestId { get; set; }
}
