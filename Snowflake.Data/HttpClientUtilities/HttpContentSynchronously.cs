namespace Tortuga.HttpClientUtilities;

internal static class HttpContentSynchronously
{
    /// <summary>
    /// Serialize the HTTP content to a memory buffer as an synchronous operation.
    /// </summary>
    /// <param name="content">The content.</param>
    /// <remarks>To avoid deadlocks, this will be executed on a background thread.</remarks>
    public static void LoadIntoBufferAsync(this HttpContent content)
    {
        TaskUtilities.RunSynchronously(() => content.LoadIntoBufferAsync());
    }

    /// <summary>
    /// Serialize the HTTP content to a memory buffer as an synchronous operation.
    /// </summary>
    /// <param name="maxBufferSize">he maximum size, in bytes, of the buffer to use.</param>
    /// <param name="content">The content.</param>
    /// <remarks>To avoid deadlocks, this will be executed on a background thread.</remarks>
    public static void LoadIntoBufferAsync(this HttpContent content, long maxBufferSize)
    {
        TaskUtilities.RunSynchronously(() => content.LoadIntoBufferAsync(maxBufferSize));
    }

    /// <summary>
    /// Serialize the HTTP content to a byte array as an synchronous operation.
    /// </summary>
    /// <param name="content">The content.</param>
    /// <remarks>To avoid deadlocks, this will be executed on a background thread.</remarks>
    public static byte[] ReadAsByteArray(this HttpContent content)
    {
        return TaskUtilities.RunSynchronously(() => content.ReadAsByteArrayAsync());
    }

    /// <summary>
    /// Serialize the HTTP content to a byte array as an synchronous operation.
    /// </summary>
    /// <param name="content">The content.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <remarks>To avoid deadlocks, this will be executed on a background thread.</remarks>
    public static byte[] ReadAsByteArray(this HttpContent content, CancellationToken cancellationToken)
    {
#if NET5_0_OR_GREATER
        return TaskUtilities.RunSynchronously(() => content.ReadAsByteArrayAsync(cancellationToken));
#else
		return TaskUtilities.RunSynchronously(() => content.ReadAsByteArrayAsync(), cancellationToken);
#endif
    }

#if !NET5_0_OR_GREATER

	/// <summary>
	/// Serializes the HTTP content and returns a stream that represents the content.
	/// </summary>
	/// <param name="content">The content.</param>
	/// <returns>The stream that represents the HTTP content.</returns>
	/// <remarks>To avoid deadlocks, this will be executed on a background thread.</remarks>
	public static Stream ReadAsStream(this HttpContent content)
	{
		return TaskUtilities.RunSynchronously(() => content.ReadAsStreamAsync());
	}

	/// <summary>
	/// Serializes the HTTP content and returns a stream that represents the content.
	/// </summary>
	/// <param name="content">The content.</param>
	/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
	/// <returns>The stream that represents the HTTP content.</returns>
	/// <remarks>To avoid deadlocks, this will be executed on a background thread.</remarks>
	public static Stream ReadAsStream(this HttpContent content, CancellationToken cancellationToken)
	{
		return TaskUtilities.RunSynchronously(() => content.ReadAsStreamAsync(), cancellationToken);
	}

#endif

    /// <summary>
    /// Serialize the HTTP content to a string as an asynchronous operation.
    /// </summary>
    /// <param name="content">The content.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    /// <remarks>To avoid deadlocks, this will be executed on a background thread.</remarks>
    public static string ReadAsString(this HttpContent content)
    {
        return TaskUtilities.RunSynchronously(() => content.ReadAsStringAsync());
    }

    /// <summary>
    /// Serialize the HTTP content to a string as an asynchronous operation.
    /// </summary>
    /// <param name="content">The content.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    /// <remarks>To avoid deadlocks, this will be executed on a background thread.</remarks>
    public static string ReadAsString(this HttpContent content, CancellationToken cancellationToken)
    {
        return TaskUtilities.RunSynchronously(() => content.ReadAsStringAsync(), cancellationToken);
    }
}
