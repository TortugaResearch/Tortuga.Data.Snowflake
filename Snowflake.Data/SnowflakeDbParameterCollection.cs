/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using System.Collections;
using System.Data.Common;

namespace Tortuga.Data.Snowflake;

public class SnowflakeDbParameterCollection : DbParameterCollection
{
	readonly object m_SyncRoot = new();

	readonly List<SnowflakeDbParameter> m_ParameterList = new();

	internal SnowflakeDbParameterCollection()
	{
	}

	public override int Count => m_ParameterList.Count;

	public override object SyncRoot => m_SyncRoot;

	public override int Add(object value)
	{
		m_ParameterList.Add((SnowflakeDbParameter)value);
		return m_ParameterList.Count - 1;
	}

	public int Add(SnowflakeDbParameter value)
	{
		m_ParameterList.Add(value);
		return m_ParameterList.Count - 1;
	}

	public SnowflakeDbParameter Add(string parameterName, SFDataType dataType)
	{
		var parameter = new SnowflakeDbParameter(parameterName, dataType);
		m_ParameterList.Add(parameter);
		return parameter;
	}

	public override void AddRange(Array values)
	{
		foreach (SnowflakeDbParameter? value in values)
			m_ParameterList.Add(value ?? throw new ArgumentException("The values array contains a null.", nameof(values)));
	}

	public override void Clear() => m_ParameterList.Clear();

	public override bool Contains(string value) => IndexOf(value) != -1;

	public override bool Contains(object value)
	{
		return value is SnowflakeDbParameter parameter && m_ParameterList.Contains(parameter);
	}

	public override void CopyTo(Array array, int index)
	{
		var untypedArray = (object[])array;
		for (var i = 0; i < m_ParameterList.Count; i++)
			untypedArray[i + index] = m_ParameterList[i];
	}

	public override IEnumerator GetEnumerator() => m_ParameterList.GetEnumerator();

	public override int IndexOf(string parameterName)
	{
		for (var i = 0; i < m_ParameterList.Count; i++)
			if (m_ParameterList[i].ParameterName == parameterName)
				return i;
		return -1;
	}

	public override int IndexOf(object value)
	{
		return value is not SnowflakeDbParameter parameter ? -1 : m_ParameterList.IndexOf(parameter);
	}

	public override void Insert(int index, object value) => m_ParameterList.Insert(index, (SnowflakeDbParameter)value);

	public override void Remove(object value) => m_ParameterList.Remove((SnowflakeDbParameter)value);

	public override void RemoveAt(string parameterName) => m_ParameterList.RemoveAt(IndexOf(parameterName));

	public override void RemoveAt(int index) => m_ParameterList.RemoveAt(index);

	protected override DbParameter GetParameter(string parameterName) => m_ParameterList[IndexOf(parameterName)];

	protected override DbParameter GetParameter(int index) => m_ParameterList[index];

	protected override void SetParameter(string parameterName, DbParameter value)
	{
		m_ParameterList[IndexOf(parameterName)] = (SnowflakeDbParameter)value;
	}

	protected override void SetParameter(int index, DbParameter value)
	{
		m_ParameterList[index] = (SnowflakeDbParameter)value;
	}

	public new SnowflakeDbParameter this[int index]
	{
		get => m_ParameterList[index];
		set => m_ParameterList[index] = value;
	}
}
