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

## Round 1 - Formatting and Namespace Setup
* General code formatting with CodeMaid and `.editorconfig`
* C# 10 features enabled
* Implicit using statements turned on
* Change the namespaces to use the `Tortuga` prefix.
* Remove unused namespaces

### using statements

A lot of files have unused `using` statements and other basic formatting issues. Having an accurate list of `using` statements is helpful when trying to determine what a class does. So we're going to fix that.

The new Implicit Usings feature is going to be turned on at the same time. This means we don't need to see general purpose using statements that we don't care about such as `using System;` and `using System.Collections.Generic`. 

### Member Sorting

The members in the classes cannot be sorted. This causes the `TestSimpleLargeResultSet` test to fail. The reason has not yet been determined, but most likely has something to do with serialization.

## Round 2 - File Scoped Namespaces

Part of the readability issue in this project is that the statements are so long that they needed to be broken over multiple lines. 

By using `namespace Snowflake.Data.Core.Authenticator;` instead of,

```
namespace Snowflake.Data.Core.Authenticator;
{
```

we remove a level of indentation. Four spaces don’t sound like much, but it can be enough to get everything onto one line. Especially when combined with other techniques that we'll be discussing later.

At the same time, we'll do another round of cleaning for the files that have two sets of using statements. 

```
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;

namespace Snowflake.Data.Tests
{
	using NUnit.Framework;
	using Snowflake.Data.Client;
	using Snowflake.Data.Core;
```

## Round 3 - Basic File Organization

As per C# conventions, each class, interface, enum, etc. should be in its own file. This reduces the chances of a merge conflict and makes it easier to find specific classes in the solution explorer.

This reorganization operation created approximately 70 new files.


## Round 4 - Compiler Warnings

There are a handful of compiler warnings to clear. The first is to remove static fields that are never used.

Then remove pointless catch blocks.

```
catch (Exception ex)
{
	throw ex;
}
```

`SnowflakeDbException` declares `SqlState `, which shadows a property in the base class for some frameworks.

HttpRequestMessage.Properties is obsolete for some frameworks, so a pair of extension methods with compiler constants are used to resolve the conflict.

## Round 5 - SnowflakeDbException

### ErrorMessage

This field should go away. There's a perfectly suitable field in the base class to handle this.

### vendorCode

It would be nice if we could remove this field, but we can't because no constructor on DBException accepts both an error code and an inner exception. So the work-around stays. Though we will rename the field to `_errorCode` to match the property name.

### SFError

This is a weird one. Instead of just reading the value of the enum directly, it makes an expensive reflection call to get the number from an attribute.

```
[SFErrorAttr(errorCode = 270001)]
INTERNAL_ERROR,

_errorCode = error.GetAttribute<SFErrorAttr>().errorCode;
```

This easy solution to this is:

```
readonly SFError _error;

_errorCode = error;


public override int ErrorCode => (int)_errorCode;
```

In order for that to work, the `SFError` enum needs to be renumbered.

```
public enum SFError
{
	INTERNAL_ERROR = 270001,
	COLUMN_INDEX_OUT_OF_BOUND = 270002,
	INVALID_DATA_CONVERSION = 270003,
```

And now `SFErrorAttr` can be deleted. 

Then to make the error codes meaningful, we add this property:

```
public SFError SFErrorCode => _errorCode;
```

Ideally we would shadow the base class's `ErrorCode` method, but we can't override and shadow a method at the same time.

### Constants

The field `CONNECTION_FAILURE_SSTATE` should be a constant.

### Resources

The library as a whole is not internationalized, which is the only reason to add a resource file. Thus we can remove `ErrorMessages.resx`, replacing it with a simple switch block. 


