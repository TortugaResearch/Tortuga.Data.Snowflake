/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using NUnit.Framework;
using System.Data;
using System.Data.Common;

namespace Tortuga.Data.Snowflake.Tests;

[TestFixture]
class SFDbCommandITAsync : SFBaseTestAsync
{
	[Test]
	public void TestExecAsyncAPI()
	{
		using (DbConnection conn = new SFConnection())
		{
			conn.ConnectionString = ConnectionString;

			Task connectTask = conn.OpenAsync(CancellationToken.None);
			Assert.AreEqual(ConnectionState.Connecting, conn.State);

			connectTask.Wait();
			Assert.AreEqual(ConnectionState.Open, conn.State);

			using (DbCommand cmd = conn.CreateCommand())
			{
				int queryResult = 0;
				cmd.CommandText = "select count(seq4()) from table(generator(timelimit => 3)) v";
				Task<DbDataReader> execution = cmd.ExecuteReaderAsync();
				Task readCallback = execution.ContinueWith((t) =>
				{
					using (DbDataReader reader = t.Result)
					{
						Assert.IsTrue(reader.Read());
						queryResult = reader.GetInt32(0);
						Assert.IsFalse(reader.Read());
					}
				});
				// query is not finished yet, result is still 0;
				Assert.AreEqual(0, queryResult);
				// block till query finished
				readCallback.Wait();
				// queryResult should be updated by callback
				Assert.AreNotEqual(0, queryResult);
			}

			conn.Close();
		}
	}

	[Test]
	public void TestCancelExecuteAsync()
	{
		CancellationTokenSource externalCancel = new CancellationTokenSource(TimeSpan.FromSeconds(8));

		using (DbConnection conn = new SFConnection())
		{
			conn.ConnectionString = ConnectionString;

			conn.Open();

			DbCommand cmd = conn.CreateCommand();
			cmd.CommandText = "select count(seq4()) from table(generator(timelimit => 20)) v";
			// external cancellation should be triggered before timeout
			cmd.CommandTimeout = 10;
			try
			{
				Task<object> t = cmd.ExecuteScalarAsync(externalCancel.Token);
				t.Wait();
				Assert.Fail();
			}
			catch
			{
				// assert that cancel is not triggered by timeout, but external cancellation
				Assert.IsTrue(externalCancel.IsCancellationRequested);
			}
			Thread.Sleep(2000);
			conn.Close();
		}
	}
}
