/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

#nullable enable

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Messages;

internal class LoginResponseData
{
	[JsonProperty(PropertyName = "token", NullValueHandling = NullValueHandling.Ignore)]
	internal string? token { get; set; }

	[JsonProperty(PropertyName = "masterToken", NullValueHandling = NullValueHandling.Ignore)]
	internal string? masterToken { get; set; }

	[JsonProperty(PropertyName = "serverVersion", NullValueHandling = NullValueHandling.Ignore)]
	internal string? serverVersion { get; set; }

	[JsonProperty(PropertyName = "parameters", NullValueHandling = NullValueHandling.Ignore)]
	internal List<NameValueParameter>? nameValueParameter { get; set; }

	[JsonProperty(PropertyName = "sessionInfo", NullValueHandling = NullValueHandling.Ignore)]
	internal SessionInfo? authResponseSessionInfo { get; set; }
}
