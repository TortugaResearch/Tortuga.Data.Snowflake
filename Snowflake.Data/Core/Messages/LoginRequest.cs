/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Messages;

class LoginRequest
{
	[JsonProperty(PropertyName = "data")]
	internal LoginRequestData data { get; set; }

	public override string ToString()
	{
		return string.Format("LoginRequest {{data: {0} }}", data.ToString());
	}
}
