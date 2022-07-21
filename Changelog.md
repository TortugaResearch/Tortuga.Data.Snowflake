## Version 3.1.0

### Features

[#3 Support AnsiString, AnsiStringFixedLength when converting DbType to SFDataType](https://github.com/TortugaResearch/Tortuga.Data.Snowflake/issues/3)

ANSI strings are fully supported by `SnowflakeDbDataType.Text`.

[Added constructor overload for Command and Connection #4](https://github.com/TortugaResearch/Tortuga.Data.Snowflake/issues/4)

These constructors are standard for ADO.NET implementations.

[#5 Add asynchronous query abilities](https://github.com/TortugaResearch/Tortuga.Data.Snowflake/issues/5)

This allows queries to continue to run after a connection is close. Later the result can be read from a separate connection.


#10 Add strongly named properties to SnowflakeDbConnectionStringBuilder](https://github.com/TortugaResearch/Tortuga.Data.Snowflake/issues/10)

## Version 3.0.2

Revert names of public classes to `SnowflakeDbXxx` insead of `SFXxxx`. This was a bad idea that makes adoption from the legacy version harder than necessary for minimal gains.

## Version 3.0.0

Refactoring efforts complete. First NuGet drop.




