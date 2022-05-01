/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Messages;

class LoginRequestClientEnv
{
	[JsonProperty(PropertyName = "APPLICATION")]
	internal String application { get; set; }

	[JsonProperty(PropertyName = "OS_VERSION")]
	internal String osVersion { get; set; }

	[JsonProperty(PropertyName = "NET_RUNTIME")]
	internal String netRuntime { get; set; }

	[JsonProperty(PropertyName = "NET_VERSION")]
	internal string netVersion { get; set; }

	[JsonProperty(PropertyName = "INSECURE_MODE")]
	internal string insecureMode { get; set; }

	public override string ToString()
	{
		return String.Format("{{ APPLICATION: {0}, OS_VERSION: {1}, NET_RUNTIME: {2}, NET_VERSION: {3}, INSECURE_MODE: {4} }}",
			application, osVersion, netRuntime, netVersion, insecureMode);
	}
}
