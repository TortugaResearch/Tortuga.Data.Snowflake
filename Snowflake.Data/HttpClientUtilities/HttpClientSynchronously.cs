namespace Tortuga.HttpClientUtilities;

internal static class HttpClientSynchronously
{
    //
    // Summary:
    //     Send a DELETE request to the specified Uri as an asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    //     The request message was already sent by the System.Net.Http.HttpClient instance.
    //     -or- The requestUri is not an absolute URI. -or- System.Net.Http.HttpClient.BaseAddress
    //     is not set.
    //
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation or timeout.
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static HttpResponseMessage Delete(this HttpClient client, string? requestUri)
    {
        return client.Send(new HttpRequestMessage(HttpMethod.Delete, requestUri));
    }

    //
    // Summary:
    //     Send a DELETE request to the specified Uri with a cancellation token as an asynchronous
    //     operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    //   cancellationToken:
    //     A cancellation token that can be used by other objects or threads to receive
    //     notice of cancellation.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    //     The request message was already sent by the System.Net.Http.HttpClient instance.
    //     -or- The requestUri is not an absolute URI. -or- System.Net.Http.HttpClient.BaseAddress
    //     is not set.
    //
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation or timeout.
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static HttpResponseMessage Delete(this HttpClient client, string? requestUri, CancellationToken cancellationToken)
    {
        return client.Send(new HttpRequestMessage(HttpMethod.Delete, requestUri), cancellationToken);
    }

    //
    // Summary:
    //     Send a DELETE request to the specified Uri as an asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    //     The request message was already sent by the System.Net.Http.HttpClient instance.
    //     -or- The requestUri is not an absolute URI. -or- System.Net.Http.HttpClient.BaseAddress
    //     is not set.
    //
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation or timeout.
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static HttpResponseMessage Delete(this HttpClient client, Uri? requestUri)
    {
        if (requestUri == null)
            return client.Send(new HttpRequestMessage() { Method = HttpMethod.Delete });
        else
            return client.Send(new HttpRequestMessage(HttpMethod.Delete, requestUri));
    }

    //
    // Summary:
    //     Send a DELETE request to the specified Uri with a cancellation token as an asynchronous
    //     operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    //   cancellationToken:
    //     A cancellation token that can be used by other objects or threads to receive
    //     notice of cancellation.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    //     The request message was already sent by the System.Net.Http.HttpClient instance.
    //     -or- The requestUri is not an absolute URI. -or- System.Net.Http.HttpClient.BaseAddress
    //     is not set.
    //
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation or timeout.
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static HttpResponseMessage Delete(this HttpClient client, Uri? requestUri, CancellationToken cancellationToken)
    {
        if (requestUri == null)
            return client.Send(new HttpRequestMessage() { Method = HttpMethod.Delete }, cancellationToken);
        else
            return client.Send(new HttpRequestMessage(HttpMethod.Delete, requestUri), cancellationToken);
    }

    //
    // Summary:
    //     Send a GET request to the specified Uri as an asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    //     The requestUri must be an absolute URI or System.Net.Http.HttpClient.BaseAddress
    //     must be set.
    //
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation or timeout.
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static HttpResponseMessage Get(this HttpClient client, string? requestUri)
    {
        return client.Send(new HttpRequestMessage(HttpMethod.Get, requestUri));
    }

    //
    // Summary:
    //     Send a GET request to the specified Uri with an HTTP completion option as an
    //     asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    //   completionOption:
    //     An HTTP completion option value that indicates when the operation should be considered
    //     completed.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    //     The requestUri must be an absolute URI or System.Net.Http.HttpClient.BaseAddress
    //     must be set.
    //
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation or timeout.
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static HttpResponseMessage Get(this HttpClient client, string? requestUri, HttpCompletionOption completionOption)
    {
        return client.Send(new HttpRequestMessage(HttpMethod.Get, requestUri), completionOption);
    }

    //
    // Summary:
    //     Send a GET request to the specified Uri with an HTTP completion option and a
    //     cancellation token as an asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    //   completionOption:
    //     An HTTP completion option value that indicates when the operation should be considered
    //     completed.
    //
    //   cancellationToken:
    //     A cancellation token that can be used by other objects or threads to receive
    //     notice of cancellation.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    //     The requestUri must be an absolute URI or System.Net.Http.HttpClient.BaseAddress
    //     must be set.
    //
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation or timeout.
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static HttpResponseMessage Get(this HttpClient client, string? requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken)
    {
        return client.Send(new HttpRequestMessage(HttpMethod.Get, requestUri), completionOption, cancellationToken);
    }

    //
    // Summary:
    //     Send a GET request to the specified Uri with a cancellation token as an asynchronous
    //     operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    //   cancellationToken:
    //     A cancellation token that can be used by other objects or threads to receive
    //     notice of cancellation.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    //     The requestUri must be an absolute URI or System.Net.Http.HttpClient.BaseAddress
    //     must be set.
    //
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation or timeout.
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static HttpResponseMessage Get(this HttpClient client, string? requestUri, CancellationToken cancellationToken)
    {
        return client.Send(new HttpRequestMessage(HttpMethod.Get, requestUri), cancellationToken);
    }

    //
    // Summary:
    //     Send a GET request to the specified Uri as an asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    //     The requestUri must be an absolute URI or System.Net.Http.HttpClient.BaseAddress
    //     must be set.
    //
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation or timeout.
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static HttpResponseMessage Get(this HttpClient client, Uri? requestUri)
    {
        if (requestUri == null)
            return client.Send(new HttpRequestMessage() { Method = HttpMethod.Get });
        else
            return client.Send(new HttpRequestMessage(HttpMethod.Get, requestUri));
    }

    //
    // Summary:
    //     Send a GET request to the specified Uri with an HTTP completion option as an
    //     asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    //   completionOption:
    //     An HTTP completion option value that indicates when the operation should be considered
    //     completed.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    //     The requestUri must be an absolute URI or System.Net.Http.HttpClient.BaseAddress
    //     must be set.
    //
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation or timeout.
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static HttpResponseMessage Get(this HttpClient client, Uri? requestUri, HttpCompletionOption completionOption)
    {
        if (requestUri == null)
            return client.Send(new HttpRequestMessage() { Method = HttpMethod.Get }, completionOption);
        else
            return client.Send(new HttpRequestMessage(HttpMethod.Get, requestUri), completionOption);
    }

    //
    // Summary:
    //     Send a GET request to the specified Uri with an HTTP completion option and a
    //     cancellation token as an asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    //   completionOption:
    //     An HTTP completion option value that indicates when the operation should be considered
    //     completed.
    //
    //   cancellationToken:
    //     A cancellation token that can be used by other objects or threads to receive
    //     notice of cancellation.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    //     The requestUri must be an absolute URI or System.Net.Http.HttpClient.BaseAddress
    //     must be set.
    //
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation or timeout.
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static HttpResponseMessage Get(this HttpClient client, Uri? requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken)
    {
        if (requestUri == null)
            return client.Send(new HttpRequestMessage() { Method = HttpMethod.Get }, completionOption, cancellationToken);
        else
            return client.Send(new HttpRequestMessage(HttpMethod.Get, requestUri), completionOption, cancellationToken);
    }

    //
    // Summary:
    //     Send a GET request to the specified Uri with a cancellation token as an asynchronous
    //     operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    //   cancellationToken:
    //     A cancellation token that can be used by other objects or threads to receive
    //     notice of cancellation.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    //     The requestUri must be an absolute URI or System.Net.Http.HttpClient.BaseAddress
    //     must be set.
    //
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation or timeout.
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static HttpResponseMessage Get(this HttpClient client, Uri? requestUri, CancellationToken cancellationToken)
    {
        if (requestUri == null)
            return client.Send(new HttpRequestMessage() { Method = HttpMethod.Get }, cancellationToken);
        else
            return client.Send(new HttpRequestMessage(HttpMethod.Get, requestUri), cancellationToken);
    }

    //
    // Summary:
    //     Sends a GET request to the specified Uri and return the response body as a byte
    //     array in an asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    //     The requestUri must be an absolute URI or System.Net.Http.HttpClient.BaseAddress
    //     must be set.
    //
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation or timeout.
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static byte[] GetByteArray(this HttpClient client, string? requestUri)
    {
        return TaskUtilities.RunSynchronously(() => client.GetByteArrayAsync(requestUri));
    }

    //
    // Summary:
    //     Sends a GET request to the specified Uri and return the response body as a byte
    //     array in an asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    //   cancellationToken:
    //     The cancellation token to cancel the operation.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation (or timeout for .NET Framework only).
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static byte[] GetByteArray(this HttpClient client, string? requestUri, CancellationToken cancellationToken)
    {
        return TaskUtilities.RunSynchronously(() => client.GetByteArrayAsync(requestUri), cancellationToken);
    }

    //
    // Summary:
    //     Send a GET request to the specified Uri and return the response body as a byte
    //     array in an asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    //     The requestUri must be an absolute URI or System.Net.Http.HttpClient.BaseAddress
    //     must be set.
    //
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation (or timeout for .NET Framework only).
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static byte[] GetByteArray(this HttpClient client, Uri? requestUri)
    {
        return TaskUtilities.RunSynchronously(() => client.GetByteArrayAsync(requestUri));
    }

    //
    // Summary:
    //     Send a GET request to the specified Uri and return the response body as a byte
    //     array in an asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    //   cancellationToken:
    //     The cancellation token to cancel the operation.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation (or timeout for .NET Framework only).
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static byte[] GetByteArray(this HttpClient client, Uri? requestUri, CancellationToken cancellationToken)
    {
        return TaskUtilities.RunSynchronously(() => client.GetByteArrayAsync(requestUri), cancellationToken);
    }

    //
    // Summary:
    //     Send a GET request to the specified Uri and return the response body as a stream
    //     in an asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    //     The requestUri must be an absolute URI or System.Net.Http.HttpClient.BaseAddress
    //     must be set.
    //
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation (or timeout for .NET Framework only).
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static Stream GetStream(this HttpClient client, string? requestUri)
    {
        return TaskUtilities.RunSynchronously(() => client.GetStreamAsync(requestUri));
    }

    //
    // Summary:
    //     Send a GET request to the specified Uri and return the response body as a stream
    //     in an asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    //   cancellationToken:
    //     The cancellation token to cancel the operation.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    public static Stream GetStream(this HttpClient client, string? requestUri, CancellationToken cancellationToken)
    {
        return TaskUtilities.RunSynchronously(() => client.GetStreamAsync(requestUri), cancellationToken);
    }

    //
    // Summary:
    //     Send a GET request to the specified Uri and return the response body as a stream
    //     in an asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    //     The requestUri must be an absolute URI or System.Net.Http.HttpClient.BaseAddress
    //     must be set.
    //
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation (or timeout for .NET Framework only).
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static Stream GetStream(this HttpClient client, Uri? requestUri)
    {
        return TaskUtilities.RunSynchronously(() => client.GetStreamAsync(requestUri));
    }

    //
    // Summary:
    //     Send a GET request to the specified Uri and return the response body as a stream
    //     in an asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    //   cancellationToken:
    //     The cancellation token to cancel the operation.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.ArgumentNullException:
    //     The requestUri is null.
    //
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation (or timeout for .NET Framework only).
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static Stream GetStream(this HttpClient client, Uri? requestUri, CancellationToken cancellationToken)
    {
        return TaskUtilities.RunSynchronously(() => client.GetStreamAsync(requestUri), cancellationToken);
    }

    //
    // Summary:
    //     Send a GET request to the specified Uri and return the response body as a string
    //     in an asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    //     The requestUri must be an absolute URI or System.Net.Http.HttpClient.BaseAddress
    //     must be set.
    //
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation (or timeout for .NET Framework only).
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static string GetString(this HttpClient client, string? requestUri)
    {
        return TaskUtilities.RunSynchronously(() => client.GetStringAsync(requestUri));
    }

    //
    // Summary:
    //     Send a GET request to the specified Uri and return the response body as a string
    //     in an asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    //   cancellationToken:
    //     The cancellation token to cancel the operation.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.ArgumentNullException:
    //     The requestUri is null.
    //
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation (or timeout for .NET Framework only).
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static string GetString(this HttpClient client, string? requestUri, CancellationToken cancellationToken)
    {
        return TaskUtilities.RunSynchronously(() => client.GetStringAsync(requestUri), cancellationToken);
    }

    //
    // Summary:
    //     Send a GET request to the specified Uri and return the response body as a string
    //     in an asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    //     The requestUri must be an absolute URI or System.Net.Http.HttpClient.BaseAddress
    //     must be set.
    //
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation (or timeout for .NET Framework only).
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static string GetString(this HttpClient client, Uri? requestUri)
    {
        return TaskUtilities.RunSynchronously(() => client.GetStringAsync(requestUri));
    }

    //
    // Summary:
    //     Send a GET request to the specified Uri and return the response body as a string
    //     in an asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    //   cancellationToken:
    //     The cancellation token to cancel the operation.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.ArgumentNullException:
    //     The requestUri is null.
    //
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation (or timeout for .NET Framework only).
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static string GetString(this HttpClient client, Uri? requestUri, CancellationToken cancellationToken)
    {
        return TaskUtilities.RunSynchronously(() => client.GetStringAsync(requestUri), cancellationToken);
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
    static readonly HttpMethod PatchMethod = HttpMethod.Patch;
#else
    static readonly HttpMethod PatchMethod = new("PATCH");
#endif

    //
    // Summary:
    //     Sends a PATCH request to a Uri designated as a string as an asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    //   content:
    //     The HTTP request content sent to the server.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    public static HttpResponseMessage Patch(this HttpClient client, string? requestUri, HttpContent content)
    {
        return client.Send(new HttpRequestMessage(PatchMethod, requestUri) { Content = content });
    }

    //
    // Summary:
    //     Sends a PATCH request with a cancellation token to a Uri represented as a string
    //     as an asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    //   content:
    //     The HTTP request content sent to the server.
    //
    //   cancellationToken:
    //     A cancellation token that can be used by other objects or threads to receive
    //     notice of cancellation.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    public static HttpResponseMessage Patch(this HttpClient client, string? requestUri, HttpContent content, CancellationToken cancellationToken)
    {
        return client.Send(new HttpRequestMessage(PatchMethod, requestUri) { Content = content }, cancellationToken);
    }

    //
    // Summary:
    //     Sends a PATCH request as an asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    //   content:
    //     The HTTP request content sent to the server.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    public static HttpResponseMessage Patch(this HttpClient client, Uri? requestUri, HttpContent content)
    {
        return client.Send(new HttpRequestMessage(PatchMethod, requestUri) { Content = content });
    }

    //
    // Summary:
    //     Sends a PATCH request with a cancellation token as an asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    //   content:
    //     The HTTP request content sent to the server.
    //
    //   cancellationToken:
    //     A cancellation token that can be used by other objects or threads to receive
    //     notice of cancellation.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    public static HttpResponseMessage Patch(this HttpClient client, Uri? requestUri, HttpContent content, CancellationToken cancellationToken)
    {
        return client.Send(new HttpRequestMessage(PatchMethod, requestUri) { Content = content }, cancellationToken);
    }

    //
    // Summary:
    //     Send a POST request to the specified Uri as an asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    //   content:
    //     The HTTP request content sent to the server.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    //     The requestUri must be an absolute URI or System.Net.Http.HttpClient.BaseAddress
    //     must be set.
    //
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation or timeout.
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static HttpResponseMessage Post(this HttpClient client, string? requestUri, HttpContent content)
    {
        return client.Send(new HttpRequestMessage(HttpMethod.Post, requestUri) { Content = content });
    }

    //
    // Summary:
    //     Send a POST request with a cancellation token as an asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    //   content:
    //     The HTTP request content sent to the server.
    //
    //   cancellationToken:
    //     A cancellation token that can be used by other objects or threads to receive
    //     notice of cancellation.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    //     The requestUri must be an absolute URI or System.Net.Http.HttpClient.BaseAddress
    //     must be set.
    //
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation or timeout.
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static HttpResponseMessage Post(this HttpClient client, string? requestUri, HttpContent content, CancellationToken cancellationToken)
    {
        return client.Send(new HttpRequestMessage(HttpMethod.Post, requestUri) { Content = content }, cancellationToken);
    }

    //
    // Summary:
    //     Send a POST request to the specified Uri as an asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    //   content:
    //     The HTTP request content sent to the server.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    //     The requestUri must be an absolute URI or System.Net.Http.HttpClient.BaseAddress
    //     must be set.
    //
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation or timeout.
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static HttpResponseMessage Post(this HttpClient client, Uri? requestUri, HttpContent content)
    {
        return client.Send(new HttpRequestMessage(HttpMethod.Post, requestUri) { Content = content });
    }

    //
    // Summary:
    //     Send a POST request with a cancellation token as an asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    //   content:
    //     The HTTP request content sent to the server.
    //
    //   cancellationToken:
    //     A cancellation token that can be used by other objects or threads to receive
    //     notice of cancellation.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    //     The requestUri must be an absolute URI or System.Net.Http.HttpClient.BaseAddress
    //     must be set.
    //
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation or timeout.
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static HttpResponseMessage Post(this HttpClient client, Uri? requestUri, HttpContent content, CancellationToken cancellationToken)
    {
        return client.Send(new HttpRequestMessage(HttpMethod.Post, requestUri) { Content = content }, cancellationToken);
    }

    //
    // Summary:
    //     Send a PUT request to the specified Uri as an asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    //   content:
    //     The HTTP request content sent to the server.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    //     The requestUri must be an absolute URI or System.Net.Http.HttpClient.BaseAddress
    //     must be set.
    //
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation or timeout.
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static HttpResponseMessage Put(this HttpClient client, string? requestUri, HttpContent content)
    {
        return client.Send(new HttpRequestMessage(HttpMethod.Put, requestUri) { Content = content });
    }

    //
    // Summary:
    //     Send a PUT request with a cancellation token as an asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    //   content:
    //     The HTTP request content sent to the server.
    //
    //   cancellationToken:
    //     A cancellation token that can be used by other objects or threads to receive
    //     notice of cancellation.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    //     The requestUri must be an absolute URI or System.Net.Http.HttpClient.BaseAddress
    //     must be set.
    //
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation or timeout.
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static HttpResponseMessage Put(this HttpClient client, string? requestUri, HttpContent content, CancellationToken cancellationToken)
    {
        return client.Send(new HttpRequestMessage(HttpMethod.Put, requestUri) { Content = content }, cancellationToken);
    }

    //
    // Summary:
    //     Send a PUT request to the specified Uri as an asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    //   content:
    //     The HTTP request content sent to the server.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    //     The requestUri must be an absolute URI or System.Net.Http.HttpClient.BaseAddress
    //     must be set.
    //
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation or timeout.
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static HttpResponseMessage Put(this HttpClient client, Uri? requestUri, HttpContent content)
    {
        return client.Send(new HttpRequestMessage(HttpMethod.Put, requestUri) { Content = content });
    }

    //
    // Summary:
    //     Send a PUT request with a cancellation token as an asynchronous operation.
    //
    // Parameters:
    //   requestUri:
    //     The Uri the request is sent to.
    //
    //   content:
    //     The HTTP request content sent to the server.
    //
    //   cancellationToken:
    //     A cancellation token that can be used by other objects or threads to receive
    //     notice of cancellation.
    //
    // Returns:
    //     The task object representing the asynchronous operation.
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    //     The requestUri must be an absolute URI or System.Net.Http.HttpClient.BaseAddress
    //     must be set.
    //
    //   T:System.Net.Http.HttpRequestException:
    //     The request failed due to an underlying issue such as network connectivity, DNS
    //     failure, server certificate validation or timeout.
    //
    //   T:System.Threading.Tasks.TaskCanceledException:
    //     .NET Core and .NET 5.0 and later only: The request failed due to timeout.
    public static HttpResponseMessage Put(this HttpClient client, Uri? requestUri, HttpContent content, CancellationToken cancellationToken)
    {
        return client.Send(new HttpRequestMessage(HttpMethod.Put, requestUri) { Content = content }, cancellationToken);
    }

#if !NET5_0_OR_GREATER

	//
	// Summary:
	//     Sends an HTTP request with the specified request.
	//
	// Parameters:
	//   request:
	//     The HTTP request message to send.
	//
	// Returns:
	//     An HTTP response message.
	//
	// Exceptions:
	//   T:System.ArgumentNullException:
	//     The request is null.
	//
	//   T:System.NotSupportedException:
	//     The HTTP version is 2.0 or higher or the version policy is set to System.Net.Http.HttpVersionPolicy.RequestVersionOrHigher.
	//     -or- The custom class derived from System.Net.Http.HttpContent does not override
	//     the System.Net.Http.HttpContent.SerializeToStream(System.IO.Stream,System.Net.TransportContext,System.Threading.CancellationToken)
	//     method. -or- The custom System.Net.Http.HttpMessageHandler does not override
	//     the System.Net.Http.HttpMessageHandler.Send(System.Net.Http.HttpRequestMessage,System.Threading.CancellationToken)
	//     method.
	//
	//   T:System.InvalidOperationException:
	//     The request message was already sent by the System.Net.Http.HttpClient instance.
	//
	//   T:System.Net.Http.HttpRequestException:
	//     The request failed due to an underlying issue such as network connectivity, DNS
	//     failure, or server certificate validation.
	//
	//   T:System.Threading.Tasks.TaskCanceledException:
	//     If the System.Threading.Tasks.TaskCanceledException exception nests the System.TimeoutException:
	//     The request failed due to timeout.
	public static HttpResponseMessage Send(this HttpClient client, HttpRequestMessage request)
	{
		return TaskUtilities.RunSynchronously(() => client.SendAsync(request));
	}

	//
	// Summary:
	//     Sends an HTTP request.
	//
	// Parameters:
	//   request:
	//     The HTTP request message to send.
	//
	//   completionOption:
	//     One of the enumeration values that specifies when the operation should complete
	//     (as soon as a response is available or after reading the response content).
	//
	// Returns:
	//     The HTTP response message.
	//
	// Exceptions:
	//   T:System.ArgumentNullException:
	//     The request is null.
	//
	//   T:System.NotSupportedException:
	//     The HTTP version is 2.0 or higher or the version policy is set to System.Net.Http.HttpVersionPolicy.RequestVersionOrHigher.
	//     -or- The custom class derived from System.Net.Http.HttpContent does not override
	//     the System.Net.Http.HttpContent.SerializeToStream(System.IO.Stream,System.Net.TransportContext,System.Threading.CancellationToken)
	//     method. -or- The custom System.Net.Http.HttpMessageHandler does not override
	//     the System.Net.Http.HttpMessageHandler.Send(System.Net.Http.HttpRequestMessage,System.Threading.CancellationToken)
	//     method.
	//
	//   T:System.InvalidOperationException:
	//     The request message was already sent by the System.Net.Http.HttpClient instance.
	//
	//   T:System.Net.Http.HttpRequestException:
	//     The request failed due to an underlying issue such as network connectivity, DNS
	//     failure, or server certificate validation.
	//
	//   T:System.Threading.Tasks.TaskCanceledException:
	//     If the System.Threading.Tasks.TaskCanceledException exception nests the System.TimeoutException:
	//     The request failed due to timeout.
	public static HttpResponseMessage Send(this HttpClient client, HttpRequestMessage request, HttpCompletionOption completionOption)
	{
		return TaskUtilities.RunSynchronously(() => client.SendAsync(request, completionOption));
	}

	//
	// Summary:
	//     Sends an HTTP request with the specified request, completion option and cancellation
	//     token.
	//
	// Parameters:
	//   request:
	//     The HTTP request message to send.
	//
	//   completionOption:
	//     One of the enumeration values that specifies when the operation should complete
	//     (as soon as a response is available or after reading the response content).
	//
	//   cancellationToken:
	//     The token to cancel the operation.
	//
	// Returns:
	//     The HTTP response message.
	//
	// Exceptions:
	//   T:System.ArgumentNullException:
	//     The request is null.
	//
	//   T:System.NotSupportedException:
	//     The HTTP version is 2.0 or higher or the version policy is set to System.Net.Http.HttpVersionPolicy.RequestVersionOrHigher.
	//     -or- The custom class derived from System.Net.Http.HttpContent does not override
	//     the System.Net.Http.HttpContent.SerializeToStream(System.IO.Stream,System.Net.TransportContext,System.Threading.CancellationToken)
	//     method. -or- The custom System.Net.Http.HttpMessageHandler does not override
	//     the System.Net.Http.HttpMessageHandler.Send(System.Net.Http.HttpRequestMessage,System.Threading.CancellationToken)
	//     method.
	//
	//   T:System.InvalidOperationException:
	//     The request message was already sent by the System.Net.Http.HttpClient instance.
	//
	//   T:System.Net.Http.HttpRequestException:
	//     The request failed due to an underlying issue such as network connectivity, DNS
	//     failure, or server certificate validation.
	//
	//   T:System.Threading.Tasks.TaskCanceledException:
	//     The request was canceled. -or- If the System.Threading.Tasks.TaskCanceledException
	//     exception nests the System.TimeoutException: The request failed due to timeout.
	public static HttpResponseMessage Send(this HttpClient client, HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken)
	{
		return TaskUtilities.RunSynchronously(() => client.SendAsync(request, completionOption, cancellationToken));
	}

	//
	// Summary:
	//     Sends an HTTP request with the specified request and cancellation token.
	//
	// Parameters:
	//   request:
	//     The HTTP request message to send.
	//
	//   cancellationToken:
	//     The token to cancel the operation.
	//
	// Returns:
	//     The HTTP response message.
	//
	// Exceptions:
	//   T:System.ArgumentNullException:
	//     The request is null.
	//
	//   T:System.NotSupportedException:
	//     The HTTP version is 2.0 or higher or the version policy is set to System.Net.Http.HttpVersionPolicy.RequestVersionOrHigher.
	//     -or- The custom class derived from System.Net.Http.HttpContent does not override
	//     the System.Net.Http.HttpContent.SerializeToStream(System.IO.Stream,System.Net.TransportContext,System.Threading.CancellationToken)
	//     method. -or- The custom System.Net.Http.HttpMessageHandler does not override
	//     the System.Net.Http.HttpMessageHandler.Send(System.Net.Http.HttpRequestMessage,System.Threading.CancellationToken)
	//     method.
	//
	//   T:System.InvalidOperationException:
	//     The request message was already sent by the System.Net.Http.HttpClient instance.
	//
	//   T:System.Net.Http.HttpRequestException:
	//     The request failed due to an underlying issue such as network connectivity, DNS
	//     failure, or server certificate validation.
	//
	//   T:System.Threading.Tasks.TaskCanceledException:
	//     The request was canceled. -or- If the System.Threading.Tasks.TaskCanceledException
	//     exception nests the System.TimeoutException: The request failed due to timeout.
	public static HttpResponseMessage Send(this HttpClient client, HttpRequestMessage request, CancellationToken cancellationToken)
	{
		return TaskUtilities.RunSynchronously(() => client.SendAsync(request, cancellationToken));
	}

#endif
}
