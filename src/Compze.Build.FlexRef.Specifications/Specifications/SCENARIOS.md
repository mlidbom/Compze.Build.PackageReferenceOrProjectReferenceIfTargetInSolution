# FlexRef Specification Scenarios

## Init Scenarios

| # | Scenario | Status | Description |
|---|----------|--------|-------------|
| 1 | `single-packable-project-with-one-consumer` | **Done** | One packable library (MyLib), one non-packable app (MyApp) that references it, one .slnx. Verifies generated FlexRef.config.xml (with AutoDiscover + commented explicit list) and build/FlexRef.props. |
| 2 | `multiple-packable-projects` | Todo | Two packable libraries (e.g. MyLib, MyUtils) where MyUtils also references MyLib, plus an app. Verifies all packable projects appear in the generated config. |
| 3 | `no-packable-projects` | Todo | Workspace with only non-packable projects. Verifies Init still succeeds, creates config with empty AutoDiscover and no commented packages, and writes FlexRef.props. |
| 4 | `already-initialized` | Todo | CLI error scenario: workspace where FlexRef.config.xml already exists. Call `InitCommand.Execute` and verify it returns exit code 1 and writes the expected error to stderr. (Not a file-comparison test — tests CLI output.) |

## Sync Scenarios

| # | Scenario | Status | Description |
|---|----------|--------|-------------|
| 1 | `single-packable-project-with-one-consumer` | **Done** | One packable library, one consuming app, one .slnx, AutoDiscover config. Verifies: Directory.Build.props created with import + UsePackageReference property, MyApp.csproj updated with flex reference pair, build/FlexRef.props written, .v3.ncrunchsolution created (no absent packages since all projects are in the solution). |
| 2 | `multiple-solutions-with-absent-packages` | Todo | Two .slnx files: one full solution containing all projects, one partial solution missing a packable library. Verifies the full solution's NCrunch file has no CustomBuildProperties, while the partial solution's NCrunch file gets UsePackageReference entries for the absent library. |
| 3 | `autodiscover-with-exclusions` | Todo | Config uses `<AutoDiscover>` with `<Exclude Name="MyLib" />`. Verifies MyLib is NOT treated as a flex reference despite being packable — MyApp.csproj keeps its plain ProjectReference, Directory.Build.props has no UsePackageReference_MyLib property. |
| 4 | `explicit-package-list` | Todo | Config uses explicit `<Package Name="..." />` instead of AutoDiscover. Verifies only the explicitly listed packages become flex references. |
| 5 | `existing-directory-build-props-with-custom-content` | Todo | start-state has a Directory.Build.props with pre-existing custom PropertyGroups (e.g. EnableSourceControlManagerQueries). Verifies Sync preserves the custom content while adding/updating FlexRef import and UsePackageReference properties. |
| 6 | `sync-is-idempotent` | Todo | expected-state is identical to start-state (which is already a fully synced workspace). Running Sync again produces no changes. Verifies the tool is safe to re-run. |
| 7 | `existing-ncrunch-file-with-custom-settings` | Todo | start-state has a .v3.ncrunchsolution with custom settings (AllowParallelTestExecution, EnableRDI, etc.). Verifies Sync preserves those settings while updating/adding FlexRef CustomBuildProperties. |
| 8 | `multiple-packable-projects-with-transitive-references` | Todo | Two packable libraries where one references the other, plus an app referencing both. Verifies each .csproj gets the correct flex reference pairs and Directory.Build.props has properties for all flex-referenced packages. |
| 9 | `not-initialized` | Todo | CLI error scenario: workspace with no FlexRef.config.xml. Call `SyncCommand.Execute` and verify it returns exit code 1 and writes the expected error to stderr. (Not a file-comparison test — tests CLI output.) |
