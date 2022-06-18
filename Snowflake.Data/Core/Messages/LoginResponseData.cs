/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Messages;

class LoginResponseData
{
	[JsonProperty(PropertyName = "token", NullValueHandling = NullValueHandling.Ignore)]
	internal string? Token { get; set; }

	[JsonProperty(PropertyName = "masterToken", NullValueHandling = NullValueHandling.Ignore)]
	internal string? MasterToken { get; set; }

	[JsonProperty(PropertyName = "serverVersion", NullValueHandling = NullValueHandling.Ignore)]
	internal string? ServerVersion { get; set; }

	[JsonProperty(PropertyName = "parameters", NullValueHandling = NullValueHandling.Ignore)]
	internal List<NameValueParameter>? NameValueParameter { get; set; }

	[JsonProperty(PropertyName = "sessionInfo", NullValueHandling = NullValueHandling.Ignore)]
	internal SessionInfo? AuthResponseSessionInfo { get; set; }
}
