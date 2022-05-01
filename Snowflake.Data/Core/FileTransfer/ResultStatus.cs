/*
 * Copyright (c) 2021 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core.FileTransfer;

/// <summary>
/// The status of the file to be uploaded/downloaded.
/// </summary>
enum ResultStatus
{
	ERROR,
	UPLOADED,
	DOWNLOADED,
	COLLISION,
	SKIPPED,
	RENEW_TOKEN,
	RENEW_PRESIGNED_URL,
	NOT_FOUND_FILE,
	NEED_RETRY,
	NEED_RETRY_WITH_LOWER_CONCURRENCY
}
