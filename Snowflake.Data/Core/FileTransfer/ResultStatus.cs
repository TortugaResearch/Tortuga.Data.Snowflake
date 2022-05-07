/*
 * Copyright (c) 2021 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core.FileTransfer;

/// <summary>
/// The status of the file to be uploaded/downloaded.
/// </summary>
enum ResultStatus
{
	ERROR = 0,
	UPLOADED = 1,
	DOWNLOADED = 2,
	COLLISION = 3,
	SKIPPED = 4,
	RENEW_TOKEN = 5,
	RENEW_PRESIGNED_URL = 6,
	NOT_FOUND_FILE = 7,
	NEED_RETRY = 8,
	NEED_RETRY_WITH_LOWER_CONCURRENCY = 9,
}
