# Overview

The `Audacia.CodeAnalysis.Analyzers` library contains custom Roslyn analyzers. These analyzers are provided to enforce coding standards where no existing analyzer exists for the rule in question.

# Contributing

In order to add a new analyzer you can follow existing analyzers as a guide. The most important thing is that each analyzer has a unique ID (the convention is to prefix them "ACL"). So if the last analyzer was "ACL1007", the next one should be "ACL1008".

Once the analyzer is finished, you should:

1. Ensure its use and purpose is documented in this README file
1. Submit a PR to merge the changes into `master`
   - when the PR completes a build will be triggered that publishes a new version of the `Audacia.CodeAnalysis.Analyzers` NuGet package
1. Update the `Audacia.CodeAnalysis.Analyzers` package in the main `Audacia.CodeAnalysis` project
1. Submit a second PR to merge the `Audacia.CodeAnalysis` change into `master`
   - when the PR completes a build will be triggered that publishes a new version of the `Audacia.CodeAnalysis` NuGet package

Note the second PR is needed as `Audacia.CodeAnalysis.Analyzers` should be consumed via the main `Audacia.CodeAnalysis` package. It cannot be a direct project reference as analyzers must be reference via a NuGet package.

**Important:** The unit test project in this solution will fail to build if the path length of items in the bin folder exceeds 260 characters. (You will see errors similar to `Could not copy Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices.Interface.dll`). Therefore, it is recommended to clone this repo into a high-level directory (e.g. C:\Projects).

## Testing changes locally

**Prerequisites**: The "Visual Studio extension development" toolset is required for testing changes locally. This can be installed by modifying your installation of visual studio, and search for this toolset under the 'Other Toolsets' header.

To run VS experimentally set the `.Vsix` project as the startup project, which launches visual studio with the analysers installed. Depending on your use case create or open an existing project. Code that violates any analysers in this project should then be flagged.

# Analyzers

## ACL1000 - Private fields should be prefixed with an underscore

The ACL1000 rule checks whether private field names are prefixed with an underscore. It also provides a code fix, which inserts an underscore.

Code with violation:

```csharp
public class MyClass
{
    private int number;
}
```

Code with fix:

```csharp
public class MyClass
{
    private int _number;
}
```

## ACL1001 - Variable declarations should not use a magic number

The ACL1001 rule checks for magic numbers being used in variable declarations. Magic numbers should generally be extracted to a well-named variable.

Code with violation:

```csharp
var totalPrice = price + 2.5m;
```

Code with fix:

```csharp
const decimal shippingCost = 2.5m;
var totalPrice = price + shippingCost;
```

An exception to this rule is where the 'magic number' is 1. This is because it is extremely common to use 1 to increment/decrement values. Therefore the following code is allowed:

```csharp
var next = previous + 1;
```

## ACL1002 - Methods should not exceed a predefined number of statements

The ACL1002 rule checks the number of statements in a method (or property or constructor) against a maximum allowed value. This maximum value can be configured globally in the .editorconfig file, or locally using the `[MaxMethodLength]` attribute (this is in the `Audacia.CodeAnalysis.Analyzers.Helpers` NuGet package, which must be installed separately). In the absence of any configured value, a default maximum value of 10 is used.

Code with violation (assuming configured maximum of 5 statements):

```csharp
public void MyMethod()
{
    var one = 1;
	var two = 2;
	var three = 3;
	var four = 4;
	var five = 5;
	var six = 6;
}
```

Code with local override of maximum allowed statements:

```csharp
[MaxMethodLength(6)]
public void MyMethod()
{
    var one = 1;
	var two = 2;
	var three = 3;
	var four = 4;
	var five = 5;
	var six = 6;
}
```

.editorconfig override of maximum allowed statements:

```
dotnet_diagnostic.ACL1002.max_statement_count = 6
```

**Argument null checks are excluded from the statement count.** So in the following code, 3 lines would be counted rather than 5. This is because argument null checks do not really add to the complexity of a method, and by including them as statements in the analysis it may dissuade people from including them.

```csharp
public void MyMethod(string arg)
{
	if (arg == null)
	{
		throw new ArgumentNullException(nameof(arg));
	}

	var one = 1;
	var two = 2;
	var three = 3;
}
```

## ACL1003 - Don't declare signatures with more than a predefined number of parameters

The ACL1003 rule checks the number of parameters for a method or constructor against a maximum allowed value. This maximum value can be configured globally in the .editorconfig file, or locally using the `[MaxParameterCount]` attribute (this is in the `Audacia.CodeAnalysis.Analyzers.Helpers` NuGet package, which must be installed separately). In the absence of any configured value, a default maximum value of 4 is used.

Code with violation (assuming configured maximum of 5 statements):

```csharp
public void MyMethod(int a, int b, int c, int d, int e)
{
}
```

Code with local override of maximum allowed statements:

```csharp
[MaxParameterCount(5)]
public void MyMethod(int a, int b, int c, int d, int e)
{
}
```

.editorconfig override of maximum allowed parameters (values for methods and constructors can be configured separately):

```
dotnet_diagnostic.ACL1003.max_method_parameter_count = 5
dotnet_diagnostic.ACL1003.max_constructor_parameter_count = 5
```

However, this does <b>not</b> work with the `record` reference type, and syntax synonyms (`record class`, `record struct`):
```csharp
// No parameter count violation.
public record Person(string a, int b, int c, int d, int e);

// Attribute is ignored.
[MaxParameterCount(1)]
public record Student(int a, int b, int c, int d, int e, int f) : Person(a, b, c, d, e);

// This includes both classes and structures
public record struct School(int a, int b); // Record struct
public record Exam(int a, int b); // Record class
```

## ACL1004 - Don't use abbreviations

The ACL1004 rule checks whether single characters or (specific) abbreviations have been used as a type, member, parameter or variable name.

Code with violation:

```csharp
var idx = 4;
```

Code with fix:

```csharp
var index = 4;
```

There are two additional pieces of configuration that can be applied:

- Exclude lambda expression parameters from the check
- Allow certain characters/abbreviations as a for loop variable

### Lambda expressions

Single characters are often used as lambda expression parameter names, therefore this can be allowed by using the following setting (which is set in the default configuration):

```
dotnet_diagnostic.ACL1004.exclude_lambdas = true
```

With the above setting in place, the following code will not result in a diagnostic.

```csharp
var match = list.First(a => a.Name == "Bob");
```

### Loop variables

Some single characters (such as `i`) are widely used in for loops, therefore specific values can be specified as allowed as for loop variables. These values are provided as a comma-separated list as follows:

```
dotnet_diagnostic.ACL1004.allowed_loop_variables = i,j
```

With the above setting in place, the following code will not result in a diagnostic.

```csharp
for (var i = 0; i < 10; i++)
```

## ACL1005 - Asynchronous method name is not suffixed with 'Async'

ACL1005 is based on the Roslynator rule [RCS1046](https://github.com/JosefPihrt/Roslynator/blob/master/docs/analyzers/RCS1046.md), which checks if asynchronous methods are suffixed with 'Async'.

ACL1005 adds an exclusion for controller actions, as they are often asynchronous but should generally not be suffixed.

## ACL1006 - Code block does not have braces

ACL1006 is based on the Roslynator rule [RCS1007](https://github.com/JosefPihrt/Roslynator/blob/master/docs/analyzers/RCS1007.md), which checks if statements such as `if`, `foreach` and `using` are followed by braces.

ACL1006 adds an exclusion for `if` statements performing an argument null check. For example, using ACL1006 rather than RCS1007, the following code becomes valid:

```csharp
public void SomeMethod(string arg)
{
	if (arg == null) throw new ArgumentNullException(nameof(arg));
}
```

The justification for this exclusion is that argument null checks, while advised, add noise to a codebase, and minimising this noise is useful.

## ACL1007 - ThenByDescending instead of OrderByDescending if follows OrderBy or OrderByDescending statement

ACL1007 is similar in function to the Roslynator rule [RCS1200](https://github.com/JosefPihrt/Roslynator/blob/master/docs/analyzers/RCS1200.md), which checks if an `OrderBy` follows an `OrderBy` or `OrderByDescending`, and suggests using `ThenBy` instead if so.

ACL1007 checks if an `OrderByDescending` follows an `OrderBy` or `OrderByDescending` and suggests using `ThenByDescending` instead if so.

Code with diagnostic:

```csharp
var x = items.OrderBy(f => f.Surname).OrderByDescending(f => f.Name);
```

Code without diagnostic:

```csharp
var x = items.OrderBy(f => f.Surname).ThenByDescending(f => f.Name);
```

## ACL1008 - Controller actions have ProducesResponseType attribute

ACL1008 checks if a controller action has at least one `ProducesResponseType` attribute and will produce a warning if not.
This applies to controller actions defined by either an Http attribute (eg. `HttpGet`) or by the parent class inheriting the `ControllerBase` class.

Code with diagnostic:

```csharp
[HttpGet]
public string Get()
{
	return 'hello';
}
```

Code without diagnostic:

```csharp
[HttpGet]
[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
public string Get()
{
	return 'hello';
}
```

## ACL1009 - Method overload should call another overload

ACL1009 is based on CSharpGuidelinesAnalyzer [AV1551](https://github.com/dennisdoomen/CSharpGuidelines/blob/5.6.0/_rules/1551.md), which ensures the more overloaded method is called from other overloads.

ACL1009 reports warnings on the three rules below:

- That an overloaded method does not call another overload (unless it is the longest in the group)
- That the longest overloaded method (the one with the most parameters) is not virtual.
- That the order of parameters in an overloaded method does not match with the parameter order of the longest overload.

Code with diagnostic:

```csharp
public void TestMethod(int i, int j)
{
	TestMethod(i, j, 0);
}

public void TestMethod(int i, string s, int j = 0)
{
	var line = string.Format(s, i, j);
}
```

Code without diagnostic:

```csharp
public void TestMethod(int i, string s)
{
	TestMethod(i, s, 0);
}

public virtual void TestMethod(int i, string s, int j = 0)
{
	var line = string.Format(s, i, j);
}
```

There is one exception for these rules and that is if the class or method is from a `MVC` `Controller` as shown below. If a class inherits from a `Controller` or `ControllerBase` or if the method has Http Atrributes e.g. `HttpGet`.

The code below will **NOT** trigger a warning since the methods are controller actions and therefore will be excluded.

```csharp
public class TestClassController : Controller
{
	[HttpGet]
	public string Get(int i)
	{
		return string.Format(""Test"", i);
	}

	[HttpGet(""Test"")]
	public string Get(int i, string s)
	{
		return string.Format(s, i);
	}
}
```

## ACL1010 - Nullable reference types enabled

ACL1010 checks whether nullable reference types have been enabled on a project.

ACL1010 reports a warning if:

- The `<Nullable>` node is omitted from a .csproj file
- The `<Nullable>` node is included in a .csproj file but has a value of `disable`

All other values are permitted.

ACL1010 also provides a code fix, which inserts or updates the <Nullable> node in a .csproj file with a default value of `enable`.


## ACL1011 - Don't nest too many control statements

ACL1011 checks how deeply nested control statements are to and raises a warning if a statement is too deeply nested, by default after 2.

For example a warning is raised for this:

```csharp
if (condition1)
{
	if (condition2)
	{
		if (condition3)
		{
			if (condition4)
			{
				Console.WriteLine("I'm in too deep!");
			}
		}
	}
}
```

The warning should be resolved with an appropriate refactor or rewrite of the code responsible, e.g.

```csharp

if (condition1 && condition2 && condition3 && condition4)
{
	Console.WriteLine("I'm in too deep!");
}
```

This analyzer considers the following to be "control statements":
- While loops: `while`
- Do loops: `do`
- For loops: `for` or `foreach`
- If statements: `if`
- Switch: either `switch` - `case` block or a `switch` expression
- Try blocks: `try`, `catch`, `finally`

Maximum allowed nesting can be configured in `.editorconfig` by setting `dotnet_diagnostic.ACL1011.max_control_statement_depth`.

## ACL1012 - Don't pass predicates into 'Where' methods with too many clauses

ACL1012 checks how many logical and clauses are contained in a `Where` method call's predicate and raises a warning if there are too many, by default after 3.

For example a warning is raised for this:

```csharp
var filtered = myCollection.Where(a => a.Length > 10 && a.Name != "IAmAllowedToBeLonger" && a.Property != null && a.Property.Value == 1);
```

Because there are too many (in this case 4) and clauses at the top level of the where statement. To resolve the warning, this could be refactored as follows:

```csharp
var filtered = myCollection
	.Where(a => a.Length < 10)
	.Where(a => a.Name != "IAmAllowedToBeLonger")
	.Where(a => a.Property != null)
	.Where(a => a.Property.Value == 1);
```

If this is overkill, you can still have less than four and clauses in each `Where`, to group together related clauses for example.

```csharp
var filtered = myCollection
	.Where(a => a.Length < 10 && a.Name != "IAmAllowedToBeLonger")
	.Where(a => a.Property != null && a.Property.Value == 1);
```

Maximum allowed clauses can be configured in `.editorconfig` by setting `dotnet_diagnostic.ACL1012.max_where_clauses`.

# Custom .editorconfig Settings in Rider

Some of the rules (e.g. ACL1002, ACL1003 and ACL1004) make use of custom .editorconfig settings. For example the maximum statement count for rule ACL1002 can be configured like this:

```
dotnet_diagnostic.ACL1002.max_statement_count = 6
```

Rider does not support these custom settings when performing its code analysis (however such settings are fully supported in Visual Studio, and also by the C# compiler itself, even when run in Rider). This issue has been raised with JetBrains (see [here](https://youtrack.jetbrains.com/issue/RIDER-53508)), however until this functionality is supported in Rider, the following workaround can be employed.

The relevant .editorconfig file(s) can be added as 'additional files' in the appropriate .csproj file(s). They should be added in order of specificity (i.e. the most specific .editorconfig added first). For example, the .csproj below declares a group of additional files called 'AnalyzerSettings' (this name can be whatever you want), and adds two .editorconfig files to this group (the first one located in the same directory as the .csproj file, and the second one located in the directory above):

```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <TargetFramework>netcoreapp3.1</TargetFramework>
  <AdditionalFileItemNames>AnalyzerSettings</AdditionalFileItemNames>
</PropertyGroup>

<ItemGroup>
  <AnalyzerSettings Include=".editorconfig" />
  <AnalyzerSettings Include="../.editorconfig" />
</ItemGroup>
```
