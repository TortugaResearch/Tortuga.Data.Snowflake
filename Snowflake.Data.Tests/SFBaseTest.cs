/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using NUnit.Framework;

namespace Tortuga.Data.Snowflake.Tests;

/*
* This is the base class for all tests that call blocking methods in the library - it uses MockSynchronizationContext to verify that
* there are no async deadlocks in the library
*
*/

[TestFixture]
public class SFBaseTest : SFBaseTestAsync
{
	[SetUp]
	public static void SetUpContext()
	{
		MockSynchronizationContext.SetupContext();
	}

	[TearDown]
	public static void TearDownContext()
	{
		MockSynchronizationContext.Verify();
	}
}
