/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using System.Text;

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing.Chunks;

class SFResultChunk : IResultChunk
{
	public string?[,]? RowSet { get; set; }

	public int RowCount { get; set; }

	public int ColCount { get; set; }

	public string? Url { get; set; }

	public DownloadState DownloadState { get; set; }
	public int ChunkIndex { get; }

	public readonly object syncPrimitive = new();

	public SFResultChunk(string?[,]? rowSet)
	{
		RowSet = rowSet;
		RowCount = rowSet?.GetLength(0) ?? 0;
		DownloadState = DownloadState.NOT_STARTED;
	}

	public SFResultChunk(string url, int rowCount, int colCount, int index)
	{
		RowCount = rowCount;
		ColCount = colCount;
		Url = url;
		ChunkIndex = index;
		DownloadState = DownloadState.NOT_STARTED;
	}

	public UTF8Buffer? ExtractCell(int rowIndex, int columnIndex)
	{
		if (RowSet == null)
			throw new InvalidOperationException($"{nameof(RowSet)} is null");

		// Convert string to UTF8Buffer. This makes this method a little slower, but this class is not used for large result sets
		var s = RowSet[rowIndex, columnIndex];
		if (s == null)
			return null;
		byte[] b = Encoding.UTF8.GetBytes(s);
		return new UTF8Buffer(b);
	}

	public void AddValue(string val, int rowCount, int colCount)
	{
		if (RowSet == null)
			throw new InvalidOperationException($"{nameof(RowSet)} is null");

		RowSet[rowCount, colCount] = val;
	}

	public int GetRowCount()
	{
		return RowCount;
	}

	public int GetChunkIndex()
	{
		return ChunkIndex;
	}
}
