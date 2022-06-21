/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using NUnit.Framework;
using System.Data;

namespace Tortuga.Data.Snowflake.Tests;

[TestFixture]
class SFDbCommandITSlow : SFBaseTest
{
	[Test]
	public void TestLongRunningQuery()
	{
		using (var conn = new SnowflakeConnection())
		{
			conn.ConnectionString = ConnectionString;

			conn.Open();

			IDbCommand cmd = conn.CreateCommand();
			cmd.CommandText = "select count(seq4()) from table(generator(timelimit => 60)) v order by 1";
			IDataReader reader = cmd.ExecuteReader();
			// only one result is returned
			Assert.IsTrue(reader.Read());

			conn.Close();
		}
	}

	[Test]
	public void TestRowsAffectedOverflowInt()
	{
		using (var conn = new SnowflakeConnection())
		{
			conn.ConnectionString = ConnectionString;
			conn.Open();

			using (IDbCommand command = conn.CreateCommand())
			{
				command.CommandText = "create or replace table test_rows_affected_overflow(c1 number)";
				command.ExecuteNonQuery();

				command.CommandText = "insert into test_rows_affected_overflow select seq4() from table(generator(rowcount=>2147484000))";
				int affected = command.ExecuteNonQuery();

				Assert.AreEqual(-1, affected);

				command.CommandText = "drop table if exists test_rows_affected_overflow";
				command.ExecuteNonQuery();
			}
			conn.Close();
		}
	}
}
