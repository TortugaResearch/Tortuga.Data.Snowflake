/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Messages;

class RenewSessionResponseData
{
	[JsonProperty(PropertyName = "sessionToken", NullValueHandling = NullValueHandling.Ignore)]
	internal string? SessionToken { get; set; }

	[JsonProperty(PropertyName = "validityInSecondsST", NullValueHandling = NullValueHandling.Ignore)]
	internal short SessionTokenValidityInSeconds { get; set; }

	[JsonProperty(PropertyName = "masterToken", NullValueHandling = NullValueHandling.Ignore)]
	internal string? MasterToken { get; set; }

	[JsonProperty(PropertyName = "validityInSecondsMT", NullValueHandling = NullValueHandling.Ignore)]
	internal short MasterTokenValidityInSeconds { get; set; }

	[JsonProperty(PropertyName = "sessionId", NullValueHandling = NullValueHandling.Ignore)]
	internal long SessionId { get; set; }
}
