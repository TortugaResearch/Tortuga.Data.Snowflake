#nullable enable

namespace Tortuga.Data.Snowflake.Core.ResponseProcessing;

public static class UTF8BufferExtension
{
	// Define an extension method that can safely be called even on null objects
	// Calling ToString() on a null object causes an exception
	public static string? SafeToString(this UTF8Buffer? v)
	{
		return v == null ? null : v.ToString();
	}
}
