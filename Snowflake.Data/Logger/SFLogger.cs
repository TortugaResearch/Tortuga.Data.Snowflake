/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Log;

interface SFLogger
{
	bool IsDebugEnabled();

	bool IsInfoEnabled();

	bool IsWarnEnabled();

	bool IsErrorEnabled();

	bool IsFatalEnabled();

	void Debug(string msg, Exception ex = null);

	void Info(string msg, Exception ex = null);

	void Warn(string msg, Exception ex = null);

	void Error(string msg, Exception ex = null);

	void Fatal(string msg, Exception ex = null);
}

enum LoggingEvent
{
	DEBUG, INFO, WARN, ERROR, FATAL
}
