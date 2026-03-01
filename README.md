# Compze.Build.FlexRef

ProjectReference and PackageReference merged into one. Automatically switches based on the open solution.

### The Problem

When a .NET solution contains many projects that are also shared as NuGet packages, you face a choice:

- **ProjectReference** — good for cross cutting development and refactoring across all projects, but requires all projects in the solution and builds are slow.
- **PackageReference** — fast builds and lightweight solutions with any subset of projects, but you lose the above advantages.

### Our Solution

- Two .props files which together enable csproj files to use FlexReference, a hybrid PackageReference/ProjectReference that
   - becomes a ProjectReference if the referenced project is in the opened solution.
   - becomes a PackageReference if it is not
- A dotnet tool that
  - Automatically ensures that all the csproj files in your source tree use flex references everywhere that they should
  - Manages custom build properties override files for NCrunch solutions so that this all works painlessly in NCrunch as well 

Then you can set up any number of `.slnx` files to fit whatever parts of your project ecosystem you need to work with at the moment. 

For instance, as we develop Compze, we can open the monolithic solution with 50+ projects (and growing fast) to do cross cutting refactoring. Or we can open Compze.Threading.slnx which contains just a handful. Both solutions use the exact same csproj files. In the monolithic solution everything becomes project references, in the threading solution almost everything becomes package references so builds and tests are super fast and your IDE can sit back and relax.

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

- Visual Studio 2026
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