/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Messages;

class LoginRequest
{
	[JsonProperty(PropertyName = "data")]
	internal LoginRequestData? Data { get; set; }

	public override string ToString() => $"LoginRequest {{data: {Data} }}";
}
