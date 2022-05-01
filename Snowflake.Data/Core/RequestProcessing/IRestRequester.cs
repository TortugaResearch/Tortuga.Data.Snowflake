/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Tortuga.Data.Snowflake.Core.Messages;

namespace Tortuga.Data.Snowflake.Core.RequestProcessing;

/// <summary>
/// The RestRequester is responsible to send out a rest request and receive response
/// </summary>
internal interface IRestRequester
{
	Task<T> PostAsync<T>(IRestRequest postRequest, CancellationToken cancellationToken);

	T Post<T>(IRestRequest postRequest);

	Task<T> GetAsync<T>(IRestRequest request, CancellationToken cancellationToken);

	T Get<T>(IRestRequest request);

	Task<HttpResponseMessage> GetAsync(IRestRequest request, CancellationToken cancellationToken);

	HttpResponseMessage Get(IRestRequest request);
}
