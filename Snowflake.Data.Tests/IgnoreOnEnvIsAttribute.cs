/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Tortuga.Data.Snowflake.Tests;

[AttributeUsage(AttributeTargets.Method)]
public class IgnoreOnEnvIsAttribute : Attribute, ITestAction
{
	readonly string key;

	readonly string[] values;

	public IgnoreOnEnvIsAttribute(string key, string[] values)
	{
		this.key = key;
		this.values = values;
	}

	public void BeforeTest(ITest test)
	{
		foreach (var value in values)
		{
			if (Environment.GetEnvironmentVariable(key) == value)
			{
				Assert.Ignore("Test is ignored when environment variable {0} is {1} ", key, value);
			}
		}
	}

	public void AfterTest(ITest test)
	{
	}

	public ActionTargets Targets
	{
		get { return ActionTargets.Test | ActionTargets.Suite; }
	}
}
