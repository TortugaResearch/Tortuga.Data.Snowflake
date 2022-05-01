# Refactoring Real Code: Snowflake Connector for .NET

Normally when people talk about refactoring, they don’t show real code. Instead, you get fake examples, often so contrived that they don’t even resemble real-world code. The reader is left with an idealized vision of what refactoring looks like, where every line of code is under test and every problem neatly fits a pattern from their favorite book.

Well screw that. The real world is messy and it’s about time we acknowledge that. With countless examples of real projects in production use on GitHub, there’s no excuse to invent fake code to demonstrate refactoring.

So that’s what we’re going to do here. In this repository you get to see a real working library be incrementally refactored to improve code quality. A new branch was created for each step along the way so you can see the progression of the code from its original state to something… well not perfect, but better.

Is this the only way to clean up the code? Of course not. So I welcome you to fork this or the original and attempt your own cleanup. 

In fact, this is actually the second attempt to refactor this library. THe first time through was done as a training exercise and exploration to see what was possible. Eventually enough knowledge was gained to fix the broken tests, allowing the majority to be run for the first time. 

In that process, it was discovered that there were places in the code are highly sensitive to the names of enumerations or the order properties appear in the class. As that would be too diffcult to unwind, we instead started over with the intent to make a production-grade refactoring. 

While the work has to be redone, the knowledge wasn't lost. The [original refactoring log](OldRefactoring.md) was kept and we refer to it from time to time to guide this effort.


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

## Round 6 - Remove Logging

A low-level library such as a database driver should not mandate a specific version of a specific logging framework. What if the user of this library prefers something other than Log4Net? Or maybe they are using Log4Net, but an incompatible version of it.

In the first pass of refactoring, we removed Log4Net as the default logger. If someone wants logging, they can implement the ` SFLogger` interface with the logger of their choice.

Then we removed the proprietary `SFLogger` interface entirely. Why ask people to implement it when .NET's offers a generic `ILogger` interface? It would be really surprising to find logging framework that didn’t support `ILogger` directly.

On the third pass of this round, we looked at the actual messages. For the most part they were not useful. With trivial messages such as "All good", they read more like temporary Console output used during development than something that would be useful to developers using the library.

Furthermore, it is quite unusual for a low-level library to have a logger built into it. Logging at this level offers very little information because, being so low-level, there isn't a significant stack trace. 

So in the end we removed the majority of the logging. The one place where it looked valuable was the ` OktaAuthenticator`, which has a six-step login process. For that we wrapped it in try-catch blocks that informed the caller what step failed.

The way that works is fairly simple. A ` lastStep` variable is updated every few lines and used if there is an exception. 

```
catch (Exception ex)
{
    throw new SnowflakeDbException("Okta Authentication in " + lastStep, ex, SFError.INTERNAL_ERROR);
}
```

### Replacing HttpUtility

For some strange reason, Log4Net was also the root package of a dependency chain that included System.Web. Once removed, we also lost `HttpUtility`.

This was replaced by `WebUtility` and `QueryHelpers` (from `Microsoft.AspNetCore.WebUtilities`).
