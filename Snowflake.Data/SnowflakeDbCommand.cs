/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Tortuga.Data.Snowflake.Core;
using Tortuga.Data.Snowflake.Core.RequestProcessing;
using Tortuga.Data.Snowflake.Core.ResponseProcessing;

namespace Tortuga.Data.Snowflake;

[System.ComponentModel.DesignerCategory("Code")]
public class SnowflakeDbCommand : DbCommand
{
	readonly SnowflakeDbParameterCollection m_ParameterCollection = new();
	string m_CommandText = "";
	SnowflakeDbConnection? m_Connection;
	SFStatement? m_SFStatement;

	public SnowflakeDbCommand()
	{
	}

	public SnowflakeDbCommand(SnowflakeDbConnection connection)
	{
		m_Connection = connection;
	}

	[AllowNull]
	public override string CommandText
	{
		get => m_CommandText;
		set => m_CommandText = value ?? "";
	}

	public override int CommandTimeout { get; set; }

	[EditorBrowsable(EditorBrowsableState.Never)]
	public override CommandType CommandType
	{
		get => CommandType.Text;

		[Obsolete($"The {nameof(CommandType)} property is not supported.", true)]
		set => throw new NotSupportedException($"The {nameof(CommandType)} property is not supported.");
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public override bool DesignTimeVisible
	{
		get => false;

		[Obsolete($"The {nameof(DesignTimeVisible)} property is not supported.", true)]
		set => throw new NotSupportedException($"The {nameof(DesignTimeVisible)} property is not supported.");
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public override UpdateRowSource UpdatedRowSource
	{
		get => UpdateRowSource.None;

		[Obsolete($"The {nameof(UpdatedRowSource)} property is not supported.", true)]
		set => throw new NotSupportedException($"The {nameof(UpdatedRowSource)} property is not supported.");
	}

	protected override DbConnection? DbConnection
	{
		get => m_Connection;

		set
		{
			if (m_Connection != null && m_Connection != value)
				throw new InvalidOperationException("Connection already set.");

			switch (value)
			{
				case null:
					if (m_Connection == null)
						return;
					else
						throw new InvalidOperationException("Unsetting the connection not supported.");

				case SnowflakeDbConnection sfc:
					m_Connection = sfc;
					return;

				default:
					throw new ArgumentException("Connection must be of type SnowflakeDbConnection.", nameof(DbConnection));
			}
		}
	}

	protected override DbParameterCollection DbParameterCollection => m_ParameterCollection;

	protected override DbTransaction? DbTransaction { get; set; }

	public override void Cancel()
	{
		// doesn't throw exception when sfStatement is null
		m_SFStatement?.Cancel();
	}

	public override int ExecuteNonQuery()
	{
		var resultSet = ExecuteInternal();
		return resultSet.CalculateUpdateCount();
	}

	public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
			throw new TaskCanceledException();

		var resultSet = await ExecuteInternalAsync(cancellationToken).ConfigureAwait(false);
		return resultSet.CalculateUpdateCount();
	}

	public override object ExecuteScalar()
	{
		var resultSet = ExecuteInternal();

		if (resultSet.Next())
			return resultSet.GetValue(0);
		else
			return DBNull.Value;
	}

	public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
			throw new TaskCanceledException();

		var result = await ExecuteInternalAsync(cancellationToken).ConfigureAwait(false);

		if (await result.NextAsync().ConfigureAwait(false))
			return result.GetValue(0);
		else
			return DBNull.Value;
	}

	[Obsolete($"The method {nameof(Prepare)} is not implemented.", true)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public override void Prepare() => throw new NotSupportedException();

	protected override DbParameter CreateDbParameter() => new SnowflakeDbParameter();

	protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
	{
		var resultSet = ExecuteInternal();
		return new SnowflakeDbDataReader(resultSet, m_Connection!, behavior);
	}

	protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
	{
		var result = await ExecuteInternalAsync(cancellationToken).ConfigureAwait(false);
		return new SnowflakeDbDataReader(result, m_Connection!, behavior);
	}

	Dictionary<string, BindingDTO> ConvertToBindList()
	{
		var parameters = m_ParameterCollection;
		var binding = new Dictionary<string, BindingDTO>();

		if (parameters == null || parameters.Count == 0)
		{
			return binding;
		}
		else
		{
			for (var i = 0; i < parameters.Count; i++)
			{
				var parameter = parameters[i];
				var bindingType = "";
				object? bindingVal;

				var effectiveValue = parameter.Value ?? DBNull.Value;

				if (effectiveValue.GetType().IsArray &&
					// byte array and char array will not be treated as array binding
					effectiveValue is not char[] &&
					effectiveValue is not byte[])
				{
					var vals = new List<object?>();
					foreach (var val in (Array)effectiveValue)
					{
						// if the user is using interface, SFDataType will be None and there will
						// a conversion from DbType to SFDataType
						// if the user is using concrete class, they should specify SFDataType.
						if (parameter.SFDataType == SFDataType.None)
						{
							var typeAndVal = SFDataConverter.CSharpTypeValToSfTypeVal(parameter.DbType, val);

							bindingType = typeAndVal.Item1;
							vals.Add(typeAndVal.Item2);
						}
						else
						{
							bindingType = parameter.SFDataType.ToSql();
							vals.Add(SFDataConverter.csharpValToSfVal(parameter.SFDataType, val));
						}
					}
					bindingVal = vals;
				}
				else
				{
					if (parameter.SFDataType == SFDataType.None)
					{
						var typeAndVal = SFDataConverter.CSharpTypeValToSfTypeVal(parameter.DbType, parameter.Value);
						bindingType = typeAndVal.Item1;
						bindingVal = typeAndVal.Item2;
					}
					else
					{
						bindingType = parameter.SFDataType.ToSql();
						bindingVal = SFDataConverter.csharpValToSfVal(parameter.SFDataType, parameter.Value);
					}
				}

				if (string.IsNullOrEmpty(parameter.ParameterName))
					throw new InvalidOperationException($"Parameter {i} does not have a ParameterName");
				binding[parameter.ParameterName!] = new BindingDTO(bindingType, bindingVal);
			}
			return binding;
		}
	}

	SFBaseResultSet ExecuteInternal(bool describeOnly = false)
	{
		if (CommandText == null)
			throw new InvalidOperationException($"{nameof(CommandText)} is null");
		return SetStatement().Execute(CommandTimeout, CommandText, ConvertToBindList(), describeOnly);
	}

	Task<SFBaseResultSet> ExecuteInternalAsync(CancellationToken cancellationToken, bool describeOnly = false)
	{
		if (CommandText == null)
			throw new InvalidOperationException($"{nameof(CommandText)} is null");
		return SetStatement().ExecuteAsync(CommandTimeout, CommandText, ConvertToBindList(), describeOnly, cancellationToken);
	}

	SFStatement SetStatement()
	{
		if (m_Connection == null)
			throw new InvalidOperationException("Can't execute command before the connection has been set.");

		var session = m_Connection.SfSession;

		// SetStatement is called when executing a command. If SfSession is null
		// the connection has never been opened. Exception might be a bit vague.
		if (session == null)
			throw new InvalidOperationException("Can't execute command before the connection has been opened.");

		m_SFStatement = new SFStatement(session); //Needed to support `Cancel`
		return m_SFStatement;
	}
}
