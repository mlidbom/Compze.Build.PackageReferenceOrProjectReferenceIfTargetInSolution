# SwitchableReferences — Usage Guide

Tooling for switching between `ProjectReference` and `PackageReference` based on which solution you open. Works with Visual Studio, Rider, `dotnet build`, and NCrunch (including grid nodes).

## How It Works

- **Default: ProjectReference** — always safe, always builds from latest source.
- **PackageReference is used only when** the project is explicitly absent from the solution or an override says so.
- **Two detection mechanisms** work together:
  - **VS/Rider/dotnet**: Auto-detects from the `.sln` file content at build time.
  - **NCrunch**: Uses explicit per-dependency flags set in `.ncrunchworkspace` Custom Build Properties (because NCrunch's isolated workspace makes solution-path detection unreliable).

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
       Each dependency needs two lines — copy this block and fill in:
         - The override property name: UsePackageReference_{PackageName with dots as underscores}
         - The .csproj filename to look for in the solution
  -->
  <PropertyGroup>
    <!-- Acme.Utilities -->
    <_UsePackage_AcmeUtilities Condition="'$(UsePackageReference_Acme_Utilities)' == 'true'">true</_UsePackage_AcmeUtilities>
    <_UsePackage_AcmeUtilities Condition="'$(_UsePackage_AcmeUtilities)' != 'true'
                                        And '$(_SwitchRef_SolutionContent)' != ''
                                        And !$(_SwitchRef_SolutionContent.Contains('Acme.Utilities.csproj'))">true</_UsePackage_AcmeUtilities>

    <!-- Acme.Core -->
    <_UsePackage_AcmeCore Condition="'$(UsePackageReference_Acme_Core)' == 'true'">true</_UsePackage_AcmeCore>
    <_UsePackage_AcmeCore Condition="'$(_UsePackage_AcmeCore)' != 'true'
                                   And '$(_SwitchRef_SolutionContent)' != ''
                                   And !$(_SwitchRef_SolutionContent.Contains('Acme.Core.csproj'))">true</_UsePackage_AcmeCore>
  </PropertyGroup>

</Project>
```

### 3. Use the flags in each `.csproj`

In every `.csproj` that references a switchable dependency, replace the plain `PackageReference` or `ProjectReference` with a conditional pair:

```xml
<!-- Acme.Utilities — switchable reference -->
<ItemGroup Condition="'$(_UsePackage_AcmeUtilities)' == 'true'">
  <PackageReference Include="Acme.Utilities" Version="3.1.0" />
</ItemGroup>
<ItemGroup Condition="'$(_UsePackage_AcmeUtilities)' != 'true'">
  <ProjectReference Include="..\Acme.Utilities\Acme.Utilities.csproj" />
</ItemGroup>
```

That's it for the build side. The `!= 'true'` condition means: if the flag is empty, unset, or anything other than `true`, default to ProjectReference.

### 4. Configure NCrunch workspace files

For each `.sln` in your repo, create or update the corresponding `.ncrunchworkspace` file.

**Full solution** (all library projects included — nothing to configure):

```xml
<WorkspaceSettings>
  <CustomBuildProperties></CustomBuildProperties>
</WorkspaceSettings>
```

**Consumer-only solution** (some libraries are NOT in the solution and should come from NuGet):

```xml
<WorkspaceSettings>
  <CustomBuildProperties>
    UsePackageReference_Acme_Utilities=true;UsePackageReference_Acme_Core=true
  </CustomBuildProperties>
</WorkspaceSettings>
```

The `CustomBuildProperties` value is a semicolon-separated list of `Name=Value` pairs. These are propagated to NCrunch grid nodes automatically.

### 5. Commit everything

All of these files are safe to commit:
- `build/SwitchableReferences.props`
- `Directory.Build.props`
- Modified `.csproj` files
- `.ncrunchworkspace` files

No gitignored files, no per-developer local overrides needed.

---

## Template: Adding a New Switchable Dependency

When you need to make a new dependency switchable, you need three things:

### Naming Convention

| Concept | Example |
|---|---|
| NuGet package name | `Acme.Utilities` |
| Override property name | `UsePackageReference_Acme_Utilities` (dots → underscores) |
| Internal flag property | `_UsePackage_AcmeUtilities` (your choice, just be consistent) |
| `.csproj` filename to detect | `Acme.Utilities.csproj` (as it appears in the `.sln` file) |

Dots are invalid in MSBuild property names (they map to XML element names). Always use underscores.

### Add to `Directory.Build.props`

Copy this block and replace the four placeholders:

```xml
    <!-- {DESCRIPTION} -->
    <{FLAG} Condition="'$({OVERRIDE_PROPERTY})' == 'true'">true</{FLAG}>
    <{FLAG} Condition="'$({FLAG})' != 'true'
                      And '$(_SwitchRef_SolutionContent)' != ''
                      And !$(_SwitchRef_SolutionContent.Contains('{CSPROJ_FILENAME}'))">true</{FLAG}>
```

| Placeholder | What to fill in | Example |
|---|---|---|
| `{DESCRIPTION}` | Human-readable comment | `Acme.Utilities` |
| `{FLAG}` | Internal flag property name | `_UsePackage_AcmeUtilities` |
| `{OVERRIDE_PROPERTY}` | Override property name | `UsePackageReference_Acme_Utilities` |
| `{CSPROJ_FILENAME}` | `.csproj` filename as in the `.sln` | `Acme.Utilities.csproj` |

### Add to each consuming `.csproj`

Copy this block and replace the four placeholders:

```xml
<!-- {DESCRIPTION} — switchable reference -->
<ItemGroup Condition="'$({FLAG})' == 'true'">
  <PackageReference Include="{PACKAGE_NAME}" Version="{VERSION}" />
</ItemGroup>
<ItemGroup Condition="'$({FLAG})' != 'true'">
  <ProjectReference Include="{PROJECT_PATH}" />
</ItemGroup>
```

| Placeholder | What to fill in | Example |
|---|---|---|
| `{DESCRIPTION}` | Human-readable comment | `Acme.Utilities` |
| `{FLAG}` | Internal flag property name (same as above) | `_UsePackage_AcmeUtilities` |
| `{PACKAGE_NAME}` | NuGet package ID | `Acme.Utilities` |
| `{VERSION}` | Package version | `3.1.0` |
| `{PROJECT_PATH}` | Relative path to the `.csproj` | `..\Acme.Utilities\Acme.Utilities.csproj` |

### Update `.ncrunchworkspace` files

For every solution that does NOT include the library project, add the override property to `CustomBuildProperties`:

```
UsePackageReference_Acme_Utilities=true
```

Separate multiple properties with semicolons.

---

## How Each Scenario Flows

| Scenario | `$(NCrunch)` | Solution content | Flag value | Result |
|---|---|---|---|---|
| VS/Rider — full solution (library included) | unset | contains `.csproj` | empty → false | **ProjectReference** |
| VS/Rider — consumer-only (library absent) | unset | does NOT contain `.csproj` | `true` | **PackageReference** |
| NCrunch — full solution workspace | `1` | skipped | empty → false | **ProjectReference** |
| NCrunch — consumer-only workspace | `1` | skipped | `true` (from `.ncrunchworkspace`) | **PackageReference** |
| `dotnet build` (no solution context) | unset | empty | empty → false | **ProjectReference** |
| CI with explicit override | unset | N/A | `true` (from env/CLI) | **PackageReference** |

---

## Optional: `Exists()` Fallback for ProjectReference

If you sometimes run `dotnet build` on a project without a solution context AND the sibling repos might not be checked out, the ProjectReference path will fail because the `.csproj` file doesn't exist on disk.

Add an `Exists()` guard to handle this gracefully:

```xml
<!-- Acme.Utilities — switchable reference with Exists() fallback -->
<ItemGroup Condition="'$(_UsePackage_AcmeUtilities)' != 'true'
                  And Exists('..\Acme.Utilities\Acme.Utilities.csproj')">
  <ProjectReference Include="..\Acme.Utilities\Acme.Utilities.csproj" />
</ItemGroup>
<ItemGroup Condition="'$(_UsePackage_AcmeUtilities)' == 'true'
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
The `.ncrunchworkspace` for your current solution is probably overriding the flag to use PackageReference. Check `CustomBuildProperties` — remove the override for that library so NCrunch uses ProjectReference.

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
If you have projects with similar names (e.g., `Utilities.csproj` and `Acme.Utilities.csproj`), use a more specific path fragment in the `.Contains()` check:

```xml
<!-- More specific: include part of the path -->
And !$(_SwitchRef_SolutionContent.Contains('src\Acme.Utilities\Acme.Utilities.csproj'))
```

---

## Known Limitations

- **Linear maintenance scaling**: Each new switchable dependency requires a property block in `Directory.Build.props`, conditional ItemGroups in consuming `.csproj` files, and potentially new entries in `.ncrunchworkspace` files. This is manageable for a moderate number of dependencies.
- **Version drift**: When using ProjectReference, you build against whatever source is checked out — not the pinned package version. This is by design (you want the latest source), but be aware of it.
- **`.csproj` filename uniqueness**: The `.Contains()` detection assumes `.csproj` filenames are unique across the solution. Use more specific path fragments if this isn't the case.
- **`$(SolutionPath)` in NCrunch**: Unreliable in NCrunch's isolated workspace — this is why NCrunch uses explicit overrides instead of auto-detection. The two paths never interfere with each other.
