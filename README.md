# Audacia.CodeAnalysis

### Standard .NET Analyzers and Rulesets for Audacia Projects

## Getting started

There are two analyzer packages available- a standard package and one with extra analyzers specifically for ASP.NET Core projects. Which one you install depends on the type of project you're working on.

Now you've installed the `Audacia.CodeAnalysis` or `Audacia.CodeAnalysis.AspNetCore` package you need to get one or more rulesets. Available rulesets are as follows:

[CodeAnalysis.ruleset](https://audacia.visualstudio.com/Audacia/_git/Audacia.CodeAnalysis?path=%2FAudacia.CodeAnalysis%2FCodeAnalysis.ruleset):
This is the base ruleset and should be included in all projects.

[CodeAnalysis.Tests.ruleset](https://audacia.visualstudio.com/Audacia/_git/Audacia.CodeAnalysis?path=%2FAudacia.CodeAnalysis%2FCodeAnalysis.Tests.ruleset):
This ruleset overrides certain rules in the base configuration specifically to allow for patterns used in tests such as public nested classes and underscores in names.

[CodeAnalysis.AspNetCore.ruleset](https://audacia.visualstudio.com/Audacia/_git/Audacia.CodeAnalysis?path=%2FAudacia.CodeAnalysis%2FCodeAnalysis.AspNetCore.ruleset):
This ruleset provides extra configuration for ASP.NET core analyzers, and disables the requirement for `.ConfigureAwait(false)` as ASP.NET core does not have a synchronization context.

Now you need to set the ruleset in your `.csproj` files. This is done by adding a `<CodeAnalysisRuleset>` element in the first property group with a path to the relevant `.ruleset`.

Finally, build your solution! You will probably notice a lot of inspection warnings that were not present before.