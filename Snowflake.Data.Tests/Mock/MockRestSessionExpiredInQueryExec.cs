/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Tortuga.Data.Snowflake.Core.Messages;
using Tortuga.Data.Snowflake.Core.RequestProcessing;

namespace Tortuga.Data.Snowflake.Tests.Mock;

class MockRestSessionExpiredInQueryExec : IMockRestRequester
{
    static private readonly int QUERY_IN_EXEC_CODE = 333333;

    static private readonly int SESSION_EXPIRED_CODE = 390112;

    private int getResultCallCount = 0;

    public MockRestSessionExpiredInQueryExec()
    {
    }

    public Task<T> PostAsync<T>(RestRequest request, CancellationToken cancellationToken)
    {
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
                    NameValueParameter = new List<NameValueParameter>()
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
                Success = false,
                Code = QUERY_IN_EXEC_CODE
            };
            return Task.FromResult<T>((T)(object)queryExecResponse);
        }
        else if (sfRequest.JsonBody is RenewSessionRequest)
        {
            return Task.FromResult<T>((T)(object)new RenewSessionResponse
            {
                Success = true,
                Data = new RenewSessionResponseData()
                {
                    SessionToken = "new_session_token"
                }
            });
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
        SFRestRequest sfRequest = (SFRestRequest)request;
        if (getResultCallCount == 0)
        {
            getResultCallCount++;
            QueryExecResponse queryExecResponse = new QueryExecResponse
            {
                Success = false,
                Code = QUERY_IN_EXEC_CODE
            };
            return Task.FromResult<T>((T)(object)queryExecResponse);
        }
        else if (getResultCallCount == 1)
        {
            getResultCallCount++;
            QueryExecResponse queryExecResponse = new QueryExecResponse
            {
                Success = false,
                Code = SESSION_EXPIRED_CODE
            };
            return Task.FromResult<T>((T)(object)queryExecResponse);
        }
        else if (getResultCallCount == 2 &&
            sfRequest.AuthorizationToken.Equals("Snowflake Token=\"new_session_token\""))
        {
            getResultCallCount++;
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
                    Parameters = new List<NameValueParameter>()
                }
            };
            return Task.FromResult<T>((T)(object)queryExecResponse);
        }
        else
        {
            QueryExecResponse queryExecResponse = new QueryExecResponse
            {
                Success = false,
                Code = 1
            };
            return Task.FromResult<T>((T)(object)queryExecResponse);
        }
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
