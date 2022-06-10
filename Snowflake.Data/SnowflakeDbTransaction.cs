/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using System.Data;
using System.Data.Common;

namespace Tortuga.Data.Snowflake;

public class SnowflakeDbTransaction : DbTransaction
{
    readonly SnowflakeDbConnection m_Connection;
    bool m_Disposed;
    readonly IsolationLevel m_IsolationLevel;

    public SnowflakeDbTransaction(IsolationLevel isolationLevel, SnowflakeDbConnection connection)
    {
        if (isolationLevel != IsolationLevel.ReadCommitted)
            throw new ArgumentOutOfRangeException(nameof(isolationLevel), isolationLevel, "Only IsolationLevel.ReadCommitted is supported.");

        m_IsolationLevel = isolationLevel;
        m_Connection = connection;

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "BEGIN";
            command.ExecuteNonQuery();
        }
    }

    public override IsolationLevel IsolationLevel => m_IsolationLevel;

    protected override DbConnection DbConnection => m_Connection;

    public override void Commit()
    {
        using (var command = m_Connection.CreateCommand())
        {
            command.CommandText = "COMMIT";
            command.ExecuteNonQuery();
        }
    }

    public override void Rollback()
    {
        using (var command = m_Connection.CreateCommand())
        {
            command.CommandText = "ROLLBACK";
            command.ExecuteNonQuery();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (m_Disposed)
            return;

        // Rollback the uncommitted transaction when the connection is open
        if (m_Connection != null && m_Connection.IsOpen())
        {
            // When there is no uncommitted transaction, Snowflake would just ignore the rollback request;
            try
            {
                Rollback();
            }
            catch { } ///Don't allow exceptions to escape the Dispose method.
		}
        m_Disposed = true;

        base.Dispose(disposing);
    }
}
