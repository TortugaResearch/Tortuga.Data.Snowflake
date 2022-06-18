/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Messages;

abstract class BaseRestResponse
{
	[JsonProperty(PropertyName = "message")]
	internal string? Message { get; set; }

	[JsonProperty(PropertyName = "code", NullValueHandling = NullValueHandling.Ignore)]
	internal int Code { get; set; }

	[JsonProperty(PropertyName = "success")]
	internal bool Success { get; set; }

	internal void FilterFailedResponse()
	{
		if (!Success)
			throw new SFException("", Code, Message, "");
	}
}
