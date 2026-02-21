# Code Review: Compze.Build.FlexRef (post-refactoring)

## Overall Assessment

The codebase is in good shape after the refactoring. `FlexRefWorkspace` is a clean domain model and facade, static mutable state is gone, responsibilities are well-separated, and the code is testable. The remaining issues are minor.

---

## Responsibility / Placement Issues

### 1. `FindAbsentFlexReferencesFor` belongs on `SlnxSolution`

`NCrunchUpdater.FindAbsentFlexReferencesFor` asks "which flex references are absent from this solution?" That's a question about the solution's own content — `SlnxSolution` owns `ProjectFileNames` and should answer it. The updater shouldn't need to dig into `solution.ProjectFileNames` directly.

**File:** `NCrunchUpdater.cs` lines 95–100  
**Suggestion:** Move to `SlnxSolution` as an instance method taking `IReadOnlyList<FlexReference>`, mirroring how `ManagedProject.FindMatchingFlexReferences` works.

### 2. `FlexReferenceResolver` is still a nested static class inside `ManagedProject`

`FlexReferenceResolver.Resolve` takes a configuration and a list of projects — it doesn't operate on a single `ManagedProject` instance. It's a standalone resolution algorithm that happens to produce `FlexReference` objects from `ManagedProject` objects. Nesting it inside `ManagedProject` suggests it's an implementation detail of one project, when it's really workspace-level logic.

**File:** `ManagedProject.FlexReferenceResolver.cs`  
**Suggestion:** Extract to a top-level `FlexReferenceResolver` class. Its only caller is `FlexRefWorkspace.ScanAndResolve`, which would call it directly.

### 3. `ManagedProject.Scanner` — same nesting concern

`Scanner.ScanDirectory` creates a `ProjectCollection`, finds `.csproj` files, and constructs `ManagedProject` instances. It's a factory/scanner, not behavior of a single project. The nesting was inherited from the original design — now that `FlexRefWorkspace` is the entry point, the scanner could be a top-level class or even a static factory method on `FlexRefWorkspace` itself.

**File:** `ManagedProject.Scanner.cs`  
**Suggestion:** Either extract to a top-level class or absorb the scan logic into `FlexRefWorkspace.ScanAndResolve` since that's the only caller via `ManagedProject.ScanDirectory`.

### 4. `DeriveNCrunchFile` is `SlnxSolution` knowledge

`NCrunchUpdater.DeriveNCrunchFile` computes the NCrunch file path from a `.slnx` file — it knows the naming convention (stem + `.v3.ncrunchsolution`). This is really the solution's knowledge about where its companion file lives. It would be natural as a property or method on `SlnxSolution`.

**File:** `NCrunchUpdater.cs` lines 25–29

---

## Design Concerns

### 5. `FlexRefPropsFileWriter.WriteToDirectory` is not called through the workspace

`SyncCommand` calls `FlexRefPropsFileWriter.WriteToDirectory(rootDirectory)` directly, while the other three update operations go through `workspace.Update*()`. This inconsistency means one update operation bypasses the workspace facade.

**File:** `SyncCommand.cs` line 31  
**Suggestion:** Add `workspace.WriteFlexRefProps()` for consistency, or consciously leave it since `FlexRefPropsFileWriter` copies an embedded resource and doesn't need workspace data.

### 6. `FlexRefConfigurationFile` has two lifecycle phases with mutable state

The class is constructed, then `Exists()` is checked, then `Load()` is called, and only then are `UseAutoDiscover`, `AutoDiscoverExclusions`, and `ExplicitPackageNames` valid. Before `Load()`, these properties have default values that silently produce wrong behavior (empty lists, `false`). The `CreateDefault` path doesn't call `Load()` at all.

**File:** `FlexRefConfigurationFile.cs`  
**Suggestion:** Consider a static factory `FlexRefConfigurationFile.Load(rootDirectory)` that returns a fully populated instance or null if the file doesn't exist. This makes the "not loaded" state unrepresentable.

---

## Minor Issues

### 7. Duplicate filename/PackageId mismatch warning

Both `FlexReferenceResolver.Resolve` (line 44) and `FlexRefConfigurationFile.CreateDefault` (line 75) independently warn when a project's `PackageId` doesn't match its `.csproj` filename. Same check, same warning text pattern, two places.

**Files:** `ManagedProject.FlexReferenceResolver.cs` line 44, `FlexRefConfigurationFile.cs` line 75

### 8. `ManagedProject` constructor only accessible via `Scanner`

`ManagedProject`'s constructor is private and requires a `ProjectCollection` — the only way to create one is through `Scanner.ParseCsproj`. This makes the class impossible to construct in tests without a real `.csproj` file on disk and MSBuild evaluation. For unit testing `FindMatchingFlexReferences`, you'd need an internal constructor or a test-friendly factory that takes pre-computed values.

**File:** `ManagedProject.cs` line 19

### 9. `FlexReference` is a `record` but doesn't benefit from record semantics

`FlexReference` is declared as `record` but has a constructor that computes `PropertyName` from `PackageId`, and uses `{ get; }` syntax rather than positional parameters. It doesn't use value equality, `with` expressions, or deconstruction anywhere. A plain `class` would communicate intent more accurately.

**File:** `FlexReference.cs`

---

## Not Issues

- **`Version="*-*"`** — Conscious design choice, well-suited to the tool's purpose.
- **No `--dry-run`** — Nice-to-have UX, not a code quality issue.
- **`fetch-depth: 0` in CI** — Intentional for MinVer.
