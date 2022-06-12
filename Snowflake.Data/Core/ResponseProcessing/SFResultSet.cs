/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Tortuga.Data.Snowflake.Core.Messages;
using Tortuga.Data.Snowflake.Core.RequestProcessing;
using Tortuga.Data.Snowflake.Core.ResponseProcessing.Chunks;

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing;

class SFResultSet : SFBaseResultSet
{
	int m_CurrentChunkRowIdx;

	int m_CurrentChunkRowCount;

	readonly IChunkDownloader? m_ChunkDownloader;

	IResultChunk m_CurrentChunk;

	public SFResultSet(QueryExecResponseData responseData, SFStatement sfStatement, CancellationToken cancellationToken) : base(sfStatement.SFSession.Configuration)
	{
		if (responseData.RowType == null)
			throw new ArgumentException($"responseData.rowType is null", nameof(responseData));
		if (responseData.RowSet == null)
			throw new ArgumentException($"responseData.rowSet is null", nameof(responseData));

		m_ColumnCount = responseData.RowType.Count;
		m_CurrentChunkRowIdx = -1;
		m_CurrentChunkRowCount = responseData.RowSet.GetLength(0);

		SFStatement = sfStatement;
		updateSessionStatus(responseData);

		if (responseData.Chunks != null)
		{
			// counting the first chunk
			//_totalChunkCount = responseData.chunks.Count;
			m_ChunkDownloader = ChunkDownloaderFactory.GetDownloader(responseData, this, cancellationToken);
		}

		m_CurrentChunk = new SFResultChunk(responseData.RowSet);
		responseData.RowSet = null;

		SFResultSetMetaData = new SFResultSetMetaData(responseData);

		m_IsClosed = false;

		m_QueryId = responseData.QueryId;
	}

	readonly string[] PutGetResponseRowTypeInfo = {
			"SourceFileName",
			"DestinationFileName",
			"SourceFileSize",
			"DestinationFileSize",
			"SourceCompressionType",
			"DestinationCompressionType",
			"ResultStatus",
			"ErrorDetails"
		};

	public void initializePutGetRowType(List<ExecResponseRowType> rowType)
	{
		foreach (var name in PutGetResponseRowTypeInfo)
		{
			rowType.Add(new ExecResponseRowType()
			{
				Name = name,
				Type = "text"
			});
		}
	}

	public SFResultSet(PutGetResponseData responseData, SFStatement sfStatement, CancellationToken cancellationToken) : base(sfStatement.SFSession.Configuration)
	{
		if (responseData.RowSet == null)
			throw new ArgumentException($"responseData.rowSet is null", nameof(responseData));

		responseData.RowType = new List<ExecResponseRowType>();
		initializePutGetRowType(responseData.RowType);

		m_ColumnCount = responseData.RowType.Count;
		m_CurrentChunkRowIdx = -1;
		m_CurrentChunkRowCount = responseData.RowSet.GetLength(0);

		this.SFStatement = sfStatement;

		m_CurrentChunk = new SFResultChunk(responseData.RowSet);
		responseData.RowSet = null;

		SFResultSetMetaData = new SFResultSetMetaData(responseData);

		m_IsClosed = false;

		m_QueryId = responseData.QueryId;
	}

	internal void resetChunkInfo(IResultChunk nextChunk)
	{
		if (m_CurrentChunk is SFResultChunk chunk)
			chunk.RowSet = null;

		m_CurrentChunk = nextChunk;
		m_CurrentChunkRowIdx = 0;
		m_CurrentChunkRowCount = m_CurrentChunk.GetRowCount();
	}

	internal override async Task<bool> NextAsync()
	{
		if (m_IsClosed)
		{
			throw new SnowflakeDbException(SFError.DATA_READER_ALREADY_CLOSED);
		}

		m_CurrentChunkRowIdx++;
		if (m_CurrentChunkRowIdx < m_CurrentChunkRowCount)
		{
			return true;
		}

		if (m_ChunkDownloader != null)
		{
			// GetNextChunk could be blocked if download result is not done yet.
			// So put this piece of code in a seperate task
			var nextChunk = await m_ChunkDownloader.GetNextChunkAsync().ConfigureAwait(false);
			if (nextChunk != null)
			{
				resetChunkInfo(nextChunk);
				return true;
			}
			else
			{
				return false;
			}
		}

		return false;
	}

	internal override bool Next()
	{
		if (m_IsClosed)
		{
			throw new SnowflakeDbException(SFError.DATA_READER_ALREADY_CLOSED);
		}

		m_CurrentChunkRowIdx++;
		if (m_CurrentChunkRowIdx < m_CurrentChunkRowCount)
		{
			return true;
		}

		if (m_ChunkDownloader != null)
		{
			var nextChunk = Task.Run(async () => await (m_ChunkDownloader.GetNextChunkAsync()).ConfigureAwait(false)).Result;
			if (nextChunk != null)
			{
				resetChunkInfo(nextChunk);
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Move cursor back one row.
	/// </summary>
	/// <returns>True if it works, false otherwise.</returns>
	internal override bool Rewind()
	{
		if (m_IsClosed)
		{
			throw new SnowflakeDbException(SFError.DATA_READER_ALREADY_CLOSED);
		}

		if (m_CurrentChunkRowIdx >= 0)
		{
			m_CurrentChunkRowIdx--;
			if (m_CurrentChunkRowIdx >= m_CurrentChunkRowCount)
			{
				return true;
			}
		}

		return false;
	}

	protected override UTF8Buffer? getObjectInternal(int columnIndex)
	{
		if (m_IsClosed)
		{
			throw new SnowflakeDbException(SFError.DATA_READER_ALREADY_CLOSED);
		}

		if (columnIndex < 0 || columnIndex >= m_ColumnCount)
		{
			throw new SnowflakeDbException(SFError.COLUMN_INDEX_OUT_OF_BOUND, columnIndex);
		}

		return m_CurrentChunk.ExtractCell(m_CurrentChunkRowIdx, columnIndex);
	}

	void updateSessionStatus(QueryExecResponseData responseData)
	{
		if (SFStatement == null)
			throw new InvalidOperationException($"{nameof(SFStatement)} is null");

		var session = SFStatement.SFSession;
		session.m_Database = responseData.FinalDatabaseName;
		session.m_Schema = responseData.FinalSchemaName;

		if (responseData.Parameters != null)
			session.UpdateSessionParameterMap(responseData.Parameters);
	}
}
