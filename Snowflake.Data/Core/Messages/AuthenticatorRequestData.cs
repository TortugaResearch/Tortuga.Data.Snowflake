/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Messages;

class AuthenticatorRequestData
{
	[JsonProperty(PropertyName = "ACCOUNT_NAME", NullValueHandling = NullValueHandling.Ignore)]
	internal string AccountName { get; set; }

	[JsonProperty(PropertyName = "AUTHENTICATOR")]
	internal string Authenticator { get; set; }

	[JsonProperty(PropertyName = "BROWSER_MODE_REDIRECT_PORT", NullValueHandling = NullValueHandling.Ignore)]
	internal string BrowserModeRedirectPort { get; set; }

	public override string ToString()
	{
		return string.Format("AuthenticatorRequestData {{ACCOUNT_NANM: {0} }}",
			AccountName.ToString());
	}
}
