namespace Tortuga.Data.Snowflake;

public class SnowflakeDbQueryStatus
{
	/// <summary>
	/// When true, indicates that the query has finished for one reason or another, and there is no reason to wait further for
	/// the query to finish.  If false, the query is still executing, so the result will not be available.
	/// </summary>
	public bool IsQueryDone { get; }

	/// <summary>
	/// true indicates that the query completely finished running without any issues, so the result is available.  false indicates
	/// the result is not ready.  You need to inspect <see cref="IsQueryDone"/> to determine if the query is still running
	/// as opposed to encountering an error.
	/// </summary>
	public bool IsQuerySuccessful { get; }

	/// <summary>
	/// The id used to track the query in Snowflake.
	/// </summary>
	public string QueryId { get; }

	public SnowflakeDbQueryStatus(string queryId, bool isQueryDone, bool isQuerySuccessful)
	{
		QueryId = queryId;
		IsQueryDone = isQueryDone;
		IsQuerySuccessful = isQuerySuccessful;
	}
}
