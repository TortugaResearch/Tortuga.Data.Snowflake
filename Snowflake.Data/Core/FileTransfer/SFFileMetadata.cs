/*
 * Copyright (c) 2021 Snowflake Computing Inc. All rights reserved.
 */

using Tortuga.Data.Snowflake.Core.FileTransfer.StorageClient;
using Tortuga.Data.Snowflake.Core.Messages;
using static Tortuga.Data.Snowflake.Core.FileTransfer.SFFileCompressionTypes;

namespace Tortuga.Data.Snowflake.Core.FileTransfer;

/// <summary>
/// Metadata used by the remote storage client to upload or download a file/stream.
/// </summary>
class SFFileMetadata
{
	/// Original source file path (full path)
	public string? SrcFilePath { set; get; }

	/// Original path or temp path when compression is enabled (full path)
	public string? RealSrcFilePath { set; get; }

	/// Original source file name
	public string? SrcFileName { set; get; }

	/// Original source file size
	public long SrcFileSize { set; get; }

	/// Temp file if compressed is required, otherwise same as src file
	public string? SrcFileToUpload { set; get; }

	/// Temp file size if compressed is required, otherwise same as src file
	public long SrcFileToUploadSize { set; get; }

	/// Destination file name (no path)
	public string? DestFileName { set; get; }

	/// Destination file size
	public long DestFileSize { set; get; }

	/// Absolute path to the destination (including the filename. /tmp/small_test_file.csv.gz)
	public string? DestPath { set; get; }

	/// Absolute path to the local location of the downloaded file
	public string? LocalLocation { set; get; }

	/// Destination file size
	public long UploadSize { set; get; }

	/// Stage info of the file
	public PutGetStageInfo? StageInfo { get; set; }

	/// True if require gzip compression
	public bool RequireCompress { set; get; }

	/// Upload and overwrite if file exists
	public bool Overwrite { set; get; }

	/// Encryption material
	public PutGetEncryptionMaterial? EncryptionMaterial { set; get; }

	/// Encryption metadata
	public SFEncryptionMetadata? EncryptionMetadata { set; get; }

	/// File message digest (after compression if required)
	public string? Sha256Digest { set; get; }

	/// Source compression
	public SFFileCompressionType SourceCompression { set; get; }

	/// Target compression
	public SFFileCompressionType TargetCompression { set; get; }

	/// Pre-signed url.
	public string? PresignedUrl { set; get; }

	/// The number of chunks to download in parallel.
	public int Parallel { get; set; }

	/// The outcome of the transfer.
	public string? ResultStatus { get; set; }

	/// The temporary directory to store files to upload/download.
	public string? TmpDir { get; set; }

	/// Storage client to use for uploading/downloading files.
	public ISFRemoteStorageClient? Client { get; set; }

	/// Last error returned from client request.
	public Exception? LastError { get; set; }

	/// Last specified max concurrency to use.
	public int LastMaxConcurrency { get; set; }
}
