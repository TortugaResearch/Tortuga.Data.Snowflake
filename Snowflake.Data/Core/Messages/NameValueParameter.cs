/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Messages;

internal class NameValueParameter
{
    [JsonProperty(PropertyName = "name")]
    internal string? name { get; set; }

    [JsonProperty(PropertyName = "value")]
    internal string? value { get; set; }
}
