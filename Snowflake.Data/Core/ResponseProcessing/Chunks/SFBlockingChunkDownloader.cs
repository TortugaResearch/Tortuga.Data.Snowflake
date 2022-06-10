/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text;
using Tortuga.Data.Snowflake.Core.RequestProcessing;
using Tortuga.Data.Snowflake.Core.Sessions;

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing.Chunks;

/// <summary>
///     Downloader implementation that will be blocked if main thread consume falls behind
/// </summary>
class SFBlockingChunkDownloader : IChunkDownloader
{
    readonly List<SFResultChunk> m_Chunks;
    readonly string m_Qrmk;
    readonly int m_NextChunkToDownloadIndex;

    // External cancellation token, used to stop donwload
    readonly CancellationToken m_ExternalCancellationToken;

    readonly int m_PrefetchThreads;

    readonly IRestRequester m_RestRequester;

    readonly Dictionary<string, string> m_ChunkHeaders;

    readonly SFBaseResultSet m_ResultSet;

    public SFBlockingChunkDownloader(int colCount, List<ExecResponseChunk> chunkInfos, string qrmk, Dictionary<string, string> chunkHeaders, CancellationToken cancellationToken, SFBaseResultSet resultSet)
    {
        if (resultSet.SFStatement == null)
            throw new ArgumentException($"resultSet.SFStatement is null", nameof(resultSet));

        m_Qrmk = qrmk;
        m_ChunkHeaders = chunkHeaders;
        m_Chunks = new List<SFResultChunk>();
        m_NextChunkToDownloadIndex = 0;
        m_ResultSet = resultSet;
        m_RestRequester = resultSet.SFStatement.SFSession.m_RestRequester;
        m_PrefetchThreads = GetPrefetchThreads(resultSet);
        m_ExternalCancellationToken = cancellationToken;

        var idx = 0;
        foreach (var chunkInfo in chunkInfos)
            m_Chunks.Add(new SFResultChunk(chunkInfo.url!, chunkInfo.rowCount, colCount, idx++));

        FillDownloads();
    }

    int GetPrefetchThreads(SFBaseResultSet resultSet)
    {
        if (resultSet.SFStatement == null)
            throw new ArgumentException($"resultSet.SFStatement is null", nameof(resultSet));

        var sessionParameters = resultSet.SFStatement.SFSession.ParameterMap;
        var val = (string)sessionParameters[SFSessionParameter.CLIENT_PREFETCH_THREADS]!;
        return int.Parse(val);
    }

    BlockingCollection<Task<IResultChunk>>? m_DownloadTasks;

    void FillDownloads()
    {
        m_DownloadTasks = new BlockingCollection<Task<IResultChunk>>(m_PrefetchThreads);

        Task.Run(() =>
        {
            foreach (var c in m_Chunks)
            {
                m_DownloadTasks.Add(DownloadChunkAsync(new DownloadContext()
                {
                    chunk = c,
                    chunkIndex = m_NextChunkToDownloadIndex,
                    qrmk = m_Qrmk,
                    chunkHeaders = m_ChunkHeaders,
                    cancellationToken = m_ExternalCancellationToken
                }));
            }

            m_DownloadTasks.CompleteAdding();
        });
    }

    public Task<IResultChunk?> GetNextChunkAsync()
    {
        if (m_DownloadTasks == null)
            throw new InvalidOperationException($"{nameof(m_DownloadTasks)} is null");

        if (m_DownloadTasks.IsCompleted)
            return Task.FromResult<IResultChunk?>(null);
        else
            return (Task<IResultChunk?>)(object)m_DownloadTasks.Take();
    }

    async Task<IResultChunk> DownloadChunkAsync(DownloadContext downloadContext)
    {
        if (downloadContext.chunk == null)
            throw new ArgumentException("downloadContext.chunk is null", nameof(downloadContext));

        var chunk = downloadContext.chunk;

        chunk.DownloadState = DownloadState.IN_PROGRESS;

        S3DownloadRequest downloadRequest = new S3DownloadRequest()
        {
            Url = new UriBuilder(chunk.Url!).Uri,
            Qrmk = downloadContext.qrmk,
            // s3 download request timeout to one hour
            RestTimeout = TimeSpan.FromHours(1),
            HttpTimeout = TimeSpan.FromSeconds(32),
            ChunkHeaders = downloadContext.chunkHeaders
        };

        var httpResponse = await m_RestRequester.GetAsync(downloadRequest, downloadContext.cancellationToken).ConfigureAwait(false);
        var stream = await httpResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);

        //TODO this shouldn't be required.
        if (httpResponse.Content.Headers.TryGetValues("Content-Encoding", out var encoding))
        {
            if (string.Compare(encoding.First(), "gzip", true) == 0)
            {
                stream = new GZipStream(stream, CompressionMode.Decompress);
            }
        }

        ParseStreamIntoChunk(stream, chunk);

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
    void ParseStreamIntoChunk(Stream content, SFResultChunk resultChunk)
    {
        var openBracket = new MemoryStream(Encoding.UTF8.GetBytes("["));
        var closeBracket = new MemoryStream(Encoding.UTF8.GetBytes("]"));

        var concatStream = new ConcatenatedStream(new Stream[3] { openBracket, content, closeBracket });

        var parser = ChunkParserFactory.GetParser(m_ResultSet.Configuration, concatStream);
        parser.ParseChunk(resultChunk);
    }
}
