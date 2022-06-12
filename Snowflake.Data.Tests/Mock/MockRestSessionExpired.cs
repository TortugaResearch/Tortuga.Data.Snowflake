/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Tortuga.Data.Snowflake.Core.Messages;
using Tortuga.Data.Snowflake.Core.RequestProcessing;

namespace Tortuga.Data.Snowflake.Tests.Mock;

class MockRestSessionExpired : IMockRestRequester
{
	static private readonly String EXPIRED_SESSION_TOKEN = "session_expired_token";

	static private readonly String TOKEN_FMT = "Snowflake Token=\"{0}\"";

	static private readonly int SESSION_EXPIRED_CODE = 390112;

	public string FirstTimeRequestID;

	public string SecondTimeRequestID;

	public MockRestSessionExpired()
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
					Token = EXPIRED_SESSION_TOKEN,
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
			if (sfRequest.AuthorizationToken.Equals(String.Format(TOKEN_FMT, EXPIRED_SESSION_TOKEN)))
			{
				FirstTimeRequestID = ExtractRequestID(sfRequest.Url.Query);
				QueryExecResponse queryExecResponse = new QueryExecResponse
				{
					Success = false,
					Code = SESSION_EXPIRED_CODE
				};
				return Task.FromResult<T>((T)(object)queryExecResponse);
			}
			else if (sfRequest.AuthorizationToken.Equals(String.Format(TOKEN_FMT, "new_session_token")))
			{
				SecondTimeRequestID = ExtractRequestID(sfRequest.Url.Query);
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
		else if (sfRequest.JsonBody is RenewSessionRequest)
		{
			return Task.FromResult<T>((T)(object)new RenewSessionResponse
			{
				Success = true,
				Data = new RenewSessionResponseData()
				{
					SessionToken = "new_session_token",
					MasterToken = "new_master_token"
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

	static string ExtractRequestID(string queries)
	{
		int start = queries.IndexOf("requestId=");
		start += 10;
		return queries.Substring(start, 36);
	}

	public void setHttpClient(HttpClient httpClient)
	{
		// Nothing to do
	}
}
