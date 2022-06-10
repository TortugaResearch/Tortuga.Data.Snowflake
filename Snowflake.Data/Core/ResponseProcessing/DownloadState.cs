/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing;

public enum DownloadState
{
    NOT_STARTED = 0,
    IN_PROGRESS = 1,
    SUCCESS = 2,
    FAILURE = 3
}
