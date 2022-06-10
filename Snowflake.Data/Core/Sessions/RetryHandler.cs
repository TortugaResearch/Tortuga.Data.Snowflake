/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

#nullable enable

using System.Net;
using Tortuga.Data.Snowflake.Core.RequestProcessing;

namespace Tortuga.Data.Snowflake.Core.Sessions;

class RetryHandler : DelegatingHandler
{
	internal RetryHandler(HttpMessageHandler innerHandler) : base(innerHandler)
	{
	}

#if NET5_0_OR_GREATER
	protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (request.RequestUri == null)
			throw new ArgumentNullException("request.RequestUri is null", nameof(request));

		HttpResponseMessage? response = null;
		var backOffInSec = 1;
		var totalRetryTime = 0;
		var maxDefaultBackoff = 16;

#pragma warning disable SYSLIB0014 // Type or member is obsolete. HttpClient alterntaive is not known.
		var p = ServicePointManager.FindServicePoint(request.RequestUri);
		p.Expect100Continue = false; // Saves about 100 ms per request
		p.UseNagleAlgorithm = false; // Saves about 200 ms per request
		p.ConnectionLimit = 20;      // Default value is 2, we need more connections for performing multiple parallel queries
#pragma warning restore SYSLIB0014 // Type or member is obsolete

		var httpTimeout = request.GetOptionOrDefault<TimeSpan>(RestRequest.HTTP_REQUEST_TIMEOUT_KEY);
		var restTimeout = request.GetOptionOrDefault<TimeSpan>(RestRequest.REST_REQUEST_TIMEOUT_KEY);

		CancellationTokenSource? childCts = null;

		var updater = new UriUpdater(request.RequestUri);

		while (true)
		{
			try
			{
				childCts = null;

				if (!httpTimeout.Equals(Timeout.InfiniteTimeSpan))
				{
					childCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
					childCts.CancelAfter(httpTimeout);
				}
				response = base.Send(request, childCts == null ? cancellationToken : childCts.Token);
			}
			catch
			{
				if (cancellationToken.IsCancellationRequested)
				{
					cancellationToken.ThrowIfCancellationRequested();
				}
				else if (childCts != null && childCts.Token.IsCancellationRequested)
				{
					totalRetryTime += (int)httpTimeout.TotalSeconds;
				}
				else
				{
					//TODO: Should probably check to see if the error is recoverable or transient.
				}
			}

			if (childCts != null)
				childCts.Dispose();

			if (response != null)
			{
				if (response.IsSuccessStatusCode)
				{
					return response;
				}
				else
				{
					bool isRetryable = IsRetryableHTTPCode((int)response.StatusCode);
					if (!isRetryable)
					{
						// No need to keep retrying, stop here
						return response;
					}
				}
			}

			// Disposing of the response if not null now that we don't need it anymore
			response?.Dispose();

			request.RequestUri = updater.Update();

			//We can't cancel a Thread.Sleep with a cancellationToken, but we can simulate it using small sleeps and checking the token.
			var totalBackoffUsed = 0;
			while (totalBackoffUsed < backOffInSec && !cancellationToken.IsCancellationRequested)
			{
				Thread.Sleep(TimeSpan.FromSeconds(1));
				totalBackoffUsed += 1;
			}

			totalRetryTime += backOffInSec;
			// Set next backoff time
			backOffInSec = backOffInSec >= maxDefaultBackoff ? maxDefaultBackoff : backOffInSec * 2;

			if (restTimeout.TotalSeconds > 0 && totalRetryTime + backOffInSec > restTimeout.TotalSeconds)
			{
				// No need to wait more than necessary if it can be avoided.
				// If the rest timeout will be reached before the next back-off,
				// use a smaller one to give the Rest request a chance to timeout early
				backOffInSec = Math.Max(1, (int)restTimeout.TotalSeconds - totalRetryTime - 1);
			}
		}
	}
#endif

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (request.RequestUri == null)
			throw new ArgumentNullException("request.RequestUri is null", nameof(request));

		HttpResponseMessage? response = null;
		var backOffInSec = 1;
		var totalRetryTime = 0;
		var maxDefaultBackoff = 16;

#pragma warning disable SYSLIB0014 // Type or member is obsolete. HttpClient alterntaive is not known.
		var p = ServicePointManager.FindServicePoint(request.RequestUri);
		p.Expect100Continue = false; // Saves about 100 ms per request
		p.UseNagleAlgorithm = false; // Saves about 200 ms per request
		p.ConnectionLimit = 20;      // Default value is 2, we need more connections for performing multiple parallel queries
#pragma warning restore SYSLIB0014 // Type or member is obsolete

		var httpTimeout = request.GetOptionOrDefault<TimeSpan>(RestRequest.HTTP_REQUEST_TIMEOUT_KEY);
		var restTimeout = request.GetOptionOrDefault<TimeSpan>(RestRequest.REST_REQUEST_TIMEOUT_KEY);

		CancellationTokenSource? childCts = null;

		var updater = new UriUpdater(request.RequestUri);

		while (true)
		{
			try
			{
				childCts = null;

				if (!httpTimeout.Equals(Timeout.InfiniteTimeSpan))
				{
					childCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
					childCts.CancelAfter(httpTimeout);
				}
				response = await base.SendAsync(request, childCts == null ? cancellationToken : childCts.Token).ConfigureAwait(false);
			}
			catch
			{
				if (cancellationToken.IsCancellationRequested)
				{
					cancellationToken.ThrowIfCancellationRequested();
				}
				else if (childCts != null && childCts.Token.IsCancellationRequested)
				{
					totalRetryTime += (int)httpTimeout.TotalSeconds;
				}
				else
				{
					//TODO: Should probably check to see if the error is recoverable or transient.
				}
			}

			if (childCts != null)
				childCts.Dispose();

			if (response != null)
			{
				if (response.IsSuccessStatusCode)
				{
					return response;
				}
				else
				{
					bool isRetryable = IsRetryableHTTPCode((int)response.StatusCode);
					if (!isRetryable)
					{
						// No need to keep retrying, stop here
						return response;
					}
				}
			}

			// Disposing of the response if not null now that we don't need it anymore
			response?.Dispose();

			request.RequestUri = updater.Update();

			await Task.Delay(TimeSpan.FromSeconds(backOffInSec), cancellationToken).ConfigureAwait(false);
			totalRetryTime += backOffInSec;
			// Set next backoff time
			backOffInSec = backOffInSec >= maxDefaultBackoff ? maxDefaultBackoff : backOffInSec * 2;

			if (restTimeout.TotalSeconds > 0 && totalRetryTime + backOffInSec > restTimeout.TotalSeconds)
			{
				// No need to wait more than necessary if it can be avoided.
				// If the rest timeout will be reached before the next back-off,
				// use a smaller one to give the Rest request a chance to timeout early
				backOffInSec = Math.Max(1, (int)restTimeout.TotalSeconds - totalRetryTime - 1);
			}
		}
	}

	/// <summary>
	/// Check whether or not the error is retryable or not.
	/// </summary>
	/// <param name="statusCode">The http status code.</param>
	/// <returns>True if the request should be retried, false otherwise.</returns>
	bool IsRetryableHTTPCode(int statusCode)
	{
		return 500 <= statusCode && statusCode < 600 ||
		// Forbidden
		statusCode == 403 ||
		// Request timeout
		statusCode == 408;
	}
}
