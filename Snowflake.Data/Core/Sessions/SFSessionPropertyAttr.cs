/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core.Sessions;

[AttributeUsage(AttributeTargets.Field)]
sealed class SFSessionPropertyAttribute : Attribute
{
	public string? DefaultValue { get; init; }
	public bool Required { get; init; }
}
