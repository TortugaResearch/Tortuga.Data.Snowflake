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

## Round 7 - Organizing Core

With roughly 60 classes, the `Core` namespace is rather crowded and it's hard to see what features and subsystems are covered by it. 

Sometimes the best way to organize code is by guessing. 

1. Pick a word. In our first attempt, we'll choose "Chunk".
2. Move every class that has the word in its name into a new folder.
3. Right-click on the folder and select "Sync Namespace with Folder Structure". This will cause the code that uses the moved classes to be modified with a new `using` statement. 
4. Review each modified class to see if it should also be moved into the new folder.
5. Repeat steps 3 and 4 until no further classes should be moved.
6. Review the results for overall consistency. Can you describe in plain English what features the folder is responsible for?

Our second word is "Session", which is the `SFSession` class and its dependencies.

Next up is "ResultSet". Upon reviewing this, it seems that "ResultSet" is the core concept and "Chunk" refers to its implementation details. So we'll rename the `Chunks` folder to `ResultSets`.

Then we go through the remaining classes to see if they are only used by classes in a particular folder. That's a hint suggesting they should be moved.

The enum `SFDataType` belongs in the `Client` folder, as that's where the primary publich API for the library lives and it's needed for `SnowflakeDbParameter`.

The file `Snowflake.cs` isn't even a class. It is just assembly-level attributes, so it should be moved to the root and renamed `Assembly.cs`.

After sweeping a few more classes into `ResultSets`, we can start looking at request processing. This namespace will deal with constructing URLs and making outbound requests. Since the naming isn't as consistent as we would need for the previous strategy, we'll instead choose a cornerstone class. Starting with `BaseRestRequest`, we'll iteratively move classes into the new folder based on how they relate to this one.

Since we're calling this folder `RequestProcessing`, it makes sense to rename the `ResultSets` folder to `ResponseProcessing`. Now we have two namespaces with clearly defined roles.

Looking at the usage of `BaseRestRequest`, it is a base class for classes throughout the code. Being a more general-purpose class, we put it back and choose a different cornerstone. The `RestRequester` could be a better candidate. It is only used by `SFSession`, and that's a different concept.

While that seems to fit, `RestRequester` only brings with it the `IRestRequester` interface. So we look for another candidate, `SFRestRequest`. This brings with it `SFStatement`.

After sorting several more classes into the message, request, response, or session folder, we circle back to `BaseRestRequest` and once again move it into `RequestProcessing`. You may find yourself frequently changing your mind when trying to sort out a large namespace. Don't worry about it; it's just part of the process.

In the end we came up with these file counts.

* Core: 19
* Messages: 28
* RequestProcessing: 7
* ResponseProcessing: 12
* ResponseProcessing/Chunks: 17
* Sessions: 12

Where did `ResponseProcessing/Chunks` come from? Well at 29 files, `ResponseProcessing` started getting big again. And since all of the chunks code is only referenced by `SFResultSet`, it can be easily pulled into its own subsystem. 

We left the `Messages` large mainly because we rarely look at it. There is no code, only simple DTOs, so charting the interactions between classes isn't as necessary.

## Round 8 - Enumerations

### SFDataType
If an eager developer decides to alphabetize this enum, it will change the number assignments. As we don't currently know if they are important, it's best to just lock those into place by explicitly numbering them.

```
public enum SFDataType
{
	None = 0,
	FIXED = 1,
	[...]
```

While this isn't necessary for application code, in a library extra care is needed to avoid breaking changes.

### SFError

This is a weird one. Instead of just reading the value of the enum directly, it makes a reflection call to get the number from an attribute. Not only is this an expensive call, developers may not even realize it is necessary. Most will assume the enum is the actual error code.

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

Ideally, we would shadow the base class's `ErrorCode` method, but we can't override and shadow a method at the same time.

### SFStatementType

The `SFStatementType` enum is treated the same as `SFError`, except it needs to be a `long`.

### SFSessionProperty

This is another example of using attributes on enums. 

```
	[SFSessionPropertyAttr(required = true)]
	PASSWORD,

	[SFSessionPropertyAttr(required = false, defaultValue = "443")]
	PORT,
```

At first glance this seems to be a more reasonable use, but when we dig deeper we find a very serious problem.

The code below treats attributes on the `SFSessionProperty` enums as a hidden global variable. If after making a request using a proxy, you then make a request that doesn't use a proxy it will behave incorrectly.

```
// Based on which proxy settings have been provided, update the required settings list
if (useProxy)
{
    // If useProxy is true, then proxyhost and proxy port are mandatory
    SFSessionProperty.ProxyHost.GetAttribute<SFSessionPropertyAttribute>().Required = true;
    SFSessionProperty.ProxyPort.GetAttribute<SFSessionPropertyAttribute>().Required = true;

    // If a username is provided, then a password is required
    if (properties.ContainsKey(SFSessionProperty.ProxyUser))
    {
        SFSessionProperty.ProxyPassword.GetAttribute<SFSessionPropertyAttribute>().Required = true;
    }
}
```

This code will have to be removed and replaced with `CheckSessionProperties(properties, useProxy);`. After which we can correct the mistakes in`SFSessionPropertyAttr`. 

This `SFSessionPropertyAttr` is being used correctly, but there are a couple of minor implementation mistakes. 

```
class SFSessionPropertyAttr : Attribute
{
    public string defaultValue { get; set; }
    public bool required { get; set; }
}
```

Specifically, the mistakes are:

1. The name should have the suffix `Attribute`. The C# compiler looks for this suffix and allows you to omit it when applying an attribute to a construct.
2. It is missing the `AttributeUsage` attribute. This tells the compiler where an attribute can be used so that it can warn the programmer of mistakes.
3. The property names are cased incorrectly.

The fixed attribute can be seen below.

```
[AttributeUsage(AttributeTargets.Field)]
class SFSessionPropertyAttribute : Attribute
{
    public string DefaultValue { get; init; }
    public bool Required { get; init; }
}
```

The usage of the attribute also changes slightly. 

Before:

```
[SFSessionPropertyAttr(required = false)]
SCHEMA,

[SFSessionPropertyAttr(required = false, defaultValue = "https")]
SCHEME,

[SFSessionPropertyAttr(required = true, defaultValue = "")]
USER,
```

After

```
[SFSessionProperty]
SCHEMA,

[SFSessionProperty(DefaultValue = "https")]
SCHEME,

[SFSessionProperty(Required = true, DefaultValue = "")]
USER,
```


#### Legacy Framework Compatibility

To support versions prior to .NET 5, this code is needed. Without it, the `init` keyword doesn't compile.

```
#if !NET5_0_OR_GREATER

namespace System.Runtime.CompilerServices;

internal static class IsExternalInit { }

#endif
```

## Round 9  - SnowflakeDbCommand

### Field Types

For some reason the type of the `connection` field is `DbConnection` instead of `SnowflakeDbConnection`. Which in turn opens the door for unsafe casts such as:

```
var session = (connection as SnowflakeDbConnection).SfSession;
```

### Fixing the error messages

What do all of these error conditions have in common?

```
// Unsetting connection not supported.
throw new SnowflakeDbException(SFError.UNSUPPORTED_FEATURE);

// Must be of type SnowflakeDbConnection.
throw new SnowflakeDbException(SFError.UNSUPPORTED_FEATURE);

// Connection already set.
throw new SnowflakeDbException(SFError.UNSUPPORTED_FEATURE);
```

They all return exactly the same error message. The actual error desciption is only available as a comment. We can fix that, and at the same time put in more appropriate exception types.

```
throw new InvalidOperationException("Unsetting the connection not supported.");

throw new ArgumentException("Connection must be of type SnowflakeDbConnection.", nameof(DbConnection));

throw new InvalidOperationException("Connection already set.");
```

### Pattern Matching to clarify intent

Error messages aside, this series of `if` statements can be hard to follow.

```
set
{
	if (value == null)
	{
		if (connection == null)
		{
			return;
		}

		// Unsetting connection not supported.
		throw new SnowflakeDbException(SFError.UNSUPPORTED_FEATURE);
	}

	if (!(value is SnowflakeDbConnection))
	{
		// Must be of type SnowflakeDbConnection.
		throw new SnowflakeDbException(SFError.UNSUPPORTED_FEATURE);
	}

	var sfc = (SnowflakeDbConnection)value;
	if (connection != null && connection != sfc)
	{
		// Connection already set.
		throw new SnowflakeDbException(SFError.UNSUPPORTED_FEATURE);
	}

	connection = sfc;
	sfStatement = new SFStatement(sfc.SfSession);
}
```

The first change is to move the guard statement up top. If the connection is already set, there is no reason to go through the rest of the branches.

The we can switch on the `value`. Now it's easy to see the three possible values by glancing at the `case` labels.


```
set
{
	if (connection != null && connection != value)
		throw new InvalidOperationException("Connection already set.");

	switch (value)
	{
		case null:
			if (connection == null)
				return;
			else
				throw new InvalidOperationException("Unsetting the connection not supported.");

		case SnowflakeDbConnection sfc:
			connection = sfc;
			sfStatement = new SFStatement(sfc.SfSession);
			return;

		default:
			throw new ArgumentException("Connection must be of type SnowflakeDbConnection.", nameof(DbConnection));
	}
}
```

### Lost Stack Traces

A common beginner mistake is to discard the stack trace of an exception.

```
catch (Exception ex)
{
	logger.Error("The command failed to execute.", ex);
	throw ex;
}
```

This can be fixed by using `throw;` instead of `throw ex;`.

### Static methods

The static method `convertToBindList` is only called in two places. In both cases, it's only argument is a field in the same class. Which means that it can be converted into a instance method that reads the field directly.

### Collection return types

Instead of returning a null, the method `convertToBindList` could return an empty dictionary. At the cost of a small allocation, we can eliminate the need for null checks down the line.

Speaking of which, **nullable reference types** are not enabled in this library. That will have to be addressed.

### Exception Types
 
The type of exception being thrown is important to caller. It gives them hints about what went wrong so they know where to start their research. Some exceptions such as `InvalidOperationException` say "you can't do that now" while `NotSupportedException` says "you can never do that".

```
throw new Exception("Can't execute command when connection has never been opened");
```

This error message has the word "when" in it, suggesting that it should be a `InvalidOperationException`. 

For more information on this concept, see [Designing with Exceptions in .NET
SEP](https://www.infoq.com/articles/Exceptions-API-Design/).

### ExecuteDbDataReader(CommandBehavior behavior)

This method and its async version are ignoring then `CommandBehavior` parameter. Fixing this will require a change to `SnowflakeDbDataReader` as well.

### AllowNull and Properties

This is the code from `DBConnection`, as reported by Visual Studio.

```
        public abstract string ConnectionString
        {
            get;
            [param: AllowNull]
            set;
        }
```

For our first attempt, we just copy it into `SnowflakeDbConnection`.

```
public override string ConnectionString { get; [param: AllowNull] set; }
```

But that gives this compiler error.

```
Error	CS8765	Nullability of type of parameter 'value' doesn't match overridden member (possibly because of nullability attributes).
```

What it really wants is this. 

```
[AllowNull]
public override string ConnectionString { get; set; }
```

But now we have a non-nullable property that can store and return a null. To fix that issue, we need to normalize incoming nulls to empty strings.

```
string _connectionString = "";

[AllowNull]
public override string ConnectionString { 
    get => _connectionString; 
    set => _connectionString = value ?? ""; 
}
```

### Legacy Support

Since the AllowNull attribute doesn’t exist in older frameworks, we need to add it to the project with a conditional compiler flag.

```
#if !NETCOREAPP3_1_OR_GREATER
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false)]
    sealed class AllowNullAttribute : Attribute
    {
        public AllowNullAttribute()
        {
        }
    }
}
#endif
``` 

### Naming Conventions

The fields in this project use a mixture of `camelCase`, `_camelCase`, and `PascalCase`. The normal `camelCase` is not acceptable because it conflicts with parameter names in constructors. And `PascalCase` conflicts with property names when properties need to be manually implemented. That leaves `_camelCase` as the natural choice for this project. And if this was a straight refactoring job, that's what we'd use.

However, the intention is to make this into a maintained **Tortuga Research** project. As such, we're going to go with with the `m_PascalCase` naming convention to be consisent with other **Tortuga Research** projects. 

### Blocking Methods with Obsolete 

Since the `Prepare` method isn't implemented, we can block calls to it at compile time using `Obsolete` and hide it from the IDE using `EditorBrowsable`. And we'll change it to a `NotSupportedException` to make it clear that it isn't a planned feature for the future.


```
[Obsolete($"The method {nameof(Prepare)} is not implemented.", true)]
[EditorBrowsable(EditorBrowsableState.Never)]
public override void Prepare() => throw new NotSupportedException();
```	

### Nullable Reference Types

Enabling Nullable Reference Types reveals a bug in the `SetStatement` method. Specifically, it is missing a null check on the connection parameter.

In some of the methods, you will also see this pattern.

```
SetStatement();
return sfStatement.Execute[...]
```

The null checker doesn't understand this. It can't detect that `SetStatement` ensures `sfStatement` isn't null.

We can, however, change `SetStatement` to return a non-null reference to `sfStatement`. (We would have liked to remove the `sfStatement` entirely, but it's needed for the `Cancel` method.)

Another change we can make is removing the initializer for `sfStatement` in the `DbConnection` property. This isn't needed because it will just be replaced when the command is executed.


### Cleaning up the constructors

The `CommandTimeout` is 0, so we don't need to explicitly set it.

The `parameterCollection` field can be initialized in its declaration.

### Blocking unsupported setters

As with the `SnowflakeDbCommandBuilder`, properties whose setters shouldn't be called can be blocked. And since they don't provide useful information, the property will be hidden as well.

```
[EditorBrowsable(EditorBrowsableState.Never)]
public override bool DesignTimeVisible
{
	get => false;

	[Obsolete($"The {nameof(DesignTimeVisible)} property is not supported.", true)]
	set => throw new NotSupportedException($"The {nameof(DesignTimeVisible)} property is not supported.");
}
```

### Readonly fields

Where possible, fields are marked as `readonly` instead of `private`. 

This doesn't help performance, but it does act as a form of documentation. Developers glancing at the list of fields can quickly spot which ones are mutable.