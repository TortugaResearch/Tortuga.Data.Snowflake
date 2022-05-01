/*
 * Copyright (c) 2019 Snowflake Computing Inc. All rights reserved.
 */

using NUnit.Framework;
using Tortuga.Data.Snowflake.Core;
using Tortuga.Data.Snowflake.Core.RequestProcessing;
using Tortuga.Data.Snowflake.Core.Sessions;

namespace Tortuga.Data.Snowflake.Tests;

[TestFixture]
class SFUriUpdaterTest
{
	[Test]
	public void TestRetryCount()
	{
		Uri uri = new Uri("https://ac.snowflakecomputing.com" + RestPath.SF_QUERY_PATH);

		UriUpdater updater = new UriUpdater(uri);

		for (int retryCount = 1; retryCount < 5; retryCount++)
		{
			Uri newUri = updater.Update();

			Assert.IsTrue(newUri.Query.Contains(RestParams.SF_QUERY_RETRY_COUNT + "=" + retryCount));
		}
	}

	[Test]
	/// This uri with query path other than query request should not have a retry counter
	public void TestRetryCountNoneQueryPath()
	{
		Uri uri = new Uri("https://ac.snowflakecomputing.com" + RestPath.SF_LOGIN_PATH);

		UriUpdater updater = new UriUpdater(uri);

		Uri newUri = updater.Update();

		Assert.IsFalse(newUri.Query.Contains(RestParams.SF_QUERY_RETRY_COUNT));
	}

	[Test]
	public void TestRequestGUIDUpdate()
	{
		Uri uri = new Uri("https://ac.snowflakecomputing.com" + RestPath.SF_LOGIN_PATH);
		UriUpdater updater = new UriUpdater(uri);

		// A uri with no request_guid at the begining should not change with the updater.
		Uri newUri = updater.Update();

		Assert.AreEqual(newUri.ToString(), uri.ToString());

		// A uri with request_guid should update that param
		string initialGuid = Guid.NewGuid().ToString();
		uri = new Uri("https://ac.snowflakecomputing.com" + RestPath.SF_LOGIN_PATH
			+ "?" + RestParams.SF_QUERY_REQUEST_GUID + "=" + initialGuid);

		updater = new UriUpdater(uri);
		newUri = updater.Update();

		Assert.IsTrue(newUri.Query.Contains(RestParams.SF_QUERY_REQUEST_GUID));
		Assert.IsFalse(newUri.Query.Contains(initialGuid));
		Assert.AreEqual(newUri.ToString().Length, uri.ToString().Length);
	}
}
