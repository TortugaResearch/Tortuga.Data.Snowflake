using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.FileTransfer;

/// <summary>
/// The encryption materials.
/// </summary>
class MaterialDescriptor
{
	[JsonProperty(PropertyName = "smkId")]
	public string? SmkId { get; set; }

	[JsonProperty(PropertyName = "queryId")]
	public string? QueryId { get; set; }

	[JsonProperty(PropertyName = "keySize")]
	public string? KeySize { get; set; }
}
