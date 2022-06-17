/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using NUnit.Framework;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Tortuga.Data.Snowflake.Tests.Mock;

namespace Tortuga.Data.Snowflake.Tests;

[TestFixture]
class SFConnectionITAsync : SFBaseTestAsync
{
	[Test]
	public void TestCancelLoginBeforeTimeout()
	{
		using (var conn = new MockSnowflakeDbConnection())
		{
			// No timeout
			var timeoutSec = 0;
			var infiniteLoginTimeOut = $"{ConnectionString};connection_timeout={timeoutSec}";

			conn.ConnectionString = infiniteLoginTimeOut;

			Assert.AreEqual(conn.State, ConnectionState.Closed);
			// At this point the connection string has not been parsed, it will return the
			// default value
			//Assert.AreEqual(120, conn.ConnectionTimeout);

			var connectionCancelToken = new CancellationTokenSource();
			var connectTask = conn.OpenAsync(connectionCancelToken.Token);

			// Sleep for 130 sec (more than the default connection timeout and the httpclient
			// timeout to make sure there are no false positive )
			Thread.Sleep(130 * 1000);

			Assert.AreEqual(ConnectionState.Connecting, conn.State);

			// Cancel the connection because it will never succeed since there is no
			// connection_timeout defined
			connectionCancelToken.Cancel();

			try
			{
				connectTask.Wait();
			}
			catch (AggregateException e)
			{
				Assert.AreEqual("System.Threading.Tasks.TaskCanceledException", e.InnerException.GetType().ToString());
			}

			Assert.AreEqual(ConnectionState.Closed, conn.State);
			Assert.AreEqual(0, conn.ConnectionTimeout);
		}
	}

	[Test]
	public void TestAsyncLoginTimeout()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			using (var conn = new MockSnowflakeDbConnection())
			{
				var timeoutSec = 5;
				var loginTimeOut5sec = $"{ConnectionString};connection_timeout={timeoutSec}";
				conn.ConnectionString = loginTimeOut5sec;

				Assert.AreEqual(conn.State, ConnectionState.Closed);

				var connectionCancelToken = new CancellationTokenSource();
				var stopwatch = Stopwatch.StartNew();
				try
				{
					var connectTask = conn.OpenAsync(connectionCancelToken.Token);
					connectTask.Wait();
				}
				catch (AggregateException e)
				{
					Assert.AreEqual(SnowflakeError.RequestTimeout, ((SnowflakeDbException)e.InnerException).SnowflakeError);
				}
				stopwatch.Stop();

				// Should timeout after 5sec
				Assert.GreaterOrEqual(stopwatch.ElapsedMilliseconds, 5 * 1000 - 30); //The -30 is to account for rounding errors in the Windows timer

				Assert.LessOrEqual(stopwatch.ElapsedMilliseconds, (6) * 1000); // But never more than 1 sec (max backoff) after the default timeout

				Assert.AreEqual(ConnectionState.Closed, conn.State);
				Assert.AreEqual(5, conn.ConnectionTimeout);
			}
		}
	}

	[Test]
	public void TestAsyncDefaultLoginTimeout()
	{
		using (var conn = new MockSnowflakeDbConnection())
		{
			conn.ConnectionString = ConnectionString;

			Assert.AreEqual(conn.State, ConnectionState.Closed);

			var connectionCancelToken = new CancellationTokenSource();
			var stopwatch = Stopwatch.StartNew();
			try
			{
				var connectTask = conn.OpenAsync(connectionCancelToken.Token);
				connectTask.Wait();
			}
			catch (AggregateException e)
			{
				Assert.AreEqual(SnowflakeError.RequestTimeout, ((SnowflakeDbException)e.InnerException).SnowflakeError);
			}
			stopwatch.Stop();

			// Should timeout after the default timeout (120 sec)
			Assert.GreaterOrEqual(stopwatch.ElapsedMilliseconds, 120 * 1000);
			// But never more than 16 sec (max backoff) after the default timeout
			Assert.LessOrEqual(stopwatch.ElapsedMilliseconds, (120 + 16) * 1000);

			Assert.AreEqual(ConnectionState.Closed, conn.State);
			Assert.AreEqual(120, conn.ConnectionTimeout);
		}
	}

	[Test]
	public void TestAsyncConnectionFailFast()
	{
		using (var conn = new SnowflakeDbConnection())
		{
			// Just a way to get a 404 on the login request and make sure there are no retry
			var invalidConnectionString = "host=docs.microsoft.com;connection_timeout=0;account=testFailFast;user=testFailFast;password=testFailFast;";

			conn.ConnectionString = invalidConnectionString;

			Assert.AreEqual(conn.State, ConnectionState.Closed);
			var connectionCancelToken = new CancellationTokenSource();
			Task connectTask = null;
			try
			{
				connectTask = conn.OpenAsync(connectionCancelToken.Token);
				connectTask.Wait();
				Assert.Fail();
			}
			catch (AggregateException e)
			{
				Assert.AreEqual(SnowflakeError.InternalError, ((SnowflakeDbException)e.InnerException).SnowflakeError);
			}

			Assert.AreEqual(ConnectionState.Closed, conn.State);
			Assert.IsTrue(connectTask.IsFaulted);
		}
	}

	[Test]
	public void TestCloseAsync()
	{
		// https://docs.microsoft.com/en-us/dotnet/api/system.data.common.dbconnection.close
		// https://docs.microsoft.com/en-us/dotnet/api/system.data.common.dbconnection.closeasync
		// An application can call Close or CloseAsync more than one time.
		// No exception is generated.
		using (var conn = new SnowflakeDbConnection())
		{
			conn.ConnectionString = ConnectionString;
			Assert.AreEqual(conn.State, ConnectionState.Closed);
			Task task = null;

			// Close the connection. It's not opened yet, but it should not have any issue
			task = conn.CloseAsync(new CancellationTokenSource().Token);
			task.Wait();
			Assert.AreEqual(conn.State, ConnectionState.Closed);

			// Open the connection
			task = conn.OpenAsync(new CancellationTokenSource().Token);
			task.Wait();
			Assert.AreEqual(conn.State, ConnectionState.Open);

			// Close the opened connection
			task = conn.CloseAsync(new CancellationTokenSource().Token);
			task.Wait();
			Assert.AreEqual(conn.State, ConnectionState.Closed);

			// Close the connection again.
			task = conn.CloseAsync(new CancellationTokenSource().Token);
			task.Wait();
			Assert.AreEqual(conn.State, ConnectionState.Closed);
		}
	}

	[Test]
	public async Task TestCloseAsyncFailure()
	{
		using (var conn = new MockSnowflakeDbConnection(new MockCloseSessionException()))
		{
			conn.ConnectionString = ConnectionString;
			Assert.AreEqual(conn.State, ConnectionState.Closed);
			Task task = null;

			// Open the connection
			task = conn.OpenAsync(new CancellationTokenSource().Token);
			task.Wait();
			Assert.AreEqual(conn.State, ConnectionState.Open);

			// Close the opened connection
			try
			{
				await conn.CloseAsync(new CancellationTokenSource().Token);
				Assert.Fail();
			}
			catch (SnowflakeDbException e)
			{
				Assert.AreEqual(MockCloseSessionException.SESSION_CLOSE_ERROR, e.ErrorCode);
			}
			Assert.AreEqual(ConnectionState.Open, conn.State);
		}
	}
}
