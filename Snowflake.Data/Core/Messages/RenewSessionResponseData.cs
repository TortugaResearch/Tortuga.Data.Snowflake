/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

#nullable enable

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Messages;

internal class RenewSessionResponseData
{
	[JsonProperty(PropertyName = "sessionToken", NullValueHandling = NullValueHandling.Ignore)]
	internal string? sessionToken { get; set; }

	[JsonProperty(PropertyName = "validityInSecondsST", NullValueHandling = NullValueHandling.Ignore)]
	internal short sessionTokenValidityInSeconds { get; set; }

	[JsonProperty(PropertyName = "masterToken", NullValueHandling = NullValueHandling.Ignore)]
	internal string? masterToken { get; set; }

	[JsonProperty(PropertyName = "validityInSecondsMT", NullValueHandling = NullValueHandling.Ignore)]
	internal short masterTokenValidityInSeconds { get; set; }

	[JsonProperty(PropertyName = "sessionId", NullValueHandling = NullValueHandling.Ignore)]
	internal long sessionId { get; set; }
}
