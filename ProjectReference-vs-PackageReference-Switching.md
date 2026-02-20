# Automatically Switching Between ProjectReference and PackageReference in .csproj Files

## Problem Statement

The goal is to be able to open solutions containing various combinations of projects, so that you can:

- **Work on subsets of large library projects** without pulling in everything, incurring build time costs and costs in IDE load having to analyze everything.
- **Work on a project that consumes a large library** in two modes:
  - With the library as a **NuGet PackageReference** (fast, no need to build the library from source).
  - With the library projects **in the solution**, participating in refactoring and NCrunch testing via **ProjectReference**.

**Which solution you open should control this.** It does not necessarily have to be entirely auto-detected from the solution contents — some setup/configuration in NCrunch or elsewhere is acceptable, as long as the goal is achieved in a stable, reliable way.

Several approaches exist to implement this switching, each with distinct trade-offs — especially when NCrunch is involved.

---

## Approaches

### 1. Configuration-Based Switching (Debug/Release)

The simplest approach uses MSBuild's `Condition` attribute tied to the build configuration:

```xml
<ItemGroup Condition="'$(Configuration)' == 'Debug'">
  <ProjectReference Include="..\MyLib\MyLib.csproj" />
</ItemGroup>

<ItemGroup Condition="'$(Configuration)' == 'Release'">
  <PackageReference Include="MyLib" Version="1.2.3" />
</ItemGroup>
```

**Pros:**
- Zero setup for individual developers; works out of the box.

**Cons:**
- Forces all Debug builds everywhere to use project references, even on CI.
- Can cause version drift and subtle bugs if local source diverges from the published package.
- NCrunch: **Reported broken** — NCrunch runs builds in an isolated workspace, so path assumptions made by the project reference don't hold. Users have reported `DirectoryNotFoundException` errors. Visual Studio itself also has known issues where it randomly uses the wrong branch of a conditional reference.

---

### 2. Custom Boolean Flag in `Directory.Build.props`

An explicit opt-in property controls the switch:

```xml
<!-- Directory.Build.props -->
<PropertyGroup>
  <UseLocalMyLib>false</UseLocalMyLib>
  <LocalMyLibRepo>../../../MyLib</LocalMyLibRepo>
</PropertyGroup>
```

```xml
<!-- .csproj -->
<ItemGroup Condition="'$(UseLocalMyLib)' == 'true'">
  <ProjectReference Include="$(LocalMyLibRepo)/src/MyLib/MyLib.csproj" />
</ItemGroup>
<ItemGroup Condition="'$(UseLocalMyLib)' == 'false'">
  <PackageReference Include="MyLib" Version="1.2.3" />
</ItemGroup>
```

**Pros:**
- Explicit and easy to understand; works in both VS and CLI.
- Flag can be committed as `false` so CI always uses packages.

**Cons:**
- Every developer wanting the local override must manually edit the file or pass `/p:UseLocalMyLib=true` on the CLI.
- NCrunch: **Works if the conditional is in the `.csproj` itself** (not in `Directory.Build.props`), and you override the flag via NCrunch's Custom Build Properties setting.

---

### 3. Gitignored Local Override File

Uses MSBuild's `Exists()` function so developers opt in by creating a gitignored file:

```xml
<!-- In Directory.Build.props or the .csproj itself -->
<Import Project="local.props" Condition="Exists('local.props')" />
```

```xml
<!-- local.props (listed in .gitignore) -->
<Project>
  <PropertyGroup>
    <UseLocalMyLib>true</UseLocalMyLib>
  </PropertyGroup>
</Project>
```

**Pros:**
- No risk of accidentally committing the override; each developer manages their own local state independently.

**Cons:**
- Slightly more infrastructure to document and maintain; new team members need to know the convention exists.
- NCrunch: **Broken in practice.** NCrunch builds in an isolated workspace where `local.props` won't exist, so `Exists('local.props')` evaluates to `false` and NCrunch silently uses stale NuGet packages — even when the project is in the solution and being actively edited. This defeats the entire purpose.

---

### 4. Solution-Aware Auto-Detection

The most fully automatic approach reads `$(SolutionPath)` at build time and dynamically promotes `PackageReference` to `ProjectReference` when the corresponding project is in the current solution:

```xml
<Choose>
  <When Condition="$(ReplacePackageReferences) AND $(HasSolution)">
    <ItemGroup>
      <SmartPackageReference Include="@(PackageReference)">
        <InSolution>$(SolutionFileContent.Contains('\%(Identity).csproj'))</InSolution>
      </SmartPackageReference>
      <ProjectReference Include="@(PackageInSolution -> '$(SmartSolutionDir)\%(SmartPath)')" />
      <PackageReference Remove="@(PackageInSolution -> '%(PackageName)')" />
    </ItemGroup>
  </When>
</Choose>
```

**Pros:**
- Zero per-developer configuration; the right reference type is always used based on context.

**Cons:**
- The MSBuild script is complex and fragile — relies on string-matching `.sln` file content, breaks if project names don't match package IDs, and can confuse VS IntelliSense or Roslyn analyzer tooling.
- NCrunch: **Untested, and `$(SolutionPath)` is unreliable in NCrunch's isolated workspace.**

---

## Approach Comparison

| Approach | Complexity | CI-Safe | VS/Rider Compat | Accidental-Commit Risk |
|---|---|---|---|---|
| Debug/Release config | Low | No | Good | High |
| Custom property flag | Low–Medium | Yes | Good | Low |
| Gitignored local file | Medium | Yes | Good | Very Low |
| Solution-aware auto | High | Yes | Can break | Medium |

---

## NCrunch Compatibility Summary

| Approach | NCrunch Status |
|---|---|
| ProjectReference in `Directory.Build.props` | **Unsupported** — NCrunch only parses `.csproj` files for project dependencies |
| Debug/Release config switching | **Broken** — workspace path failures reported |
| Custom flag in `.csproj` | **Works** — override flag via Custom Build Properties |
| Gitignored local file (`Exists()`) | **Broken** — NCrunch silently uses stale NuGet packages |
| `$(NCrunch) == '1'` guard in `.csproj` | **Confirmed working** — endorsed by NCrunch's developer |
| Custom flag + `$(NCrunch)` OR guard | **Most robust combination** |
| Solution-aware auto-detection | **Untested** — `$(SolutionPath)` unreliable in NCrunch workspace |

### Key NCrunch Constraints

- **ProjectReference items must live inside `.csproj` files**, not in `Directory.Build.props` or other imported files. NCrunch's manipulation and workspacing only targets project files.
- **NCrunch sets `$(NCrunch) = '1'`** as an MSBuild property during all its builds, which can be used as a condition.
- **NCrunch exposes `$(NCrunchOriginalSolutionPath)` and `$(NCrunchOriginalSolutionDir)`** for advanced scenarios.
- **Custom Build Properties** set in `.ncrunchworkspace` files are propagated to grid nodes.

---

## Recommended Hybrid Approach

The final recommended design combines two mechanisms to handle all scenarios:

### Design Philosophy

**Default to ProjectReference** (safe — always builds from source). Only opt into PackageReference when explicitly told or when auto-detection confirms the project isn't in the solution. Using the project reference is always safe, just potentially sub-optimal (more/slower building), but guarantees you're using and testing the latest source.

### Per-Dependency Granularity via Naming Convention

Use underscores (not dots — dots are invalid in MSBuild property names / XML element names):

```
UsePackageReference_MyProj_LibA=true
UsePackageReference_MyProj_LibB=true
```

### `Directory.Build.props` (repo root)

Centralize the flag derivation logic:

```xml
<!-- Step 1: Read solution content once, during VS/Rider builds only.
     Skipped entirely when NCrunch is running. -->
<PropertyGroup Condition="'$(NCrunch)' != '1'
                       And '$(SolutionPath)' != ''
                       And '$(SolutionPath)' != '*Undefined*'
                       And Exists('$(SolutionPath)')">
  <_SolutionContent>
    $([System.IO.File]::ReadAllText('$(SolutionPath)'))
  </_SolutionContent>
</PropertyGroup>

<!-- Step 2: Derive all flags centrally -->
<PropertyGroup>
  <!-- LibA -->
  <_UsePackage_LibA Condition="'$(UsePackageReference_MyProj_LibA)' == 'true'">true</_UsePackage_LibA>
  <_UsePackage_LibA Condition="'$(_UsePackage_LibA)' != 'true'
                             And '$(_SolutionContent)' != ''
                             And !$(_SolutionContent.Contains('LibA.csproj'))">true</_UsePackage_LibA>

  <!-- LibB -->
  <_UsePackage_LibB Condition="'$(UsePackageReference_MyProj_LibB)' == 'true'">true</_UsePackage_LibB>
  <_UsePackage_LibB Condition="'$(_UsePackage_LibB)' != 'true'
                             And '$(_SolutionContent)' != ''
                             And !$(_SolutionContent.Contains('LibB.csproj'))">true</_UsePackage_LibB>
</PropertyGroup>
```

### In each `.csproj` that has a switchable dependency

Keep the actual reference items in the project file (required for NCrunch compatibility):

```xml
<ItemGroup Condition="'$(_UsePackage_LibA)' == 'true'">
  <PackageReference Include="MyProj.LibA" Version="1.2.3" />
</ItemGroup>
<ItemGroup Condition="'$(_UsePackage_LibA)' != 'true'">
  <ProjectReference Include="..\LibA\LibA.csproj" />
</ItemGroup>
```

Use conditional `ItemGroup` (not conditional items) — NuGet restore has had historical issues with conditions on individual `PackageReference` items not being respected correctly during Visual Studio's design-time builds.

### NCrunch `.ncrunchworkspace` Files

Full solution (nothing to set — the default is ProjectReference):

```xml
<WorkspaceSettings>
  <CustomBuildProperties></CustomBuildProperties>
</WorkspaceSettings>
```

Consumer-only solution (library not present):

```xml
<WorkspaceSettings>
  <CustomBuildProperties>
    UsePackageReference_MyProj_LibA=true;UsePackageReference_MyProj_LibB=true
  </CustomBuildProperties>
</WorkspaceSettings>
```

### VS/Rider Builds (Non-NCrunch)

For VS/Rider, the solution-content auto-detection handles everything. For the less common case where you want PackageReference from the command line, set environment variables before launching:

```powershell
# OpenConsumerOnly.ps1
$env:UsePackageReference_MyProj_LibA = "true"
$env:UsePackageReference_MyProj_LibB = "true"
rider64 ConsumerOnly.sln
```

For the full solution, just launch the IDE normally — no env vars, no script needed.

---

## How Each Scenario Flows

| Scenario | `$(NCrunch)` | `$(_SolutionContent)` | `_UsePackage_LibA` | Result |
|---|---|---|---|---|
| VS/Rider, full solution (LibA included) | unset | set, contains `LibA.csproj` | false | **ProjectReference** |
| VS/Rider, consumer-only (LibA absent) | unset | set, no `LibA.csproj` | true | **PackageReference** |
| NCrunch, full solution workspace | `1` | skipped | unset → false | **ProjectReference** |
| NCrunch, consumer-only workspace | `1` | skipped | true (from workspace) | **PackageReference** |
| `dotnet build` (no solution context) | unset | empty | false | **ProjectReference** |

---

## Known Limitations and Concerns

### String Matching Fragility
The `.Contains('LibA.csproj')` check matches against raw `.sln` text. If two projects share a filename in different folders (e.g., `LibA.csproj` and `Company.LibA.csproj`), you'd get a false positive. Use a more specific path fragment in the check if this is a risk.

### `$(SolutionPath)` in CLI Builds
`dotnet build` without a solution context leaves `$(SolutionPath)` empty, which falls through to ProjectReference (the safe default). This may fail if the sibling repo isn't checked out alongside. Adding an `Exists()` guard on the ProjectReference path handles this:

```xml
<ItemGroup Condition="'$(_UsePackage_LibA)' != 'true'
                  And Exists('..\LibA\LibA.csproj')">
  <ProjectReference Include="..\LibA\LibA.csproj" />
</ItemGroup>
<ItemGroup Condition="'$(_UsePackage_LibA)' == 'true'
                   Or !Exists('..\LibA\LibA.csproj')">
  <PackageReference Include="MyProj.LibA" Version="1.2.3" />
</ItemGroup>
```

### Linear Maintenance Scaling
Each new switchable library needs: a new property block in `Directory.Build.props`, a new conditional ItemGroup pair in the consuming `.csproj`, and potentially new entries in every `.ncrunchworkspace` file. For a handful of libraries this is fine; for dozens it becomes tedious. A custom MSBuild task or SDK-style NuGet package that automates the pattern would be worth it at scale.

### Version Drift Risk
When the ProjectReference path is active, you build against whatever commit of the library is currently checked out — not the pinned package version. This is "safe" in the sense of always being correct at build time, but can silently introduce API mismatches that only surface when you publish and switch back to PackageReference.

---

## Reusable Tooling

The recommended hybrid approach has been packaged as reusable tooling in the `build/` directory:

| File | Purpose |
|---|---|
| `build/SwitchableReferences.props` | Shared MSBuild infrastructure — import once per repo |
| `build/SwitchableReferences.README.md` | Step-by-step usage guide with copy-paste templates |
| `build/examples/Directory.Build.props.example` | Example showing per-dependency flag declarations |
| `build/examples/MyApp.csproj.example` | Example showing conditional ItemGroups in a project file |
| `build/examples/ConsumerOnly.ncrunchworkspace.example` | Example NCrunch workspace with overrides |

See [build/SwitchableReferences.README.md](build/SwitchableReferences.README.md) for full instructions.

---

## References

- [NCrunch: Troubleshooting Project Build Issues](https://www.ncrunch.net/documentation/troubleshooting_project-build-issues)
- [NCrunch: Build Properties](https://www.ncrunch.net/documentation/troubleshooting_ncrunch-build-properties)
- [NCrunch Forum: Directory.Build.props and ProjectReference](https://forum.ncrunch.net/yaf_postst3160_Directory-Build-props-and-ReferenceProject-entries.aspx)
- [NCrunch Forum: Conditional Project/DLL Reference](https://forum.ncrunch.net/yaf_postst840_Conditional-Project-DLL-reference-problem.aspx)
- [NCrunch Forum: Conditionally Referenced Project Fails to Build](https://forum.ncrunch.net/yaf_postst3522_Conditionally-referenced-project-fails-to-build.aspx)
- [Stack Overflow: Override NuGet PackageReference with Local ProjectReference](https://stackoverflow.com/questions/36636116/override-a-nuget-package-reference-with-a-local-project-reference)
- [Stack Overflow: Conditional PackageReference/ProjectReference Problems](https://stackoverflow.com/questions/70688633/problems-with-conditional-packagereference-projectreference-nodes-in-csproj-f)
- [Amplifying F#: Switch PackageReference to ProjectReference](https://amplifyingfsharp.io/blog/switchpackagetoprojref/)
- [MSBuild Property Functions (DevBlogs)](https://devblogs.microsoft.com/oldnewthing/20230327-00/?p=107974)
