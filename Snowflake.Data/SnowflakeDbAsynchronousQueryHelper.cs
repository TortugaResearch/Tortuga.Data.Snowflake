﻿using Tortuga.Data.Snowflake.Core.RequestProcessing;

namespace Tortuga.Data.Snowflake;

/// <summary>
/// Methods to help perform asynchronous queries.
/// </summary>
public static class SnowflakeDbAsynchronousQueryHelper
{
	/// <summary>
	/// Starts a query asynchronously.
	/// </summary>
	/// <param name="cmd"></param>
	/// <returns>The query id.</returns>
	public static SnowflakeDbQueryStatus StartAsynchronousQuery(SnowflakeDbCommand cmd)
	{
		if (cmd == null)
			throw new ArgumentNullException(nameof(cmd), $"{nameof(cmd)} is null.");

		return cmd.StartAsynchronousQuery();
	}

	/// <summary>
	/// Starts a query asynchronously.
	/// </summary>
	/// <param name="cmd"></param>
	/// <param name="cancellationToken"></param>
	/// <returns>The query id.</returns>
	public static async Task<SnowflakeDbQueryStatus> StartAsynchronousQueryAsync(SnowflakeDbCommand cmd, CancellationToken cancellationToken)
	{
		if (cmd == null)
			throw new ArgumentNullException(nameof(cmd), $"{nameof(cmd)} is null.");

		return await cmd.StartAsynchronousQueryAsync(cancellationToken).ConfigureAwait(false);
	}

	// https://docs.snowflake.com/en/sql-reference/functions/result_scan.html
	// select * from table(result_scan('query id'));

	// https://docs.snowflake.com/en/sql-reference/functions/query_history.html
	// select *
	// from table(information_schema.query_history())
	// only returns

	// https://docs.snowflake.com/en/sql-reference/account-usage/query_history.html
	// Latency for the view may be up to 45 minutes.

	/// <summary>
	/// Use to get the status of a query to determine if you can fetch the result.
	/// </summary>
	/// <param name="conn"></param>
	/// <param name="queryId"></param>
	/// <returns></returns>
	public static SnowflakeDbQueryStatus GetQueryStatus(SnowflakeDbConnection conn, string queryId)
	{
		if (conn == null)
			throw new ArgumentNullException(nameof(conn), $"{nameof(conn)} is null.");

		if (string.IsNullOrEmpty(queryId))
			throw new ArgumentException($"{nameof(queryId)} is null or empty.", nameof(queryId));

		return GetQueryStatusAsync(conn, queryId, CancellationToken.None).Result;
	}

	/// <summary>
	/// Use to get the status of a query to determine if you can fetch the result.
	/// </summary>
	/// <param name="conn"></param>
	/// <param name="queryId"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public static async Task<SnowflakeDbQueryStatus> GetQueryStatusAsync(SnowflakeDbConnection conn,
		string queryId, CancellationToken cancellationToken)
	{
		if (conn == null)
			throw new ArgumentNullException(nameof(conn), $"{nameof(conn)} is null.");

		if (string.IsNullOrEmpty(queryId))
			throw new ArgumentException($"{nameof(queryId)} is null or empty.", nameof(queryId));

		return await GetStatusUsingRestApiAsync(conn, queryId, cancellationToken).ConfigureAwait(false);
	}

	private static async Task<SnowflakeDbQueryStatus> GetStatusUsingRestApiAsync(SnowflakeDbConnection conn, string queryId, CancellationToken cancellationToken)
	{
		var sfStatement = new SFStatement(conn.SfSession!);
		var r = await sfStatement.CheckQueryStatusAsync(0, queryId, cancellationToken).ConfigureAwait(false);
		return r;
	}

	/// <summary>
	/// Can use the resulting <see cref="SnowflakeDbCommand"/> to fetch the results of the query.
	/// </summary>
	/// <param name="conn"></param>
	/// <param name="queryId"></param>
	/// <returns></returns>
	public static SnowflakeDbCommand CreateQueryResultsCommand(SnowflakeDbConnection conn, string queryId)
	{
		if (conn == null)
			throw new ArgumentNullException(nameof(conn), $"{nameof(conn)} is null.");

		if (string.IsNullOrEmpty(queryId))
			throw new ArgumentException($"{nameof(queryId)} is null or empty.", nameof(queryId));

		return CreateQueryResultsCommandForRestApi(conn, queryId);
	}

	private static SnowflakeDbCommand CreateQueryResultsCommandForRestApi(SnowflakeDbConnection conn, string queryId)
	{
		var cmd = (SnowflakeDbCommand)conn.CreateCommand();
		cmd.HandleAsyncResponse = true;
		cmd.CommandText = queryId;
		return cmd;
	}
}
