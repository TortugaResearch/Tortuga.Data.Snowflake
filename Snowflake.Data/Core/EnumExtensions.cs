/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

#nullable enable

namespace Tortuga.Data.Snowflake.Core;

public static class EnumExtensions
{
	public static TAttribute? GetAttribute<TAttribute>(this Enum value)
		where TAttribute : Attribute
	{
		var type = value.GetType();
		var memInfo = type.GetMember(value.ToString());
		var attributes = memInfo[0].GetCustomAttributes(typeof(TAttribute), false);
		return attributes.Length > 0 ? (TAttribute)attributes[0] : null;
	}
}
