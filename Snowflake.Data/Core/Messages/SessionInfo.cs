/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Messages;

class SessionInfo
{
	[JsonProperty(PropertyName = "databaseName")]
	internal string? DatabaseName { get; set; }

	[JsonProperty(PropertyName = "schemaName")]
	internal string? SchemaName { get; set; }

	[JsonProperty(PropertyName = "warehouseName")]
	internal string? WarehouseName { get; set; }

	[JsonProperty(PropertyName = "roleName")]
	internal string? RoleName { get; set; }
}
