/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using Tortuga.Data.Snowflake.Core;
using Tortuga.Data.Snowflake.Core.Sessions;

namespace Tortuga.Data.Snowflake;

[System.ComponentModel.DesignerCategory("Code")]
public class SnowflakeDbConnection : DbConnection
{
	internal ConnectionState m_ConnectionState;
	internal int m_ConnectionTimeout;

	static readonly object s_markerObject = new();

	string m_ConnectionString = "";

	public SnowflakeDbConnection()
	{
		m_ConnectionState = ConnectionState.Closed;
		m_ConnectionTimeout = int.Parse(SFSessionProperty.CONNECTION_TIMEOUT.GetAttribute<SFSessionPropertyAttribute>()?.DefaultValue ?? "0");
	}

	[AllowNull]
	public override string ConnectionString
	{
		get => m_ConnectionString;
		set => m_ConnectionString = value ?? "";
	}

	public override int ConnectionTimeout => this.m_ConnectionTimeout;

	public override string Database => SfSession?.m_Database ?? "";

	/// <summary>
	///     If the connection to the database is closed, the DataSource returns whatever is contained
	///     in the ConnectionString for the DataSource keyword. If the connection is open and the
	///     ConnectionString data source keyword's value starts with "|datadirectory|", the property
	///     returns whatever is contained in the ConnectionString for the DataSource keyword only. If
	///     the connection to the database is open, the property returns what the native provider
	///     returns for the DBPROP_INIT_DATASOURCE, and if that is empty, the native provider's
	///     DBPROP_DATASOURCENAME is returned.
	///     Note: not yet implemented
	/// </summary>
	public override string DataSource => "";

	public SecureString? Password { get; set; }

	public override string ServerVersion => SfSession?.m_ServerVersion ?? "";
	public override ConnectionState State => m_ConnectionState;
	internal SFSession? SfSession { get; set; }

	SnowflakeDbConfiguration m_Configuration = SnowflakeDbConfiguration.Default;

	/// <summary>
	/// Gets or sets the configuration.
	/// </summary>
	/// <value>The configuration.</value>
	/// <remarks>This defaults to SnowflakeDbConfiguration.Default.</remarks>
	public SnowflakeDbConfiguration Configuration
	{
		get => m_Configuration;
		set
		{
			if (m_Configuration == value)
				return;

			if (State != ConnectionState.Closed)
				throw new InvalidOperationException("Cannot change configuration while the connection is open.");

			m_Configuration = value;
		}
	}

	public override void ChangeDatabase(string databaseName)
	{
		var alterDbCommand = $"use database {databaseName}";

		using (var cmd = CreateCommand())
		{
			cmd.CommandText = alterDbCommand;
			cmd.ExecuteNonQuery();
		}
	}

	public async Task ChangeDatabaseAsync(string databaseName)
	{
		string alterDbCommand = $"use database {databaseName}";

		using (var cmd = CreateCommand())
		{
			cmd.CommandText = alterDbCommand;
			await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
		}
	}

	public override void Close()
	{
		if (m_ConnectionState != ConnectionState.Closed && SfSession != null)
			SfSession.Close();

		m_ConnectionState = ConnectionState.Closed;
	}

	public Task CloseAsync(CancellationToken cancellationToken)
	{
		var taskCompletionSource = new TaskCompletionSource<object>();

		if (cancellationToken.IsCancellationRequested)
		{
			taskCompletionSource.SetCanceled();
		}
		else
		{
			if (m_ConnectionState != ConnectionState.Closed && SfSession != null)
			{
				SfSession.CloseAsync(cancellationToken).ContinueWith(
					previousTask =>
					{
						if (previousTask.IsFaulted)
						{
							// Exception from SfSession.CloseAsync
							taskCompletionSource.SetException(previousTask.Exception!.InnerException ?? previousTask.Exception);
						}
						else if (previousTask.IsCanceled)
						{
							m_ConnectionState = ConnectionState.Closed;
							taskCompletionSource.SetCanceled();
						}
						else
						{
							taskCompletionSource.SetResult(s_markerObject);
							m_ConnectionState = ConnectionState.Closed;
						}
					}, cancellationToken);
			}
			else
			{
				taskCompletionSource.SetResult(s_markerObject);
			}
		}
		return taskCompletionSource.Task;
	}

	public bool IsOpen() => m_ConnectionState == ConnectionState.Open;

	public override void Open()
	{
		SetSession();
		try
		{
			SfSession!.Open();
		}
		catch (SnowflakeDbException)
		{
			m_ConnectionState = ConnectionState.Closed;
			throw;
		}
		catch (Exception e)
		{
			// Otherwise when Dispose() is called, the close request would timeout.
			m_ConnectionState = ConnectionState.Closed;
			throw new SnowflakeDbException(e, SnowflakeDbException.CONNECTION_FAILURE_SSTATE, SFError.INTERNAL_ERROR, "Unable to connect. " + e.Message);
		}
		m_ConnectionState = ConnectionState.Open;
	}

	public override async Task OpenAsync(CancellationToken cancellationToken)
	{
		RegisterConnectionCancellationCallback(cancellationToken);
		SetSession();
		try
		{
			await SfSession!.OpenAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (SnowflakeDbException)
		{
			m_ConnectionState = ConnectionState.Closed;
			throw;
		}
		catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			m_ConnectionState = ConnectionState.Closed;
			throw;
		}
		catch (Exception ex)
		{
			// Otherwise when Dispose() is called, the close request would timeout.
			m_ConnectionState = ConnectionState.Closed;
			throw new SnowflakeDbException(ex, SnowflakeDbException.CONNECTION_FAILURE_SSTATE, SFError.INTERNAL_ERROR, "Unable to connect. " + ex.Message);
		}
		m_ConnectionState = ConnectionState.Open;
	}

	/// <summary>
	///     Register cancel callback. Two factors: either external cancellation token passed down from upper
	///     layer or timeout reached. Whichever comes first would trigger query cancellation.
	/// </summary>
	/// <param name="externalCancellationToken">cancellation token from upper layer</param>
	internal void RegisterConnectionCancellationCallback(CancellationToken externalCancellationToken)
	{
		if (!externalCancellationToken.IsCancellationRequested)
		{
			externalCancellationToken.Register(() => { m_ConnectionState = ConnectionState.Closed; });
		}
	}

	protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
	{
		// Parameterless BeginTransaction() method of the super class calls this method with IsolationLevel.Unspecified,
		// Change the isolation level to ReadCommitted
		if (isolationLevel == IsolationLevel.Unspecified)
		{
			isolationLevel = IsolationLevel.ReadCommitted;
		}

		return new SnowflakeDbTransaction(isolationLevel, this);
	}

	protected override DbCommand CreateDbCommand()
	{
		return new SnowflakeDbCommand(this);
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			Close();
		}
		catch
		{
			// Prevent an exception from being thrown when disposing of this object
		}
	}

	/// <summary>
	/// Create a new SFSession with the connection string settings.
	/// </summary>
	/// <exception cref="SnowflakeDbException">If the connection string can't be processed</exception>
	private void SetSession()
	{
		if (ConnectionString == null)
			throw new InvalidOperationException($"{nameof(ConnectionString)} is null");

		SfSession = new SFSession(ConnectionString, Password, Configuration);
		m_ConnectionTimeout = (int)SfSession.m_ConnectionTimeout.TotalSeconds;
		m_ConnectionState = ConnectionState.Connecting;
	}
}
