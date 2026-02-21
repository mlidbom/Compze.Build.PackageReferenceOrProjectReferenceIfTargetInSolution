# Compze.Build.FlexRef

Solution-aware MSBuild reference resolution for .NET projects.

### The Problem

When a .NET solution contains many projects that are also published as NuGet packages, you face a choice:

- **ProjectReference** — good for cross cutting development and refactoring across all projects, but requires all projects in the solution and builds are slow.
- **PackageReference** — fast builds and lightweight solutions with any subset of projects, but you lose the above advantages.

### Our Solution

A dotnet tool that generates the MSBuild boilerplate to turn your references into flex references, which become project references if the referenced project is in the solution, and package references if not.

Then you can set up any number of `.slnx` solutions to fit what you need at the moment.

## Quick Start

Install the tool:

```shell
dotnet tool install --global Compze.Build.FlexRef
```

Initialize in your repository root:

```shell
flexref init
```

This scans for packable projects, creates `FlexRef.config.xml`, and writes `build/FlexRef.props`.

Review the generated config, then sync:

```shell
flexref sync
```

This updates `Directory.Build.props`, all `.csproj` files with flex references, and NCrunch solution files.

## What It Generates

`flexref sync` manages the following files:

- **`build/FlexRef.props`** — shared MSBuild infrastructure that reads the `.slnx` at build time to determine which projects are present.
- **`Directory.Build.props`** — imports `FlexRef.props` and declares per-dependency detection properties.
- **`.csproj` files** — conditional `PackageReference` / `ProjectReference` pairs for each flex reference.
- **`.v3.ncrunchsolution` files** — NCrunch custom build properties makes this work in NCrunch.

## Configuration

`FlexRef.config.xml` controls which packages become flex references:

```xml
<FlexRef>
  <AutoDiscover />
</FlexRef>
```

`<AutoDiscover />` finds all packable projects automatically. To exclude specific packages:

```xml
<FlexRef>
  <AutoDiscover>
    <Exclude Name="Acme.Internal" />
  </AutoDiscover>
</FlexRef>
```

Or list packages explicitly instead:

```xml
<FlexRef>
  <Package Name="Acme.Core" />
  <Package Name="Acme.Utilities" />
</FlexRef>
```

## Compatibility

### Confirmed to work with:

- Visual Studio 2022+
- JetBrains Rider
- VS Code (C# Dev Kit and/or ReSharper)
- `dotnet build` / `dotnet restore` CLI
- NCrunch (via generated `.v3.ncrunchsolution` files)

**Note:** Only `.slnx` solution files are supported. Classic `.sln` files are not.

## CLI / CI Overrides

```shell
dotnet build /p:UsePackageReference_Acme_Utilities=true
```

## License

[Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0)