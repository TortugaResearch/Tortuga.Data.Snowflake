# Refactoring Log

This is the refactoring log for `Tortuga.Data.Snowflake`. It starts with version 2.0.11 of `Snowflake.Data`.

## Round 0 - Setup and Validation

* Rename projects and folders.
* Change supported frameworks to: net472, netstandard2.0, netcoreapp3.1, net6.0
* Fix compiler warnings for package conflicts.
* Add .editorconfig file.
* Fix tests.
* Delete tests marked as ignored.
* General cleanup of project files.

Tests failed with .NET 4.6.2 with a platform not supported exception, so the minimum version was left at 4.7.2.

At this point all remaining tests are passing. 