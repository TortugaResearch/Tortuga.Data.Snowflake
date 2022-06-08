/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

#nullable enable

namespace Tortuga.Data.Snowflake.Core.RequestProcessing;

/// <summary>
/// The RestRequester is responsible to send out a rest request and receive response
/// </summary>
internal interface IRestRequester
{
	Task<T> PostAsync<T>(RestRequest postRequest, CancellationToken cancellationToken);

	T Post<T>(RestRequest postRequest);

	Task<T> GetAsync<T>(RestRequest request, CancellationToken cancellationToken);

	T Get<T>(RestRequest request);

	Task<HttpResponseMessage> GetAsync(RestRequest request, CancellationToken cancellationToken);

	HttpResponseMessage Get(RestRequest request);
}
