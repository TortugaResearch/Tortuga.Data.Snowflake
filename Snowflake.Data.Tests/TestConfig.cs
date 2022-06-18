/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Tortuga.Data.Snowflake.Tests;

/*
* This is the base class for all tests that call async metodes in the library - it does not use a special SynchronizationContext
*
*/

public class TestConfig
{
	[JsonProperty(PropertyName = "SNOWFLAKE_TEST_USER", NullValueHandling = NullValueHandling.Ignore)]
	internal string User { get; set; }

	[JsonProperty(PropertyName = "SNOWFLAKE_TEST_PASSWORD", NullValueHandling = NullValueHandling.Ignore)]
	internal string Password { get; set; }

	[JsonProperty(PropertyName = "SNOWFLAKE_TEST_ACCOUNT", NullValueHandling = NullValueHandling.Ignore)]
	internal string Account { get; set; }

	[JsonProperty(PropertyName = "SNOWFLAKE_TEST_HOST", NullValueHandling = NullValueHandling.Ignore)]
	internal string Host { get; set; }

	[JsonProperty(PropertyName = "SNOWFLAKE_TEST_PORT", NullValueHandling = NullValueHandling.Ignore)]
	internal string Port { get; set; }

	[JsonProperty(PropertyName = "SNOWFLAKE_TEST_WAREHOUSE", NullValueHandling = NullValueHandling.Ignore)]
	internal string Warehouse { get; set; }

	[JsonProperty(PropertyName = "SNOWFLAKE_TEST_DATABASE", NullValueHandling = NullValueHandling.Ignore)]
	internal string Database { get; set; }

	[JsonProperty(PropertyName = "SNOWFLAKE_TEST_SCHEMA", NullValueHandling = NullValueHandling.Ignore)]
	internal string Schema { get; set; }

	[JsonProperty(PropertyName = "SNOWFLAKE_TEST_ROLE", NullValueHandling = NullValueHandling.Ignore)]
	internal string Role { get; set; }

	[JsonProperty(PropertyName = "SNOWFLAKE_TEST_PROTOCOL", NullValueHandling = NullValueHandling.Ignore)]
	internal string Protocol { get; set; }

	[JsonProperty(PropertyName = "SNOWFLAKE_TEST_OKTA_USER", NullValueHandling = NullValueHandling.Ignore)]
	internal string OktaUser { get; set; }

	[JsonProperty(PropertyName = "SNOWFLAKE_TEST_OKTA_PASSWORD", NullValueHandling = NullValueHandling.Ignore)]
	internal string OktaPassword { get; set; }

	[JsonProperty(PropertyName = "SNOWFLAKE_TEST_OKTA_URL", NullValueHandling = NullValueHandling.Ignore)]
	internal string OktaURL { get; set; }

	[JsonProperty(PropertyName = "SNOWFLAKE_TEST_JWT_USER", NullValueHandling = NullValueHandling.Ignore)]
	internal string JwtAuthUser { get; set; }

	[JsonProperty(PropertyName = "SNOWFLAKE_TEST_PEM_FILE", NullValueHandling = NullValueHandling.Ignore)]
	internal string PemFilePath { get; set; }

	[JsonProperty(PropertyName = "SNOWFLAKE_TEST_P8_FILE", NullValueHandling = NullValueHandling.Ignore)]
	internal string P8FilePath { get; set; }

	[JsonProperty(PropertyName = "SNOWFLAKE_TEST_PWD_PROTECTED_PK_FILE", NullValueHandling = NullValueHandling.Ignore)]
	internal string PwdProtectedPrivateKeyFilePath { get; set; }

	[JsonProperty(PropertyName = "SNOWFLAKE_TEST_PK_CONTENT", NullValueHandling = NullValueHandling.Ignore)]
	internal string PrivateKey { get; set; }

	[JsonProperty(PropertyName = "SNOWFLAKE_TEST_PROTECTED_PK_CONTENT", NullValueHandling = NullValueHandling.Ignore)]
	internal string PwdProtectedPrivateKey { get; set; }

	[JsonProperty(PropertyName = "SNOWFLAKE_TEST_PK_PWD", NullValueHandling = NullValueHandling.Ignore)]
	internal string PrivateKeyFilePwd { get; set; }

	[JsonProperty(PropertyName = "SNOWFLAKE_TEST_OAUTH_TOKEN", NullValueHandling = NullValueHandling.Ignore)]
	internal string OAuthToken { get; set; }

	[JsonProperty(PropertyName = "SNOWFLAKE_TEST_EXP_OAUTH_TOKEN", NullValueHandling = NullValueHandling.Ignore)]
	internal string ExpOauthToken { get; set; }

	[JsonProperty(PropertyName = "PROXY_HOST", NullValueHandling = NullValueHandling.Ignore)]
	internal string ProxyHost { get; set; }

	[JsonProperty(PropertyName = "PROXY_PORT", NullValueHandling = NullValueHandling.Ignore)]
	internal string ProxyPort { get; set; }

	[JsonProperty(PropertyName = "AUTH_PROXY_HOST", NullValueHandling = NullValueHandling.Ignore)]
	internal string AuthProxyHost { get; set; }

	[JsonProperty(PropertyName = "AUTH_PROXY_PORT", NullValueHandling = NullValueHandling.Ignore)]
	internal string AuthProxyPort { get; set; }

	[JsonProperty(PropertyName = "AUTH_PROXY_USER", NullValueHandling = NullValueHandling.Ignore)]
	internal string AuthProxyUser { get; set; }

	[JsonProperty(PropertyName = "AUTH_PROXY_PWD", NullValueHandling = NullValueHandling.Ignore)]
	internal string AuthProxyPwd { get; set; }

	[JsonProperty(PropertyName = "NON_PROXY_HOSTS", NullValueHandling = NullValueHandling.Ignore)]
	internal string NonProxyHosts { get; set; }

	public TestConfig()
	{
		Protocol = "https";
		Port = "443";
	}
}
