/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;
using Tortuga.HttpClientUtilities;

namespace Tortuga.Data.Snowflake.Core.RequestProcessing;

class RestRequester : IRestRequester
{
	protected HttpClient _HttpClient;

	public RestRequester(HttpClient httpClient)
	{
		_HttpClient = httpClient;
	}

	public T Post<T>(RestRequest request)
	{
		using (var response = Send(HttpMethod.Post, request, default))
		{
			var json = response.Content.ReadAsString();
			return JsonConvert.DeserializeObject<T>(json, JsonUtils.JsonSettings)!;
		}
	}

	public async Task<T> PostAsync<T>(RestRequest request, CancellationToken cancellationToken)
	{
		using (var response = await SendAsync(HttpMethod.Post, request, cancellationToken).ConfigureAwait(false))
		{
			var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			return JsonConvert.DeserializeObject<T>(json, JsonUtils.JsonSettings)!;
		}
	}

	public T Get<T>(RestRequest request)
	{
		//Run synchronous in a new thread-pool task.
		return Task.Run(async () => await (GetAsync<T>(request, CancellationToken.None)).ConfigureAwait(false)).Result;
	}

	public async Task<T> GetAsync<T>(RestRequest request, CancellationToken cancellationToken)
	{
		using (var response = await GetAsync(request, cancellationToken).ConfigureAwait(false))
		{
			var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			return JsonConvert.DeserializeObject<T>(json, JsonUtils.JsonSettings)!;
		}
	}

	public Task<HttpResponseMessage> GetAsync(RestRequest request, CancellationToken cancellationToken)
	{
		return SendAsync(HttpMethod.Get, request, cancellationToken);
	}

	public HttpResponseMessage Get(RestRequest request)
	{
		return Send(HttpMethod.Get, request, default);
	}

	private HttpResponseMessage Send(HttpMethod method, RestRequest request, CancellationToken externalCancellationToken)
	{
		return Send(request.ToRequestMessage(method), request.RestTimeout, externalCancellationToken);
	}

	private async Task<HttpResponseMessage> SendAsync(HttpMethod method, RestRequest request, CancellationToken externalCancellationToken)
	{
		return await SendAsync(request.ToRequestMessage(method), request.RestTimeout, externalCancellationToken).ConfigureAwait(false);
	}

	protected virtual HttpResponseMessage Send(HttpRequestMessage message, TimeSpan restTimeout, CancellationToken externalCancellationToken)
	{
		// merge multiple cancellation token
		using (var restRequestTimeout = new CancellationTokenSource(restTimeout))
		{
			using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken, restRequestTimeout.Token))
			{
				HttpResponseMessage? response = null;
				try
				{
					response = _HttpClient.Send(message, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);
					response.EnsureSuccessStatusCode();

					return response;
				}
				catch (Exception)
				{
					// Disposing of the response if not null now that we don't need it anymore
					response?.Dispose();
					if (restRequestTimeout.IsCancellationRequested)
						throw new SnowflakeDbException(SnowflakeError.RequestTimeout);
					else
						throw;
				}
			}
		}
	}

	protected virtual async Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, TimeSpan restTimeout, CancellationToken externalCancellationToken)
	{
		// merge multiple cancellation token
		using (var restRequestTimeout = new CancellationTokenSource(restTimeout))
		{
			using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken, restRequestTimeout.Token))
			{
				HttpResponseMessage? response = null;
				try
				{
					response = await _HttpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token).ConfigureAwait(false);
					response.EnsureSuccessStatusCode();

					return response;
				}
				catch (Exception)
				{
					// Disposing of the response if not null now that we don't need it anymore
					response?.Dispose();
					if (restRequestTimeout.IsCancellationRequested)
						throw new SnowflakeDbException(SnowflakeError.RequestTimeout);
					else
						throw;
				}
			}
		}
	}
}
