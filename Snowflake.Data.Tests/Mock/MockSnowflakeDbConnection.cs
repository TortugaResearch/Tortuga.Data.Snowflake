/*
 * Copyright (c) 2021 Snowflake Computing Inc. All rights reserved.
 */

using System.Data;
using Tortuga.Data.Snowflake.Core.RequestProcessing;
using Tortuga.Data.Snowflake.Core.Sessions;

namespace Tortuga.Data.Snowflake.Tests.Mock;

class MockSnowflakeDbConnection : SnowflakeDbConnection
{
	private IMockRestRequester _restRequester;

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
		}
		catch
		{
			// Otherwise when Dispose() is called, the close request would timeout.
			m_ConnectionState = System.Data.ConnectionState.Closed;
			throw;
		}
		OnSessionEstablished();
	}

	public override Task OpenAsync(CancellationToken cancellationToken)
	{
		RegisterConnectionCancellationCallback(cancellationToken);

		SetMockSession();

		return SfSession.OpenAsync(cancellationToken).ContinueWith(
			previousTask =>
			{
				if (previousTask.IsFaulted)
				{
					// Exception from SfSession.OpenAsync
					Exception sfSessionEx = previousTask.Exception;
					m_ConnectionState = ConnectionState.Closed;
					throw new SnowflakeDbException(sfSessionEx.InnerException, SFError.INTERNAL_ERROR, "Unable to connect");
				}
				if (previousTask.IsCanceled)
				{
					m_ConnectionState = ConnectionState.Closed;
				}
				else
				{
					OnSessionEstablished();
				}
			},
			cancellationToken);
	}

	private void SetMockSession()
	{
		SfSession = new SFSession(ConnectionString, Password, _restRequester);

		m_ConnectionTimeout = (int)SfSession.connectionTimeout.TotalSeconds;

		m_ConnectionState = ConnectionState.Connecting;
	}

	private void OnSessionEstablished()
	{
		m_ConnectionState = ConnectionState.Open;
	}
}
