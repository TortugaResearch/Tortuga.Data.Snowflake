/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core;

internal class PutGetStageInfo
{
    [JsonProperty(PropertyName = "locationType", NullValueHandling = NullValueHandling.Ignore)]
    internal string? locationType { get; set; }

    [JsonProperty(PropertyName = "location", NullValueHandling = NullValueHandling.Ignore)]
    internal string? location { get; set; }

    [JsonProperty(PropertyName = "path", NullValueHandling = NullValueHandling.Ignore)]
    internal string? path { get; set; }

    [JsonProperty(PropertyName = "region", NullValueHandling = NullValueHandling.Ignore)]
    internal string? region { get; set; }

    [JsonProperty(PropertyName = "storageAccount", NullValueHandling = NullValueHandling.Ignore)]
    internal string? storageAccount { get; set; }

    [JsonProperty(PropertyName = "isClientSideEncrypted", NullValueHandling = NullValueHandling.Ignore)]
    internal bool isClientSideEncrypted { get; set; }

    [JsonProperty(PropertyName = "creds", NullValueHandling = NullValueHandling.Ignore)]
    internal Dictionary<string, string>? stageCredentials { get; set; }

    [JsonProperty(PropertyName = "presignedUrl", NullValueHandling = NullValueHandling.Ignore)]
    internal string? presignedUrl { get; set; }

    [JsonProperty(PropertyName = "endPoint", NullValueHandling = NullValueHandling.Ignore)]
    internal string? endPoint { get; set; }
}
