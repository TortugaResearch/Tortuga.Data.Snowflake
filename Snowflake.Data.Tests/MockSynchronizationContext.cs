using NUnit.Framework;

namespace Tortuga.Data.Snowflake.Tests;

/*
 * This class will not deadlock, but it will cause tests to fail if the Send or Post methods have been called during a test
 */

public sealed class MockSynchronizationContext : SynchronizationContext
{
	int callCount = 0;

	public override void Post(SendOrPostCallback d, object state)
	{
		callCount++;
		base.Post(d, state);
	}

	public override void Send(SendOrPostCallback d, object state)
	{
		callCount++;
		base.Send(d, state);
	}

	public static void SetupContext()
	{
		SetSynchronizationContext(new MockSynchronizationContext());
	}

	public static void Verify()
	{
		var ctx = (MockSynchronizationContext)Current;
		Assert.Zero(ctx.callCount, "MockSynchronizationContext was called - this can cause deadlock. Make sure ConfigureAwait(false) is used in every await point in the library");
		SetSynchronizationContext(null);
	}
}
