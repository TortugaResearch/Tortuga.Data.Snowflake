/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Messages;

internal class PutGetEncryptionMaterial
{
    [JsonProperty(PropertyName = "queryStageMasterKey", NullValueHandling = NullValueHandling.Ignore)]
    internal string? queryStageMasterKey { get; set; }

    [JsonProperty(PropertyName = "queryId", NullValueHandling = NullValueHandling.Ignore)]
    internal string? queryId { get; set; }

    [JsonProperty(PropertyName = "smkId", NullValueHandling = NullValueHandling.Ignore)]
    internal long smkId { get; set; }
}
