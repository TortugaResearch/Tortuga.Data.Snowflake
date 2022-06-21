﻿/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using System.Collections;
using System.Data.Common;

namespace Tortuga.Data.Snowflake;

public class SnowflakeParameterCollection : DbParameterCollection, IList<SnowflakeParameter>
{
	readonly object m_SyncRoot = new();

	readonly List<SnowflakeParameter> m_ParameterList = new();

	internal SnowflakeParameterCollection()
	{
	}

	public override int Count => m_ParameterList.Count;

	public override object SyncRoot => m_SyncRoot;

	public override int Add(object value)
	{
		m_ParameterList.Add((SnowflakeParameter)value);
		return m_ParameterList.Count - 1;
	}

	public int Add(SnowflakeParameter value)
	{
		m_ParameterList.Add(value);
		return m_ParameterList.Count - 1;
	}

	public SnowflakeParameter Add(string parameterName, SnowflakeDataType dataType)
	{
		var parameter = new SnowflakeParameter(parameterName, dataType);
		m_ParameterList.Add(parameter);
		return parameter;
	}

	public override void AddRange(Array values)
	{
		if (values == null)
			throw new ArgumentNullException(nameof(values), $"{nameof(values)} is null.");

		foreach (SnowflakeParameter? value in values)
			m_ParameterList.Add(value ?? throw new ArgumentException("The values array contains a null.", nameof(values)));
	}

	public override void Clear() => m_ParameterList.Clear();

	public override bool Contains(string value) => IndexOf(value) != -1;

	public override bool Contains(object value)
	{
		return value is SnowflakeParameter parameter && m_ParameterList.Contains(parameter);
	}

	public override void CopyTo(Array array, int index)
	{
		if (array == null)
			throw new ArgumentNullException(nameof(array), $"{nameof(array)} is null.");

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
		return value is not SnowflakeParameter parameter ? -1 : m_ParameterList.IndexOf(parameter);
	}

	public override void Insert(int index, object value) => m_ParameterList.Insert(index, (SnowflakeParameter)value);

	public override void Remove(object value) => m_ParameterList.Remove((SnowflakeParameter)value);

	public override void RemoveAt(string parameterName) => m_ParameterList.RemoveAt(IndexOf(parameterName));

	public override void RemoveAt(int index) => m_ParameterList.RemoveAt(index);

	protected override DbParameter GetParameter(string parameterName) => m_ParameterList[IndexOf(parameterName)];

	protected override DbParameter GetParameter(int index) => m_ParameterList[index];

	protected override void SetParameter(string parameterName, DbParameter value)
	{
		m_ParameterList[IndexOf(parameterName)] = (SnowflakeParameter)value;
	}

	protected override void SetParameter(int index, DbParameter value)
	{
		m_ParameterList[index] = (SnowflakeParameter)value;
	}

	public int IndexOf(SnowflakeParameter item) => m_ParameterList.IndexOf(item);

	public void Insert(int index, SnowflakeParameter item) => m_ParameterList.Insert(index, item);

	void ICollection<SnowflakeParameter>.Add(SnowflakeParameter item) => Add(item);

	public bool Contains(SnowflakeParameter item) => m_ParameterList.Contains(item);

	public void CopyTo(SnowflakeParameter[] array, int arrayIndex) => m_ParameterList.CopyTo(array, arrayIndex);

	public bool Remove(SnowflakeParameter item) => m_ParameterList.Remove(item);

	IEnumerator<SnowflakeParameter> IEnumerable<SnowflakeParameter>.GetEnumerator() => m_ParameterList.GetEnumerator();

	public new SnowflakeParameter this[int index]
	{
		get => m_ParameterList[index];
		set => m_ParameterList[index] = value;
	}
}