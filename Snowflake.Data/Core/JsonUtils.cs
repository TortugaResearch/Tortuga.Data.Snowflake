/*
 * Copyright (c) 2021 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Tortuga.Data.Snowflake.Core;

class JsonUtils
{
	/// <summary>
	/// Default serialization settings for JSON serialization and deserialization.
	/// This is to avoid issues when changes are made system wide to the default and keep
	/// our settings locals.
	/// </summary>
	public static readonly JsonSerializerSettings JsonSettings =
		new() { ContractResolver = new DefaultContractResolver() { NamingStrategy = new DefaultNamingStrategy() } };
}
