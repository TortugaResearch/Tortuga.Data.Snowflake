/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core;

static class HttpUtil
{
	public static T? GetOptionOrDefault<T>(this HttpRequestMessage message, string key)
	{
#if NET5_0_OR_GREATER
		if (message.Options.TryGetValue<T>(new(key), out var value))
			return value;
		else
			return default;
#else
		if (message.Properties.TryGetValue(key, out var value))
			return (T)value;
		else
			return default;
#endif
	}

	public static void SetOption<T>(this HttpRequestMessage message, string key, T value)
	{
#if NET5_0_OR_GREATER
		message.Options.Set(new(key), value);
#else
		message.Properties[key] = value;
#endif
	}
}
