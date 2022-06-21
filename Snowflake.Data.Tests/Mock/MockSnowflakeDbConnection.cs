﻿/*
 * Copyright (c) 2021 Snowflake Computing Inc. All rights reserved.
 */

using System.Data;
using Tortuga.Data.Snowflake.Core.RequestProcessing;
using Tortuga.Data.Snowflake.Core.Sessions;

namespace Tortuga.Data.Snowflake.Tests.Mock;

class MockSnowflakeDbConnection : SnowflakeConnection
{
	readonly private IMockRestRequester _restRequester;

	public MockSnowflakeDbConnection(IMockRestRequester requester)
	{
		_restRequester = requester;
	}

	public MockSnowflakeDbConnection()
	{
		// Default requester
		_restRequester = new MockRetryUntilRestTimeoutRestRequester();
	}

	public override void Open()
	{
		SetMockSession();
		try
		{
			SfSession.Open();
			OnSessionEstablished();
		}
		catch (SnowflakeException)
		{
			m_ConnectionState = ConnectionState.Closed;
			throw;
		}
		catch (Exception ex)
		{
			// Otherwise when Dispose() is called, the close request would timeout.
			m_ConnectionState = System.Data.ConnectionState.Closed;
			throw new SnowflakeException(ex, SnowflakeError.InternalError, "Unable to connect");
		}
	}

	public async override Task OpenAsync(CancellationToken cancellationToken)
	{
		RegisterConnectionCancellationCallback(cancellationToken);

		SetMockSession();

		try
		{
			await SfSession.OpenAsync(cancellationToken).ConfigureAwait(false);
			OnSessionEstablished();
		}
		catch (SnowflakeException)
		{
			m_ConnectionState = ConnectionState.Closed;
			throw;
		}
		catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			m_ConnectionState = ConnectionState.Closed;
			throw;
		}
		catch (Exception ex) when (ex is not TaskCanceledException || !cancellationToken.IsCancellationRequested)
		{
			m_ConnectionState = ConnectionState.Closed;
			throw new SnowflakeException(ex, SnowflakeError.InternalError, "Unable to connect");
		}
	}

	private void SetMockSession()
	{
		SfSession = new SFSession(ConnectionString, Password, _restRequester, SnowflakeConfiguration.Default);

		m_ConnectionTimeout = (int)SfSession.m_ConnectionTimeout.TotalSeconds;

		m_ConnectionState = ConnectionState.Connecting;
	}

	private void OnSessionEstablished()
	{
		m_ConnectionState = ConnectionState.Open;
	}
}
