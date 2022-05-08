## Round 0 - Initial Validation

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

After this, many of the tests still fail. This is not entirely unexpected because the documentation lacks instructions on how to setup the Snowflake database. But since this is just training exercise, we'll let that slide.

## Round 1 - Fixing the build script

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

## Round 2 - Basic File Organization

As per C# conventions, each class, interface, enum, etc. should be in its own file. This reduces the chances of a merge conflict and makes it easier to find specific classes in the solution explorer.

This reorganization operation created approximately 70 new files.


## Round 3 - File Clenaup

A lot of files have unused `using` statements and other basic formatting issues. Having an accurate list of `using` statements is helpful when trying to determine what a class does. So we're going to fix that.

The new Implicit Usings feature is going to be turned on at the same time. This means we don't need to see general purpose using statements that we don't care about such as `using System;` and `using System.Collections.Generic`. 

At the same time, we can do some other basic cleanup tasks such as alphabetizing the method names, removing excessive spacing, etc. To assist in this, we'll be using the `Code Maid` extension.

Another thing we'll do is cleaning up the files that have two sets of using statements. 

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

## Round 4 - SnowflakeDbCommand

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


## Round 5 - SnowflakeDbCommandBuilder

### Readonly Properties

The properties `DbCommandBuilder.QuotePrefix` and `DbCommandBuilder.QuoteSuffix` have setters because some databases support more than one quoting style.

Since that's not the case for Snowflake, the properties should be changed to return a constant and the setter marked as not supported. To make this 100% clear to callers, we can further restrict it by marking it as obsolete. 

```
[Obsolete($"The property {nameof(QuoteSuffix)} cannot be changed.", true)]
set => throw new NotSupportedException($"The property {nameof(QuotePrefix)} cannot be changed.");
```


### Numeric Formatting

In a few places, we see lines of code like this:

```
return string.Format(CultureInfo.InvariantCulture, "{0}", parameterOrdinal);
```

While it will return the correct value, the `Format` function is slow and shouldn't be used if alternatives such as `ToString` are available.

```
return parameterOrdinal.ToString(CultureInfo.InvariantCulture);
```

Here's another example,

```
return string.Format(CultureInfo.InvariantCulture, "{0}", parameterName);
```

We can remove the `Format` call by transforming it into this:

```
return parameterName.ToString(CultureInfo.InvariantCulture);
```

And according to the documentation, that just returns the original string. Leaving us with:

```
return parameterName;
```

### Nullable Reference Types

Enabling nullable reference types in this file is as easy as adding `#nullable enable` and a couple of `?` characters.

## Round 6 - SnowflakeDbCommand Revisted

Having picked up a couple of tricks in round 5, we return to `SnowflakeDbCommand` to do some more cleanup.

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

The log methods can be removed. We don't need to know when a DbCommand object is created.

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


## Round 7 - SnowflakeDbConnection

### Nullability Issues

In several places, `taskCompletionSource.SetResult` is called with a `null`. While not really problematic, this causes the null checker to complain. So a statically defined marker object can be passed to it instead.

```
static object s_markerObject = new();

taskCompletionSource.SetResult(s_markerObject);
```

The `Database` property is defined by the expression 

```
_connectionState == ConnectionState.Open ? SfSession?.database : string.Empty;`
```

This is safe, but the null checker doesn't known that. Thankfully, it can be simplified to just `SfSession?.database ?? ""`.

The `SfSession` property is set by a call to `SetSession`, so again the null checker doesn't understand. Fortunately this only occurs in 2 places, so the `!` modifer can quiet the compiler.

## Finalizer

This class has a finalizer. Which means `GC.SuppressFinalize(this)` must be added to the `Dispose(bool)` method to avoid unnecessarily adding the object to the finalizer queue.

It should be noted that Microsoft no longer recommends implementing finalizers. So we will just eliminate it.

## Naming conventions

Here are some fields in the class.

```
internal ConnectionState _connectionState;
internal int _connectionTimeout;
private bool disposed = false;
```

As consistency is important, the naming convention needs to be fixed. Since knowing which are fields vs parameters/locals can be helpful, we'll go with the _camelCase option. (This also works well when using s_camelCase for static fields.)

### Evaluate single-line, private methods.

Consider this method.

```
private void OnSessionEstablished()
{
	_connectionState = ConnectionState.Open;
}
```

Its sole purpose is to set the connection state to `Open`. There's no reason to believe it will grow with time. Nor are there any complex expressions that need to avoid duplicating. So this method can be inlined. 

### Dispose Pattern

Normally calls to `Close` are simply forwarded to `Dispose`. But in the case of `DbConnection`, the script is flipped. The call to `Dispose` is forwarded to `Close`. And even after the object is closed/disposed, it can be reopened. 

In order to properly support this, a change needs to be made.


```
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
		_logger.Error("Unable to close connection", ex);
	}

	//we no longer read from this field
	//_disposed = true;

	//Not needed. ComponentBase.Dispose(bool) does nothing.
	//base.Dispose(disposing);
}
```

The exception being swallowed is a common design pattern for .NET. It is needed for `finally` blocks to work correctly.

## Round 8 - SnowflakeDbDataAdapter

* Removed unused fields.
* Removed unused private constructor.
* Removed unnecessary call to `GC.SuppressFinalize`.

## Round 9 - SnowflakeDbDataReader

* Removed unused fields.
* Removed unused constructor parameters.

### Implement GetEnumerator

The basic pattern for implementing `GetEnumerator` for a `DBDataReader` is this: 

```

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

```
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

## Round 10 - SnowflakeDbException

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

## Round 11 - SnowflakeDbFactory

The `SnowflakeDbFactory` class is a good example of the Abstract Factory Pattern.


Ideally, we would take advantage of modern C# to make the types visible. This means changing this:

```
public override DbCommand CreateCommand() => new SnowflakeDbCommand();
```

into this:

```
public override SnowflakeDbCommand CreateCommand() => new SnowflakeDbCommand();
```

Unfortunately, this isn't possible because .NET Framework doesn't support covariant return types for overrides.

So the only cleanup for this file is removing type redundant type name in this line.

```
public static readonly SnowflakeDbFactory Instance = new SnowflakeDbFactory();
```

## Round 12 - SnowflakeDbParameter

### Remove Unnecessary Initializers

Remove the assignments for `SFDataType` and `OriginType` in the default constructor as they will be automatically initialized to that value.

### Fix parameter names in constructors

By convention, parameters are in `camelCase`.

Again, we'll mark the overridden property as obsolete. 

### Nullability

Enabling null checking on this class catches potential null reference exceptions in `SnowflakeDbCommand`.

## Round 13 - SnowflakeDbParameterCollection

This is an old collection design from a time when .NET didn't have generics. So a lot of runtime casts are needed.

### SyncRoot

This should be a read-only object field. 

It should have never existed in the first place. But it does, so it should be honored.

### Add

The `tryCastThrow` method isn't needed. The built-in type check when casting is sufficient.

A type-safe version of `Add` should be created.

### AddRange

This code:

```
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

```
public override void AddRange(Array values)
{
	foreach(SnowflakeDbParameter value in values)
		_parameterList.Add(value);
}
``` 

### CopyTo 

This is easy to implement with a simple cast.

```
public override void CopyTo(Array array, int index)
{
	_parameterList.CopyTo((SnowflakeDbParameter[])array, index);
}
```

### IndexOf

This function:

```
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

```
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

```
public new SnowflakeDbParameter this[int index]
{
	get => _parameterList[index];
	set => _parameterList[index] = value;
}
```

## Round 14 - SnowflakeDbTransaction

### Finalizer

Again, finalizers are no longer recommended. 

### Isolation Level

This should throw an `ArgumentOutOfRangeException` in the constructor.

### Dispose

Just like in `SnowflakeDbConnection`, this should swallow exceptions.

## Round 15 - SFConfiguration

This class represents a set of global settings. This is problematic because it is shared by all connections/commands. If the caller can change it, then presumably they have reason to change it. And that reason may be different for different parts of the application.

Probably these settings should be moved into the connection string.

### Fields shouldn't be public

Change the public fields into properties. 

Also initialize them inline.

### Instance shouldn't be a method

This always returns the same value, so it has property semantics. Furthermore, there already is another property that does the same thing. It just happens to be marked internal. 

## Round 16 - SFConfigurationSectionHandler

The static constructor is empty. So it can be removed.

The only public constructor is empty. So it can be removed.

The `IConfigurationSectionHandler.Create` method returns null. So it doesn't fulfill the interface and can be removed. So does the interface.

At this point the whole class is empty and can be removed.

## Round 17 - Fix Compiler Warnings

One of the compiler warnings was an outdated version of .NET Core. Fix that required updating some packages to resolve version conflicts.

## Round 18 - Remove Log4Net

A low-level library such as a database driver should not mandate a specific version of a specific logging framework.

A simple console logger has been added to replace Log4Net as the default logger.

(If this was exercise was meant for production, the `Log4NetImpl` class would be moved to its own library.)

For some strange reason, Log4Net was also the root package of a dependency chain that included System.Web. Once removed, we also lost `HttpUtility`.

This was replaced by `WebUtility` and `QueryHelpers` (from `Microsoft.AspNetCore.WebUtilities`). 

## Round 19 - Replace SFLogger

The proprietary interface `SFLogger` should be replaced with .NET's general purpose `ILogger` interface. All major logging frameworks are expected to support `ILogger` directly.

## Round 20 - Remove the Logger entirely

It is quite unusual for a low-level library to have a logger built into it. Logging at this level offers very little information because, being so low-level, there isn't a significant stack trace. 

Furthermore, some of the messages are trivial such as "All good". They read more like temporary Console output used during development than something that would be useful to developers using the library.

If you disagree, well that's why this is separate from rounds 18 and 19.

## Round 21 - AuthenticatorFactory

The code in this class is surprisingly hard to read. There's not much going on, but the lines are so long that the important parts get lost in the noise. Fortunately, there are a few tricks to deal with that.

```
if (!session.properties.TryGetValue(SFSessionProperty.PRIVATE_KEY_FILE, out var pkPath) &&
	!session.properties.TryGetValue(SFSessionProperty.PRIVATE_KEY, out var pkContent))
```

### Local variables


Since `session.properties` is used a lot, we can capture it in a local variable.

```
var properties = session.properties;

[...]

if (!properties.TryGetValue(SFSessionProperty.PRIVATE_KEY_FILE, out var pkPath) &&
	!properties.TryGetValue(SFSessionProperty.PRIVATE_KEY, out var pkContent))
```

### Static Usings


Next, we introduce a `static using` declaration so we don't need to repeat the enum name.

```
using static Snowflake.Data.Core.SFSessionProperty;

[...]

if (!properties.TryGetValue(PRIVATE_KEY_FILE, out var pkPath) &&
	!properties.TryGetValue(PRIVATE_KEY, out var pkContent))
```

### Discards

The output parameters of the `TryGet` methods aren't being used, so we can use discards.

```
if (!properties.TryGetValue(PRIVATE_KEY_FILE, out _) &&
	!properties.TryGetValue(PRIVATE_KEY, out _))
```

### Correct Methods

Though in this case, if we really don't need the value then we can choose a different method. 

```
if (!properties.ContainsKey(PRIVATE_KEY_FILE) &&
	!properties.ContainsKey(PRIVATE_KEY))
```

### Array type inference

Here is another trick to remove boiler plate.

```
throw new SnowflakeDbException(
	SFError.INVALID_CONNECTION_STRING,
	new object[] { invalidStringDetail });
```

The type `object` is not necessary, as the compiler can infer if from the context.

```
throw new SnowflakeDbException(
	SFError.INVALID_CONNECTION_STRING,
	new[] { invalidStringDetail });
```

Technically it's inferring a `string[]` array, because that's the type of object being put inside it. But arrays are 'covariant', which means you can give a `string[]` array to a function that expects an `object[]` array.

### Params

A strange thing about this call is that we didn't need to create the array at all. The parameter is marked with `params`, which means the compiler will create the array for us.

```
throw new SnowflakeDbException(
	SFError.INVALID_CONNECTION_STRING,
	invalidStringDetail);
```

### File Scoped Namespaces

Part of the readability issue is that the statements were so long that they needed to be broken over multiple lines. 

By using `namespace Snowflake.Data.Core.Authenticator;` instead of,

```
namespace Snowflake.Data.Core.Authenticator;
{
```

we remove a level of indentation. Four spaces don’t sound like much, but it can be enough to get everything onto one line. Especially when combined with other techniques shown above.


### Validation

Validation for each authenticator is performed in the `AuthenticationFactory` rather than the authenticators themselves. 

This is a problem because if those authenticators are created via any other means, the validation won't be performed.

It can be fixed by moving the validation into the constructor of each authenticator class.

## Round 22 - BaseAuthenticator and its subclasses

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

```
protected abstract void SetSpecializedAuthenticatorData(ref LoginRequestData data);
```

Fortunatelly none of the subclasses does this, so the `ref` flag can be removed.

### Constants vs Read-only Fields

In most subclasses, there is a static field named `AUTH_NAME`. In addition to the naming issue -- class members should be `PascalCase` if `public`/`protected` -- this should be a constant.

### IAuthenticator interface

Every subclass of `BaseAuthenticator` implement this interface, so it can be moved into that class. 

Normally the interface methods would be exposed in the base class as `abstract` methods. But the interface methods `Authenticate` and `AuthenticateAsync` always call `Login` and `LoginAsync`.

This means we can combine them. Simply rename `Authenticate` and `AuthenticateAsync` to be `Login` and `LoginAsync`. Then make the real `Login` and `LoginAsync` methods `virtual`. 

## Round 23 - IAuthenticator

This interface is marked as `internal` and thus can't be used for mocking. The only thing that implements it is `BaseAuthenticator`. At this point there is no purpose to it and thus it can be deleted.

## Round 24 - BaseRestRequest and IRestRequest 

Several of the classes such as `IdpTokenRestRequest` inherit from both `BaseRestRequest` and `IRestRequest`. Since `BaseRestRequest` already implements `IRestRequest`, there is no reason to mention it twice.

It looks like the reason they originally did this was that the `ToRequestMessage` method in `BaseRestRequest` wasn't marked as `abstract`. Instead, they used:

```
HttpRequestMessage IRestRequest.ToRequestMessage(HttpMethod method)
{
    throw new NotImplementedException();
}
```

The correct pattern in this case is `internal abstract` as the author didn't want to expose the method on the public API. 

```
internal abstract HttpRequestMessage ToRequestMessage(HttpMethod method);
```

At this point, `IRestRequest` is no longer needed. Just like `IAuthenticator`, it is marked as `internal` and only has one implementation.

## Round 25 - Naming Conventions

The last round of refactoring for the Authenticator folder is just updating the member names to be consistent with .NET and project standards.

## Round 26 - StorageClient Namespace

For this round, we'll go by issue type rather than class.

### Fields vs Properties

There are several classes with non-private fields that should be converted into properties. This is important because many libraries don't work with fields and will just ignore them. So being consistent here will prevent future problems.

### Property Naming Convention

Many of these fields/properties use `camelCase` instead of the .NET standard for public members, which is `PascalCase`. When correcting this issue, the code must be reviewed to see if it used in JSON or XML serialization. 

If it is, then casing may be important. But that doesn't mean we shouldn't fix the casing now, only that we need to use this pattern at the same time.

```
[JsonProperty(PropertyName = "contentLength")]
public long ContentLength { get; set; }
```


If we ignore the casing issue now, a future developer may be inclined to 'fix the mistake'. This will cause runtime failures, as the compiler doesn't have a way to know that the casing was important for serialization. 

Sometimes this can be covered by automatic case conversion enabled on the serializer. But that has to be setup when the project is started and used consistently, which is something we can't promise in legacy code. 

Another change that can occur is a developer making the name more informative. For example, changing `ContentLength` to `ContentLengthInBytes` or `ContentLengthInCharacters`. This is the kind of mistake that automatic case conversion can't mitigate.

### Unused Members and Classes

Several constants, fields, and even a whole class are never used and should be deleted.

### Readonly Fields

Fields that are marked as `static readonly` can generally be changed to `const`.

Fields that are initialized in the constructor and never changed should be marked as `readonly`.

### Unused Parameters

In `SFS3Client.setCommonClientConfig`, the `parallel` parameter isn't used. This should be removed because it incorrectly implies that one can set the max connections per server.

## Round 27 - FileTransfer

### Random isn't Secure

First up is the use of `Random` in `EncryptionProvider`. This class should never be used when encryption is involved. If you a random number that is cryptographically secure, always use `RandomNumberGenerator` in `System.Security.Cryptography`.

### Casing Conventions (again)

This code demonstrates the important of naming conventions.

```
/// The temporary directory to store files to upload/download.
public string SHA256_DIGEST { get; set; }

/// File message digest (after compression if required)
public string sha256Digest { set; get; }
```

As you can see, there are two properties with names that differ only by casing convention. 

Fortunately, one of the two properties is not used and can be deleted. Otherwise, careful inspection of the code would be needed to determine if they represent the same or different values, and if different, if each use is correct.

### Readonly fields

For private fields, sometimes it is easier to just mark all of them as `readonly`, then revert the ones that are actually modified. We did this with `SFFileTransferAgent` and discovered that not only are all of the fields read-only, there is also some unreachable code.

```
if (INJECT_WAIT_IN_PUT > 0)
{
    Thread.Sleep(INJECT_WAIT_IN_PUT);
}
```

Since `INJECT_WAIT_IN_PUT` is always 0, the compiler recognized that the sleep call could never happen. 

After deleting this block of code, the compiler tells us that `INJECT_WAIT_IN_PUT` is never used. So it can be deleted as well.

### Unused Fields/Parameters

As before, unused fields are removed, which triggers the removal of unused parameters.

## Round 28 - SecretDetector

This class is marked as `internal`, so it is only used by code inside `Snowflake.Data`? 

No, it is only used by code inside `Snowflake.Data.Tests`. Which means it should be placed in the `Snowflake.Data.Tests` project.

Long-term, we need to rethink the whole `InternalsVisibleTo` thing and whether or not it is appropriate at all in this project.

## Round 29 - Static Classes

Classes with only static members such as `ChunkDownloaderFactory` should be marked as `static`. 

Allowing objects of these types to be created is misleading to the developers, as those objects would not be useful for anything.

## Round 30 - Message Classes

Whether you call them "messages", "DTOs", "models", "requests/responses" etc., the purpose remains the same; these are the classes that are used to transmit data to and from outside the application.

Currently they are scattered throughout the Core folder, making them hard to locate. While there is no 'right number' of classes in a folder, having more than 80 it probably too many unless they all are serving the same role.

## Round 31 - Factory Methods and Globals

Factory methods are incredibly useful. There are many times when you don't know at compile time which class to use because it depends on some value in a configuration class. For example, `SFConfiguration` dictates which `IChunkDownloader`, `IChunkParser` to employ.

Globals are generally bad. No, that's not quite right. Hidden globals are generally bad.

`ChunkDownloaderFactory` and `ChunkParserFactory` both have a hidden global. Specifically, they call `SFConfiguration.Instance`.

There are two ways we can eliminate the hidden global.

1. Add a `SFConfiguration` to the `GetDownloader` and `GetParser` static functions.
2. Move `GetDownloader` and `GetParser` into `SFConfiguration` as methods.

We're going with option 2 because it follows the OOP concept of putting data and the logic that operates on that data together. When someone glances at `SFConfiguration`, they can now see exactly what its properties control. 

This open also eliminates two single-function classes. While it can happen, it is rare for a well-designed class to only have one function.

## Round 32 - Fixing the Tests

Ideally the tests are re-run after each round of refactoring. But in the real world, that doesn't always happen. Perhaps the tests can only be run on a specific machine. Or the tests take too long to run. Or you simply get caught up in the refactoring and forget to check your work.

Regardless of the reason, the tests weren't done and now we have to go back and see what happened.

When troubleshooting, it is useful to have two copies of the code. That way you can step through the original and new code at the same time, watching for differences.

### Test Design Failure

Consider this line of code:

```
Assert.AreEqual(SFError.IDP_SAML_POSTBACK_NOTFOUND.GetAttribute<SFErrorAttr>().errorCode, e.ErrorCode);
```

Way back in round 3, we eliminated the need to use reflection to get the error codes. That gave us this:

```
Assert.AreEqual(SFError.IDP_SAML_POSTBACAK_NOTFOUND, e.ErrorCode);
```

At first glance, this code looks correctly. The numeric value of `IDP_SAML_POSTBACAK_NOTFOUND` and `e.ErrorCode` are equal.

But `e.ErrorCode` is not a `SFError`. They are different types, so `Assert.AreEqual` is going to fail. What we need is this code:

```
Assert.AreEqual(SFError.IDP_SAML_POSTBACAK_NOTFOUND, e.SFErrorCode);
```

The property `e.SFErrorCode` is of the correct type, allowing the test to pass.

### Initialization Order

Compare these three code blocks.

```
//Version 1
private static int blockLengthBits = 24;
private static int blockLength = 1 << blockLengthBits;
private static int metaBlockLengthBits = 15;
private static int metaBlockLength = 1 << metaBlockLengthBits;

//Version 2
private static int blockLength = 1 << blockLengthBits;
private static int blockLengthBits = 24;
private static int metaBlockLength = 1 << metaBlockLengthBits;
private static int metaBlockLengthBits = 15;

//Version 3
private const int blockLength = 1 << blockLengthBits;
private const int blockLengthBits = 24;
private const int metaBlockLength = 1 << metaBlockLengthBits;
private const int metaBlockLengthBits = 15;
```

Version 1 and 3 have the same values for each field. But version 2 is very different. Why?

For non-constant values, the fields are initialized in code order. So when Code Maid alphabetized our code for us in v2, it changed the initialization order. Which means `blockLengthBits` was 0 when we read it to set `blockLength`. And since they have the wrong values, the code broke.

Constants are different. When they are initialized, it traces through the dependencies to ensure things are done in the correct order.

Fortunately, these values should have been constant all along, so the fix was easy.

### Library Change

Back in round 18, we removed the Log4Net library and that caused the legacy `HttpUtility` to be dropped as well. Rather than trying to figure out how to get it working again, we replaced it with the modern alternatives, `WebUtility` and `QueryHelpers`. 

Unfortunately, there is a slight behavioral difference in the two libraries.

With the old code, the following line would generate a proper query string. With the new code, calling `.ToString()` just returns the typename of the `queryParams` object, which isn't exactly useful.

```
uriBuilder.Query = queryParams.ToString();
```

To fix this significantly more code needs to be written.

```
//Clear the query and apply the new query parameters
uriBuilder.Query = "";

var uri = uriBuilder.Uri.ToString();
foreach (var keyPair in queryParams)
    foreach (var value in keyPair.Value)
        uri = QueryHelpers.AddQueryString(uri, keyPair.Key, value);

uriBuilder = new UriBuilder(uri);
```

This is clearly a flaw in the `QueryHelpers` library. There is no overload of `QueryHelpers.AddQueryString` that accepts the return type from `QueryHelpers.ParseQuery`. Nor are there methods to remove items from a query string.

## Round 33 - Attributes and Long Enums

### SFStatementTypeAttr
Again, we see the anti-pattern of using an attribute to store the enum's actual value.

```
internal enum SFStatementType

{
    [SFStatementTypeAttr(typeId = 0x0000)]
    UNKNOWN,

    [SFStatementTypeAttr(typeId = 0x1000)]
    SELECT,
```

A slight wrinkle in this design is that the original `typeId` is a long, so the enum would need to be a long as well. Fortunately, C# has a rarely used feature where you can do just that.

```
internal enum SFStatementType : long

{
    UNKNOWN = 0x0000,
    SELECT = 0x1000,
```

### SFSessionPropertyAttr

This `SFSessionPropertyAttr` is being used correctly, but there are a couple of minor implementation mistakes. 

```
class SFSessionPropertyAttr : Attribute
{
    public string defaultValue { get; set; }
    public bool required { get; set; }
}
```

The mistakes are:

1. The name should have the suffix `Attribute`. The C# compiler looks for this suffix and allows you to omit it when applying an attribute to a construct.
2. It is missing the `AttributeUsage` attribute. This tells the compiler where an attribute can be used so that it can warn the programmer of mistakes.
3. The property names are cased incorrectly.

The fixed attribute can be seen below.

```
[AttributeUsage(AttributeTargets.Field)]
class SFSessionPropertyAttribute : Attribute
{
    public string DefaultValue { get; set; }
    public bool Required { get; set; }
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

## Round 34 - Organizing Core

With nearly 60 classes, the `Core` namespace is still rather crowded and it's hard to see what features and subsystems are covered by it. 

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

* Core: 17
* Messages: 28
* RequestProcessing: 8
* ResponseProcessing: 12
* ResponseProcessing/Chunks: 14
* Sessions: 7

Where did `ResponseProcessing/Chunks` come from? Well at 28 files, `ResponseProcessing` started getting big again. And since all of the chunks code is only referenced by `SFResultSet`, it can be easily pulled into its own subsystem. 

We left the `Messages` large mainly because we rarely look at it. There is no code, only simple DTOs, so charting the interactions between classes isn't as necessary.

## Round 35 - RequestProcessing

Where possible, static fields are marked with `const`. Of those remaining, `readonly` is applied if allowed. This is mostly a documentation thing, making it easier to see where the mutable fields are.

Naming conventions are addressed.

Protected fields are converted into properties.

## Round 36 - Nullability checks in Authenticator

With a lot of the code cleaned up, next we go back to the `Authenticator` namespace and add its missing null checks. Mostly this is pretty rote stuff, but there is one section in `OktaAuthenticator` where we do something odd.

```
if (_samlRawHtmlString == null)
    throw new NullReferenceException($"Internal error. {nameof(_samlRawHtmlString)} should have been set previously.");
```

Here we are checking to see if `_samlRawHtmlString` is null. If it is, we throw the same exception that would have been thrown anyways, but with a little bit of added context. We are using `NullReferenceException` instead of `InvalidOperationException` to indicate that if it is null, that's a bug in the library, not the user's code.

## Round 37 - Nullability checks in FileTransfer and StorageClient

Enabling nullability checks in this namespace revealed more initialization order issues, specifically in `SFFileCompressionTypes`. In many cases, fixing this requires putting the fields back in the correct order and slapping a huge warning sign across the file to tell people to not re-sort it.

In this case, each of the fields in question are only used in one place. So we can just inline them and eliminate the problem entirely.

Occasionally we had to use the `!` modifier after a null check to inform the compiler that the value still isn't null.

```
if (_resultsMetas[index].LastError != null)
{
    _transferMetadata.RowSet[index, 5] = _resultsMetas[index].LastError!.ToString();
}
```

In another we found a constructor that uses `TryGetValue`. That sounds innocent enough, but if it returns `false`, then `_blobServiceClient` remains null, breaking the other methods in the object.

```
public SFSnowflakeAzureClient(PutGetStageInfo stageInfo)
{
    // Get the Azure SAS token and create the client
    if (stageInfo.StageCredentials.TryGetValue(AzureSasToken, out string sasToken))
    {
        string blobEndpoint = string.Format("https://{0}.blob.core.windows.net", stageInfo.StorageAccount);
        _blobServiceClient = new BlobServiceClient(new Uri(blobEndpoint),
            new AzureSasCredential(sasToken));
    }
}
```

So we added this else clause,

```
else throw new ArgumentException($"Could not find {AzureSasToken} key in {nameof(stageInfo)}.", nameof(stageInfo));
```

In another place, we use this pattern for the null checks.

```
private void updatePresignedUrl(SFFileMetadata fileMeta)
{
    string filePathToReplace = getFilePathFromPutCommand(_query);
    string fileNameToReplaceWith = fileMeta?.DestFileName ?? throw new ArgumentException($"{nameof(fileMeta.DestFileName)} was null.", nameof(fileMeta));
```

In some cases, we can fix the nullablility issue by adding a constructor. Consider `FileHeader`,

```
internal class FileHeader
{
    public long ContentLength { get; set; }

    public string? Digest { get; set; }

    public SFEncryptionMetadata? EncryptionMetadata { get; set; }
}

return new FileHeader
{
    Digest = response.Metadata[SfcDigest],
    ContentLength = response.ContentLength,
    EncryptionMetadata = encryptionMetadata
};

```

If we could turn that initializer into a constructor, then we can guarantee that the fields are not null. Then we wouldn't have to perform null checks later. But there are places where we see this instead,

```
if (fileMetadata.ResultStatus == ResultStatus.Uploaded.ToString())
{
    return new FileHeader();
}
```

So now we're forced to add null-checks whenever reading the `Digest` or `EncryptionMetadata` properties.

## Round 38 - Nullability checks in RequestProcessing

Nothing particularly interesting here, just the usual assortment of missing null check.

## Round 39 - ResponseProcessing and Chunks

* Another round of fixing nullability issues.
* Fixed naming issues
* Mark unimplemented methods as obsolete/hidden
* Single-use, private methods are inlined when helpful for nullability checks.

## Round 40 - Sessions

* Move nested classes into their own files. Including doubly-nested classes.
* Fixed naming issues
* Fields changed to properties where appropriate
* Fixed nullability issues.


## Round 41 - Core

* Fixed nullability issues.
* Fixed naming issues

### Mutable Attributes

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

This code will have to be removed and replaced with `CheckSessionProperties(properties, useProxy);`.

### Resources

The library as a whole is not internationalized, which is the only reason to add a resourece file. ErrorMessages.resx

## Round 42 - .NET 5.0 Support

In Round 43, we’re going to need features from .NET 5. But before we start that refactoring job, we need to make sure the current code actually works with .NET 5. Mostly this means fixing more nullability issues. Some of them trickier than what we’ve seen before.

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
#if !NET5_0_OR_GREATER
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


### Exception Filters (catch-when)

There are places in the code that not only expect a non-null inner exception, but demand it be of a specific type. 

```
catch (Exception ex) 
{
    AmazonS3Exception err = (AmazonS3Exception)ex.InnerException;
```

If either of these pre-conditions fail, the error handler will throw its own error, causing the original error to be lost. We can fix that by using an exception filter.

```
catch (Exception ex) when (ex.InnerException is AmazonS3Exception)
{
    AmazonS3Exception err = (AmazonS3Exception)ex.InnerException;
```

While this will no longer catch other types of exceptions, that’s acceptable because it wasn’t handling them correctly anyways.

## Round 43 - Race Condition in HttpUtil?

While working on the .NET 5 update, we came across this line.

```
private Dictionary<string, HttpClient> _httpClients = new Dictionary<string, HttpClient>();
```

It looks innocent enough, just another member field in a ` HttpUtil ` object. But then we see this line…

```
static internal HttpUtil Instance { get; } = new HttpUtil();
```

This makes ` HttpUtil` a singleton. Which in turn means all its member fields are really static fields, at least as far as concurrency issues are concerned. 

Is there a lock around calls to `_httpClients`? At first glance it doesn’t look like it.

```
private HttpClient RegisterNewHttpClientIfNecessary(HttpClientConfig config)
{
    string name = config.ConfKey ?? throw new ArgumentException($"{nameof(config.ConfKey)} is null", nameof(config));
    if (!_httpClients.ContainsKey(name))
    {
        var httpClient = new HttpClient(new RetryHandler(setupCustomHttpHandler(config))) { Timeout = Timeout.InfiniteTimeSpan };

        // Add the new client key to the list
        _httpClients.Add(name, httpClient);
    }

    return _httpClients[name];
}
```

But actually, ` RegisterNewHttpClientIfNecessary` is currently called only in one place. At that place takes out a lock.

```
internal HttpClient GetHttpClient(HttpClientConfig config)
{
    lock (_httpClientProviderLock)
    {
        return RegisterNewHttpClientIfNecessary(config);
    }
}
```

This is not acceptable. Every developer who looks at this code is going to have to review it for potential race conditions. They can’t just look at it any one place to understand whether or not it’s written correctly.

Fortunately, there is an easy alternative that uses a ` ConcurrentDictionary` which eliminates the lock and private function. 

```
internal HttpClient GetHttpClient(HttpClientConfig config)
{
    string name = config.ConfKey ?? throw new ArgumentException($"{nameof(config.ConfKey)} is null", nameof(config));

    return _httpClients.GetOrAdd(name, _ => new HttpClient(new RetryHandler(setupCustomHttpHandler(config))) { Timeout = Timeout.InfiniteTimeSpan });
}
```

Taking a more holistic look at ` HttpUtil`, one has to ask why it is a singleton class rather than just a static module? It implements no interfaces and is sealed, so there are no opportunities for polymorphism.  The constructor is private, so there will never be more than one instance. 

Looking further, it is only used by `SFSession` and is specially designed for that class’s needs. It isn’t a generic utility class at all. Furthermore, it is at the heart of what `SFSession`, which is managing HTTP connections. 

With these factors in mind, the two remaining functions should just be moved into `SFSession`.

