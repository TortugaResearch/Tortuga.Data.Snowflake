/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core;

class PutGetStageInfo
{
	[JsonProperty(PropertyName = "locationType", NullValueHandling = NullValueHandling.Ignore)]
	internal string? LocationType { get; set; }

	[JsonProperty(PropertyName = "location", NullValueHandling = NullValueHandling.Ignore)]
	internal string? Location { get; set; }

	[JsonProperty(PropertyName = "path", NullValueHandling = NullValueHandling.Ignore)]
	internal string? Path { get; set; }

	[JsonProperty(PropertyName = "region", NullValueHandling = NullValueHandling.Ignore)]
	internal string? Region { get; set; }

	[JsonProperty(PropertyName = "storageAccount", NullValueHandling = NullValueHandling.Ignore)]
	internal string? StorageAccount { get; set; }

	[JsonProperty(PropertyName = "isClientSideEncrypted", NullValueHandling = NullValueHandling.Ignore)]
	internal bool IsClientSideEncrypted { get; set; }

	[JsonProperty(PropertyName = "creds", NullValueHandling = NullValueHandling.Ignore)]
	internal Dictionary<string, string>? StageCredentials { get; set; }

	[JsonProperty(PropertyName = "presignedUrl", NullValueHandling = NullValueHandling.Ignore)]
	internal string? PresignedUrl { get; set; }

	[JsonProperty(PropertyName = "endPoint", NullValueHandling = NullValueHandling.Ignore)]
	internal string? EndPoint { get; set; }
}
