/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Messages;

class LoginRequestClientEnv
{
	[JsonProperty(PropertyName = "APPLICATION")]
	internal string? Application { get; set; }

	[JsonProperty(PropertyName = "OS_VERSION")]
	internal string? OSVersion { get; set; }

	[JsonProperty(PropertyName = "NET_RUNTIME")]
	internal string? NetRuntime { get; set; }

	[JsonProperty(PropertyName = "NET_VERSION")]
	internal string? NetVersion { get; set; }

	[JsonProperty(PropertyName = "INSECURE_MODE")]
	internal string? InsecureMode { get; set; }

	public override string ToString() => $"{{ APPLICATION: {Application}, OS_VERSION: {OSVersion}, NET_RUNTIME: {NetRuntime}, NET_VERSION: {NetVersion}, INSECURE_MODE: {InsecureMode} }}";
}
