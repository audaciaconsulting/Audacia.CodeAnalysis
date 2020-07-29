# Audacia.CodeAnalysis

The `Audacia.CodeAnalysis` repository contains .NET analyzers and rulesets to provide checks on Audacia's coding standards.

## Getting started

There are two analyzer packages available - a standard package and one with extra analyzers specifically for ASP.NET Core projects. Which one you install depends on the type of project you're working on.

Now you've installed the `Audacia.CodeAnalysis` or `Audacia.CodeAnalysis.AspNetCore` package you need to get one or more rulesets. The rulesets are provided as `.editorconfig` files. `.editorconfig` files should generally be located at the root of your repo or solution, however they can be located anywhere in the folder hierarchy, and the file closest a particular code file will be used. See [here](https://docs.microsoft.com/en-us/visualstudio/ide/create-portable-custom-editor-options?view=vs-2019#file-hierarchy-and-precedence) for more information.

Each ruleset is standalone (in that it contains all configured rules), and the available rulesets are as follows:

[General](https://dev.azure.com/audacia/Audacia/_git/Audacia.CodeAnalysis?path=%2FRulesets%2FAudacia.CodeAnalysis%2FGeneral%2F.editorconfig):
This is a general ruleset that contains a default set of rules and severities without being geared toward a particular kind of application. Generally speaking, you will either use one of the more specific rulesets below, or you will add this general ruleset in the root of your repo and then use the specific rulesets to override rules within certain sub-folders of your repo.

[Tests](https://dev.azure.com/audacia/Audacia/_git/Audacia.CodeAnalysis?path=%2FRulesets%2FAudacia.CodeAnalysis%2FTests%2F.editorconfig):
This ruleset relaxes certain rules in the general configuration specifically to allow for patterns commonly used in tests such as public nested classes and underscores in names.

[Libraries](https://dev.azure.com/audacia/Audacia/_git/Audacia.CodeAnalysis?path=%2FRulesets%2FAudacia.CodeAnalysis%2FLibraries%2F.editorconfig):
This ruleset changes the severity of certain rules in the general configuration specifically for use in libraries, for example to enforce stricter rules around documentation.

[AspNetCore](https://dev.azure.com/audacia/Audacia/_git/Audacia.CodeAnalysis?path=%2FRulesets%2FAudacia.CodeAnalysis%2FAspNetCore%2F.editorconfig): See the [Audacia.CodeAnalysis.AspNetCore](https://dev.azure.com/audacia/Audacia/_git/Audacia.CodeAnalysis?path=%2FRulesets%2FAudacia.CodeAnalysis.AspNetCore%2FREADME.md&_a=preview) project for more information.

Finally, build your solution! You will probably notice a lot of inspection warnings that were not present before.

## More Information on Analyzers

The analyzers used come from a variety of sources.

### Audacia Custom Analyzers

Audacia has a set of custom analyzers. See [here](https://dev.azure.com/audacia/Audacia/_git/Audacia.CodeAnalysis?path=%2FAnalyzers%2FAudacia.CodeAnalysis.Analyzers%2FREADME.md&_a=preview) for more information.

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