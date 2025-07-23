# Audacia.CodeAnalysis.AspNetCore

The [CodeAnalysis.AspNetCore.ruleset](https://github.com/DotNetAnalyzers/AspNetCoreAnalyzers) provides extra configuration for ASP.NET Core analyzers, and disables certain other rules where controller actions do not align with the rule, for example adding an `Async` suffix to all `async` methods. This ruleset is applicable to ASP.NET Core Web API projects.

See the [Audacia.CodeAnalysis](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/blob/master/README.md) project for full information about how to use the code analysis library.

# Contributing

We welcome contributions! Please feel free to check our [Contribution Guidelines](https://github.com/audaciaconsulting/.github/blob/main/CONTRIBUTING.md) for feature requests, issue reporting and guidelines.

## Updating the analyzers to support a new C# version

If any of the dependent analyzer packages are being upgraded to reference a newer version of `Microsoft.CodeAnalysis.CSharp.Workspaces` that results in a new minimum supported C# version (as per the official [Roslyn NuGet-packages.md](https://github.com/dotnet/roslyn/blob/main/docs/wiki/NuGet-packages.md)), the major version of `Audacia.CodeAnalysis.EntityFrameworkCore` must be incremented.

The Description in the `.csproj` must be updated to include the minimum version of C#/.NET that is supported.

e.g
```xml
<PropertyGroup>
    <Description>
        Code analysis packages and rulesets for apps using Entity Framework Core.
        This package supports projects operating a minimum version of C# 12 and .NET 8.
    </Description>
</PropertyGroup>
```