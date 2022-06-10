/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Messages;

class AuthenticatorResponseData
{
	[JsonProperty(PropertyName = "tokenUrl", NullValueHandling = NullValueHandling.Ignore)]
	internal string? tokenUrl { get; set; }

	[JsonProperty(PropertyName = "ssoUrl", NullValueHandling = NullValueHandling.Ignore)]
	internal string? ssoUrl { get; set; }

	[JsonProperty(PropertyName = "proofKey", NullValueHandling = NullValueHandling.Ignore)]
	internal string? proofKey { get; set; }
}
