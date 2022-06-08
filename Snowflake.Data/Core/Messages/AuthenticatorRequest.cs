/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

#nullable enable

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Messages;

class AuthenticatorRequest
{
	[JsonProperty(PropertyName = "data")]
	internal AuthenticatorRequestData? Data { get; set; }

	public override string ToString() => $"AuthenticatorRequest {{data: {Data} }}";
}
