/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

#nullable enable

using Tortuga.Data.Snowflake.Core.RequestProcessing;

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing;

internal class S3DownloadRequest : RestRequest
{
	const string SSE_C_ALGORITHM = "x-amz-server-side-encryption-customer-algorithm";

	const string SSE_C_KEY = "x-amz-server-side-encryption-customer-key";

	const string SSE_C_AES = "AES256";

	internal string? Qrmk { get; set; }

	internal Dictionary<string, string>? ChunkHeaders { get; set; }

	internal override HttpRequestMessage ToRequestMessage(HttpMethod method)
	{
		if (Url == null)
			throw new InvalidOperationException($"{nameof(Url)} is null");

		HttpRequestMessage message = newMessage(method, Url);
		if (ChunkHeaders != null)
		{
			foreach (var item in ChunkHeaders)
			{
				message.Headers.Add(item.Key, item.Value);
			}
		}
		else
		{
			message.Headers.Add(SSE_C_ALGORITHM, SSE_C_AES);
			message.Headers.Add(SSE_C_KEY, Qrmk);
		}

		return message;
	}
}
