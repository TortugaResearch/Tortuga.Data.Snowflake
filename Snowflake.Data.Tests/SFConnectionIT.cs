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
class SFConnectionIT : SFBaseTest
{
	[Test]
	public void TestBasicConnection()
	{
		using (var conn = new SnowflakeConnection())
		{
			conn.ConnectionString = ConnectionString;
			conn.Open();
			Assert.AreEqual(ConnectionState.Open, conn.State);

			Assert.AreEqual(120, conn.ConnectionTimeout);
			// Data source is empty string for now
			Assert.AreEqual("", ((SnowflakeConnection)conn).DataSource);

			var serverVersion = ((SnowflakeConnection)conn).ServerVersion;
			if (!string.Equals(serverVersion, "Dev"))
			{
				var versionElements = serverVersion.Split('.');
				Assert.AreEqual(3, versionElements.Length);
			}

			conn.Close();
			Assert.AreEqual(ConnectionState.Closed, conn.State);
		}
	}

	[Test]
	public void TestApplicationName()
	{
		var validApplicationNames = new[] { "test1234", "test_1234", "test-1234", "test.1234" };
		var invalidApplicationNames = new[] { "1234test", "test$A", "test<script>" };

		// Valid names
		foreach (var appName in validApplicationNames)
		{
			using (var conn = new SnowflakeConnection())
			{
				conn.ConnectionString = ConnectionString;
				conn.ConnectionString += $"application={appName}";
				conn.Open();
				Assert.AreEqual(ConnectionState.Open, conn.State);

				conn.Close();
				Assert.AreEqual(ConnectionState.Closed, conn.State);
			}
		}

		// Invalid names
		foreach (var appName in invalidApplicationNames)
		{
			using (var conn = new SnowflakeConnection())
			{
				conn.ConnectionString = ConnectionString;
				conn.ConnectionString += $"application={appName}";
				try
				{
					conn.Open();
					Assert.Fail();
				}
				catch (SnowflakeException e)
				{
					// Expected
					Assert.AreEqual("08006", e.SqlState); // Connection failure
				}

				Assert.AreEqual(ConnectionState.Closed, conn.State);
			}
		}
	}

	[Test]
	public void TestIncorrectUserOrPasswordBasicConnection()
	{
		using (var conn = new SnowflakeConnection())
		{
			conn.ConnectionString = $"scheme={TestConfig.Protocol};host={TestConfig.Host};port={TestConfig.Port};" +
		$"account={TestConfig.Account};role={TestConfig.Role};db={TestConfig.Database};schema={TestConfig.Schema};warehouse={TestConfig.Warehouse};user={"unknown"};password={TestConfig.Password};";

			Assert.AreEqual(conn.State, ConnectionState.Closed);
			try
			{
				conn.Open();
				Assert.Fail();
			}
			catch (SnowflakeException e)
			{
				// Expected
				Assert.AreEqual("08006", e.SqlState); // Connection failure
			}

			Assert.AreEqual(ConnectionState.Closed, conn.State);
		}
	}

	public void TestCrlCheckSwitchConnection()
	{
		using (var conn = new SnowflakeConnection())
		{
			conn.ConnectionString = ConnectionString + ";INSECUREMODE=true";
			conn.Open();
			Assert.AreEqual(ConnectionState.Open, conn.State);
		}

		using (var conn = new SnowflakeConnection())
		{
			conn.ConnectionString = ConnectionString;
			conn.Open();
			Assert.AreEqual(ConnectionState.Open, conn.State);
		}

		using (var conn = new SnowflakeConnection())
		{
			conn.ConnectionString = ConnectionString + ";INSECUREMODE=false";
			conn.Open();
			Assert.AreEqual(ConnectionState.Open, conn.State);
		}

		using (var conn = new SnowflakeConnection())
		{
			conn.ConnectionString = ConnectionString;
			conn.Open();
			Assert.AreEqual(ConnectionState.Open, conn.State);
		}

		using (var conn = new SnowflakeConnection())
		{
			conn.ConnectionString = ConnectionString + ";INSECUREMODE=false";
			conn.Open();
			Assert.AreEqual(ConnectionState.Open, conn.State);
		}

		using (var conn = new SnowflakeConnection())
		{
			conn.ConnectionString = ConnectionString + ";INSECUREMODE=true";
			conn.Open();
			Assert.AreEqual(ConnectionState.Open, conn.State);
		}
	}

	[Test]
	public void TestConnectViaSecureString()
	{
		var connEntries = ConnectionString.Split(';');
		var connectionStringWithoutPassword = "";
		using (var conn = new SnowflakeConnection())
		{
			var password = new System.Security.SecureString();
			foreach (var entry in connEntries)
			{
				if (!entry.StartsWith("password="))
				{
					connectionStringWithoutPassword += entry;
					connectionStringWithoutPassword += ';';
				}
				else
				{
					var pass = entry.Substring(9);
					foreach (var c in pass)
					{
						password.AppendChar(c);
					}
				}
			}
			conn.ConnectionString = connectionStringWithoutPassword;
			conn.Password = password;
			conn.Open();

			Assert.AreEqual(TestConfig.Database.ToUpper(), conn.Database);
			Assert.AreEqual(conn.State, ConnectionState.Open);

			conn.Close();
		}
	}

	[Test]
	public void TestLoginTimeout()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			using (var conn = new MockSnowflakeDbConnection())
			{
				var timeoutSec = 5;
				var loginTimeOut5sec = $"{ConnectionString};connection_timeout={timeoutSec}";

				conn.ConnectionString = loginTimeOut5sec;

				Assert.AreEqual(conn.State, ConnectionState.Closed);
				var stopwatch = Stopwatch.StartNew();
				try
				{
					conn.Open();
					Assert.Fail("Timeout exception did not occur");
				}
				catch (SnowflakeException e)
				{
					Assert.AreEqual(SnowflakeError.RequestTimeout, e.SnowflakeError);
				}
				stopwatch.Stop();

				//Should timeout before the default timeout (120 sec) * 1000
				Assert.Less(stopwatch.ElapsedMilliseconds, 120 * 1000);
				// Should timeout after the defined connection timeout
				Assert.GreaterOrEqual(stopwatch.ElapsedMilliseconds, timeoutSec * 1000);
				Assert.AreEqual(5, conn.ConnectionTimeout);
			}
		}
	}

	[Test]
	public async Task TestLoginTimeoutAsync()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			using (var conn = new MockSnowflakeDbConnection())
			{
				var timeoutSec = 5;
				var loginTimeOut5sec = string.Format(ConnectionString + "connection_timeout={0}", timeoutSec);

				conn.ConnectionString = loginTimeOut5sec;

				Assert.AreEqual(conn.State, ConnectionState.Closed);
				var stopwatch = Stopwatch.StartNew();
				try
				{
					await conn.OpenAsync().ConfigureAwait(false);
					Assert.Fail("Connection did not timeout");
				}
				catch (SnowflakeException e)
				{
					Assert.AreEqual(SnowflakeError.RequestTimeout, e.SnowflakeError);
				}
				stopwatch.Stop();

				//Should timeout before the default timeout (120 sec) * 1000
				Assert.Less(stopwatch.ElapsedMilliseconds, 120 * 1000);
				// Should timeout after the defined connection timeout
				Assert.GreaterOrEqual(stopwatch.ElapsedMilliseconds, timeoutSec * 1000);
				Assert.AreEqual(5, conn.ConnectionTimeout);
			}
		}
	}

	[Test]
	public void TestDefaultLoginTimeout()
	{
		using (var conn = new MockSnowflakeDbConnection())
		{
			conn.ConnectionString = ConnectionString;

			// Default timeout is 120 sec
			Assert.AreEqual(120, conn.ConnectionTimeout);

			Assert.AreEqual(conn.State, ConnectionState.Closed);
			var stopwatch = Stopwatch.StartNew();
			try
			{
				conn.Open();
				Assert.Fail("Connection did not timeout");
			}
			catch (SnowflakeException e)
			{
				Assert.AreEqual(SnowflakeError.RequestTimeout, e.SnowflakeError);

				stopwatch.Stop();
				// Should timeout after the default timeout (120 sec)
				Assert.GreaterOrEqual(stopwatch.ElapsedMilliseconds, 120 * 1000);
				// But never more than 16 sec (max backoff) after the default timeout
				Assert.LessOrEqual(stopwatch.ElapsedMilliseconds, (120 + 16) * 1000);
			}
		}
	}

	[Test]
	public void TestConnectionFailFast()
	{
		using (var conn = new SnowflakeConnection())
		{
			// Just a way to get a 404 on the login request and make sure there are no retry
			var invalidConnectionString = "host=docs.microsoft.com;connection_timeout=0;account=testFailFast;user=testFailFast;password=testFailFast;";

			conn.ConnectionString = invalidConnectionString;

			Assert.AreEqual(conn.State, ConnectionState.Closed);
			try
			{
				conn.Open();
				Assert.Fail();
			}
			catch (SnowflakeException e)
			{
				Assert.AreEqual(SnowflakeError.InternalError, e.SnowflakeError);
			}

			Assert.AreEqual(ConnectionState.Closed, conn.State);
		}
	}

	[Test]
	public void TestValidateDefaultParameters()
	{
		var connectionString = $"scheme={TestConfig.Protocol};host={TestConfig.Host};port={TestConfig.Port};" +
		$"account={TestConfig.Account};role={TestConfig.Role};db={TestConfig.Database};schema={TestConfig.Schema};warehouse={"WAREHOUSE_NEVER_EXISTS"};user={TestConfig.User};password={TestConfig.Password};";

		// By default should validate parameters
		using (var conn = new SnowflakeConnection())
		{
			try
			{
				conn.ConnectionString = connectionString;
				conn.Open();
				Assert.Fail();
			}
			catch (SnowflakeException e)
			{
				Assert.AreEqual(390201, e.ErrorCode);
			}
		}

		// This should succeed
		using (var conn = new SnowflakeConnection())
		{
			conn.ConnectionString = connectionString + ";VALIDATE_DEFAULT_PARAMETERS=false";
			conn.Open();
		}
	}

	[Test]
	public void TestInvalidConnectionString()
	{
		var invalidStrings = new[] {
		// missing required connection property password
		"ACCOUNT=testaccount;user=testuser",
		// invalid account value
		"ACCOUNT=A=C;USER=testuser;password=123",
			"complete_invalid_string",
		};

		var expectedErrorCode = new[] { SnowflakeError.MissingConnectionProperty, SnowflakeError.InvalidConnectionString, SnowflakeError.InvalidConnectionString };

		using (var conn = new SnowflakeConnection())
		{
			for (var i = 0; i < invalidStrings.Length; i++)
			{
				try
				{
					conn.ConnectionString = invalidStrings[i];
					conn.Open();
					Assert.Fail();
				}
				catch (SnowflakeException e)
				{
					Assert.AreEqual(expectedErrorCode[i], e.SnowflakeError);
				}
			}
		}
	}

	[Test]
	public void TestUnknownConnectionProperty()
	{
		using (var conn = new SnowflakeConnection())
		{
			// invalid propety will be ignored.
			conn.ConnectionString = ConnectionString + ";invalidProperty=invalidvalue;";

			conn.Open();
			Assert.AreEqual(conn.State, ConnectionState.Open);
			conn.Close();
		}
	}

	[Test]
	[IgnoreOnEnvIs("snowflake_cloud_env", new string[] { "AZURE", "GCP" })]
	public void TestSwitchDb()
	{
		using (var conn = new SnowflakeConnection())
		{
			conn.ConnectionString = ConnectionString;

			Assert.AreEqual(conn.State, ConnectionState.Closed);

			conn.Open();

			Assert.AreEqual(TestConfig.Database.ToUpper(), conn.Database);
			Assert.AreEqual(conn.State, ConnectionState.Open);

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				conn.ChangeDatabase("SNOWFLAKE_SAMPLE_DATA");
				Assert.AreEqual("SNOWFLAKE_SAMPLE_DATA", conn.Database);
			}

			conn.Close();
		}
	}

	[Test]
	public void TestConnectWithoutHost()
	{
		using (var conn = new SnowflakeConnection())
		{
			var connStrFmt = "account={0};user={1};password={2}";
			conn.ConnectionString = string.Format(connStrFmt, TestConfig.Account,
				TestConfig.User, TestConfig.Password);
			// Check that connection succeeds if host is not specified in test configs, i.e. default should work.
			if (string.IsNullOrEmpty(TestConfig.Host))
			{
				conn.Open();
				Assert.AreEqual(conn.State, ConnectionState.Open);
				conn.Close();
			}
		}
	}

	[Test]
	public void TestConnectWithDifferentRole()
	{
		using (var conn = new SnowflakeConnection())
		{
			var host = TestConfig.Host;
			if (string.IsNullOrEmpty(host))
			{
				host = $"{TestConfig.Account}.snowflakecomputing.com";
			}

			conn.ConnectionString = $"scheme={TestConfig.Protocol};host={TestConfig.Host};port={TestConfig.Port};user={TestConfig.User};password={TestConfig.Password};account={TestConfig.Account};role=public;db=snowflake_sample_data;schema=information_schema;warehouse=WH_NOT_EXISTED;validate_default_parameters=false";
			conn.Open();
			Assert.AreEqual(conn.State, ConnectionState.Open);

			using (var command = conn.CreateCommand())
			{
				command.CommandText = "select current_role()";
				Assert.AreEqual(command.ExecuteScalar().ToString(), "PUBLIC");

				command.CommandText = "select current_database()";
				CollectionAssert.Contains(new[] { "SNOWFLAKE_SAMPLE_DATA", "" }, command.ExecuteScalar().ToString());

				command.CommandText = "select current_schema()";
				CollectionAssert.Contains(new[] { "INFORMATION_SCHEMA", "" }, command.ExecuteScalar().ToString());

				command.CommandText = "select current_warehouse()";
				// Command will return empty string if the hardcoded warehouse does not exist.
				Assert.AreEqual("", command.ExecuteScalar().ToString());
			}
			conn.Close();
		}
	}

	// Test that when a connection is disposed, a close would send out and unfinished transaction would be roll back.
	[Test]
	public void TestConnectionDispose()
	{
		using (var conn = new SnowflakeConnection())
		{
			// Setup
			conn.ConnectionString = ConnectionString;
			conn.Open();
			var command = conn.CreateCommand();
			command.CommandText = "create or replace table testConnDispose(c int)";
			command.ExecuteNonQuery();

			var t1 = conn.BeginTransaction();
			var t1c1 = conn.CreateCommand();
			t1c1.Transaction = t1;
			t1c1.CommandText = "insert into testConnDispose values (1)";
			t1c1.ExecuteNonQuery();
		}

		using (var conn = new SnowflakeConnection())
		{
			// Previous connection would be disposed and
			// uncommitted txn would rollback at this point
			conn.ConnectionString = ConnectionString;
			conn.Open();
			var command = conn.CreateCommand();
			command.CommandText = "SELECT * FROM testConnDispose";
			var reader = command.ExecuteReader();
			Assert.IsFalse(reader.Read());

			// Cleanup
			command.CommandText = "DROP TABLE IF EXISTS testConnDispose";
			command.ExecuteNonQuery();
		}
	}

	[Test]
	public void TestUnknownAuthenticator()
	{
		var wrongAuthenticators = new[]
		{
			"http://snowflakecomputing.okta.com",
			"https://snowflake.com",
			"unknown",
	};

		foreach (var wrongAuthenticator in wrongAuthenticators)
		{
			try
			{
				var conn = new SnowflakeConnection() { ConnectionString = "scheme=http;host=test;port=8080;user=test;password=test;account=test;authenticator=" + wrongAuthenticator };
				conn.Open();
				Assert.Fail("Authentication of {0} should fail", wrongAuthenticator);
			}
			catch (SnowflakeException e)
			{
				Assert.AreEqual(SnowflakeError.UnknownAuthenticator, e.SnowflakeError);
			}
		}
	}

	[Test]
	public void TestInValidOAuthTokenConnection()
	{
		try
		{
			using (var conn = new SnowflakeConnection())
			{
				conn.ConnectionString
					= ConnectionStringWithoutAuth
					+ ";authenticator=oauth;token=notAValidOAuthToken";
				conn.Open();
				Assert.AreEqual(ConnectionState.Open, conn.State);
				Assert.Fail();
			}
		}
		catch (SnowflakeException e)
		{
			// Invalid OAuth access token
			Assert.AreEqual(390303, e.ErrorCode);
		}
	}

	[Test]
	public void TestInvalidProxySettingFromConnectionString()
	{
		using (var conn = new SnowflakeConnection())
		{
			conn.ConnectionString =
				ConnectionString + "connection_timeout=5;useProxy=true;proxyHost=Invalid;proxyPort=8080";
			try
			{
				conn.Open();
				Assert.Fail();
			}
			catch (SnowflakeException e)
			{
				// Expected
				Assert.AreEqual(SnowflakeError.RequestTimeout, e.SnowflakeError);
			}
		}
	}

	[Test]
	public void TestUseProxyFalseWithInvalidProxyConnectionString()
	{
		using (var conn = new SnowflakeConnection())
		{
			conn.ConnectionString =
				ConnectionString + ";useProxy=false;proxyHost=Invalid;proxyPort=8080";
			conn.Open();
			// Because useProxy=false, the proxy settings are ignored
		}
	}

	[Test]
	public void TestInvalidProxySettingWithByPassListFromConnectionString()
	{
		using (var conn = new SnowflakeConnection())
		{
			conn.ConnectionString = $"{ConnectionString};useProxy=true;proxyHost=Invalid;proxyPort=8080;nonProxyHosts=*.foo.com %7C{TestConfig.Account}.snowflakecomputing.com|localhost";
			conn.Open();
			// Because testConfig.host is in the bypass list, the proxy should not be used
		}
	}
}
