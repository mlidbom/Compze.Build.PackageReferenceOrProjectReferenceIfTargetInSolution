# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## [Unreleased]

## [0.5.0-alpha.1] - 2026-02-22

### Changed
- Refactored into a dotnet tool that makes everything automatic

## [0.2.0-alpha.3] - 2026-02-21

### Changed
- Renamed to Compze.Build.FlexRef

## [0.2.0-alpha.2] - 2026-02-21

### Added
- Updates to readme and internal structure updates.

## [0.2.0-alpha.1] - 2026-02-21

### Added
- Initial public release
- Solution-aware `.slnx` parsing — auto-detects which projects are in the current solution
- Conditional `PackageReference` / `ProjectReference` switching based on solution membership
- NCrunch compatibility via `CustomBuildProperties` flags
- CLI / CI override support via `/p:` properties or environment variables
- Example workspace with `Acme.Full.slnx` and `Acme.AppOnly.slnx` demonstrating both modes
- NuGet content package distributing the `.props` file
- GitHub Actions CI and publish workflows
