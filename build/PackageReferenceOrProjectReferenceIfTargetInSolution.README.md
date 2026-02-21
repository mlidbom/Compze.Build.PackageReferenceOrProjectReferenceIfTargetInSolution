# PackageReferenceOrProjectReferenceIfTargetInSolution

Solution-aware MSBuild reference resolution for .NET projects.

### The Problem

When a .NET solution contains many projects that are also published as NuGet packages, you face a choice:

- **ProjectReference** — great for development (source debugging, instant refactoring across all projects), but requires all projects in the solution.
- **PackageReference** — works with any subset of projects, but you lose all the above advantages.

### Our Solution

Create focused solution files for different parts of your codebase:

```
MyFramework.everything.slnx         ← contains all projects
MyFramework.top-level-concerns.slnx ← excludes utility projects to keep the solution small and fast
MyFramework.Utilities.slnx          ← just utilities + their tests
MyFramework.Samples.slnx            ← just samples (framework = NuGet packages)
```

At build time, projects present in the current `.slnx` are referenced as **ProjectReference**. Projects absent from the solution are automatically referenced as **PackageReference**.

### Requirements

- `.slnx` solution format (.NET 10+ default; migrate older solutions with `dotnet sln migrate`)

### Compatibility

Confirmed to work with:

- Visual Studio 2022 and 2026
- JetBrains Rider
- VS Code (C# Dev Kit)
- `dotnet build` / `dotnet restore` CLI
- NCrunch (including grid nodes)

---

## Installation

1. Copy `PackageReferenceOrProjectReferenceIfTargetInSolution.props` into your repository (e.g. into a `build/` folder).

2. Import it from your `Directory.Build.props` (create the file if it doesn't exist):

```xml
<Project>
  <Import Project="$(MSBuildThisFileDirectory)build\PackageReferenceOrProjectReferenceIfTargetInSolution.props" />
</Project>
```

> **Why a file copy instead of a NuGet auto-import?** This tool must participate in NuGet restore's project graph evaluation. NuGet package imports aren't available until after restore completes.

---

## Usage

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

References **must** be in the `.csproj` file itself (not in imported files) and use conditional `<ItemGroup>` (not conditional attributes on individual items).

### 3. Configure NCrunch (if applicable)

NCrunch cannot evaluate the auto-detection, so it needs explicit flags in `.v3.ncrunchsolution` files for consumer-only solutions:

```xml
<SolutionConfiguration>
  <Settings>
    <CustomBuildProperties>
      <Value>UsePackageReference_Acme_Utilities = true</Value>
      <Value>UsePackageReference_Acme_Core = true</Value>
    </CustomBuildProperties>
  </Settings>
</SolutionConfiguration>
```

Full solutions (all projects included) need no configuration.

---

## CLI / CI Overrides

```shell
dotnet build /p:UsePackageReference_Acme_Utilities=true
```

---

## Troubleshooting

- **NCrunch shows stale test results** — Check that `CustomBuildProperties` in your `.v3.ncrunchsolution` matches which libraries are/aren't in the solution.
- **Build error: project file not found** — The sibling project isn't checked out, or you're building without a solution context (default is ProjectReference). Build via the `.slnx` instead.
- **VS IntelliSense shows wrong references** — Close and reopen the solution, or run a manual NuGet restore.

---

## License

[Unlicense](https://unlicense.org/)