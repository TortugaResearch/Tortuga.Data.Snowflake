﻿/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Tortuga.Data.Snowflake.Core.Messages;
using Tortuga.Data.Snowflake.Core.RequestProcessing;

namespace Tortuga.Data.Snowflake.Tests.Mock;

class MockCloseSessionGone : IMockRestRequester
{
    static private readonly int SESSION_GONE = 390111;

    public T Get<T>(RestRequest request)
    {
        return Task.Run(async () => await (GetAsync<T>(request, CancellationToken.None)).ConfigureAwait(false)).Result;
    }

    public Task<T> GetAsync<T>(RestRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult<T>((T)(object)null);
    }

    public Task<HttpResponseMessage> GetAsync(RestRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult<HttpResponseMessage>(null);
    }

    public HttpResponseMessage Get(RestRequest request)
    {
        return null;
    }

    public T Post<T>(RestRequest postRequest)
    {
        return Task.Run(async () => await (PostAsync<T>(postRequest, CancellationToken.None)).ConfigureAwait(false)).Result;
    }

    public Task<T> PostAsync<T>(RestRequest postRequest, CancellationToken cancellationToken)
    {
        SFRestRequest sfRequest = (SFRestRequest)postRequest;
        if (sfRequest.JsonBody is LoginRequest)
        {
            LoginResponse authnResponse = new LoginResponse
            {
                Data = new LoginResponseData()
                {
                    Token = "session_token",
                    MasterToken = "master_token",
                    AuthResponseSessionInfo = new SessionInfo(),
                    NameValueParameter = new List<NameValueParameter>()
                },
                Success = true
            };

            // login request return success
            return Task.FromResult<T>((T)(object)authnResponse);
        }
        CloseResponse closeResponse = new CloseResponse
        {
            Message = "Session no longer exists.  New login required to access the service.",
            Data = null,
            Code = SESSION_GONE,
            Success = false
        };
        return Task.FromResult<T>((T)(object)closeResponse);
    }

    public void setHttpClient(HttpClient httpClient)
    {
        // Nothing to do
    }
}
