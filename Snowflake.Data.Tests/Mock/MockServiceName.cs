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
        var param = new NameValueParameter { Name = "SERVICE_NAME" };
        if (!message.Headers.Contains("X-Snowflake-Service"))
        {
            param.Value = INIT_SERVICE_NAME;
        }
        else
        {
            IEnumerable<string> headerValues = message.Headers.GetValues("X-Snowflake-Service");
            foreach (string value in headerValues)
            {
                param.Value = value + 'a';
            }
        }

        SFRestRequest sfRequest = (SFRestRequest)request;
        if (sfRequest.JsonBody is LoginRequest)
        {
            LoginResponse authnResponse = new LoginResponse
            {
                Data = new LoginResponseData()
                {
                    Token = "session_token",
                    MasterToken = "master_token",
                    AuthResponseSessionInfo = new SessionInfo(),
                    NameValueParameter = new List<NameValueParameter>() { param }
                },
                Success = true
            };

            // login request return success
            return Task.FromResult<T>((T)(object)authnResponse);
        }
        else if (sfRequest.JsonBody is QueryRequest)
        {
            QueryExecResponse queryExecResponse = new QueryExecResponse
            {
                Success = true,
                Data = new QueryExecResponseData
                {
                    RowSet = new string[,] { { "1" } },
                    RowType = new List<ExecResponseRowType>()
                            {
                                new ExecResponseRowType
                                {
                                    Name = "colone",
                                    Type = "FIXED"
                                }
                            },
                    Parameters = new List<NameValueParameter> { param }
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
