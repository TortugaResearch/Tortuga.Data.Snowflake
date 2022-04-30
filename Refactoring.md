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

### Framework Version

Tests failed with .NET 4.6.2 with a platform not supported exception, so the minimum version was left at 4.7.2.


### Unit Tests

After adding a `parameters.json` file, all tests are still failing with this error message. 

```
	OneTimeSetUp: System.IO.FileNotFoundException : Could not find file 'C:\WINDOWS\system32\parameters.json'.
```

The problem is in this line of code:

```
StreamReader reader = new StreamReader("parameters.json");
```

An application's current directory is not guaranteed, so tests should use absolute paths. 

```
var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "parameters.json");
StreamReader reader = new StreamReader(path);
```

The connection string builder (in the tests, not `SnowflakeDbConnectionStringBuilder`) also needed to be changed to ignore the warehouse and role parameters.

At this point all remaining tests are passing. 

### Fixing the build script

Consider this section of `Snowflake.Data.Tests.csproj`

```
  <Target Name="CopyCustomContent" AfterTargets="AfterBuild">
	<Copy SourceFiles="parameters.json" DestinationFolder="$(OutDir)" />
	<Copy SourceFiles="App.config" DestinationFolder="$(OutDir)" />
  </Target>
```

Does it work? Yes, mostly.
Should you do it this way? No.

Set the "Copy to Output Directory" flag so that other developers will understand your intentions.

```
  <ItemGroup>
	<None Update="App.config">
	  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
	</None>
	<None Update="parameters.json">
	  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
	</None>
  </ItemGroup>
```

And while we're at it, remove this silliness.

```
  <ItemGroup>
	<Folder Include="Properties\" />
  </ItemGroup>
```

It doesn't hurt anything, but it makes a non-existent folder appear in the solution explorer.

## Round 1 - Formatting
* General code formatting with CodeMaid and `.editorconfig`
* C# 10 features enabled
* Implicit using statements turned on
* Change the namespaces to use the `Tortuga` prefix.
* Remove unused namespaces

The members in the classes cannot be sorted. This causes the `TestSimpleLargeResultSet` test to fail. The reason has not yet been determined, but most likely has something to do with serialization.

## Round 2 - File Scoped Namespaces

Part of the readability issue in this project is that the statements are so long that they needed to be broken over multiple lines. 

By using `namespace Snowflake.Data.Core.Authenticator;` instead of,

```
namespace Snowflake.Data.Core.Authenticator;
{
```

we remove a level of indentation. Four spaces don’t sound like much, but it can be enough to get everything onto one line. Especially when combined with other techniques that we'll be discussing later.

