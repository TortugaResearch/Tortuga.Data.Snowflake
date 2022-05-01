/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Messages;

class RenewSessionRequest
{
	[JsonProperty(PropertyName = "oldSessionToken")]
	internal string oldSessionToken { get; set; }

	[JsonProperty(PropertyName = "requestType")]
	internal string requestType { get; set; }
}
