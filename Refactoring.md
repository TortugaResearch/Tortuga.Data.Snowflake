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

```csharp
StreamReader reader = new StreamReader("parameters.json");
```

An application's current directory is not guaranteed, so tests should use absolute paths. 

```csharp
var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "parameters.json");
StreamReader reader = new StreamReader(path);
```

The connection string builder (in the tests, not `SnowflakeDbConnectionStringBuilder`) also needed to be changed to ignore the warehouse and role parameters.

At this point all remaining tests are passing. 

### Fixing the build script

Consider this section of `Snowflake.Data.Tests.csproj`

```xml
  <Target Name="CopyCustomContent" AfterTargets="AfterBuild">
	<Copy SourceFiles="parameters.json" DestinationFolder="$(OutDir)" />
	<Copy SourceFiles="App.config" DestinationFolder="$(OutDir)" />
  </Target>
```

Does it work? Yes, mostly.
Should you do it this way? No.

Set the "Copy to Output Directory" flag so that other developers will understand your intentions.

```xml
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

```xml
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

```csharp
namespace Snowflake.Data.Core.Authenticator;
{
```

we remove a level of indentation. Four spaces don’t sound like much, but it can be enough to get everything onto one line. Especially when combined with other techniques that we'll be discussing later.

At the same time, we'll do another round of cleaning for the files that have two sets of using statements. 

```csharp
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

```csharp
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

```csharp
[SFErrorAttr(errorCode = 270001)]
INTERNAL_ERROR,

_errorCode = error.GetAttribute<SFErrorAttr>().errorCode;
```

This easy solution to this is:

```csharp
readonly SFError _error;

_errorCode = error;


public override int ErrorCode => (int)_errorCode;
```

In order for that to work, the `SFError` enum needs to be renumbered.

```csharp
public enum SFError
{
	INTERNAL_ERROR = 270001,
	COLUMN_INDEX_OUT_OF_BOUND = 270002,
	INVALID_DATA_CONVERSION = 270003,
```

And now `SFErrorAttr` can be deleted. 

Then to make the error codes meaningful, we add this property:

```csharp
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

```csharp
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

```csharp
public enum SFDataType
{
	None = 0,
	FIXED = 1,
	[...]
```

While this isn't necessary for application code, in a library extra care is needed to avoid breaking changes.

### SFError

This is a weird one. Instead of just reading the value of the enum directly, it makes a reflection call to get the number from an attribute. Not only is this an expensive call, developers may not even realize it is necessary. Most will assume the enum is the actual error code.

```csharp
[SFErrorAttr(errorCode = 270001)]
INTERNAL_ERROR,

_errorCode = error.GetAttribute<SFErrorAttr>().errorCode;
```

This easy solution to this is:

```csharp
readonly SFError _error;

_errorCode = error;


public override int ErrorCode => (int)_errorCode;
```

In order for that to work, the `SFError` enum needs to be renumbered.

```csharp
public enum SFError
{
	INTERNAL_ERROR = 270001,
	COLUMN_INDEX_OUT_OF_BOUND = 270002,
	INVALID_DATA_CONVERSION = 270003,
```

And now `SFErrorAttr` can be deleted. 

Then to make the error codes meaningful, we add this property:

```csharp
public SFError SFErrorCode => _errorCode;
```

Ideally, we would shadow the base class's `ErrorCode` method, but we can't override and shadow a method at the same time.

### SFStatementType

The `SFStatementType` enum is treated the same as `SFError`, except it needs to be a `long`.

### SFSessionProperty

This is another example of using attributes on enums. 

```csharp
	[SFSessionPropertyAttr(required = true)]
	PASSWORD,

	[SFSessionPropertyAttr(required = false, defaultValue = "443")]
	PORT,
```

At first glance this seems to be a more reasonable use, but when we dig deeper we find a very serious problem.

The code below treats attributes on the `SFSessionProperty` enums as a hidden global variable. If after making a request using a proxy, you then make a request that doesn't use a proxy it will behave incorrectly.

```csharp
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

```csharp
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

```csharp
[AttributeUsage(AttributeTargets.Field)]
class SFSessionPropertyAttribute : Attribute
{
    public string DefaultValue { get; init; }
    public bool Required { get; init; }
}
```

The usage of the attribute also changes slightly. 

Before:

```csharp
[SFSessionPropertyAttr(required = false)]
SCHEMA,

[SFSessionPropertyAttr(required = false, defaultValue = "https")]
SCHEME,

[SFSessionPropertyAttr(required = true, defaultValue = "")]
USER,
```

After

```csharp
[SFSessionProperty]
SCHEMA,

[SFSessionProperty(DefaultValue = "https")]
SCHEME,

[SFSessionProperty(Required = true, DefaultValue = "")]
USER,
```


#### Legacy Framework Compatibility

To support versions prior to .NET 5, this code is needed. Without it, the `init` keyword doesn't compile.

```csharp
#if !NET5_0_OR_GREATER

namespace System.Runtime.CompilerServices;

internal static class IsExternalInit { }

#endif
```

## Round 9  - SnowflakeDbCommand

### Field Types

For some reason the type of the `connection` field is `DbConnection` instead of `SnowflakeDbConnection`. Which in turn opens the door for unsafe casts such as:

```csharp
var session = (connection as SnowflakeDbConnection).SfSession;
```

### Fixing the error messages

What do all of these error conditions have in common?

```csharp
// Unsetting connection not supported.
throw new SnowflakeDbException(SFError.UNSUPPORTED_FEATURE);

// Must be of type SnowflakeDbConnection.
throw new SnowflakeDbException(SFError.UNSUPPORTED_FEATURE);

// Connection already set.
throw new SnowflakeDbException(SFError.UNSUPPORTED_FEATURE);
```

They all return exactly the same error message. The actual error desciption is only available as a comment. We can fix that, and at the same time put in more appropriate exception types.

```csharp
throw new InvalidOperationException("Unsetting the connection not supported.");

throw new ArgumentException("Connection must be of type SnowflakeDbConnection.", nameof(DbConnection));

throw new InvalidOperationException("Connection already set.");
```

### Pattern Matching to clarify intent

Error messages aside, this series of `if` statements can be hard to follow.

```csharp
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


```csharp
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

```csharp
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

```csharp
throw new Exception("Can't execute command when connection has never been opened");
```

This error message has the word "when" in it, suggesting that it should be a `InvalidOperationException`. 

For more information on this concept, see [Designing with Exceptions in .NET
SEP](https://www.infoq.com/articles/Exceptions-API-Design/).

### ExecuteDbDataReader(CommandBehavior behavior)

This method and its async version are ignoring then `CommandBehavior` parameter. Fixing this will require a change to `SnowflakeDbDataReader` as well.

### AllowNull and Properties

This is the code from `DBConnection`, as reported by Visual Studio.

```csharp
        public abstract string ConnectionString
        {
            get;
            [param: AllowNull]
            set;
        }
```

For our first attempt, we just copy it into `SnowflakeDbConnection`.

```csharp
public override string ConnectionString { get; [param: AllowNull] set; }
```

But that gives this compiler error.

```csharp
Error	CS8765	Nullability of type of parameter 'value' doesn't match overridden member (possibly because of nullability attributes).
```

What it really wants is this. 

```csharp
[AllowNull]
public override string ConnectionString { get; set; }
```

But now we have a non-nullable property that can store and return a null. To fix that issue, we need to normalize incoming nulls to empty strings.

```csharp
string _connectionString = "";

[AllowNull]
public override string ConnectionString { 
    get => _connectionString; 
    set => _connectionString = value ?? ""; 
}
```

### Legacy Support

Since the AllowNull attribute doesn’t exist in older frameworks, we need to add it to the project with a conditional compiler flag.

```csharp
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


```csharp
[Obsolete($"The method {nameof(Prepare)} is not implemented.", true)]
[EditorBrowsable(EditorBrowsableState.Never)]
public override void Prepare() => throw new NotSupportedException();
```	

### Nullable Reference Types

Enabling Nullable Reference Types reveals a bug in the `SetStatement` method. Specifically, it is missing a null check on the connection parameter.

In some of the methods, you will also see this pattern.

```csharp
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

```csharp
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
However, the intention is to make this into a maintained **Tortuga Research** project. As such, we're going to go with with the `m_PascalCase` naming convention to be consisent with other **Tortuga Research** projects. 

## Round 10 - SnowflakeDbCommandBuilder

### Readonly Properties

The properties `DbCommandBuilder.QuotePrefix` and `DbCommandBuilder.QuoteSuffix` have setters because some databases support more than one quoting style.

Since that's not the case for Snowflake, the properties should be changed to return a constant and the setter marked as not supported. To make this 100% clear to callers, we can further restrict it by marking it as obsolete. 

```csharp
[Obsolete($"The property {nameof(QuoteSuffix)} cannot be changed.", true)]
set => throw new NotSupportedException($"The property {nameof(QuotePrefix)} cannot be changed.");
```


### Numeric Formatting

In a few places, we see lines of code like this:

```csharp
return string.Format(CultureInfo.InvariantCulture, "{0}", parameterOrdinal);
```

While it will return the correct value, the `Format` function is slow and shouldn't be used if alternatives such as `ToString` are available.

```csharp
return parameterOrdinal.ToString(CultureInfo.InvariantCulture);
```

Here's another example,

```csharp
return string.Format(CultureInfo.InvariantCulture, "{0}", parameterName);
```

We can remove the `Format` call by transforming it into this:

```csharp
return parameterName.ToString(CultureInfo.InvariantCulture);
```

And according to the documentation, that just returns the original string. Leaving us with:

```csharp
return parameterName;
```

### Nullable Reference Types

Enabling nullable reference types in this file is as easy as adding `#nullable enable` and a couple of `?` characters.


## Round 11 - SnowflakeDbConnection

### Nullability Issues

In several places, `taskCompletionSource.SetResult` is called with a `null`. While not really problematic, this causes the null checker to complain. So a statically defined marker object can be passed to it instead.

```csharp
static object s_markerObject = new();

taskCompletionSource.SetResult(s_markerObject);
```

The `Database` property is defined by the expression 

```csharp
_connectionState == ConnectionState.Open ? SfSession?.database : string.Empty;`
```

This is safe, but the null checker doesn't known that. Thankfully, it can be simplified to just `SfSession?.database ?? ""`.

The `SfSession` property is set by a call to `SetSession`, so again the null checker doesn't understand. Fortunately this only occurs in 2 places, so the `!` modifer can quiet the compiler.

## Finalizer

This class has a finalizer. Which means `GC.SuppressFinalize(this)` must be added to the `Dispose(bool)` method to avoid unnecessarily adding the object to the finalizer queue.

It should be noted that Microsoft no longer recommends implementing finalizers. So we will just eliminate it.


### Evaluate single-line, private methods.

Consider this method.

```csharp
private void OnSessionEstablished()
{
	_connectionState = ConnectionState.Open;
}
```

Its sole purpose is to set the connection state to `Open`. There's no reason to believe it will grow with time. Nor are there any complex expressions that need to avoid duplicating. So this method can be inlined. 

### Dispose Pattern

Normally calls to `Close` are simply forwarded to `Dispose`. But in the case of `DbConnection`, the script is flipped. The call to `Dispose` is forwarded to `Close`. And even after the object is closed/disposed, it can be reopened. 

In order to properly support this, a change needs to be made.


```csharp
protected override void Dispose(bool disposing)
{
	//Remove this check, it prevents a re-opened connection from being disposed.
	//if (_disposed)
	//    return;

	try
	{
		this.Close();
	}
	catch (Exception ex)
	{
		// Prevent an exception from being thrown when disposing of this object
	}

	//we no longer read from this field
	//_disposed = true;

	//Not needed. ComponentBase.Dispose(bool) does nothing.
	//base.Dispose(disposing);
}
```

The exception being swallowed is a common design pattern for .NET. It is needed for `finally` blocks to work correctly.

## Round 12 - SnowflakeDbDataAdapter

* Removed unused fields.
* Removed unused private constructor.
* Removed unnecessary call to `GC.SuppressFinalize`.
* Removed the explicit interface implementation.
* Fixed the strongly typed methods.

On that last point, this code is wrong.

```csharp
new public SnowflakeDbCommand SelectCommand
{
	get { return _selectCommand; }
	set { _selectCommand = value; }
}
```

If the caller uses `DbDataAdapter.SelectCommand` instead of `SnowflakeDbDataAdapter.SelectCommand`, they will get the wrong value.

Instead, the code should have looked like this:

```csharp
new public SnowflakeDbCommand? SelectCommand
{
	get { return (SnowflakeDbCommand?)base.SelectCommand; }
	set { base.SelectCommand = value; }
}
```

Note how it reuses the base class's property for storing the value. This isn't 100% type safe, but it's what we need to do in order to prevent hard to detect bugs.

## Round 13 - SnowflakeDbDataReader

As per modern conventions, we're going to use expression bodies for simple methods and properties. If the expression has something interesting that the developer needs to be aware of, we'll leave it as the older block style. For example, in `GetByte` we want to call out that all but the first element in the byte array is being discarded. So, we leave that on its own line.

```csharp
public override byte GetByte(int ordinal)
{
	var bytes = m_ResultSet.GetValue<byte[]>(ordinal);
	return bytes[0];
}
```

We are also using `var` where possible to reduce unnecessary boilerplate. This will also make refactoring easier, as types won't need to be changed in multiple places.

### Implement GetEnumerator

The basic pattern for implementing `GetEnumerator` for a `DBDataReader` is this: 

```csharp

public override IEnumerator GetEnumerator()
{
	return new Enumerator(this);
}

class Enumerator : IEnumerator
{
	SnowflakeDbDataReader _parent;
	public Enumerator(SnowflakeDbDataReader parent)
	{
		_parent = parent;
	}

	object IEnumerator.Current => _parent;

	bool IEnumerator.MoveNext()
	{
		return _parent.Read();
	}

	void IEnumerator.Reset()
	{
		throw new NotSupportedException();
	}
}
```

With `yield return`, we can simplify it to:

```csharp
public override IEnumerator GetEnumerator()
{
	while (Read())
	{
		yield return this;
	}
}
```

### CommandBehavior.CloseConnection

When this flag is set, closing the `DbDataReader` must result in the associated connection being closed.

### Use nameof

This is a minor change, but it is less error prone to use `nameof(dataOffset)` instead of `"dataOffset"`.

### Hide NextResult

This method always returns false, so we'll mark it as `EditorBrowsableState.Never`. But since calling it won't throw an exception, we don't need to take the extra step of marking it obsolete.

### ArgumentOutOfRangeException

These are modified to return the invalid value as part of the exception.

```csharp
throw new ArgumentOutOfRangeException(nameof(dataOffset), "Non negative number is required.");

throw new ArgumentOutOfRangeException(nameof(dataOffset), dataOffset, "Non negative number is required.");
```

### Constructor Parameter Checking

In theory the parameters for the `SnowflakeDbDataReader` will never be null. In practice mistakes happen, so null checks are added as a precaution.

```csharp
internal SnowflakeDbDataReader(SFBaseResultSet resultSet, SnowflakeDbConnection connection, CommandBehavior commandBehavior)
{
	m_ResultSet = resultSet ?? throw new ArgumentNullException(nameof(resultSet), $"{nameof(resultSet)} is null."); ;
	m_Connection = connection ?? throw new ArgumentNullException(nameof(connection), $"{nameof(connection)} is null.");
	m_CommandBehavior = commandBehavior;
	m_SchemaTable = PopulateSchemaTable(resultSet);
	RecordsAffected = resultSet.CalculateUpdateCount();
}
```

## Round 14 – SnowflakeDbParameter

### Remove Unnecessary Initializers

Remove the assignments for `SFDataType` and `OriginType` in the default constructor as they will be automatically initialized to that value.

### Fix parameter names in constructors

By convention, parameters are in `camelCase`.

Again, we'll mark the overridden property as obsolete. 

### Nullability

Enabling null checking on this class catches potential null reference exceptions in `SnowflakeDbCommand`.

## Round 15 - SnowflakeDbParameterCollection

This is an old collection design from a time when .NET didn't have generics. So a lot of runtime casts are needed.

### SyncRoot

This should be a read-only object field. 

It should have never existed in the first place. But it does, so it should be honored.

### Add

The `tryCastThrow` method isn't needed. The built-in type check when casting is sufficient.

A type-safe version of `Add` should be created.

### AddRange

This code:

```csharp
public override void AddRange(Array values)
{
	IEnumerator e = values.GetEnumerator();
	while (e.MoveNext())
	{
		parameterList.Add(tryCastThrow(e.Current));
	}
}
```

Can be converted into a normal for-each loop:

```csharp
public override void AddRange(Array values)
{
	foreach(SnowflakeDbParameter value in values)
		_parameterList.Add(value);
}
``` 

### CopyTo 

The base case is easy to implement with a simple cast.

```csharp
public override void CopyTo(Array array, int index)
{
	_parameterList.CopyTo((SnowflakeDbParameter[])array, index);
}
```

For the full version, a bit more is needed.

```csharp
public override void CopyTo(Array array, int index)
{
	if (array is SnowflakeDbParameter[] sTypedArray)
		m_ParameterList.CopyTo(sTypedArray, index);
	else if (array is DbParameter[] dTypedArray)
		for (var i = 0; i < m_ParameterList.Count; i++)
			dTypedArray[i + index] = m_ParameterList[i];
	else if (array is IDataParameter[] iTypedArray)
		for (var i = 0; i < m_ParameterList.Count; i++)
			iTypedArray[i + index] = m_ParameterList[i];
	else if (array is IDbDataParameter[] idTypedArray)
		for (var i = 0; i < m_ParameterList.Count; i++)
			idTypedArray[i + index] = m_ParameterList[i];
}
```

Arguably we didn't need to take it quite this far, but it will make it easier on the caller. 

Though now we run into a code duplication issue. To resolve it, we can leverage array covariance and cast the target collection to `object[]`.

```csharp
public override void CopyTo(Array array, int index)
{
	switch (array)
	{
		case SnowflakeDbParameter[] sTypedArray:
			m_ParameterList.CopyTo(sTypedArray, index);
			break;

		case DbParameter[]:
		case IDbDataParameter[]:
		case IDataParameter[]:
			var untypedArray = (object[])array;
			for (var i = 0; i < m_ParameterList.Count; i++)
				untypedArray[i + index] = m_ParameterList[i];
			break;

		default: throw new InvalidCastException($"{nameof(array)} is not a supported array type.");
	}
}
```

But what if there is another valid array type that we're not aware of? We can just try it and allow the runtime to throw the InvalidCastException.

```csharp
public override void CopyTo(Array array, int index)
{
	if (array is SnowflakeDbParameter[] sTypedArray)
		m_ParameterList.CopyTo(sTypedArray, index);
	else
	{
		var untypedArray = (object[])array;
		for (var i = 0; i < m_ParameterList.Count; i++)
			untypedArray[i + index] = m_ParameterList[i];
	}
}
```

At this point, we have to ask why have the special case at all?

```csharp
public override void CopyTo(Array array, int index)
{
	var untypedArray = (object[])array;
	for (var i = 0; i < m_ParameterList.Count; i++)
		untypedArray[i + index] = m_ParameterList[i];
}
```


### IndexOf

This function:

```csharp
public override int IndexOf(string parameterName)
{
	int index = 0;
	foreach (SnowflakeDbParameter parameter in _parameterList)
	{
		if (String.Compare(parameterName, parameter.ParameterName) == 0)
		{
			return index;
		}
		index++;
	}
	return -1;
}
```

should be just a normal for loop.

```csharp
public override int IndexOf(string parameterName)
{
	for (int i = 0; i < _parameterList.Count; i++)
		if (_parameterList[i].ParameterName == parameterName)
			return i;
	return -1;
}
```

### Fields should be private

There is no need to mark `_parameterList` internal. Anything that reads it can read the class itself.

For `for` loops, this property is needed.

```csharp
public new SnowflakeDbParameter this[int index]
{
	get => _parameterList[index];
	set => _parameterList[index] = value;
}
```

## Round 16 - SnowflakeDbTransaction

### Finalizer

Again, finalizers are no longer recommended. 

### Isolation Level

This should throw an `ArgumentOutOfRangeException` in the constructor.

### Dispose

Just like in `SnowflakeDbConnection`, this should swallow exceptions.



## Round 17 - AuthenticatorFactory

The code in this class is surprisingly hard to read. There's not much going on, but the lines are so long that the important parts get lost in the noise. Fortunately, there are a few tricks to deal with that.

```csharp
if (!session.properties.TryGetValue(SFSessionProperty.PRIVATE_KEY_FILE, out var pkPath) &&
	!session.properties.TryGetValue(SFSessionProperty.PRIVATE_KEY, out var pkContent))
```

### Local variables


Since `session.properties` is used a lot, we can capture it in a local variable.

```csharp
var properties = session.properties;

[...]

if (!properties.TryGetValue(SFSessionProperty.PRIVATE_KEY_FILE, out var pkPath) &&
	!properties.TryGetValue(SFSessionProperty.PRIVATE_KEY, out var pkContent))
```

### Static Usings


Next, we introduce a `static using` declaration so we don't need to repeat the enum name.

```csharp
using static Snowflake.Data.Core.SFSessionProperty;

[...]

if (!properties.TryGetValue(PRIVATE_KEY_FILE, out var pkPath) &&
	!properties.TryGetValue(PRIVATE_KEY, out var pkContent))
```

### Discards

The output parameters of the `TryGet` methods aren't being used, so we can use discards.

```csharp
if (!properties.TryGetValue(PRIVATE_KEY_FILE, out _) &&
	!properties.TryGetValue(PRIVATE_KEY, out _))
```

### Correct Methods

Though in this case, if we really don't need the value then we can choose a different method. 

```csharp
if (!properties.ContainsKey(PRIVATE_KEY_FILE) &&
	!properties.ContainsKey(PRIVATE_KEY))
```

### Array type inference

Here is another trick to remove boiler plate.

```csharp
throw new SnowflakeDbException(
	SFError.INVALID_CONNECTION_STRING,
	new object[] { invalidStringDetail });
```

The type `object` is not necessary, as the compiler can infer if from the context.

```csharp
throw new SnowflakeDbException(
	SFError.INVALID_CONNECTION_STRING,
	new[] { invalidStringDetail });
```

Technically it's inferring a `string[]` array, because that's the type of object being put inside it. But arrays are 'covariant', which means you can give a `string[]` array to a function that expects an `object[]` array.

### Params

A strange thing about this call is that we didn't need to create the array at all. The parameter is marked with `params`, which means the compiler will create the array for us.

```csharp
throw new SnowflakeDbException(
	SFError.INVALID_CONNECTION_STRING,
	invalidStringDetail);
```

### File Scoped Namespaces

Part of the readability issue is that the statements were so long that they needed to be broken over multiple lines. 

By using `namespace Snowflake.Data.Core.Authenticator;` instead of,

```csharp
namespace Snowflake.Data.Core.Authenticator;
{
```

we remove a level of indentation. Four spaces don’t sound like much, but it can be enough to get everything onto one line. Especially when combined with other techniques shown above.


### Validation

Validation for each authenticator is performed in the `AuthenticationFactory` rather than the authenticators themselves. 

This is a problem because if those authenticators are created via any other means, the validation won't be performed.

It can be fixed by moving the validation into the constructor of each authenticator class.

## Round 18 - BaseAuthenticator and its Subclasses

### Protected Fields

In ideomatic .NET code, fields are almost always private. Normally a protected field should be converted into a property with proper access controls.

#### ClientEnv

The `ClientEnv` field is easy. We just need to change it from `protected` to `readonly`.

#### session

The `session` field is slightly trickier. It is set by the base class constructor, and then again in the subclass constructor. Once this redundancy is removed, it can be made `readonly`.

It still needs to be `protected` because at least one subclass reads from it. So we mark it as a readonly property instead.

#### authName

The last field has a few interesting characteristics.

* Its value is determined by a constant in each subclass (except `OktaAuthenticator`).
* It should never be modified.

This sounds more like an abstract, read-only property than a field.

### ref parameters

The `SetSpecializedAuthenticatorData` method incorrectly marks its parameter as `ref`. If a subclass were to replace the parameter `data` insead of just modifying it, the code would fail.

```csharp
protected abstract void SetSpecializedAuthenticatorData(ref LoginRequestData data);
```

Fortunatelly none of the subclasses does this, so the `ref` flag can be removed.

### Constants vs Read-only Fields

In most subclasses, there is a static field named `AUTH_NAME`. This should be a constant.

### IAuthenticator interface

Every subclass of `BaseAuthenticator` implement this interface, so it can be moved into that class. 

Normally the interface methods would be exposed in the base class as `abstract` methods. But the interface methods `Authenticate` and `AuthenticateAsync` always call `Login` and `LoginAsync`.

This means we can combine them. Simply rename `Authenticate` and `AuthenticateAsync` to be `Login` and `LoginAsync`. Then make the real `Login` and `LoginAsync` methods `virtual`. 

### Sync Over Async

There are places where `Task.Run` is used haphazardly to call asynchronous methods from a synchronous context. This is essentially caused by the design of `HttpClient`, which originally didn't have any synchronous methods, and the design of `System.Data`, which originally only had synchronous methods.

To address this, a set of extension methods were created. For down-level clients, these simulate the core synchronous methods from .NET Core 5. Then it stacks on synchronous methods for all of the asynchronous methods that doesn't exist yet.

While not a perfect solution, it will allow the library to slowly eliminate the 'sync over async' situations as `HttpClient` is improved.

## Round 19 - IAuthenticator and BaseAuthenticator

This interface is marked as `internal` and thus can't be used for mocking. The only thing that implements it is `BaseAuthenticator`. At this point there is no purpose to it and thus it can be deleted.

To match .NET naming conventions for base classes, `BaseAuthenticator` simply becomes `Authenticator`. The `Base` prefix is unnecessary.

Another naming convention is that namespaces should be plural. So, the `Core.Authenticator` namespace is renamed `Authenticators`. 

## Round 20 - Configuration

As a public class, `SFConfiguration` is moved out of `Core` and renamed to `SnowflakeDbConfiguration` to be more consistent with other public classes.

Change the public fields into properties.

The static method `Instance` should also be a property. To make it more descriptive, it will also be renamed to `Default`.

### Global Singletons

The `SFConfiguration` class doesn't make sense as a singleton. If we really want only one instance, then we could just replace it with a static class and save some code.

But what if the application needs two instances of the configuration? Perhaps some calls need to use the V2 Json parser and some need the original.

In order to facility this, the following changes will be made.

* The `Instance` property will be renamed to `Default`.
* The `SnowflakeDbConfiguration` will be made into an immutable record. This is to avoid accidentally changing the default when you think you're changing a copy.
* The `SnowflakeDbConfiguration.Default` property will add a setter.

This gives you two ways to change the default configuration:


```csharp
//Modify a copy of existing record
SnowflakeDbConfiguration.Default = SnowflakeDbConfiguration.Default with { ChunkDownloaderVersion = 2 };

//Create a new object
SnowflakeDbConfiguration.Default = new(useV2JsonParser: true, useV2ChunkDownloader: false, chunkDownloaderVersion: 3)

```

Then to add flexability, the configuration will flow 

* The `SnowflakeDbConnection` object will have a property called `Configuration`. 
* The configuration will flow through the session and result set to the response processing components.

### SFConfigurationSectionHandler

This isn't used anywhere so it can be deleted.

## Round 21 - Core.Authenticators

* Add `#nullable enable` to every each file in the namespace. Update properties where appropriate.

## Round 22 - Core.FileTransfer.StorageClient 

* Add `#nullable enable` to each file in the namespace. Update properties where appropriate.
* Change the public fields in `EncryptionData` to properties
* Change the public fields in `KeyWrappingMetadataInfo` to properties
* Replace readonly strings with constants
* Standardize field names
* Mark fields as readonly where possible
* Replace aync calls such as `HttpClient.GetStreamAsync` with sync calls such as `HttpClient.GetStream`. This allows us to remove the `Task.Wait()` calls.
* Mark methods as static where possible.
* You don't need to check if a directory already exists before creating it. `Directory.CreateDirectory` does both.
* Add `using` statements to memory streams. (Technically not necessary, but will prevent compiler warnings later.)
* The `SFRemoteStorageUtil.DownloadOneFile` method can throw a null exception.
* Use `var` where appropriate
* In `SFSnowflakeAzureClient`, m_BlobServiceClient can be null. But some methods assume it won't be.

### Not fixed


The `SFGCSClient.GetFileHeader` method reports errors by modifying an input parameter. This is fundementally wrong, but there isn't an obvious way to fix it. And it is causing problems with null checks.

Methods in `SFRemoteStorageUtil` destroys the stack trace when there is an error.

## Round 23 - Core.FileTransfer

* Add `#nullable enable` to each file in the namespace. Update properties where appropriate.
* Use `var` where appropriate
* Add `using` statements where appropriate.
* Check parameters for null values
* Remove unused fields
* Lined up parameters in method signatures and calls

## Round 24 - Core.Messages

* Add `#nullable enable` to each file in the namespace. Update properties where appropriate.
* Assume any null checks on messages that don't currently exist aren't needed. (Long term, appropriate null checks should be added.)
* Use string interpolation

## Round 25 - Core.RequestProcessing

* Add `#nullable enable` to each file in the namespace. Update properties where appropriate.
* Mark static strings as const
* Lined up parameters in method signatures and calls
* Drop the `IRestRequest` interface. The `BaseRestRequest` serves the same purpose.
* Rename `BaseRestRequest` to just `RestRequest`
* Make `RestRequest.ToRequestMessage` abstract.
* Use `var` where appropriate
* Create real sync methods in `RestRequester`. This uses the `HttpContentSynchronously` we created in round 18.
* Fix missing stack traces in `RestRequester`
* Changed the proxy test to accept a connection timeout as a valid result.
* Updated other tests to not expect an aggregate exception

## RetryHandler

Now that we are making sync calls to `HttpClient`, we need to extend the `RetryHandler` to support them. This class modifies the behavior of a `HttpClient` directly.

A bit of non-standard code is the backoff delay. We can't cancel a Thread.Sleep with a cancellationToken, but we can simulate it using small sleeps and checking the token.

```csharp
var totalBackoffUsed = 0;
while (totalBackoffUsed < backOffInSec && !cancellationToken.IsCancellationRequested)
{
	Thread.Sleep(TimeSpan.FromSeconds(1));
	totalBackoffUsed += 1;
}
```

## Error Handling in SnowflakeDbConnection

While investigating the `RetryHandler` issue, we came across this awkward bit of error handling. 

```csharp
public override void Open()
{
	SetSession();
	try
	{
		SfSession!.Open();
	}
	catch (Exception e)
	{
		// Otherwise when Dispose() is called, the close request would timeout.
		m_ConnectionState = ConnectionState.Closed;
		if (!(e is SnowflakeDbException))
			throw new SnowflakeDbException(e, SnowflakeDbException.CONNECTION_FAILURE_SSTATE, SFError.INTERNAL_ERROR, "Unable to connect. " + e.Message);
		else
			throw;
	}
	m_ConnectionState = ConnectionState.Open;
}

public override Task OpenAsync(CancellationToken cancellationToken)
{
	RegisterConnectionCancellationCallback(cancellationToken);
	SetSession();

	return SfSession!.OpenAsync(cancellationToken).ContinueWith(
		previousTask =>
		{
			if (previousTask.IsFaulted)
			{
				// Exception from SfSession.OpenAsync
				var sfSessionEx = previousTask.Exception!;
				m_ConnectionState = ConnectionState.Closed;
				throw new SnowflakeDbException(sfSessionEx, SnowflakeDbException.CONNECTION_FAILURE_SSTATE, SFError.INTERNAL_ERROR, "Unable to connect");
			}
			else if (previousTask.IsCanceled)
			{
				m_ConnectionState = ConnectionState.Closed;
			}
			else
			{
				// Only continue if the session was opened successfully
				m_ConnectionState = ConnectionState.Open;
			}
		},
		cancellationToken);
}
```

In `Open`, the code's intent would be much clearer if multiple catch blocks were uses.

```
public override void Open()
{
	SetSession();
	try
	{
		SfSession!.Open();
	}
	catch (SnowflakeDbException)
	{
		m_ConnectionState = ConnectionState.Closed;
		throw;
	}
	catch (Exception e) when (e is not SnowflakeDbException)
	{
		// Otherwise when Dispose() is called, the close request would timeout.
		m_ConnectionState = ConnectionState.Closed;
		throw new SnowflakeDbException(e, SnowflakeDbException.CONNECTION_FAILURE_SSTATE, SFError.INTERNAL_ERROR, "Unable to connect. " + e.Message);
	}
	m_ConnectionState = ConnectionState.Open;
}
```

The `OpenAsync` function uses a really old style of working with tasks. This style was appropriate around the .NET 4 era, but shouldn't be used in modern code.

You can see here how using async/await makes the code nearly identical to the sync version.

```
public override async Task OpenAsync(CancellationToken cancellationToken)
{
	RegisterConnectionCancellationCallback(cancellationToken);
	SetSession();
	try
	{
		await SfSession!.OpenAsync(cancellationToken).ConfigureAwait(false);
	}
	catch (SnowflakeDbException)
	{
		m_ConnectionState = ConnectionState.Closed;
		throw;
	}
	catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
	{
		m_ConnectionState = ConnectionState.Closed;
		throw;
	}
	catch (Exception ex)
	{
		// Otherwise when Dispose() is called, the close request would timeout.
		m_ConnectionState = ConnectionState.Closed;
		throw new SnowflakeDbException(ex, SnowflakeDbException.CONNECTION_FAILURE_SSTATE, SFError.INTERNAL_ERROR, "Unable to connect. " + ex.Message);
	}
	m_ConnectionState = ConnectionState.Open;
}
```


## Round 26 - Core.ResponseProcessing.Chunks

* Add `#nullable enable` to each file in the namespace. Update properties where appropriate.
* Change `ParseChunk` to `ParseChunkAsync`. Add a real sync version named `ParseChunk`.
* Change callers of `ParseChunkAsync` to use `ParseChunk` when appropriate.
* Use `var` where appropriate
* Assume any null checks on messages that don't currently exist aren't needed. (Long term, appropriate null checks should be added.)
* Lined up parameters in method signatures and calls
* Mark classes static when they only contain static methods.
* Removed unnecessary Task.Run in `SFBlockingChunkDownloader.DownloadChunkAsync`
* Mark fields as readonly where possible
* Add missing `.ConfigureAwait(false)`
* Fix casing on public properties in `SFResultChunk`

## Round 27 - Core.ResponseProcessing

* Add `#nullable enable` to each file in the namespace. Update properties where appropriate.
* Use `var` where appropriate
* Mark fields as readonly where possible
* Fix casing in parameter names

### Exceptions when field or property is null

When a required field or property is null, we throw a `InvalidOperationException` to alert the caller that the object isn't capable of completing the method call. For example, 

```csharp
if (Url == null)
	throw new InvalidOperationException($"{nameof(Url)} is null");
```


### Unused, null-returning methods

This method was found in SFResultSetMetaData. It isn't used and can be removed.

```csharp
internal DataTable toDataTable()
{
	return null;
}
```

## Round 28 - Core.Sessions

* Add `#nullable enable` to each file in the namespace. Update properties where appropriate.
* Lined up parameters in method signatures and calls
* Change public fields into public properties
* Assume any null checks on messages that don't currently exist aren't needed. (Long term, appropriate null checks should be added.)
* Remove unused fields.


### HttpUtil should be a static class

While singletons are a well established pattern, they shouldn't be used indiscriminetly. The class `HttpUtil` implements no interfaces. Nor does it have a base class. So there is no reason to make it an object at all. It can instead be treated as any other static class.

### Unused assignments

In these two lines, the variable `timeoutInSec` has a value assigned to it. But before it is read, the value is over-written. So the assignment should be removed to improve clarity. This in turn means `recommendedMinTimeoutSec` can be deleted entirely.

```csharp
int recommendedMinTimeoutSec = RestRequest.DEFAULT_REST_RETRY_SECONDS_TIMEOUT;
int timeoutInSec = recommendedMinTimeoutSec;
```

### Not fixed

The `SFSessionProperties` class overrides `Equals` without overriding `GetHashCode`.

To supress the compiler warning, a fake override was created. To make it more obvious what's happening, a `#pragma warning disable` will be used instead.

```csharp
public override int GetHashCode()
{
	return base.GetHashCode();
}
```

## Round 29 - Core

* Add `#nullable enable` to each file in the namespace. Update properties where appropriate.
* Lined up parameters in method signatures and calls
* Standardize on C# names for types. For example, `int` vs `Int32`.


## Round 30 - SecretDetector

This internal class is only used by the tests, so it should be moved into the test project.

## Round 32 - String Interpolation

Use string interpolation instead of `string.Format` where appropriate. In most cases, the string interpolation will be easier to read.

```csharp
string loginTimeOut5sec = String.Format(ConnectionString + "connection_timeout={0}",	timeoutSec);
string loginTimeOut5sec = $"{ConnectionString}connection_timeout={timeoutSec}";
```

Once this change is made, it becomes more apparent that there is a `;` missing in the original code.

```csharp
string loginTimeOut5sec = $"{ConnectionString};connection_timeout={timeoutSec}";
```

## Round 33 - Nullable Reference Types

Now that all of the classes have been update to be null aware, the project can be marked with ``. This in turn means that `#nullable enable` can be removed from all of the individual files.

## Round 34 - Class Visibility

All classes, interfaces, enums, etc. in the `Core` namespace are changed from public to internal. None of these are meant to be accessed directly by users of the library.

## Round 35 - Formatting

Enable CodeMaid's Format on Save feature. Apply formatting to all files.

## Round 36 - Compiler Messages

Compiler messages are less significant than warnings. Often these are stylistic choices such as whether or not to use `var`. Occasionally, however, they do reflect a more serious issue. So it is best to clear them from time to time by either fixing the 'issue', suppressing the message, or reconfiguring the analyzer’s settings. In this project, those settings are stored in the `.editorconfig` file.

The types of issues caught in this pass included…

* Use `var` where possible.
*  Use target typed `new` where possible
* Mark fields as `readonly` where possible
* Fix spelling errors
* Use initializers
* Capitalize methods correctly 
* Capitalize properties correctly. (Use `JsonProperty` in cases where the class may be serialized, as serialization may be case sensitive.)
* Fields that are never read
* Methods that can be marked as `static`
* Use `nameof(…)` when referring to parameter names
* Replace string literals with char literals

### Constants and Unreachable Code

While adding `readonly`, it was noticed that ` INJECT_WAIT_IN_PUT` could instead be marked as a constant. Once that was done, the compiler was able to detect that a `Thread.Sleep` call could never actually occur. This demonstrates the advantage of being as strict as possible when using `readonly` and `const`.

```csharp
const int INJECT_WAIT_IN_PUT = 0;

if (INJECT_WAIT_IN_PUT > 0)
     Thread.Sleep(INJECT_WAIT_IN_PUT);
```

### Unused Fields and Parameters

One of the messages indicated that the `magicBytes` field was read, so it could be removed. This in turn meant the matching constructor parameter can be removed.

## Simplify Null Checks

The `?.` syntax can be used to remove explicit null checks.

```csharp
return v == null ? null : v.ToString();
return v?.ToString();
```

Pattern matching can also be used to remove a null check.

```csharp
var parameter = value as SnowflakeDbParameter;
return parameter != null && m_ParameterList.Contains(parameter);

return value is SnowflakeDbParameter parameter && m_ParameterList.Contains(parameter);
```


## Round 37 - Test Cleanup

* Move classes into individual files.
* Rename `SFError` to `SnowflakeError` for consistency with other type names.
* Change assertions to use enums instead of hard-coded numbers


```csharp
Assert.AreEqual(270053, e.ErrorCode);

Assert.AreEqual(SnowflakeError.UnsupportedDotnetType, e.SnowflakeError);
```

## Round 38 - Static Classes

Classes with only static members such as `ChunkDownloaderFactory` should be marked as `static`. 

Allowing objects of these types to be created is misleading to the developers, as those objects would not be useful for anything.

