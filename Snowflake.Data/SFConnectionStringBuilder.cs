/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using System.Data.Common;

namespace Tortuga.Data.Snowflake;

#pragma warning disable CA1710 // Identifiers should have correct suffix

public class SFConnectionStringBuilder : DbConnectionStringBuilder, ICollection<KeyValuePair<string, object>>
{
	public void Add(KeyValuePair<string, object> item) => Add(item.Key, item.Value);

	public bool Contains(KeyValuePair<string, object> item)
	{
		return base.TryGetValue(item.Key, out var value) && Equals(value, item.Value);
	}

	public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
	{
		if (array == null)
			throw new ArgumentNullException(nameof(array));

		var i = arrayIndex;
		foreach (string? key in base.Keys) //key is never null. .NET Core 3.1 has a nullability bug.
			array[i] = new(key!, base[key!]);
	}

	public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
	{
		foreach (string? key in base.Keys) //key is never null. .NET Core 3.1 has a nullability bug.
			yield return new(key!, base[key!]);
	}

	public bool Remove(KeyValuePair<string, object> item)
	{
		var found = base.TryGetValue(item.Key, out var value) && Equals(value, item.Value);
		if (found)
			base.Remove(item.Key);
		return found;
	}
}
