/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Messages;

class PutGetResponseData : IQueryExecResponseData
{
	[JsonProperty(PropertyName = "command", NullValueHandling = NullValueHandling.Ignore)]
	internal string? Command { get; set; }

	[JsonProperty(PropertyName = "localLocation", NullValueHandling = NullValueHandling.Ignore)]
	internal string? LocalLocation { get; set; }

	[JsonProperty(PropertyName = "src_locations", NullValueHandling = NullValueHandling.Ignore)]
	internal List<string>? SourceLocations { get; set; }

	[JsonProperty(PropertyName = "parallel", NullValueHandling = NullValueHandling.Ignore)]
	internal int Parallel { get; set; }

	[JsonProperty(PropertyName = "threshold", NullValueHandling = NullValueHandling.Ignore)]
	internal int Threshold { get; set; }

	[JsonProperty(PropertyName = "autoCompress", NullValueHandling = NullValueHandling.Ignore)]
	internal bool AutoCompress { get; set; }

	[JsonProperty(PropertyName = "overwrite", NullValueHandling = NullValueHandling.Ignore)]
	internal bool Overwrite { get; set; }

	[JsonProperty(PropertyName = "sourceCompression", NullValueHandling = NullValueHandling.Ignore)]
	internal string? SourceCompression { get; set; }

	[JsonProperty(PropertyName = "stageInfo", NullValueHandling = NullValueHandling.Ignore)]
	internal PutGetStageInfo? StageInfo { get; set; }

	[JsonProperty(PropertyName = "encryptionMaterial", NullValueHandling = NullValueHandling.Ignore)]
	[JsonConverter(typeof(SingleOrArrayConverter<PutGetEncryptionMaterial>))]
	internal List<PutGetEncryptionMaterial>? EncryptionMaterial { get; set; }

	[JsonProperty(PropertyName = "queryId", NullValueHandling = NullValueHandling.Ignore)]
	public string? QueryId { get; set; }

	[JsonProperty(PropertyName = "sqlState", NullValueHandling = NullValueHandling.Ignore)]
	public string? SqlState { get; set; }

	[JsonProperty(PropertyName = "presignedUrl", NullValueHandling = NullValueHandling.Ignore)]
	internal string? PresignedUrl { get; set; }

	[JsonProperty(PropertyName = "presignedUrls", NullValueHandling = NullValueHandling.Ignore)]
	internal List<string>? PresignedUrls { get; set; }

	[JsonProperty(PropertyName = "rowtype", NullValueHandling = NullValueHandling.Ignore)]
	internal List<ExecResponseRowType>? RowType { get; set; }

	[JsonProperty(PropertyName = "rowset", NullValueHandling = NullValueHandling.Ignore)]
#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
	internal string?[,]? RowSet { get; set; }

#pragma warning restore CA1814 // Prefer jagged arrays over multidimensional

	[JsonProperty(PropertyName = "parameters", NullValueHandling = NullValueHandling.Ignore)]
	internal List<NameValueParameter>? Parameters { get; set; }

	[JsonProperty(PropertyName = "statementTypeId", NullValueHandling = NullValueHandling.Ignore)]
	internal long StatementTypeId { get; set; }
}
