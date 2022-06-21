using NUnit.Framework;
using System.Data;

namespace Tortuga.Data.Snowflake.Tests;

[TestFixture]
class SFDbCommandAsynchronous : SFBaseTest
{
	SnowflakeDbConnection StartSnowflakeConnection()
	{
		var conn = new SnowflakeDbConnection();
		conn.ConnectionString = ConnectionString;

		conn.Open();

		return conn;
	}

	[Test]
	public void TestLongRunningQuery()
	{
		string queryId;
		using (var conn = StartSnowflakeConnection())
		{
			using (var cmd = (SnowflakeDbCommand)conn.CreateCommand())
			{
				cmd.CommandText = "select count(seq4()) from table(generator(timelimit => 15)) v order by 1";
				var status = cmd.StartAsynchronousQuery();
				Assert.False(status.IsQueryDone);
				Assert.False(status.IsQuerySuccessful);
				queryId = status.QueryId;
			}

			Assert.IsNotEmpty(queryId);
		}

		// start a new connection to make sure works across sessions
		using (var conn = StartSnowflakeConnection())
		{
			SnowflakeDbQueryStatus status;
			do
			{
				status = SnowflakeDbAsynchronousQueryHelper.GetQueryStatus(conn, queryId);
				if (status.IsQueryDone)
				{
					break;
				}
				else
				{
					Assert.False(status.IsQuerySuccessful);
				}

				Thread.Sleep(5000);
			} while (true);

			// once it finished, it should be successfull
			Assert.True(status.IsQuerySuccessful);
		}

		// start a new connection to make sure works across sessions
		using (var conn = StartSnowflakeConnection())
		{
			using (var cmd = SnowflakeDbAsynchronousQueryHelper.CreateQueryResultsCommand(conn, queryId))
			{
				using (IDataReader reader = cmd.ExecuteReader())
				{
					// only one result is returned
					Assert.IsTrue(reader.Read());
				}
			}

			conn.Close();
		}
	}

	[Test]
	public void TestSimpleCommand()
	{
		string queryId;

		using (var conn = StartSnowflakeConnection())
		{
			using (var cmd = (SnowflakeDbCommand)conn.CreateCommand())
			{
				cmd.CommandText = "select 1";

				var status = cmd.StartAsynchronousQuery();
				// even a fast asynchronous call will not be done initially
				Assert.False(status.IsQueryDone);
				Assert.False(status.IsQuerySuccessful);
				queryId = status.QueryId;

				Assert.IsNotEmpty(queryId);
			}
		}

		// start a new connection to make sure works across sessions
		using (var conn = StartSnowflakeConnection())
		{
			SnowflakeDbQueryStatus status;
			status = SnowflakeDbAsynchronousQueryHelper.GetQueryStatus(conn, queryId);
			// since query is so fast, expect it to be done the first time we check the status
			Assert.True(status.IsQueryDone);
			Assert.True(status.IsQuerySuccessful);
		}

		// start a new connection to make sure works across sessions
		using (var conn = StartSnowflakeConnection())
		{
			// because this query is so quick, we do not need to check the status before fetching the result

			using (var cmd = SnowflakeDbAsynchronousQueryHelper.CreateQueryResultsCommand(conn, queryId))
			{
				var val = cmd.ExecuteScalar();

				Assert.AreEqual(1L, (long)val);
			}

			conn.Close();
		}
	}
}
