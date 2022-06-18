using System.Globalization;

namespace Tortuga.HttpClientUtilities;

static class TaskUtilities
{
	static readonly TaskFactory s_TaskFactory = new(default, default, default, TaskScheduler.Default);

	/// <summary>
	/// Runs the task on a separate thread and waits for it to complete.
	/// </summary>
	/// <typeparam name="TResult">The type of result expected.</typeparam>
	/// <param name="func">The function.</param>
	/// <returns>TResult.</returns>
	public static TResult RunSynchronously<TResult>(this Func<Task<TResult>> func)
	{
		var cultureUi = CultureInfo.CurrentUICulture;
		var culture = CultureInfo.CurrentCulture;
		return s_TaskFactory.StartNew(() =>
		{
			Thread.CurrentThread.CurrentCulture = culture;
			Thread.CurrentThread.CurrentUICulture = cultureUi;
			return func();
		}, default, default, TaskScheduler.Default).Unwrap().GetAwaiter().GetResult();
	}

	public static void RunSynchronously(this Func<Task> func)
	{
		var cultureUi = CultureInfo.CurrentUICulture;
		var culture = CultureInfo.CurrentCulture;
		s_TaskFactory.StartNew(() =>
		{
			Thread.CurrentThread.CurrentCulture = culture;
			Thread.CurrentThread.CurrentUICulture = cultureUi;
			return func();
		}, default, default, TaskScheduler.Default).Unwrap().GetAwaiter().GetResult();
	}

	public static TResult RunSynchronously<TResult>(this Func<Task<TResult>> func, CancellationToken cancellationToken)
	{
		var cultureUi = CultureInfo.CurrentUICulture;
		var culture = CultureInfo.CurrentCulture;
		return s_TaskFactory.StartNew(() =>
		{
			Thread.CurrentThread.CurrentCulture = culture;
			Thread.CurrentThread.CurrentUICulture = cultureUi;
			return func();
		}, cancellationToken, default, TaskScheduler.Default).Unwrap().GetAwaiter().GetResult();
	}

	public static void RunSynchronously(this Func<Task> func, CancellationToken cancellationToken)
	{
		var cultureUi = CultureInfo.CurrentUICulture;
		var culture = CultureInfo.CurrentCulture;
		s_TaskFactory.StartNew(() =>
		{
			Thread.CurrentThread.CurrentCulture = culture;
			Thread.CurrentThread.CurrentUICulture = cultureUi;
			return func();
		}, cancellationToken, default, TaskScheduler.Default).Unwrap().GetAwaiter().GetResult();
	}
}
