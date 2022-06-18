/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing.Chunks;

class SFReusableChunk : IResultChunk
{
	public int RowCount { get; set; }

	public int ColCount { get; set; }

	public string? Url { get; set; }

	public int chunkIndexToDownload { get; set; }

	readonly BlockResultData data;

	internal SFReusableChunk(int colCount)
	{
		ColCount = colCount;
		data = new BlockResultData();
	}

	internal void Reset(ExecResponseChunk chunkInfo, int chunkIndex)
	{
		RowCount = chunkInfo.rowCount;
		Url = chunkInfo.url;
		chunkIndexToDownload = chunkIndex;
		data.Reset(RowCount, ColCount, chunkInfo.uncompressedSize);
	}

	public int GetRowCount()
	{
		return RowCount;
	}

	public int GetChunkIndex()
	{
		return chunkIndexToDownload;
	}

	public UTF8Buffer? ExtractCell(int rowIndex, int columnIndex)
	{
		return data.get(rowIndex * ColCount + columnIndex);
	}

	public void AddCell(string val)
	{
		// This method should not be used - we want to avoid unnecessary conversions between string and bytes
		throw new NotImplementedException();
	}

	public void AddCell(byte[]? bytes, int length)
	{
		data.Add(bytes, length);
	}

	class BlockResultData
	{
		const int NULL_VALUE = -100;
		const int blockLengthBits = 24;
		const int blockLength = 1 << blockLengthBits;
		const int metaBlockLengthBits = 15;
		const int metaBlockLength = 1 << metaBlockLengthBits;

		int m_BlockCount;

		int m_MetaBlockCount;

		readonly List<byte[]> m_Data = new();
		readonly List<int[]> m_Offsets = new();
		readonly List<int[]> m_Lengths = new();
		int m_NextIndex;
		int m_CurrentDatOffset;

		internal BlockResultData()
		{ }

		internal void Reset(int rowCount, int colCount, int uncompressedSize)
		{
			m_CurrentDatOffset = 0;
			m_NextIndex = 0;
			var bytesNeeded = uncompressedSize - (rowCount * 2) - (rowCount * colCount);
			m_BlockCount = GetBlock(bytesNeeded - 1) + 1;
			m_MetaBlockCount = GetMetaBlock(rowCount * colCount - 1) + 1;
		}

		public UTF8Buffer? get(int index)
		{
			var length = m_Lengths[GetMetaBlock(index)][GetMetaBlockIndex(index)];

			if (length == NULL_VALUE)
			{
				return null;
			}
			else
			{
				var offset = m_Offsets[GetMetaBlock(index)][GetMetaBlockIndex(index)];

				// Create string from the char arrays
				if (SpaceLeftOnBlock(offset) < length)
				{
					var copied = 0;
					var cell = new byte[length];
					while (copied < length)
					{
						var copySize = Math.Min(length - copied, SpaceLeftOnBlock(offset + copied));
						Array.Copy(m_Data[GetBlock(offset + copied)], GetBlockOffset(offset + copied), cell, copied, copySize);

						copied += copySize;
					}
					return new UTF8Buffer(cell);
				}
				else
				{
					return new UTF8Buffer(m_Data[GetBlock(offset)], GetBlockOffset(offset), length);
				}
			}
		}

		public void Add(byte[]? bytes, int length)
		{
			if (m_Data.Count < m_BlockCount || m_Offsets.Count < m_MetaBlockCount)
			{
				AllocateArrays();
			}

			if (bytes == null)
			{
				m_Lengths[GetMetaBlock(m_NextIndex)]
					[GetMetaBlockIndex(m_NextIndex)] = NULL_VALUE;
			}
			else
			{
				var offset = m_CurrentDatOffset;

				// store offset and length
				var block = GetMetaBlock(m_NextIndex);
				var index = GetMetaBlockIndex(m_NextIndex);
				m_Offsets[block][index] = offset;
				m_Lengths[block][index] = length;

				// copy bytes to data array
				var copied = 0;
				if (SpaceLeftOnBlock(offset) < length)
				{
					while (copied < length)
					{
						var copySize = Math.Min(length - copied, SpaceLeftOnBlock(offset + copied));
						Array.Copy(bytes, copied, m_Data[GetBlock(offset + copied)], GetBlockOffset(offset + copied), copySize);
						copied += copySize;
					}
				}
				else
				{
					Array.Copy(bytes, 0, m_Data[GetBlock(offset)], GetBlockOffset(offset), length);
				}
				m_CurrentDatOffset += length;
			}
			m_NextIndex++;
		}

		static int GetBlock(int offset)
		{
			return offset >> blockLengthBits;
		}

		static int GetBlockOffset(int offset)
		{
			return offset & (blockLength - 1);
		}

		static int SpaceLeftOnBlock(int offset)
		{
			return blockLength - GetBlockOffset(offset);
		}

		static int GetMetaBlock(int index)
		{
			return index >> metaBlockLengthBits;
		}

		static int GetMetaBlockIndex(int index)
		{
			return index & (metaBlockLength - 1);
		}

		void AllocateArrays()
		{
			while (m_Data.Count < m_BlockCount)
			{
				m_Data.Add(new byte[1 << blockLengthBits]);
			}
			while (m_Offsets.Count < m_MetaBlockCount)
			{
				m_Offsets.Add(new int[1 << metaBlockLengthBits]);
				m_Lengths.Add(new int[1 << metaBlockLengthBits]);
			}
		}
	}
}
