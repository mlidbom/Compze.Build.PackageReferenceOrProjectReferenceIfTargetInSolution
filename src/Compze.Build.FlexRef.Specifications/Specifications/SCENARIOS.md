# FlexRef Specification Scenarios

## Init Scenarios

| # | Scenario | Status | Description |
|---|----------|--------|-------------|
| 1 | `single-packable-project-with-one-consumer` | **Done** | One packable library (MyLib), one non-packable app (MyApp) that references it, one .slnx. Verifies generated FlexRef.config.xml (with AutoDiscover + commented explicit list) and build/FlexRef.props. |
| 2 | `multiple-packable-projects` | **Done** | Two packable libraries (MyLib, MyUtils) where MyUtils references MyLib, plus a non-packable app. Verifies all packable projects appear in the generated config's comment block. |
| 3 | `no-packable-projects` | **Done** | Workspace with only non-packable projects. Verifies Init still succeeds, creates config with AutoDiscover and no commented packages, and writes FlexRef.props. |
| 4 | `already-initialized` | **Done** | Workspace where FlexRef.config.xml already exists. Verifies `workspace.Init()` throws `ConfigurationAlreadyExistsException`. |

## Sync Scenarios

| # | Scenario | Status | Description |
|---|----------|--------|-------------|
| 1 | `single-packable-project-with-one-consumer` | **Done** | One packable library, one consuming app, one .slnx, AutoDiscover config. Verifies: Directory.Build.props created with import + UsePackageReference property, MyApp.csproj updated with flex reference pair, build/FlexRef.props written, .v3.ncrunchsolution created (no absent packages since all projects are in the solution). |
| 2 | `multiple-solutions-with-absent-packages` | **Done** | Two .slnx files: Full.slnx containing all projects, AppOnly.slnx with only the app. Verifies AppOnly's NCrunch file gets `UsePackageReference_MyLib = true`, while Full's NCrunch file has empty Settings. |
| 3 | `autodiscover-with-exclusions` | **Done** | Config uses `<AutoDiscover>` with `<Exclude Name="MyLib" />`. Two packable libraries (MyLib, MyUtils). Verifies MyLib is NOT treated as a flex reference — MyApp.csproj keeps its plain ProjectReference to MyLib, gets flex ref only for MyUtils. |
| 4 | `explicit-package-list` | **Done** | Config uses explicit `<Package Name="MyUtils" />` instead of AutoDiscover. Despite MyLib also being packable, only MyUtils becomes a flex reference. |
| 5 | `existing-directory-build-props-with-custom-content` | **Done** | start-state has a Directory.Build.props with a pre-existing PropertyGroup (EnableSourceControlManagerQueries). Verifies Sync preserves the custom content while adding FlexRef import and UsePackageReference properties. |
| 6 | `sync-is-idempotent` | **Done** | start-state is an already-synced workspace (identical to expected-state). Running Sync again produces no changes. Verifies the tool is safe to re-run. |
| 7 | `existing-ncrunch-file-with-custom-settings` | **Done** | start-state has a .v3.ncrunchsolution with custom settings (AllowParallelTestExecution, EnableRDI, etc.) and the solution is missing MyLib. Verifies Sync preserves those settings while adding CustomBuildProperties for the absent package. |
| 8 | `multiple-packable-projects-with-transitive-references` | **Done** | Two packable libraries (MyLib, MyUtils) where MyUtils references MyLib, plus an app referencing MyUtils. Verifies each .csproj gets the correct flex reference pairs and Directory.Build.props has properties for all flex-referenced packages. |
| 9 | `not-initialized` | **Done** | Workspace with no FlexRef.config.xml. Verifies `workspace.Sync()` throws `ConfigurationNotFoundException`. |
