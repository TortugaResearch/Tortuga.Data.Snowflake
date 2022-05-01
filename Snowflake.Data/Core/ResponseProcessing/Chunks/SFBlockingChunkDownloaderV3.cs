/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Tortuga.Data.Snowflake.Core.RequestProcessing;
using Tortuga.Data.Snowflake.Core.Sessions;

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing.Chunks;

class SFBlockingChunkDownloaderV3 : IChunkDownloader
{
	private List<SFReusableChunk> chunkDatas = new List<SFReusableChunk>();

	private string qrmk;

	private int nextChunkToDownloadIndex;

	private int nextChunkToConsumeIndex;

	// External cancellation token, used to stop donwload
	private CancellationToken externalCancellationToken;

	private readonly int prefetchSlot;

	private readonly IRestRequester _RestRequester;

	private Dictionary<string, string> chunkHeaders;

	private readonly SFBaseResultSet ResultSet;

	private readonly List<ExecResponseChunk> chunkInfos;

	private readonly List<Task<IResultChunk>> taskQueues;

	public SFBlockingChunkDownloaderV3(int colCount,
		List<ExecResponseChunk> chunkInfos, string qrmk,
		Dictionary<string, string> chunkHeaders,
		CancellationToken cancellationToken,
		SFBaseResultSet ResultSet)
	{
		this.qrmk = qrmk;
		this.chunkHeaders = chunkHeaders;
		this.nextChunkToDownloadIndex = 0;
		this.ResultSet = ResultSet;
		this._RestRequester = ResultSet.sfStatement.SfSession.restRequester;
		this.prefetchSlot = Math.Min(chunkInfos.Count, GetPrefetchThreads(ResultSet));
		this.chunkInfos = chunkInfos;
		this.nextChunkToConsumeIndex = 0;
		this.taskQueues = new List<Task<IResultChunk>>();
		externalCancellationToken = cancellationToken;

		for (int i = 0; i < prefetchSlot; i++)
		{
			SFReusableChunk reusableChunk = new SFReusableChunk(colCount);
			reusableChunk.Reset(chunkInfos[nextChunkToDownloadIndex], nextChunkToDownloadIndex);
			chunkDatas.Add(reusableChunk);

			taskQueues.Add(DownloadChunkAsync(new DownloadContextV3()
			{
				chunk = reusableChunk,
				qrmk = this.qrmk,
				chunkHeaders = this.chunkHeaders,
				cancellationToken = this.externalCancellationToken
			}));

			nextChunkToDownloadIndex++;
		}
	}

	private int GetPrefetchThreads(SFBaseResultSet resultSet)
	{
		Dictionary<SFSessionParameter, object> sessionParameters = resultSet.sfStatement.SfSession.ParameterMap;
		String val = (String)sessionParameters[SFSessionParameter.CLIENT_PREFETCH_THREADS];
		return Int32.Parse(val);
	}

	/*public Task<IResultChunk> GetNextChunkAsync()
	{
		return _downloadTasks.IsCompleted ? Task.FromResult<SFResultChunk>(null) : _downloadTasks.Take();
	}*/

	public async Task<IResultChunk> GetNextChunkAsync()
	{
		if (nextChunkToConsumeIndex < chunkInfos.Count)
		{
			Task<IResultChunk> chunk = taskQueues[nextChunkToConsumeIndex % prefetchSlot];

			if (nextChunkToDownloadIndex < chunkInfos.Count && nextChunkToConsumeIndex > 0)
			{
				SFReusableChunk reusableChunk = chunkDatas[nextChunkToDownloadIndex % prefetchSlot];
				reusableChunk.Reset(chunkInfos[nextChunkToDownloadIndex], nextChunkToDownloadIndex);

				taskQueues[nextChunkToDownloadIndex % prefetchSlot] = DownloadChunkAsync(new DownloadContextV3()
				{
					chunk = reusableChunk,
					qrmk = this.qrmk,
					chunkHeaders = this.chunkHeaders,
					cancellationToken = externalCancellationToken
				});
				nextChunkToDownloadIndex++;
			}

			nextChunkToConsumeIndex++;
			return await chunk;
		}
		else
		{
			return await Task.FromResult<IResultChunk>(null);
		}
	}

	private async Task<IResultChunk> DownloadChunkAsync(DownloadContextV3 downloadContext)
	{
		//logger.Info($"Start donwloading chunk #{downloadContext.chunkIndex}");
		SFReusableChunk chunk = downloadContext.chunk;

		S3DownloadRequest downloadRequest =
			new S3DownloadRequest()
			{
				Url = new UriBuilder(chunk.Url).Uri,
				qrmk = downloadContext.qrmk,
				// s3 download request timeout to one hour
				RestTimeout = TimeSpan.FromHours(1),
				HttpTimeout = Timeout.InfiniteTimeSpan, // Disable timeout for each request
				chunkHeaders = downloadContext.chunkHeaders
			};

		using (var httpResponse = await _RestRequester.GetAsync(downloadRequest, downloadContext.cancellationToken)
					   .ConfigureAwait(continueOnCapturedContext: false))
		using (Stream stream = await httpResponse.Content.ReadAsStreamAsync()
			.ConfigureAwait(continueOnCapturedContext: false))
		{
			await ParseStreamIntoChunk(stream, chunk);
		}
		return chunk;
	}

	/// <summary>
	///     Content from s3 in format of
	///     ["val1", "val2", null, ...],
	///     ["val3", "val4", null, ...],
	///     ...
	///     To parse it as a json, we need to preappend '[' and append ']' to the stream
	/// </summary>
	/// <param name="content"></param>
	/// <param name="resultChunk"></param>
	private async Task ParseStreamIntoChunk(Stream content, IResultChunk resultChunk)
	{
		IChunkParser parser = new ReusableChunkParser(content);
		await parser.ParseChunk(resultChunk);
	}
}
