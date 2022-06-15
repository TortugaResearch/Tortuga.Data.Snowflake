/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;
using System.Reflection;

namespace Tortuga.Data.Snowflake.Tests;

/*
* This is the base class for all tests that call async metodes in the library - it does not use a special SynchronizationContext
*
*/

[SetUpFixture]
public class SFBaseTestAsync
{
	protected string ConnectionStringWithoutAuth
	{
		get
		{
			return $"scheme={TestConfig.Protocol};host={TestConfig.Host};port={TestConfig.Port};account={TestConfig.Account};db={TestConfig.Database};schema={TestConfig.Schema}";
		}
	}

	protected string ConnectionString
	{
		get
		{
			return ConnectionStringWithoutAuth + $";user={TestConfig.User};password={TestConfig.Password};";
		}
	}

	protected TestConfig TestConfig { get; set; }

	[OneTimeSetUp]
	public void SFTestSetup()
	{
		var cloud = Environment.GetEnvironmentVariable("snowflake_cloud_env");
		Assert.IsTrue(cloud == null || cloud == "AWS" || cloud == "AZURE" || cloud == "GCP", "{0} is not supported. Specify AWS, AZURE or GCP as cloud environment", cloud);

		var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "parameters.json");
		var reader = new StreamReader(path);

		var testConfigString = reader.ReadToEnd();

		// Local JSON settings to avoid using system wide settings which could be different
		// than the default ones
		var JsonSettings = new JsonSerializerSettings()
		{
			ContractResolver = new DefaultContractResolver()
			{
				NamingStrategy = new DefaultNamingStrategy()
			}
		};
		var testConfigs = JsonConvert.DeserializeObject<Dictionary<string, TestConfig>>(testConfigString, JsonSettings);

		if (testConfigs.TryGetValue("testconnection", out var testConnectionConfig))
		{
			TestConfig = testConnectionConfig;
		}
		else
		{
			Assert.Fail("Failed to load test configuration");
		}
	}
}
