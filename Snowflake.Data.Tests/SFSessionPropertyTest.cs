/*
 * Copyright (c) 2019 Snowflake Computing Inc. All rights reserved.
 */

using NUnit.Framework;
using System.Security;
using Tortuga.Data.Snowflake.Core;

namespace Tortuga.Data.Snowflake.Tests;

/// <summary>
/// The purpose of these testcases is to test if the connections string
/// can be parsed correctly into properties for a session.
/// </summary>
class SFSessionPropertyTest
{
	private class Testcase
	{
		public string ConnectionString { get; set; }
		public SecureString SecurePassword { get; set; }
		public SFSessionProperties ExpectedProperties { get; set; }

		public void TestValidCase()
		{
			SFSessionProperties actualProperties = SFSessionProperties.parseConnectionString(ConnectionString, SecurePassword);
			Assert.AreEqual(actualProperties, ExpectedProperties);
		}
	}

	[Test]
	public void TestValidConnectionString()
	{
		Testcase[] testcases = new Testcase[]
		{
				new Testcase()
				{
					ConnectionString = "ACCOUNT=testaccount;USER=testuser;PASSWORD=123;",
					ExpectedProperties = new SFSessionProperties()
					{
						{ SFSessionProperty.ACCOUNT, "testaccount" },
						{ SFSessionProperty.USER, "testuser" },
						{ SFSessionProperty.HOST, "testaccount.snowflakecomputing.com" },
						{ SFSessionProperty.AUTHENTICATOR, "snowflake" },
						{ SFSessionProperty.SCHEME, "https" },
						{ SFSessionProperty.CONNECTION_TIMEOUT, "120" },
						{ SFSessionProperty.PASSWORD, "123" },
						{ SFSessionProperty.PORT, "443" },
						{ SFSessionProperty.VALIDATE_DEFAULT_PARAMETERS, "true" },
						{ SFSessionProperty.USEPROXY, "false" },
						{ SFSessionProperty.INSECUREMODE, "false" },
					},
				},
		};

		foreach (Testcase testcase in testcases)
		{
			testcase.TestValidCase();
		}
	}
}
