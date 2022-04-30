/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core;

internal class AuthenticatorResponse : BaseRestResponse
{
	[JsonProperty(PropertyName = "data")]
	internal AuthenticatorResponseData data { get; set; }
}
