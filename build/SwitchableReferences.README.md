# SwitchableReferences — Usage Guide

Tooling for switching between `ProjectReference` and `PackageReference` based on which solution you open. Works with Visual Studio, Rider, `dotnet build`, and NCrunch (including grid nodes).

## How It Works

- **Default: ProjectReference** — always safe, always builds from latest source.
- **PackageReference is used only when** the project is explicitly absent from the solution or an override says so.
- **Two detection mechanisms** work together:
  - **VS/Rider/dotnet**: Parses the `.slnx` file to extract project filenames at build time.
  - **NCrunch**: Uses explicit per-dependency flags set in `.v3.ncrunchsolution` Custom Build Properties (because NCrunch's isolated workspace makes solution-path detection unreliable).

## Constraints

These constraints come from NCrunch and NuGet restore behaviour. They're non-negotiable:

1. **`<ProjectReference>` items must be in the `.csproj` file itself** — not in `Directory.Build.props` or other imported files. NCrunch only parses `.csproj` files for project dependencies.
2. **Use conditional `<ItemGroup>`** (not conditional attributes on individual items) — NuGet restore has had issues with item-level conditions during VS design-time builds.
3. **Property derivation logic CAN live in `Directory.Build.props`** — only the `<ProjectReference>` / `<PackageReference>` elements themselves must be in `.csproj`.

---

## Setup — Step by Step

### 1. Get `SwitchableReferences.props` into your repo

Copy `build/SwitchableReferences.props` into your repository. Common locations:

- `build/SwitchableReferences.props` (recommended)
- A shared git submodule
- A well-known path relative to your repo root

### 2. Import it in `Directory.Build.props`

In your `Directory.Build.props` (create one at the repo root if you don't have one), import the shared file and declare your switchable dependencies:

```xml
<Project>

  <!-- Import the shared infrastructure -->
  <Import Project="$(MSBuildThisFileDirectory)build\SwitchableReferences.props" />

  <!-- Declare switchable dependencies.
       Each dependency needs one property — copy this block and fill in:
         - The property name: UsePackageReference_{PackageName with dots as underscores}
         - The .csproj filename to match (wrapped in | delimiters)
       The same property is used in .csproj conditions, NCrunch
       CustomBuildProperties, and env var / CLI overrides.
  -->
  <PropertyGroup>
    <!-- Acme.Utilities -->
    <UsePackageReference_Acme_Utilities
        Condition="'$(UsePackageReference_Acme_Utilities)' != 'true'
                 And '$(_SwitchRef_SolutionProjects)' != ''
                 And !$(_SwitchRef_SolutionProjects.Contains('|Acme.Utilities.csproj|'))">true</UsePackageReference_Acme_Utilities>

    <!-- Acme.Core -->
    <UsePackageReference_Acme_Core
        Condition="'$(UsePackageReference_Acme_Core)' != 'true'
                 And '$(_SwitchRef_SolutionProjects)' != ''
                 And !$(_SwitchRef_SolutionProjects.Contains('|Acme.Core.csproj|'))">true</UsePackageReference_Acme_Core>
  </PropertyGroup>

</Project>
```

### 3. Use the flags in each `.csproj`

In every `.csproj` that references a switchable dependency, replace the plain `PackageReference` or `ProjectReference` with a conditional pair:

```xml
<!-- Acme.Utilities — switchable reference -->
<ItemGroup Condition="'$(UsePackageReference_Acme_Utilities)' == 'true'">
  <PackageReference Include="Acme.Utilities" Version="3.1.0" />
</ItemGroup>
<ItemGroup Condition="'$(UsePackageReference_Acme_Utilities)' != 'true'">
  <ProjectReference Include="..\Acme.Utilities\Acme.Utilities.csproj" />
</ItemGroup>
```

That's it for the build side. The `!= 'true'` condition means: if the property is empty, unset, or anything other than `true`, default to ProjectReference.

### 4. Configure NCrunch solution files

For each `.sln` in your repo, create or update the corresponding `.v3.ncrunchsolution` file.

**Full solution** (all library projects included — nothing to configure):

```xml
<SolutionConfiguration>
  <Settings>
    <CustomBuildProperties />
  </Settings>
</SolutionConfiguration>
```

**Consumer-only solution** (some libraries are NOT in the solution and should come from NuGet):

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

Each `<Value>` element is a `Name = Value` pair. These are propagated to NCrunch grid nodes automatically.

### 5. Commit everything

All of these files are safe to commit:
- `build/SwitchableReferences.props`
- `Directory.Build.props`
- Modified `.csproj` files
- `.v3.ncrunchsolution` files

No gitignored files, no per-developer local overrides needed.

---

## Template: Adding a New Switchable Dependency

When you need to make a new dependency switchable, you need three things:

### Naming Convention

| Concept | Example |
|---|---|
| NuGet package name | `Acme.Utilities` |
| Property name | `UsePackageReference_Acme_Utilities` (dots → underscores) |
| `.csproj` filename to detect | `Acme.Utilities.csproj` (as it appears in the `.slnx` file) |

Dots are invalid in MSBuild property names (they map to XML element names). Always use underscores.

The same property name is used everywhere: `Directory.Build.props` auto-detection,
`.csproj` conditions, NCrunch `CustomBuildProperties`, and env var / CLI overrides.

### Add to `Directory.Build.props`

Copy this block and replace the three placeholders:

```xml
    <!-- {DESCRIPTION} -->
    <{PROPERTY}
        Condition="'$({PROPERTY})' != 'true'
                 And '$(_SwitchRef_SolutionProjects)' != ''
                 And !$(_SwitchRef_SolutionProjects.Contains('|{CSPROJ_FILENAME}|'))">true</{PROPERTY}>
```

| Placeholder | What to fill in | Example |
|---|---|---|
| `{DESCRIPTION}` | Human-readable comment | `Acme.Utilities` |
| `{PROPERTY}` | Property name | `UsePackageReference_Acme_Utilities` |
| `{CSPROJ_FILENAME}` | `.csproj` filename as in the `.slnx` | `Acme.Utilities.csproj` |

### Add to each consuming `.csproj`

Copy this block and replace the four placeholders:

```xml
<!-- {DESCRIPTION} — switchable reference -->
<ItemGroup Condition="'$({PROPERTY})' == 'true'">
  <PackageReference Include="{PACKAGE_NAME}" Version="{VERSION}" />
</ItemGroup>
<ItemGroup Condition="'$({PROPERTY})' != 'true'">
  <ProjectReference Include="{PROJECT_PATH}" />
</ItemGroup>
```

| Placeholder | What to fill in | Example |
|---|---|---|
| `{DESCRIPTION}` | Human-readable comment | `Acme.Utilities` |
| `{PROPERTY}` | Property name (same as above) | `UsePackageReference_Acme_Utilities` |
| `{PACKAGE_NAME}` | NuGet package ID | `Acme.Utilities` |
| `{VERSION}` | Package version | `3.1.0` |
| `{PROJECT_PATH}` | Relative path to the `.csproj` | `..\Acme.Utilities\Acme.Utilities.csproj` |

### Update `.v3.ncrunchsolution` files

For every solution that does NOT include the library project, add the override property to `CustomBuildProperties`:

```xml
<Value>UsePackageReference_Acme_Utilities = true</Value>
```

Each property gets its own `<Value>` element inside `<CustomBuildProperties>`.

---

## How Each Scenario Flows

| Scenario | `$(NCrunch)` | Solution content | Flag value | Result |
|---|---|---|---|---|
| VS/Rider — full solution (library included) | unset | project in `.slnx` | empty → false | **ProjectReference** |
| VS/Rider — consumer-only (library absent) | unset | project NOT in `.slnx` | `true` | **PackageReference** |
| NCrunch — full solution workspace | `1` | skipped | empty → false | **ProjectReference** |
| NCrunch — consumer-only workspace | `1` | skipped | `true` (from `.v3.ncrunchsolution`) | **PackageReference** |
| `dotnet build` (no solution context) | unset | empty | empty → false | **ProjectReference** |
| CI with explicit override | unset | N/A | `true` (from env/CLI) | **PackageReference** |

---

## Optional: `Exists()` Fallback for ProjectReference

If you sometimes run `dotnet build` on a project without a solution context AND the sibling repos might not be checked out, the ProjectReference path will fail because the `.csproj` file doesn't exist on disk.

Add an `Exists()` guard to handle this gracefully:

```xml
<!-- Acme.Utilities — switchable reference with Exists() fallback -->
<ItemGroup Condition="'$(UsePackageReference_Acme_Utilities)' != 'true'
                  And Exists('..\Acme.Utilities\Acme.Utilities.csproj')">
  <ProjectReference Include="..\Acme.Utilities\Acme.Utilities.csproj" />
</ItemGroup>
<ItemGroup Condition="'$(UsePackageReference_Acme_Utilities)' == 'true'
                   Or !Exists('..\Acme.Utilities\Acme.Utilities.csproj')">
  <PackageReference Include="Acme.Utilities" Version="3.1.0" />
</ItemGroup>
```

This silently falls back to the NuGet package if the project file isn't on disk. Only use this if you actually need it — it adds a subtle "which reference am I getting?" ambiguity.

---

## Optional: CLI / CI Overrides

Since MSBuild automatically picks up environment variables as properties, you can force PackageReference from the command line:

```shell
# Via MSBuild property
dotnet build /p:UsePackageReference_Acme_Utilities=true

# Via environment variable (also works)
set UsePackageReference_Acme_Utilities=true
dotnet build
```

This is useful for CI pipelines that should always use NuGet packages.

---

## Troubleshooting

### NCrunch shows stale test results after editing a library project
The `.v3.ncrunchsolution` for your current solution is probably overriding the flag to use PackageReference. Check `CustomBuildProperties` — remove the override for that library so NCrunch uses ProjectReference.

### Build error: project file not found
The ProjectReference path in the `.csproj` doesn't resolve. Either:
- The sibling repo isn't checked out at the expected relative path.
- You're building outside a solution context and the default (ProjectReference) is being used. Add the `Exists()` fallback, or build via the `.sln` file.

### NCrunch can't find the project reference
Ensure the `<ProjectReference>` is in the `.csproj` file itself — not in `Directory.Build.props` or an imported file. NCrunch only parses `.csproj` files for project dependencies.

### NuGet restore picks the wrong reference type
Use conditional `<ItemGroup>` (the pattern shown above), not conditional attributes on individual `<PackageReference>` items. NuGet restore has had issues with item-level conditions in VS design-time builds.

### Visual Studio IntelliSense shows wrong references
Close and reopen the solution, or run a manual NuGet restore. VS caches reference resolution aggressively and sometimes needs a nudge after changing conditions.

### The `.Contains()` check matches the wrong project
The `|` delimiters in `_SwitchRef_SolutionProjects` prevent most false positives (e.g. `Company.Acme.Core.csproj` won't match `|Acme.Core.csproj|`). If you have genuinely ambiguous filenames, the solution is to rename the `.csproj` file to be unique.

---

## Known Limitations

- **Linear maintenance scaling**: Each new switchable dependency requires a property block in `Directory.Build.props`, conditional ItemGroups in consuming `.csproj` files, and potentially new entries in `.v3.ncrunchsolution` files. This is manageable for a moderate number of dependencies.
- **Version drift**: When using ProjectReference, you build against whatever source is checked out — not the pinned package version. This is by design (you want the latest source), but be aware of it.
- **`.slnx` only**: The shared infrastructure parses `.slnx` (XML solution format). Classic `.sln` files are not supported. .NET 10+ SDK defaults to `.slnx`; for older SDKs, migrate with `dotnet sln migrate`.
- **`.csproj` filename uniqueness**: The `|` delimiters ensure exact filename matching, so `Company.Acme.Core.csproj` won't false-match against `|Acme.Core.csproj|`. However, if two genuinely different projects share the same `.csproj` filename, rename one.
- **NCrunch requires explicit flags**: NCrunch's `.csproj` parser builds a dependency graph *before* MSBuild runs, so it cannot evaluate property functions like `File.ReadAllText` or `Regex.Replace`. The `UsePackageReference_*` flags must be set explicitly in `.v3.ncrunchsolution` `CustomBuildProperties` for consumer-only solutions. These same flags are auto-detected for VS/Rider/CLI builds.
