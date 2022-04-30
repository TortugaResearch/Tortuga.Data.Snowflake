/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core;

internal class SessionInfo
{
	[JsonProperty(PropertyName = "databaseName")]
	internal string databaseName { get; set; }

	[JsonProperty(PropertyName = "schemaName")]
	internal string schemaName { get; set; }

	[JsonProperty(PropertyName = "warehouseName")]
	internal string warehouseName { get; set; }

	[JsonProperty(PropertyName = "roleName")]
	internal string roleName { get; set; }
}
