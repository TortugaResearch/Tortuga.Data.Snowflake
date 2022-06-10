/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Messages;

abstract class BaseRestResponse
{
	[JsonProperty(PropertyName = "message")]
	internal string? message { get; set; }

	[JsonProperty(PropertyName = "code", NullValueHandling = NullValueHandling.Ignore)]
	internal int code { get; set; }

	[JsonProperty(PropertyName = "success")]
	internal bool success { get; set; }

	internal void FilterFailedResponse()
	{
		if (!success)
			throw new SnowflakeDbException("", code, message, "");
	}
}
