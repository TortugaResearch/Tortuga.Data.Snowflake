using System.Net;

namespace Tortuga.HttpClientUtilities;

static class HttpContentSynchronously
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

#if !NET6_0_OR_GREATER

	/// <summary>
	/// Serializes the HTTP content into a stream of bytes and copies it to stream.
	/// </summary>
	/// <param name="content">The content.</param>
	/// <param name="stream">The target stream.</param>
	/// <param name="context">Information about the transport (for example, the channel binding token). This parameter may be null.</param>
	/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
	public static void CopyTo(this HttpContent content, Stream stream, TransportContext? context, CancellationToken cancellationToken)
	{
		TaskUtilities.RunSynchronously(() => content.CopyToAsync(stream, context, cancellationToken), cancellationToken);
	}

	/// <summary>
	/// Serialize the HTTP content into a stream of bytes and copies it to the stream
	/// object provided as the stream parameter.</summary>
	/// <param name="content">The content.</param>
	/// <param name="stream">The target stream.</param>
	/// <param name="context">Information about the transport (channel binding token, for example). This parameter may be null.</param>
	/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
	public static Task CopyToAsync(this HttpContent content, Stream stream, TransportContext? context, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return content.CopyToAsync(stream, context);
	}

	/// <summary>
	/// Serialize the HTTP content into a stream of bytes and copies it to the stream object provided as the stream parameter.
	/// </summary>
	/// <param name="content">The content.</param>
	/// <param name="stream">The target stream.</param>
	/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
	public static Task CopyToAsync(this HttpContent content, Stream stream, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return content.CopyToAsync(stream);
	}

	/// <summary>
	/// Serialize the HTTP content to a byte array as an asynchronous operation.
	/// </summary>
	/// <param name="content">The content.</param>
	/// <param name="cancellationToken">//     The cancellation token to cancel the operation.</param>
	public static Task<byte[]> ReadAsByteArrayAsync(this HttpContent content, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return content.ReadAsByteArrayAsync();
	}

	/// <summary>
	/// Serialize the HTTP content and return a stream that represents the content as an asynchronous operation.
	/// </summary>
	/// <param name="content">The content.</param>
	/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
	public static Task<Stream> ReadAsStreamAsync(this HttpContent content, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return content.ReadAsStreamAsync();
	}

	/// <summary>
	/// Serialize the HTTP content to a string as an asynchronous operation.
	/// </summary>
	/// <param name="content">The content.</param>
	/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
	public static Task<string> ReadAsStringAsync(this HttpContent content, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return content.ReadAsStringAsync();
	}

#endif
}
