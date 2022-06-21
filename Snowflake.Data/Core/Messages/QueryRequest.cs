/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Messages;

class QueryRequest
{
	[JsonProperty(PropertyName = "sqlText")]
	internal string? SqlText { get; set; }

	[JsonProperty(PropertyName = "describeOnly")]
	internal bool DescribeOnly { get; set; }

	[JsonProperty(PropertyName = "bindings")]
	internal Dictionary<string, BindingDTO>? ParameterBindings { get; set; }

	/// <summary>
	/// indicates whether query should be asynchronous
	/// </summary>
	[JsonProperty(PropertyName = "asyncExec")]
	internal bool asyncExec { get; set; }
}
