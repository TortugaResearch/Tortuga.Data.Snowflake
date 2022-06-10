/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Messages;

internal class RenewSessionResponse : BaseRestResponse
{
    [JsonProperty(PropertyName = "data")]
    internal RenewSessionResponseData? data { get; set; }
}
