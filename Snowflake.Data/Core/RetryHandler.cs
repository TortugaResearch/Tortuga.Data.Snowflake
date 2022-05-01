/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using System.Net;
using static Tortuga.Data.Snowflake.Core.HttpUtil;

namespace Tortuga.Data.Snowflake.Core;

class RetryHandler : DelegatingHandler
{
	internal RetryHandler(HttpMessageHandler innerHandler) : base(innerHandler)
	{
	}

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage requestMessage,
		CancellationToken cancellationToken)
	{
		HttpResponseMessage response = null;
		int backOffInSec = 1;
		int totalRetryTime = 0;
		int maxDefaultBackoff = 16;

#pragma warning disable SYSLIB0014 // Type or member is obsolete. HttpClient alterntaive is not known.
		ServicePoint p = ServicePointManager.FindServicePoint(requestMessage.RequestUri);
		p.Expect100Continue = false; // Saves about 100 ms per request
		p.UseNagleAlgorithm = false; // Saves about 200 ms per request
		p.ConnectionLimit = 20;      // Default value is 2, we need more connections for performing multiple parallel queries
#pragma warning restore SYSLIB0014 // Type or member is obsolete

		var httpTimeout = requestMessage.GetOptionOrDefault<TimeSpan>(BaseRestRequest.HTTP_REQUEST_TIMEOUT_KEY);
		var restTimeout = requestMessage.GetOptionOrDefault<TimeSpan>(BaseRestRequest.REST_REQUEST_TIMEOUT_KEY);

		CancellationTokenSource childCts = null;

		UriUpdater updater = new UriUpdater(requestMessage.RequestUri);

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
				response = await base.SendAsync(requestMessage, childCts == null ?
					cancellationToken : childCts.Token).ConfigureAwait(false);
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
			{
				childCts.Dispose();
			}

			if (response != null)
			{
				if (response.IsSuccessStatusCode)
				{
					return response;
				}
				else
				{
					bool isRetryable = isRetryableHTTPCode((int)response.StatusCode);
					if (!isRetryable)
					{
						// No need to keep retrying, stop here
						return response;
					}
				}
			}

			// Disposing of the response if not null now that we don't need it anymore
			response?.Dispose();

			requestMessage.RequestUri = updater.Update();

			await Task.Delay(TimeSpan.FromSeconds(backOffInSec), cancellationToken).ConfigureAwait(false);
			totalRetryTime += backOffInSec;
			// Set next backoff time
			backOffInSec = backOffInSec >= maxDefaultBackoff ?
					maxDefaultBackoff : backOffInSec * 2;

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
	private bool isRetryableHTTPCode(int statusCode)
	{
		return 500 <= statusCode && statusCode < 600 ||
		// Forbidden
		statusCode == 403 ||
		// Request timeout
		statusCode == 408;
	}
}
