# Audacia.CodeAnalysis

The `Audacia.CodeAnalysis` repository contains .NET analyzers and rulesets to provide checks on Audacia's coding standards.

## Getting started

There are two analyzer packages available - a standard package and one with extra analyzers specifically for ASP.NET Core projects. Which one you install depends on the type of project you're working on.

Now you've installed the `Audacia.CodeAnalysis` or `Audacia.CodeAnalysis.AspNetCore` package you need to get one or more rulesets. Available rulesets are as follows:

[CodeAnalysis.ruleset](https://audacia.visualstudio.com/Audacia/_git/Audacia.CodeAnalysis?path=%2FAudacia.CodeAnalysis%2FCodeAnalysis.ruleset):
This is the base ruleset and should be included in all projects.

[CodeAnalysis.Tests.ruleset](https://audacia.visualstudio.com/Audacia/_git/Audacia.CodeAnalysis?path=%2FAudacia.CodeAnalysis%2FCodeAnalysis.Tests.ruleset):
This ruleset overrides certain rules in the base configuration specifically to allow for patterns used in tests such as public nested classes and underscores in names.

[CodeAnalysis.Libraries.ruleset](https://audacia.visualstudio.com/Audacia/_git/Audacia.CodeAnalysis?path=%2FAudacia.CodeAnalysis%2FCodeAnalysis.Libraries.ruleset):
This ruleset overrides certain rules in the base configuration specifically for use in libraries, for example to enforce stricter rules around documentation.

[CodeAnalysis.AspNetCore.ruleset](https://audacia.visualstudio.com/Audacia/_git/Audacia.CodeAnalysis?path=%2FAudacia.CodeAnalysis%2FCodeAnalysis.AspNetCore.ruleset): See the [Audacia.CodeAnalysis.AspNetCore](https://dev.azure.com/audacia/Audacia/_git/Audacia.CodeAnalysis?path=%2FAudacia.CodeAnalysis.AspNetCore%2FREADME.md&_a=preview) project for more information.

Now you need to set the ruleset in your `.csproj` files. This is done by adding a `<CodeAnalysisRuleset>` element in the first property group with a path to the relevant `.ruleset` relative to the project file. If the ruleset is in the same folder as project, don't forget to prepend the filename with `./`, for example `<CodeAnalysisRuleset>./CodeAnalysis.ruleset</CodeAnalysisRuleset>`. If you're using any of `CodeAnalysis.Tests.ruleset`, `CodeAnalysis.Libraries.ruleset` or `CodeAnalysis.AspNetCore.ruleset` then you don't need to specify the base ruleset in your `.csproj` as it is referenced by the other rulesets.

Finally, build your solution! You will probably notice a lot of inspection warnings that were not present before.

## More Information on Analyzers

The analyzers used come from a variety of sources.

### CSharpGuidelinesAnalyzer

The [CSharpGuidelinesAnalyzer](https://github.com/bkoelman/CSharpGuidelinesAnalyzer) implements analyzers for the C# guidelines (found [here](https://csharpcodingguidelines.com/)) that, for a long time have been the basis of Audacia's C# coding standards.

All rules are prefixed `AV`.

### Roslynator

The [Roslynator](https://github.com/JosefPihrt/Roslynator) project is a large collection of analyzers covering most areas of C# code.

All rules are prefixed `RCS`.

### Microsoft Analyzers

Microsoft provide a number of analyzers for checking best practice C# and .NET usage. See [here](https://docs.microsoft.com/en-us/visualstudio/code-quality/code-analysis-for-managed-code-warnings) and [here](https://github.com/dotnet/roslyn-analyzers) for more information.

All rules are prefixed `CA`.

### SecurityCodeScan

[SecurityCodeScan](https://security-code-scan.github.io/) provides analyzers for checking common security issues.

All rules are prefixed `SCS`.

### StyleCop

[StyleCop](https://github.com/DotNetAnalyzers/StyleCopAnalyzers) provides analyzers for checking formatting, naming, layout and other related areas.

All rules are prefixed `SA`.

### ReflectionAnalyzers

[ReflectionAnalyzers](https://github.com/DotNetAnalyzers/ReflectionAnalyzers) provides analyzers for checking correct usage of reflection in C# code.

All rules are prefixed `REFL`.

### IDisposableAnalyzers

[IDisposableAnalyzers](https://github.com/DotNetAnalyzers/IDisposableAnalyzers) provides analyzers for checking correct usage of `IDisposable` in C# code.

All rules are prefixed `IDISP`.

### DocumentationAnalyzers

[DocumentationAnalyzers](https://github.com/DotNetAnalyzers/DocumentationAnalyzers) provides analyzers for checking correct usage in documentation.

All rules are prefixed with `DOC`.