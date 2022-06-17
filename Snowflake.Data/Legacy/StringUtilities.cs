namespace Tortuga.Data.Snowflake.Legacy;

#if !NETCOREAPP3_1_OR_GREATER

static class StringUtilities
{
	public static bool Contains(this string stringBeingSearched, string value, StringComparison comparisonType)
	{
		return stringBeingSearched.IndexOf(value, comparisonType) >= 0;
	}

	public static int IndexOf(this string stringBeingSearched, char value, StringComparison comparisonType)
	{
		return stringBeingSearched.IndexOf(value.ToString(), comparisonType);
	}

	public static bool Contains(this string stringBeingSearched, char value, StringComparison comparisonType)
	{
		return stringBeingSearched.IndexOf(value.ToString(), comparisonType) >= 0;
	}

	//public static int IndexOf(this string stringBeingSearched, char value, int startIndex, StringComparison comparisonType)
	//{
	//	return stringBeingSearched.IndexOf(value.ToString(), startIndex, comparisonType);
	//}

	public static string Replace(this string stringBeingSearched, string oldValue, string? newValue, StringComparison comparisonType)
	{
		if (comparisonType != StringComparison.Ordinal)
			throw new ArgumentException("Only StringComparison.Ordinal is supported prior to .NET 6");
		return stringBeingSearched.Replace(oldValue, newValue);
	}
}

#endif
