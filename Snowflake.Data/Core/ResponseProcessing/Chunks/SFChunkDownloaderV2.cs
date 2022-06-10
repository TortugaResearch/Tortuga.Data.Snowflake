/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text;
using Tortuga.Data.Snowflake.Core.RequestProcessing;

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing.Chunks;

class SFChunkDownloaderV2 : IChunkDownloader
{
	readonly List<SFResultChunk> m_Chunks;

	readonly string m_Qrmk;

	// External cancellation token, used to stop donwload
	readonly CancellationToken m_ExternalCancellationToken;

	//TODO: parameterize prefetch slot
	const int prefetchSlot = 5;

	readonly IRestRequester m_RestRequester;
	readonly SnowflakeDbConfiguration m_Configuration;
	readonly Dictionary<string, string> m_ChunkHeaders;

	public SFChunkDownloaderV2(int colCount, List<ExecResponseChunk> chunkInfos, string qrmk, Dictionary<string, string> chunkHeaders, CancellationToken cancellationToken, IRestRequester restRequester, SnowflakeDbConfiguration configuration)
	{
		m_Qrmk = qrmk;
		m_ChunkHeaders = chunkHeaders;
		m_Chunks = new List<SFResultChunk>();
		m_RestRequester = restRequester;
		m_Configuration = configuration;
		m_ExternalCancellationToken = cancellationToken;

		var idx = 0;
		foreach (ExecResponseChunk chunkInfo in chunkInfos)
			m_Chunks.Add(new SFResultChunk(chunkInfo.url!, chunkInfo.rowCount, colCount, idx++));

		FillDownloads();
	}

	BlockingCollection<Lazy<Task<IResultChunk>>>? m_DownloadTasks;
	ConcurrentQueue<Lazy<Task<IResultChunk>>>? m_DownloadQueue;

	void RunDownloads()
	{
		try
		{
			while (m_DownloadQueue!.TryDequeue(out var task) && !m_ExternalCancellationToken.IsCancellationRequested)
			{
				if (!task.IsValueCreated)
					task.Value.Wait(m_ExternalCancellationToken);
			}
		}
		catch
		{
			//Don't blow from background threads.
		}
	}

	void FillDownloads()
	{
		m_DownloadTasks = new BlockingCollection<Lazy<Task<IResultChunk>>>();

		foreach (var c in m_Chunks)
		{
			var t = new Lazy<Task<IResultChunk>>(() => DownloadChunkAsync(new DownloadContextV2()
			{
				chunk = c,
				chunkIndex = c.ChunkIndex,
				qrmk = m_Qrmk,
				chunkHeaders = m_ChunkHeaders,
				cancellationToken = m_ExternalCancellationToken,
			}));

			m_DownloadTasks.Add(t);
		}

		m_DownloadTasks.CompleteAdding();

		m_DownloadQueue = new ConcurrentQueue<Lazy<Task<IResultChunk>>>(m_DownloadTasks);

		for (var i = 0; i < prefetchSlot && i < m_Chunks.Count; i++)
			Task.Run(new Action(RunDownloads));
	}

	public Task<IResultChunk?> GetNextChunkAsync()
	{
		if (m_DownloadTasks == null)
			throw new InvalidOperationException($"{nameof(m_DownloadTasks)} is null");

		if (m_DownloadTasks.IsAddingCompleted)
			return Task.FromResult<IResultChunk?>(null);
		else
			return (Task<IResultChunk?>)(object)m_DownloadTasks.Take().Value;
	}

	async Task<IResultChunk> DownloadChunkAsync(DownloadContextV2 downloadContext)
	{
		if (downloadContext.chunk == null)
			throw new ArgumentException("downloadContext.chunk is null", nameof(downloadContext));

		var chunk = downloadContext.chunk;

		chunk.DownloadState = DownloadState.IN_PROGRESS;

		var downloadRequest = new S3DownloadRequest()
		{
			Url = new UriBuilder(chunk.Url!).Uri,
			Qrmk = downloadContext.qrmk,
			// s3 download request timeout to one hour
			RestTimeout = TimeSpan.FromHours(1),
			HttpTimeout = TimeSpan.FromSeconds(16),
			ChunkHeaders = downloadContext.chunkHeaders
		};

		Stream stream;
		using (var httpResponse = await m_RestRequester.GetAsync(downloadRequest, downloadContext.cancellationToken).ConfigureAwait(false))
		using (stream = await httpResponse.Content.ReadAsStreamAsync().ConfigureAwait(false))
		{
			if (httpResponse.Content.Headers.TryGetValues("Content-Encoding", out var encoding))
			{
				if (string.Equals(encoding.First(), "gzip", StringComparison.OrdinalIgnoreCase))
				{
					stream = new GZipStream(stream, CompressionMode.Decompress);
				}
			}

			parseStreamIntoChunk(stream, chunk);
		}

		chunk.DownloadState = DownloadState.SUCCESS;

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
	void parseStreamIntoChunk(Stream content, SFResultChunk resultChunk)
	{
		var openBracket = new MemoryStream(Encoding.UTF8.GetBytes("["));
		var closeBracket = new MemoryStream(Encoding.UTF8.GetBytes("]"));

		var concatStream = new ConcatenatedStream(new Stream[3] { openBracket, content, closeBracket });

		var parser = ChunkParserFactory.GetParser(m_Configuration, concatStream);
		parser.ParseChunk(resultChunk);
	}
}
