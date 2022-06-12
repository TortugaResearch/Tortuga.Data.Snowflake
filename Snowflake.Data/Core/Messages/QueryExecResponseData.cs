/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;
using Tortuga.Data.Snowflake.Core.ResponseProcessing.Chunks;

namespace Tortuga.Data.Snowflake.Core.Messages;

class QueryExecResponseData
{
	[JsonProperty(PropertyName = "parameters", NullValueHandling = NullValueHandling.Ignore)]
	internal List<NameValueParameter>? Parameters { get; set; }

	[JsonProperty(PropertyName = "rowtype", NullValueHandling = NullValueHandling.Ignore)]
	internal List<ExecResponseRowType>? RowType { get; set; }

	[JsonProperty(PropertyName = "rowset", NullValueHandling = NullValueHandling.Ignore)]
	internal string?[,]? RowSet { get; set; }

	[JsonProperty(PropertyName = "total", NullValueHandling = NullValueHandling.Ignore)]
	internal long Total { get; set; }

	[JsonProperty(PropertyName = "returned", NullValueHandling = NullValueHandling.Ignore)]
	internal long Returned { get; set; }

	[JsonProperty(PropertyName = "queryId", NullValueHandling = NullValueHandling.Ignore)]
	internal string? QueryId { get; set; }

	[JsonProperty(PropertyName = "sqlState", NullValueHandling = NullValueHandling.Ignore)]
	internal string? sqlState { get; set; }

	[JsonProperty(PropertyName = "databaseProvider", NullValueHandling = NullValueHandling.Ignore)]
	internal string? databaseProvider { get; set; }

	[JsonProperty(PropertyName = "finalDatabaseName", NullValueHandling = NullValueHandling.Ignore)]
	internal string? FinalDatabaseName { get; set; }

	[JsonProperty(PropertyName = "finalSchemaName", NullValueHandling = NullValueHandling.Ignore)]
	internal string? FinalSchemaName { get; set; }

	[JsonProperty(PropertyName = "finalWarehouseName", NullValueHandling = NullValueHandling.Ignore)]
	internal string? FinalWarehouseName { get; set; }

	[JsonProperty(PropertyName = "finalRoleName", NullValueHandling = NullValueHandling.Ignore)]
	internal string? FinalRoleName { get; set; }

	[JsonProperty(PropertyName = "numberOfBinds", NullValueHandling = NullValueHandling.Ignore)]
	internal int NumberOfBinds { get; set; }

	[JsonProperty(PropertyName = "statementTypeId", NullValueHandling = NullValueHandling.Ignore)]
	internal long StatementTypeId { get; set; }

	[JsonProperty(PropertyName = "version", NullValueHandling = NullValueHandling.Ignore)]
	internal int Version { get; set; }

	[JsonProperty(PropertyName = "chunks", NullValueHandling = NullValueHandling.Ignore)]
	internal List<ExecResponseChunk>? Chunks { get; set; }

	[JsonProperty(PropertyName = "qrmk", NullValueHandling = NullValueHandling.Ignore)]
	internal string? Qrmk { get; set; }

	[JsonProperty(PropertyName = "chunkHeaders", NullValueHandling = NullValueHandling.Ignore)]
	internal Dictionary<string, string>? ChunkHeaders { get; set; }

	// ping pong response data
	[JsonProperty(PropertyName = "getResultUrl", NullValueHandling = NullValueHandling.Ignore)]
	internal string? GetResultUrl { get; set; }

	[JsonProperty(PropertyName = "progressDesc", NullValueHandling = NullValueHandling.Ignore)]
	internal string? ProgressDesc { get; set; }

	[JsonProperty(PropertyName = "queryAbortAfterSecs", NullValueHandling = NullValueHandling.Ignore)]
	internal long QueryAbortAfterSecs { get; set; }
}
