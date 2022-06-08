/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

#nullable enable

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Messages;

internal class QueryExecResponse : BaseRestResponse
{
	[JsonProperty(PropertyName = "data")]
	internal QueryExecResponseData? data { get; set; }
}
