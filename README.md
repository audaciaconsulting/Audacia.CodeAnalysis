# Overview

The `Audacia.CodeAnalysis` repo contains static code analysis configuration and analyzers.

The `dotnet-roslyn` folder contains:
- Example `.editorconfig` files - see `dotnet-roslyn/config`
- Custom Roslyn analyzers and helpers - see `dotnet-roslyn/analyzers`

The `eslint` folder contains:
- Some default `eslint` config - see `eslint/config/audacia-eslint-config`

# Change History

The `Audacia.CodeAnalysis` repository change history can be found in the following changelogs:
- Roslyn
  - CodeAnalyzers [changelog](dotnet-roslyn/analyzers/Audacia.CodeAnalysis.Analyzers/CHANGELOG.md)
  - CodeAnalysis [changelog](dotnet-roslyn/config/Audacia.CodeAnalysis/CHANGELOG.md)
- ES Lint
  - ES Lint Config [changelog](eslint/config/audacia-eslint-config/CHANGELOG.md)
  - ES Lint Plugin Angular [changelog](eslint/plugins/audacia-eslint-plugin-angular/CHANGELOG.md)
  - ES Lint Plugin Vue [changelog](eslint/plugins/audacia-eslint-plugin-vue/CHANGELOG.md)

# Contributing
We welcome contributions! Please feel free to check our [Contribution Guidelines](https://github.com/audaciaconsulting/.github/blob/main/CONTRIBUTING.md) for feature requests, issue reporting and guidelines.

## Updating the analyzers to support a new C# version

If any of the dependent analyzer packages are being upgraded to reference a newer version of `Microsoft.CodeAnalysis.CSharp.Workspaces` that results in a new minimum supported C# version (as per the official [Roslyn NuGet-packages.md](https://github.com/dotnet/roslyn/blob/main/docs/wiki/NuGet-packages.md)), the major version of `Audacia.CodeAnalysis` must be incremented.

The description in the `.csproj` must be updated to include the minimum version of C#/.NET that is supported.

e.g

```csharp
    <PropertyGroup>
        <Description>This package supports C# 12 and .NET 8 as a minimum.</Description>
    </PropertyGroup>
```