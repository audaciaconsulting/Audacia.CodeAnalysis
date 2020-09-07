# Overview

The `Audacia.CodeAnalysis.Analyzers` library contains custom Roslyn analyzers. These analyzers are provided to enforce coding standards where no existing analyzer exists for the rule in question.

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

ACL1005 is based on the Roslynator rule [RCS1046](https://github.com/JosefPihrt/Roslynator/blob/master/docs/analyzers/RCS1046.md), which checks if asynchronous methods are suffixed with 'Async'. ACL1005 adds an exclusion for controller actions, as they are often asynchronous but should generally not be suffixed.