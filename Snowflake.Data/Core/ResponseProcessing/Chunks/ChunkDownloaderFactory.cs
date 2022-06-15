/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Tortuga.Data.Snowflake.Core.Messages;

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing.Chunks;

static class ChunkDownloaderFactory
{
	public static IChunkDownloader GetDownloader(QueryExecResponseData responseData, SFBaseResultSet resultSet, CancellationToken cancellationToken)
	{
		var ChunkDownloaderVersion = resultSet.Configuration.ChunkDownloaderVersion;
		if (resultSet.Configuration.UseV2ChunkDownloader)
			ChunkDownloaderVersion = 2;

		switch (ChunkDownloaderVersion)
		{
			case 1:
				return new SFBlockingChunkDownloader(responseData.RowType!.Count,
					responseData.Chunks!,
					responseData.Qrmk!,
					responseData.ChunkHeaders!,
					cancellationToken,
					resultSet);

			case 2:

				if (resultSet.SFStatement == null)
					throw new ArgumentNullException(nameof(resultSet), $"resultSet.SFStatement is null");

				return new SFChunkDownloaderV2(responseData.RowType!.Count,
					responseData.Chunks!,
					responseData.Qrmk!,
					responseData.ChunkHeaders!,
					cancellationToken,
					resultSet.SFStatement.SFSession.RestRequester, resultSet.Configuration);

			default:
				return new SFBlockingChunkDownloaderV3(responseData.RowType!.Count,
					responseData.Chunks!,
					responseData.Qrmk!,
					responseData.ChunkHeaders!,
					cancellationToken,
					resultSet);
		}
	}
}
