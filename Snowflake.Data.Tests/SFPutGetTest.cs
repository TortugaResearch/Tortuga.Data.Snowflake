/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using NUnit.Framework;
using System.Data;
using System.Runtime.InteropServices;

namespace Tortuga.Data.Snowflake.Tests;

[TestFixture]
class SFPutGetTest : SFBaseTest
{
	[Test]
	[TestCase("gzip")]
	[TestCase("bzip2")]
	[TestCase("brotli")]
	[TestCase("deflate")]
	[TestCase("raw_deflate")]
	[TestCase("zstd")]
	public void TestPutGetCommand(string compressionType)
	{
		var DATABASE_NAME = TestConfig.Database;
		var SCHEMA_NAME = TestConfig.Schema;
		const string TEST_TEMP_TABLE_NAME = "TEST_TEMP_TABLE_NAME";
		const string TEST_TEMP_STAGE_NAME = "TEST_TEMP_STAGE_NAME";

		const string USER_STAGE = "USER_STAGE";
		const string TABLE_STAGE = "TABLE_STAGE";
		const string NAMED_STAGE = "NAMED_STAGE";

		const string FALSE_COMPRESS = "FALSE";
		const string TRUE_COMPRESS = "TRUE";

		const string UPLOADED = "UPLOADED";
		const string DOWNLOADED = "DOWNLOADED";

		const string COL1 = "C1";
		const string COL2 = "C2";
		const string COL3 = "C3";
		const string COL1_DATA = "FIRST";
		const string COL2_DATA = "SECOND";
		const string COL3_DATA = "THIRD";
		const string ROW_DATA =
		  COL1_DATA + "," + COL2_DATA + "," + COL3_DATA + "\n" +
		  COL1_DATA + "," + COL2_DATA + "," + COL3_DATA + "\n" +
		  COL1_DATA + "," + COL2_DATA + "," + COL3_DATA + "\n" +
		  COL1_DATA + "," + COL2_DATA + "," + COL3_DATA + "\n";

		var createTable = $"create or replace table {TEST_TEMP_TABLE_NAME} ({COL1} STRING," +
		$"{COL2} STRING," +
		$"{COL3} STRING)";
		var createStage = $"create or replace stage {TEST_TEMP_STAGE_NAME}";

		var copyIntoTable = $"COPY INTO {TEST_TEMP_TABLE_NAME}";
		var copyIntoStage = $"COPY INTO {TEST_TEMP_TABLE_NAME} FROM @{DATABASE_NAME}.{SCHEMA_NAME}.{TEST_TEMP_STAGE_NAME}";

		var removeFile = $"REMOVE @{DATABASE_NAME}.{SCHEMA_NAME}.%{TEST_TEMP_TABLE_NAME}";
		var removeFileUser = $"REMOVE @~/";

		var dropStage = $"DROP STAGE IF EXISTS {TEST_TEMP_STAGE_NAME}";
		var dropTable = $"DROP TABLE IF EXISTS {TEST_TEMP_TABLE_NAME}";

		var stageTypes = new[] { USER_STAGE, TABLE_STAGE, NAMED_STAGE };
		var autoCompressTypes = new[] { FALSE_COMPRESS, TRUE_COMPRESS };

		foreach (var stageType in stageTypes)
		{
			foreach (var autoCompressType in autoCompressTypes)
			{
				using (var conn = new SFConnection())
				{
					conn.ConnectionString = ConnectionString;
					conn.Open();

					// Create a temp file with specified file extension
					var filePath = Path.GetTempPath() + Guid.NewGuid().ToString() + ".csv" +
						(autoCompressType == FALSE_COMPRESS ? "" : "." + compressionType);
					// Write row data to temp file
					File.WriteAllText(filePath, ROW_DATA);

					var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
					Directory.CreateDirectory(tempDirectory);

					var putQuery = "";
					if (stageType == USER_STAGE)
					{
						putQuery = $"PUT file://{filePath} @~/";
					}
					else if (stageType == TABLE_STAGE)
					{
						putQuery = $"PUT file://{filePath} @{DATABASE_NAME}.{SCHEMA_NAME}.%{TEST_TEMP_TABLE_NAME}";
					}
					else if (stageType == NAMED_STAGE)
					{
						putQuery = $"PUT file://{filePath} @{DATABASE_NAME}.{SCHEMA_NAME}.{TEST_TEMP_STAGE_NAME}";
					}

					var getQuery = $"GET @{DATABASE_NAME}.{SCHEMA_NAME}.%{TEST_TEMP_TABLE_NAME} file://{tempDirectory}";

					var fileName = "";
					var copyIntoUser = $"COPY INTO {TEST_TEMP_TABLE_NAME} FROM @~/";
					if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
					{
						fileName = filePath.Substring(filePath.LastIndexOf('\\') + 1);
						removeFileUser += fileName;
						copyIntoUser += fileName;
					}
					else
					{
						fileName = filePath.Substring(filePath.LastIndexOf('/') + 1);
						removeFileUser += fileName;
						copyIntoUser += fileName;
					}

					// Windows user contains a '~' in the path which causes an error
					if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
					{
						if (stageType == USER_STAGE)
						{
							putQuery = $"PUT file://C:\\\\Users\\{Environment.UserName}\\AppData\\Local\\Temp\\{fileName} @~/";
						}
						else if (stageType == TABLE_STAGE)
						{
							putQuery = $"PUT file://C:\\\\Users\\{Environment.UserName}\\AppData\\Local\\Temp\\{fileName} @{DATABASE_NAME}.{SCHEMA_NAME}.%{TEST_TEMP_TABLE_NAME}";
						}
						else if (stageType == NAMED_STAGE)
						{
							putQuery = $"PUT file://C:\\\\Users\\{Environment.UserName}\\AppData\\Local\\Temp\\{fileName} @{DATABASE_NAME}.{SCHEMA_NAME}.{TEST_TEMP_STAGE_NAME}";
						}
					}

					// Add PUT compress option
					putQuery += $" AUTO_COMPRESS={autoCompressType}";

					using (var command = conn.CreateCommand())
					{
						// Create temp table
						command.CommandText = createTable;
						command.ExecuteNonQuery();

						// Create temp stage
						command.CommandText = createStage;
						command.ExecuteNonQuery();

						// Upload file
						command.CommandText = putQuery;
						var reader = command.ExecuteReader();
						while (reader.Read())
						{
							// Check file status
							Assert.AreEqual(reader.GetString(4), UPLOADED);
							// Check source and destination compression type
							if (autoCompressType == FALSE_COMPRESS)
							{
								Assert.AreEqual(reader.GetString(6), "none");
								Assert.AreEqual(reader.GetString(7), "none");
							}
							else
							{
								Assert.AreEqual(reader.GetString(6), compressionType);
								Assert.AreEqual(reader.GetString(7), compressionType);
							}
						}

						// Copy into temp table
						if (stageType == USER_STAGE)
						{
							command.CommandText = copyIntoUser;
						}
						else if (stageType == TABLE_STAGE)
						{
							command.CommandText = copyIntoTable;
						}
						else if (stageType == NAMED_STAGE)
						{
							command.CommandText = copyIntoStage;
						}
						command.ExecuteNonQuery();

						// Check contents are correct
						command.CommandText = $"SELECT * FROM {TEST_TEMP_TABLE_NAME}";
						reader = command.ExecuteReader();
						while (reader.Read())
						{
							Assert.AreEqual(reader.GetString(0), COL1_DATA);
							Assert.AreEqual(reader.GetString(1), COL2_DATA);
							Assert.AreEqual(reader.GetString(2), COL3_DATA);
						}

						// Check row count is correct
						command.CommandText = $"SELECT COUNT(*) FROM {TEST_TEMP_TABLE_NAME}";
						Assert.AreEqual(command.ExecuteScalar(), 4);

						// Download file
						command.CommandText = getQuery;
						reader = command.ExecuteReader();
						while (reader.Read())
						{
							// Check file status
							Assert.AreEqual(reader.GetString(4), DOWNLOADED);
						}

						// Delete downloaded files
						Directory.Delete(tempDirectory, true);

						// Remove files from staging
						command.CommandText = removeFile;
						command.ExecuteNonQuery();

						// Remove user file from staging
						command.CommandText = removeFileUser;
						command.ExecuteNonQuery();

						// Drop temp stage
						command.CommandText = dropStage;
						command.ExecuteNonQuery();

						// Drop temp table
						command.CommandText = dropTable;
						command.ExecuteNonQuery();
					}

					// Delete temp file
					File.Delete(filePath);

					conn.Close();
					Assert.AreEqual(ConnectionState.Closed, conn.State);
				}
			}
		}
	}
}
