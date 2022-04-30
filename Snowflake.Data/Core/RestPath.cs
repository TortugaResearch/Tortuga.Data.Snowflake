namespace Tortuga.Data.Snowflake.Core;

internal static class RestPath
{
	internal const string SF_SESSION_PATH = "/session";

	internal const string SF_LOGIN_PATH = SF_SESSION_PATH + "/v1/login-request";

	internal const string SF_TOKEN_REQUEST_PATH = SF_SESSION_PATH + "/token-request";

	internal const string SF_AUTHENTICATOR_REQUEST_PATH = SF_SESSION_PATH + "/authenticator-request";

	internal const string SF_QUERY_PATH = "/queries/v1/query-request";
}
