/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Messages;

class QueryRequest
{
	[JsonProperty(PropertyName = "sqlText")]
	internal string? sqlText { get; set; }

	[JsonProperty(PropertyName = "describeOnly")]
	internal bool describeOnly { get; set; }

	[JsonProperty(PropertyName = "bindings")]
	internal Dictionary<string, BindingDTO>? parameterBindings { get; set; }
}
