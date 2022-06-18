/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.FileTransfer.StorageClient;

class RemoteLocation
{
	[JsonProperty(PropertyName = "bucket")]
	public string? Bucket { get; set; }

	[JsonProperty(PropertyName = "key")]
	public string? Key { get; set; }
}
