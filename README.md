# PackageReferenceOrProjectReferenceIfTargetInSolution

Solution-aware MSBuild reference resolution for .NET projects.

### The Problem

When a .NET solution contains many projects that are also published as NuGet packages, you face a choice:

- **ProjectReference** — good for cross cutting development and refactoring across all projects, but requires all projects in the solution and builds are slow.
- **PackageReference** — fast builds and lightweight solutions with any subset of projects, but you lose the above advantages.

### Our Solution

Leverage MSBuild to make your references become project references if the referenced project is in the solution, and package references if not.

Then you can set up any number of solutions to fit what you need at the moment.

## Installation

1. Copy `PackageReferenceOrProjectReferenceIfTargetInSolution.props` into your repository (e.g. into a `build/` folder).

2. Import it from your `Directory.Build.props` (create the file if it doesn't exist):

```xml
<Project>
  <Import Project="$(MSBuildThisFileDirectory)build\PackageReferenceOrProjectReferenceIfTargetInSolution.props" />
</Project>
```

---
## Workspace setup

### 1. Declare switchable dependencies in `Directory.Build.props`

After the import, add one property per switchable dependency:

```xml
  <PropertyGroup>
    <UsePackageReference_Acme_Utilities
        Condition="'$(UsePackageReference_Acme_Utilities)' != 'true'
                 And '$(_SwitchRef_SolutionProjects)' != ''
                 And !$(_SwitchRef_SolutionProjects.Contains('|Acme.Utilities.csproj|'))">true</UsePackageReference_Acme_Utilities>

    <UsePackageReference_Acme_Core
        Condition="'$(UsePackageReference_Acme_Core)' != 'true'
                 And '$(_SwitchRef_SolutionProjects)' != ''
                 And !$(_SwitchRef_SolutionProjects.Contains('|Acme.Core.csproj|'))">true</UsePackageReference_Acme_Core>
  </PropertyGroup>
```

Property name convention: `UsePackageReference_{PackageName_with_dots_replaced_by_underscores}`

### 2. Add conditional references in each `.csproj`

```xml
<ItemGroup Condition="'$(UsePackageReference_Acme_Utilities)' == 'true'">
  <PackageReference Include="Acme.Utilities" Version="3.1.0" />
</ItemGroup>
<ItemGroup Condition="'$(UsePackageReference_Acme_Utilities)' != 'true'">
  <ProjectReference Include="..\Acme.Utilities\Acme.Utilities.csproj" />
</ItemGroup>
```

## Compatibility


### Confirmed to work with:

- Visual Studio 2026
- JetBrains Rider
- VS Code (C# Dev Kit and/or Resharper)
- `dotnet build` / `dotnet restore` CLI

### NCrunch  Workaround

We have been unable to get automatic detection working with ncrunch.
Our workaround is to set the required build properties in the ncrunch solution configuration.\
That is: `UsePackageReference_Acme_Utilities = true` etc in My.slnx.v3.ncrunchsolution


## CLI / CI Overrides

```shell
dotnet build /p:UsePackageReference_Acme_Utilities=true
```

## License

[Unlicense](https://unlicense.org/)