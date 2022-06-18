/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Authenticators;

class IdpTokenResponse
{
	[JsonProperty(PropertyName = "cookieToken")]
	internal string? CookieToken { get; set; }
}
