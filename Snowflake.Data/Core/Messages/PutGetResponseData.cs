/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Messages;

internal class PutGetResponseData : IQueryExecResponseData
{
	[JsonProperty(PropertyName = "command", NullValueHandling = NullValueHandling.Ignore)]
	internal string command { get; set; }

	[JsonProperty(PropertyName = "localLocation", NullValueHandling = NullValueHandling.Ignore)]
	internal string localLocation { get; set; }

	[JsonProperty(PropertyName = "src_locations", NullValueHandling = NullValueHandling.Ignore)]
	internal List<string> src_locations { get; set; }

	[JsonProperty(PropertyName = "parallel", NullValueHandling = NullValueHandling.Ignore)]
	internal int parallel { get; set; }

	[JsonProperty(PropertyName = "threshold", NullValueHandling = NullValueHandling.Ignore)]
	internal int threshold { get; set; }

	[JsonProperty(PropertyName = "autoCompress", NullValueHandling = NullValueHandling.Ignore)]
	internal bool autoCompress { get; set; }

	[JsonProperty(PropertyName = "overwrite", NullValueHandling = NullValueHandling.Ignore)]
	internal bool overwrite { get; set; }

	[JsonProperty(PropertyName = "sourceCompression", NullValueHandling = NullValueHandling.Ignore)]
	internal string sourceCompression { get; set; }

	[JsonProperty(PropertyName = "stageInfo", NullValueHandling = NullValueHandling.Ignore)]
	internal PutGetStageInfo stageInfo { get; set; }

	[JsonProperty(PropertyName = "encryptionMaterial", NullValueHandling = NullValueHandling.Ignore)]
	[JsonConverter(typeof(SingleOrArrayConverter<PutGetEncryptionMaterial>))]
	internal List<PutGetEncryptionMaterial> encryptionMaterial { get; set; }

	[JsonProperty(PropertyName = "queryId", NullValueHandling = NullValueHandling.Ignore)]
	public string queryId { get; set; }

	[JsonProperty(PropertyName = "sqlState", NullValueHandling = NullValueHandling.Ignore)]
	public string sqlState { get; set; }

	[JsonProperty(PropertyName = "presignedUrl", NullValueHandling = NullValueHandling.Ignore)]
	internal string presignedUrl { get; set; }

	[JsonProperty(PropertyName = "presignedUrls", NullValueHandling = NullValueHandling.Ignore)]
	internal List<string> presignedUrls { get; set; }

	[JsonProperty(PropertyName = "rowtype", NullValueHandling = NullValueHandling.Ignore)]
	internal List<ExecResponseRowType> rowType { get; set; }

	[JsonProperty(PropertyName = "rowset", NullValueHandling = NullValueHandling.Ignore)]
	internal string[,] rowSet { get; set; }

	[JsonProperty(PropertyName = "parameters", NullValueHandling = NullValueHandling.Ignore)]
	internal List<NameValueParameter> parameters { get; set; }

	[JsonProperty(PropertyName = "statementTypeId", NullValueHandling = NullValueHandling.Ignore)]
	internal long statementTypeId { get; set; }
}
