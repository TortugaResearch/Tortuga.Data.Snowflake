/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core;

internal interface IMockRestRequester : IRestRequester
{
	void setHttpClient(HttpClient httpClient);
}
