/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

#nullable enable

using System.Text;
using Tortuga.Data.Snowflake.Core.FileTransfer;
using Tortuga.Data.Snowflake.Core.Messages;
using Tortuga.Data.Snowflake.Core.ResponseProcessing;
using Tortuga.Data.Snowflake.Core.Sessions;

namespace Tortuga.Data.Snowflake.Core.RequestProcessing;

class SFStatement
{
	internal SFSession SFSession { get; set; }

	private const string SF_QUERY_CANCEL_PATH = "/queries/v1/abort-request";

	private const string SF_AUTHORIZATION_SNOWFLAKE_FMT = "Snowflake Token=\"{0}\"";

	private const int SF_SESSION_EXPIRED_CODE = 390112;

	private const int SF_QUERY_IN_PROGRESS = 333333;

	private const int SF_QUERY_IN_PROGRESS_ASYNC = 333334;

	private string? m_RequestId;

	private readonly object m_RequestIdLock = new();

	private readonly IRestRequester m_RestRequester;

	private CancellationTokenSource? m_TimeoutTokenSource;

	// Merged cancellation token source for all cancellation signal.
	// Cancel callback will be registered under token issued by this source.
	private CancellationTokenSource? m_LinkedCancellationTokenSource;

	// Flag indicating if the SQL query is a regular query or a PUT/GET query
	internal bool m_IsPutGetQuery = false;

	internal SFStatement(SFSession session)
	{
		SFSession = session;
		m_RestRequester = session.restRequester;
	}

	private void AssignQueryRequestId()
	{
		lock (m_RequestIdLock)
		{
			if (m_RequestId != null)
				throw new SnowflakeDbException(SFError.STATEMENT_ALREADY_RUNNING_QUERY);

			m_RequestId = Guid.NewGuid().ToString();
		}
	}

	private void ClearQueryRequestId()
	{
		lock (m_RequestIdLock)
			m_RequestId = null;
	}

	private SFRestRequest BuildQueryRequest(string sql, Dictionary<string, BindingDTO>? bindings, bool describeOnly)
	{
		AssignQueryRequestId();

		var startTime = DateTime.UtcNow - new DateTime(1970, 1, 1);
		var secondsSinceEpoch = Convert.ToInt64(startTime.TotalMilliseconds).ToString();
		var parameters = new Dictionary<string, string?>()
			{
				{ RestParams.SF_QUERY_REQUEST_ID, m_RequestId },
				{ RestParams.SF_QUERY_REQUEST_GUID, Guid.NewGuid().ToString() },
				{ RestParams.SF_QUERY_START_TIME, secondsSinceEpoch },
			};

		var queryUri = SFSession.BuildUri(RestPath.SF_QUERY_PATH, parameters);

		var postBody = new QueryRequest()
		{
			sqlText = sql,
			parameterBindings = bindings,
			describeOnly = describeOnly,
		};

		return new SFRestRequest
		{
			Url = queryUri,
			authorizationToken = string.Format(SF_AUTHORIZATION_SNOWFLAKE_FMT, SFSession.sessionToken),
			serviceName = SFSession.ParameterMap.ContainsKey(SFSessionParameter.SERVICE_NAME)
							? (string)SFSession.ParameterMap[SFSessionParameter.SERVICE_NAME] : null,
			jsonBody = postBody,
			HttpTimeout = Timeout.InfiniteTimeSpan,
			RestTimeout = Timeout.InfiniteTimeSpan,
			isPutGet = m_IsPutGetQuery
		};
	}

	private SFRestRequest BuildResultRequest(string resultPath)
	{
		var uri = SFSession.BuildUri(resultPath);
		return new SFRestRequest()
		{
			Url = uri,
			authorizationToken = String.Format(SF_AUTHORIZATION_SNOWFLAKE_FMT, SFSession.sessionToken),
			HttpTimeout = Timeout.InfiniteTimeSpan,
			RestTimeout = Timeout.InfiniteTimeSpan
		};
	}

	private void CleanUpCancellationTokenSources()
	{
		if (m_LinkedCancellationTokenSource != null)
		{
			// This should also take care of cleaning up the cancellation callback that was registered.
			// https://github.com/microsoft/referencesource/blob/master/mscorlib/system/threading/CancellationTokenSource.cs#L552
			m_LinkedCancellationTokenSource.Dispose();
			m_LinkedCancellationTokenSource = null;
		}
		if (m_TimeoutTokenSource != null)
		{
			m_TimeoutTokenSource.Dispose();
			m_TimeoutTokenSource = null;
		}
	}

	private SFBaseResultSet BuildResultSet(QueryExecResponse response, CancellationToken cancellationToken)
	{
		if (response.success)
			return new SFResultSet(response.data!, this, cancellationToken);

		throw new SnowflakeDbException(response.data!.sqlState!, response.code, response.message, response.data!.queryId!);
	}

	/// <summary>
	///     Register cancel callback. Two factors: either external cancellation token passed down from upper
	///     layer or timeout reached. Whichever comes first would trigger query cancellation.
	/// </summary>
	/// <param name="timeout">query timeout. 0 means no timeout</param>
	/// <param name="externalCancellationToken">cancellation token from upper layer</param>
	private void registerQueryCancellationCallback(int timeout, CancellationToken externalCancellationToken)
	{
		m_TimeoutTokenSource = timeout > 0 ? new CancellationTokenSource(timeout * 1000) : new CancellationTokenSource(Timeout.InfiniteTimeSpan);

		m_LinkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(m_TimeoutTokenSource.Token, externalCancellationToken);

		if (!m_LinkedCancellationTokenSource.IsCancellationRequested)
		{
			m_LinkedCancellationTokenSource.Token.Register(() =>
				{
					try
					{
						Cancel();
					}
					catch
					{
						// Prevent an unhandled exception from being thrown
					}
				});
		}
	}

	private bool RequestInProgress(BaseRestResponse? r) => r?.code == SF_QUERY_IN_PROGRESS || r?.code == SF_QUERY_IN_PROGRESS_ASYNC;

	private bool SessionExpired(BaseRestResponse? r) => r?.code == SF_SESSION_EXPIRED_CODE;

	internal async Task<SFBaseResultSet> ExecuteAsync(int timeout, string sql, Dictionary<string, BindingDTO> bindings, bool describeOnly, CancellationToken cancellationToken)
	{
		registerQueryCancellationCallback(timeout, cancellationToken);
		var queryRequest = BuildQueryRequest(sql, bindings, describeOnly);
		try
		{
			QueryExecResponse? response = null;
			bool receivedFirstQueryResponse = false;
			while (!receivedFirstQueryResponse)
			{
				response = await m_RestRequester.PostAsync<QueryExecResponse>(queryRequest, cancellationToken).ConfigureAwait(false);
				if (SessionExpired(response))
				{
					SFSession.renewSession();
					queryRequest.authorizationToken = string.Format(SF_AUTHORIZATION_SNOWFLAKE_FMT, SFSession.sessionToken);
				}
				else
				{
					receivedFirstQueryResponse = true;
				}
			}

			var lastResultUrl = response!.data!.getResultUrl;

			while (RequestInProgress(response) || SessionExpired(response))
			{
				var req = BuildResultRequest(lastResultUrl!);
				response = await m_RestRequester.GetAsync<QueryExecResponse>(req, cancellationToken).ConfigureAwait(false);

				if (SessionExpired(response))
					SFSession.renewSession();
				else
					lastResultUrl = response.data?.getResultUrl;
			}

			return BuildResultSet(response, cancellationToken);
		}
		finally
		{
			CleanUpCancellationTokenSources();
			ClearQueryRequestId();
		}
	}

	internal SFBaseResultSet Execute(int timeout, string sql, Dictionary<string, BindingDTO> bindings, bool describeOnly)
	{
		// Trim the sql query and check if this is a PUT/GET command
		string trimmedSql = TrimSql(sql);

		try
		{
			if (trimmedSql.StartsWith("PUT") || trimmedSql.StartsWith("GET"))
			{
				m_IsPutGetQuery = true;
				var response = ExecuteHelper<PutGetExecResponse, PutGetResponseData>(timeout, sql, bindings, describeOnly);

				var fileTransferAgent = new SFFileTransferAgent(trimmedSql, SFSession, response.data!, CancellationToken.None);

				// Start the file transfer
				fileTransferAgent.execute();

				// Get the results of the upload/download
				return fileTransferAgent.result();
			}
			else
			{
				registerQueryCancellationCallback(timeout, CancellationToken.None);
				var queryRequest = BuildQueryRequest(sql, bindings, describeOnly);
				QueryExecResponse? response = null;

				bool receivedFirstQueryResponse = false;
				while (!receivedFirstQueryResponse)
				{
					response = m_RestRequester.Post<QueryExecResponse>(queryRequest);
					if (SessionExpired(response))
					{
						SFSession.renewSession();
						queryRequest.authorizationToken = string.Format(SF_AUTHORIZATION_SNOWFLAKE_FMT, SFSession.sessionToken);
					}
					else
					{
						receivedFirstQueryResponse = true;
					}
				}

				var lastResultUrl = response?.data?.getResultUrl;
				while (RequestInProgress(response) || SessionExpired(response))
				{
					var req = BuildResultRequest(lastResultUrl!);
					response = m_RestRequester.Get<QueryExecResponse>(req);

					if (SessionExpired(response))
					{
						SFSession.renewSession();
					}
					else
					{
						lastResultUrl = response.data?.getResultUrl;
					}
				}
				return BuildResultSet(response!, CancellationToken.None);
			}
		}
		finally
		{
			CleanUpCancellationTokenSources();
			ClearQueryRequestId();
		}
	}

	private SFRestRequest? BuildCancelQueryRequest()
	{
		lock (m_RequestIdLock)
		{
			if (m_RequestId == null)
				return null;

			var parameters = new Dictionary<string, string>()
				{
					{ RestParams.SF_QUERY_REQUEST_ID, Guid.NewGuid().ToString() },
					{ RestParams.SF_QUERY_REQUEST_GUID, Guid.NewGuid().ToString() },
				};
			var uri = SFSession.BuildUri(SF_QUERY_CANCEL_PATH, parameters);

			var postBody = new QueryCancelRequest()
			{
				requestId = m_RequestId
			};

			return new SFRestRequest()
			{
				Url = uri,
				authorizationToken = string.Format(SF_AUTHORIZATION_SNOWFLAKE_FMT, SFSession.sessionToken),
				jsonBody = postBody
			};
		}
	}

	internal void Cancel()
	{
		var request = BuildCancelQueryRequest();
		if (request == null)
		{
			CleanUpCancellationTokenSources();
			return;
		}

		m_RestRequester.Post<NullDataResponse>(request);

		CleanUpCancellationTokenSources();
	}

	/// <summary>
	/// Execute a sql query and return the response.
	/// </summary>
	/// <param name="timeout">The query timeout.</param>
	/// <param name="sql">The sql query.</param>
	/// <param name="bindings">Parameter bindings or null if no parameters.</param>
	/// <param name="describeOnly">Flag indicating if this will only return the metadata.</param>
	/// <returns>The response data.</returns>
	/// <exception>The http request fails or the response code is not succes</exception>
	internal T ExecuteHelper<T, U>(int timeout, string sql, Dictionary<string, BindingDTO>? bindings, bool describeOnly)
		where T : BaseQueryExecResponse<U>
		where U : IQueryExecResponseData
	{
		registerQueryCancellationCallback(timeout, CancellationToken.None);
		var queryRequest = BuildQueryRequest(sql, bindings, describeOnly);
		try
		{
			T? response = null;
			var receivedFirstQueryResponse = false;
			while (!receivedFirstQueryResponse)
			{
				response = m_RestRequester.Post<T>(queryRequest);
				if (SessionExpired(response))
				{
					SFSession.renewSession();
					queryRequest.authorizationToken = string.Format(SF_AUTHORIZATION_SNOWFLAKE_FMT, SFSession.sessionToken);
				}
				else
				{
					receivedFirstQueryResponse = true;
				}
			}

			if (typeof(T) == typeof(QueryExecResponse))
			{
				var queryResponse = (QueryExecResponse)(object)response!;
				var lastResultUrl = queryResponse.data!.getResultUrl;
				while (RequestInProgress(queryResponse) || SessionExpired(queryResponse))
				{
					var req = BuildResultRequest(lastResultUrl!);
					response = m_RestRequester.Get<T>(req);

					if (SessionExpired(response))
						SFSession.renewSession();
					else
						lastResultUrl = queryResponse.data?.getResultUrl;
				}
			}

			if (!response!.success)
				throw new SnowflakeDbException(response.data!.sqlState!, response.code, response.message, response.data!.queryId!);

			return response;
		}
		finally
		{
			ClearQueryRequestId();
		}
	}

	/// <summary>
	/// Trim the query by removing spaces and comments at the beginning.
	/// </summary>
	/// <param name="originalSql">The original sql query.</param>
	/// <returns>The query without the blanks and comments at the beginning.</returns>
	private static string TrimSql(string originalSql)
	{
		var sqlQueryBuf = originalSql.ToCharArray();
		var builder = new StringBuilder();

		// skip old c-style comment
		var idx = 0;
		var sqlQueryLen = sqlQueryBuf.Length;
		do
		{
			if (('/' == sqlQueryBuf[idx]) && (idx + 1 < sqlQueryLen) && ('*' == sqlQueryBuf[idx + 1]))
			{
				// Search for the matching */
				var matchingPos = originalSql.IndexOf("*/", idx + 2);
				if (matchingPos >= 0) // Found the comment closing, skip to after
					idx = matchingPos + 2;
			}
			else if ((sqlQueryBuf[idx] == '-') && (idx + 1 < sqlQueryLen) && (sqlQueryBuf[idx + 1] == '-'))
			{
				// Search for the new line
				var newlinePos = originalSql.IndexOf("\n", idx + 2);

				if (newlinePos >= 0) // Found the new line, skip to after
					idx = newlinePos + 1;
			}

			builder.Append(sqlQueryBuf[idx]);
			idx++;
		}
		while (idx < sqlQueryLen);

		var trimmedQuery = builder.ToString();

		return trimmedQuery;
	}
}
