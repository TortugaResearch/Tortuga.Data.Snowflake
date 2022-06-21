## Version 3.0.0

Refactoring efforts complete. First NuGet drop.

## Version 3.0.2

Revert names of public classes to `SnowflakeDbXxx` insead of `SFXxxx`. This was a bad idea that makes adoption from the legacy version harder than necessary for minimal gains.

## Version 3.1.0

### Features

[Support AnsiString, AnsiStringFixedLength when converting DbType to SFDataType #3
](https://github.com/TortugaResearch/Tortuga.Data.Snowflake/issues/3)

ANSI strings are fully supported by `SnowflakeDbDataType.Text`.

[Added constructor overload for Command and Connection #4](https://github.com/TortugaResearch/Tortuga.Data.Snowflake/issues/4)

These constructors are standard for ADO.NET implementations.

[Add asynchronous query abilities #5](https://github.com/TortugaResearch/Tortuga.Data.Snowflake/issues/5)

This allows queries to continue to run after a connection is close. Later the result can be read from a separate connection.
