# Audacia.CodeAnalysis.EntityFrameworkCore

The [CodeAnalysis.EntityFrameworkCore.ruleset](https://github.com/dotnet/efcore/tree/main/src/EFCore.Analyzers) adds analyzers for Entity Framework Core for projects using this library.

See the [Audacia.CodeAnalysis](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/blob/master/README.md) project for full information about how to use the code analysis library.

# Contributing
We welcome contributions! Please feel free to check our [Contribution Guidelines](https://github.com/audaciaconsulting/.github/blob/main/CONTRIBUTING.md) for feature requests, issue reporting and guidelines.

## Updating the analyzers to support a new C# version

If any of the dependent analyzer packages are being upgraded to reference a newer version of Microsoft.CodeAnalysis.CSharp.Workspaces that results in a new minimum supported C# version (as per the official [Roslyn NuGet-packages.md](https://github.com/dotnet/roslyn/blob/main/docs/wiki/NuGet-packages.md)), the major version of Audacia.CodeAnalysis.EntityFrameworkCore must be incremented.

The description in the `.csproj` must be updated to include the minimum version of C#/.NET that is supported.

e.g

```xml
    <PropertyGroup>
        <Description>This package supports C# 12 and .NET 8 as a minimum.</Description>
    </PropertyGroup>
```