/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Tests
{
    using NUnit.Framework;
    using Tortuga.Data.Snowflake.Core;

    [TestFixture]
    class SFSessionTest
    {
        // Mock test for session gone
        [Test]
        public void TestSessionGoneWhenClose()
        {
            Mock.MockCloseSessionGone restRequester = new Mock.MockCloseSessionGone();
            SFSession sfSession = new SFSession("account=test;user=test;password=test", null, restRequester);
            sfSession.Open();
            sfSession.close(); // no exception is raised.
        }
    }
}