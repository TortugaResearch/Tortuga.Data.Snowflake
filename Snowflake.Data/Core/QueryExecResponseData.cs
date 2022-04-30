/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core;

internal class QueryExecResponseData
{
	[JsonProperty(PropertyName = "parameters", NullValueHandling = NullValueHandling.Ignore)]
	internal List<NameValueParameter> parameters { get; set; }

	[JsonProperty(PropertyName = "rowtype", NullValueHandling = NullValueHandling.Ignore)]
	internal List<ExecResponseRowType> rowType { get; set; }

	[JsonProperty(PropertyName = "rowset", NullValueHandling = NullValueHandling.Ignore)]
	internal string[,] rowSet { get; set; }

	[JsonProperty(PropertyName = "total", NullValueHandling = NullValueHandling.Ignore)]
	internal long total { get; set; }

	[JsonProperty(PropertyName = "returned", NullValueHandling = NullValueHandling.Ignore)]
	internal long returned { get; set; }

	[JsonProperty(PropertyName = "queryId", NullValueHandling = NullValueHandling.Ignore)]
	internal string queryId { get; set; }

	[JsonProperty(PropertyName = "sqlState", NullValueHandling = NullValueHandling.Ignore)]
	internal string sqlState { get; set; }

	[JsonProperty(PropertyName = "databaseProvider", NullValueHandling = NullValueHandling.Ignore)]
	internal string databaseProvider { get; set; }

	[JsonProperty(PropertyName = "finalDatabaseName", NullValueHandling = NullValueHandling.Ignore)]
	internal string finalDatabaseName { get; set; }

	[JsonProperty(PropertyName = "finalSchemaName", NullValueHandling = NullValueHandling.Ignore)]
	internal string finalSchemaName { get; set; }

	[JsonProperty(PropertyName = "finalWarehouseName", NullValueHandling = NullValueHandling.Ignore)]
	internal string finalWarehouseName { get; set; }

	[JsonProperty(PropertyName = "finalRoleName", NullValueHandling = NullValueHandling.Ignore)]
	internal string finalRoleName { get; set; }

	[JsonProperty(PropertyName = "numberOfBinds", NullValueHandling = NullValueHandling.Ignore)]
	internal int numberOfBinds { get; set; }

	[JsonProperty(PropertyName = "statementTypeId", NullValueHandling = NullValueHandling.Ignore)]
	internal long statementTypeId { get; set; }

	[JsonProperty(PropertyName = "version", NullValueHandling = NullValueHandling.Ignore)]
	internal int version { get; set; }

	[JsonProperty(PropertyName = "chunks", NullValueHandling = NullValueHandling.Ignore)]
	internal List<ExecResponseChunk> chunks { get; set; }

	[JsonProperty(PropertyName = "qrmk", NullValueHandling = NullValueHandling.Ignore)]
	internal string qrmk { get; set; }

	[JsonProperty(PropertyName = "chunkHeaders", NullValueHandling = NullValueHandling.Ignore)]
	internal Dictionary<string, string> chunkHeaders { get; set; }

	// ping pong response data
	[JsonProperty(PropertyName = "getResultUrl", NullValueHandling = NullValueHandling.Ignore)]
	internal string getResultUrl { get; set; }

	[JsonProperty(PropertyName = "progressDesc", NullValueHandling = NullValueHandling.Ignore)]
	internal string progressDesc { get; set; }

	[JsonProperty(PropertyName = "queryAbortAfterSecs", NullValueHandling = NullValueHandling.Ignore)]
	internal long queryAbortAfterSecs { get; set; }
}
