/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Messages;

internal class BaseQueryExecResponse<T> : BaseRestResponse
where T : IQueryExecResponseData
{
	[JsonProperty(PropertyName = "data")]
	internal T data { get; set; }
}
