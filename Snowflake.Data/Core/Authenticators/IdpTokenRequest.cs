/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Authenticators;

class IdpTokenRequest
{
    [JsonProperty(PropertyName = "username")]
    internal string? Username { get; set; }

    [JsonProperty(PropertyName = "password")]
    internal string? Password { get; set; }
}
