/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using NUnit.Framework;
using Tortuga.Data.Snowflake.Core.RequestProcessing;
using Tortuga.Data.Snowflake.Core.ResponseProcessing;
using Tortuga.Data.Snowflake.Core.Sessions;

namespace Tortuga.Data.Snowflake.Tests;

/**
 * Mock rest request test
 */

[TestFixture]
class SFStatementTest
{
	// Mock test for session token renew
	[Test]
	public void TestSessionRenew()
	{
		Mock.MockRestSessionExpired restRequester = new Mock.MockRestSessionExpired();
		SFSession sfSession = new SFSession("account=test;user=test;password=test", null, restRequester, SnowflakeDbConfiguration.Default);
		sfSession.Open();
		SFStatement statement = new SFStatement(sfSession);
		SFBaseResultSet resultSet = statement.Execute(0, "select 1", null, false, false);
		Assert.AreEqual(true, resultSet.Next());
		Assert.AreEqual("1", resultSet.GetString(0));
		Assert.AreEqual("new_session_token", sfSession.m_SessionToken);
		Assert.AreEqual("new_master_token", sfSession.m_MasterToken);
		Assert.AreEqual(restRequester.FirstTimeRequestID, restRequester.SecondTimeRequestID);
	}

	// Mock test for session renew during query execution
	[Test]
	public void TestSessionRenewDuringQueryExec()
	{
		Mock.MockRestSessionExpiredInQueryExec restRequester = new Mock.MockRestSessionExpiredInQueryExec();
		SFSession sfSession = new SFSession("account=test;user=test;password=test", null, restRequester, SnowflakeDbConfiguration.Default);
		sfSession.Open();
		SFStatement statement = new SFStatement(sfSession);
		SFBaseResultSet resultSet = statement.Execute(0, "select 1", null, false, false);
		Assert.AreEqual(true, resultSet.Next());
		Assert.AreEqual("1", resultSet.GetString(0));
	}

	// Mock test for Service Name
	// The Mock requester would take in the X-Snowflake-Service header in the request
	// and append a character 'a' at the end, send back as SERVICE_NAME parameter
	// This test is to assure that SETVICE_NAME parameter would be upgraded to the session
	[Test]
	public void TestServiceName()
	{
		var restRequester = new Mock.MockServiceName();
		var sfSession = new SFSession("account=test;user=test;password=test", null, restRequester, SnowflakeDbConfiguration.Default);
		sfSession.Open();
		var expectServiceName = Mock.MockServiceName.INIT_SERVICE_NAME;
		Assert.AreEqual(expectServiceName, sfSession.ParameterMap[SFSessionParameter.SERVICE_NAME]);
		for (int i = 0; i < 5; i++)
		{
			var statement = new SFStatement(sfSession);
			statement.Execute(0, "SELECT 1", null, false, false);
			expectServiceName += "a";
			Assert.AreEqual(expectServiceName, sfSession.ParameterMap[SFSessionParameter.SERVICE_NAME]);
		}
	}
}
