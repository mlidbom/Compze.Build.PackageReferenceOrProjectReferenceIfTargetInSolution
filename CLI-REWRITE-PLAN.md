# FlexRef CLI Tool — Rewrite Plan

## What the CLI Tool should do

A `dotnet tool` invoked as `flexref sync`. It reads a config file and the project structure, then generates/updates the boilerplate XML in `Directory.Build.props`, each consuming `.csproj`, and each `.v3.ncrunchsolution` file. It is **idempotent** — running it again produces the same output, and running it after adding a new project patches in the new entries.

## Current State

- The CLI project exists at `src/Compze.Build.FlexRef.Cli/` with `.csproj` file intact (see below), but all `.cs` source files have been deleted. The project needs to be rewritten from scratch.
- The `example/` directory contains the hand-written versions of all files (the "before" state that the tool should be able to produce as output).
- `example/FlexRef.config.xml` exists with `<FlexRef><AutoDiscover /></FlexRef>`.

## Existing Project File

`src/Compze.Build.FlexRef.Cli/Compze.Build.FlexRef.Cli.csproj` already exists with:
- `net10.0` target framework
- `PackAsTool = true`, `ToolCommandName = flexref`
- MinVer for versioning
- NuGet metadata

This file should be kept as-is.

## Critical Implementation Rule: Use .NET XML Libraries

**All XML reading and writing MUST use `System.Xml.Linq` (`XDocument`, `XElement`, etc.).**

Do NOT use string/line-based manipulation for XML files. No `File.ReadAllLines` + `List<string>` + index arithmetic. No hand-rolled `ExtractAttributeValue` with `IndexOf`. No `line.Trim().StartsWith("<ItemGroup")` pattern matching.

Why:
- `.csproj`, `Directory.Build.props`, `.slnx`, `.v3.ncrunchsolution`, and `FlexRef.config.xml` are all well-formed XML.
- `XDocument` handles parsing, querying (LINQ), manipulation (add/remove/replace elements), and serialization correctly.
- `XDocument.Load(path, LoadOptions.PreserveWhitespace)` preserves formatting when needed.
- String-based XML manipulation is a bug factory: it breaks on multi-line elements, attributes with spaces, comments, self-closing tags, namespaces, and encoding edge cases.

## Config File Format

Location: `FlexRef.config.xml` at the repository root. sync cannot be run without it, you need to call init first to create the config file with default values and populate it with all discovered projects. Removing projects is far easier than manually adding them...

### Auto-discover mode (every project with `<PackageId>` or `<IsPackable>true` becomes switchable):

```xml
<FlexRef>
  <AutoDiscover />
</FlexRef>
```

### Auto-discover with exclusions:

```xml
<FlexRef>
  <AutoDiscover>
    <Exclude Name="Acme.Internal" />
  </AutoDiscover>
</FlexRef>
```

### Explicit package list:

```xml
<FlexRef>
  <Package Name="Acme.Core" />
  <Package Name="Acme.Utilities" />
</FlexRef>
```

### Combined (auto-discover plus explicit additions):

```xml
<FlexRef>
  <AutoDiscover>
    <Exclude Name="Acme.Internal" />
  </AutoDiscover>
  <Package Name="Some.External.Lib" />
</FlexRef>
```