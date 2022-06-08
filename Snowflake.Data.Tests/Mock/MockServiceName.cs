/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Tortuga.Data.Snowflake.Core.Messages;
using Tortuga.Data.Snowflake.Core.RequestProcessing;

namespace Tortuga.Data.Snowflake.Tests.Mock;

class MockServiceName : IMockRestRequester
{
    public const string INIT_SERVICE_NAME = "init";

    public Task<T> PostAsync<T>(RestRequest request, CancellationToken cancellationToken)
    {
        var message = request.ToRequestMessage(HttpMethod.Post);
        var param = new NameValueParameter { name = "SERVICE_NAME" };
        if (!message.Headers.Contains("X-Snowflake-Service"))
        {
            param.value = INIT_SERVICE_NAME;
        }
        else
        {
            IEnumerable<string> headerValues = message.Headers.GetValues("X-Snowflake-Service");
            foreach (string value in headerValues)
            {
                param.value = value + 'a';
            }
        }

        SFRestRequest sfRequest = (SFRestRequest)request;
        if (sfRequest.jsonBody is LoginRequest)
        {
            LoginResponse authnResponse = new LoginResponse
            {
                data = new LoginResponseData()
                {
                    token = "session_token",
                    masterToken = "master_token",
                    authResponseSessionInfo = new SessionInfo(),
                    nameValueParameter = new List<NameValueParameter>() { param }
                },
                success = true
            };

            // login request return success
            return Task.FromResult<T>((T)(object)authnResponse);
        }
        else if (sfRequest.jsonBody is QueryRequest)
        {
            QueryExecResponse queryExecResponse = new QueryExecResponse
            {
                success = true,
                data = new QueryExecResponseData
                {
                    rowSet = new string[,] { { "1" } },
                    rowType = new List<ExecResponseRowType>()
                            {
                                new ExecResponseRowType
                                {
                                    name = "colone",
                                    type = "FIXED"
                                }
                            },
                    parameters = new List<NameValueParameter> { param }
                }
            };
            return Task.FromResult<T>((T)(object)queryExecResponse);
        }
        else
        {
            return Task.FromResult<T>((T)(object)null);
        }
    }

    public T Post<T>(RestRequest postRequest)
    {
        return Task.Run(async () => await (PostAsync<T>(postRequest, CancellationToken.None)).ConfigureAwait(false)).Result;
    }

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

    public void setHttpClient(HttpClient httpClient)
    {
        // Nothing to do
    }
}
