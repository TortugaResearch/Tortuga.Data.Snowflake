/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core.RequestProcessing;

internal interface IMockRestRequester : IRestRequester
{
	void setHttpClient(HttpClient httpClient);
}
