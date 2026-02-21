# Code Review: Compze.Build.FlexRef (post-refactoring)

## Overall Assessment

The codebase is in good shape after the refactoring. `FlexRefWorkspace` is a clean domain model and facade, static mutable state is gone, responsibilities are well-separated, and the code is testable. The remaining issues are minor.

---

## Responsibility / Placement Issues

### 2. `FlexReferenceResolver` is still a nested static class inside `ManagedProject`

`FlexReferenceResolver.Resolve` takes a configuration and a list of projects — it doesn't operate on a single `ManagedProject` instance. It's a standalone resolution algorithm that happens to produce `FlexReference` objects from `ManagedProject` objects. Nesting it inside `ManagedProject` suggests it's an implementation detail of one project, when it's really workspace-level logic.

**File:** `ManagedProject.FlexReferenceResolver.cs`  
**Suggestion:** Extract to a top-level `FlexReferenceResolver` class. Its only caller is `FlexRefWorkspace.ScanAndResolve`, which would call it directly.

### 3. `ManagedProject.Scanner` — same nesting concern

`Scanner.ScanDirectory` creates a `ProjectCollection`, finds `.csproj` files, and constructs `ManagedProject` instances. It's a factory/scanner, not behavior of a single project. The nesting was inherited from the original design — now that `FlexRefWorkspace` is the entry point, the scanner could be a top-level class or even a static factory method on `FlexRefWorkspace` itself.

**File:** `ManagedProject.Scanner.cs`  
**Suggestion:** Either extract to a top-level class or absorb the scan logic into `FlexRefWorkspace.ScanAndResolve` since that's the only caller via `ManagedProject.ScanDirectory`.

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

