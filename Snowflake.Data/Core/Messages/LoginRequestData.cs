/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

#nullable enable

using Newtonsoft.Json;
using Tortuga.Data.Snowflake.Core.Sessions;

namespace Tortuga.Data.Snowflake.Core.Messages;

class LoginRequestData
{
	[JsonProperty(PropertyName = "CLIENT_APP_ID")]
	internal string? clientAppId { get; set; }

	[JsonProperty(PropertyName = "CLIENT_APP_VERSION")]
	internal string? clientAppVersion { get; set; }

	[JsonProperty(PropertyName = "ACCOUNT_NAME", NullValueHandling = NullValueHandling.Ignore)]
	internal string? accountName { get; set; }

	[JsonProperty(PropertyName = "LOGIN_NAME", NullValueHandling = NullValueHandling.Ignore)]
	internal string? loginName { get; set; }

	[JsonProperty(PropertyName = "PASSWORD", NullValueHandling = NullValueHandling.Ignore)]
	internal string? password { get; set; }

	[JsonProperty(PropertyName = "AUTHENTICATOR", NullValueHandling = NullValueHandling.Ignore)]
	internal string? Authenticator { get; set; }

	[JsonProperty(PropertyName = "CLIENT_ENVIRONMENT")]
	internal LoginRequestClientEnv? clientEnv { get; set; }

	[JsonProperty(PropertyName = "RAW_SAML_RESPONSE", NullValueHandling = NullValueHandling.Ignore)]
	internal string? RawSamlResponse { get; set; }

	[JsonProperty(PropertyName = "TOKEN", NullValueHandling = NullValueHandling.Ignore)]
	internal string? Token { get; set; }

	[JsonProperty(PropertyName = "PROOF_KEY", NullValueHandling = NullValueHandling.Ignore)]
	internal string? ProofKey { get; set; }

	[JsonProperty(PropertyName = "SESSION_PARAMETERS", NullValueHandling = NullValueHandling.Ignore)]
	internal Dictionary<SFSessionParameter, object?>? SessionParameters { get; set; }

	public override string ToString() => $"LoginRequestData {{ClientAppVersion: {clientAppVersion},\n AccountName: {accountName},\n loginName: {loginName},\n ClientEnv: {clientEnv?.ToString()},\n authenticator: {Authenticator} }}";
}
