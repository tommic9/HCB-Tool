# HCB Revit Add-in Solution Overview

## Purpose

`HCB Revit Add-in` is a native C# solution for Autodesk Revit 2025 and 2026. Its purpose is to migrate internal production tools from the legacy `HCB_Tools` Python environment into a maintainable, testable, and distributable add-in architecture.

The solution is designed around two goals:

- deliver everyday production tools directly inside Revit through a structured ribbon UI
- package the result as a repeatable MSI installer that can be published and upgraded cleanly

## Solution Structure

The repository is split into two main projects.

### `src/HCB.RevitAddin`

This is the runtime add-in loaded by Revit. It targets `net8.0-windows`, uses WPF for dialogs, and references the Revit API through the `Nice3point.Revit.Api.*` packages.

Core structural rules:

- each business workflow lives in its own folder under `Features/`
- command, service, models, and UI classes are kept close together per feature
- shared bootstrap and reusable infrastructure live under `Infrastructure/`
- common WPF controls and styles live under `UI/`
- static assets such as icons and CSV data are copied with the build output

### `src/HCB.RevitInstaller`

This is a WiX v5 MSI project that packages the add-in for end users.

Installer characteristics:

- builds separate packages for Revit 2025 and Revit 2026
- installs per user under `%AppData%\Autodesk\Revit\Addins\<year>\`
- bundles both the `.addin` manifest and the full `HCB.RevitAddin` payload directory
- supports major upgrade behavior through stable `UpgradeCode` values per Revit version

## Architecture Summary

The add-in follows a modular feature architecture rather than a single monolithic command layer.

Typical feature composition:

- `...Command.cs` integrates with the Revit external command entry point
- `...Service.cs` contains the business logic
- `Models/` contains option and result DTOs
- `UI/` contains WPF windows for feature-specific interaction

This keeps Revit API entry points thin and allows feature logic to remain isolated and easier to evolve.

Cross-cutting concerns are handled centrally:

- ribbon registration and icon loading
- shared dialogs and selection windows
- batch file discovery and metadata extraction
- offline workflows based on file inspection and `TransmissionData`

## Functional Scope

The current codebase includes tools in several categories.

### Views And Filters

- `CopyFilters`
- `ViewFiltersBulkEdit`
- `ViewFiltersLegend`
- `TransferViewTemplates`
- `ViewsDuplicate`

### Naming, Reporting, And Cleanup

- `RenameViews`
- `RenameSheets`
- `RenameMaterials`
- `RenameFamilyContent`
- `FamilyParameterReport`
- `PurgeAnnotations`

### MEP And Model Data Workflows

- `FlowChanger`
- `HCWireSize`
- `SystemAssigner`
- `SpaceToElement`
- `SpaceParamLinked`
- `LevelFromHVACElements`
- `DuctFittingNumbering`
- `DuctFittingsArea`
- `MassOfDuctsFittings`
- `Estimate`

### Batch And Without-Open Workflows

- `BatchFileScan`
- `UnloadLinks`
- `UpgradeAndCopy`
- `BatchAddSharedFamilyParameters`

These workflows are especially important because they move part of the toolset beyond active-document operations and toward scalable batch processing of project and family files.

## Build And Release Flow

### Local Development

The solution uses configuration names tied to the target Revit version:

- `Debug R25`
- `Debug R25 Deploy`
- `Debug R26`
- `Debug R26 Deploy`
- `Release R25`
- `Release R26`

The add-in project can optionally deploy directly to the user's Revit addins directory during build, which is useful for local testing.

### MSI Release

The MSI project first builds the matching add-in output, then stages the payload into its own `obj/<configuration>/Payload/` directory, generates the `.addin` manifest, and finally packages everything into an MSI.

Release artifacts are produced in:

- `src/HCB.RevitInstaller/bin/Release R25/`
- `src/HCB.RevitInstaller/bin/Release R26/`

The current default MSI product version is `1.0.1`.

## Why This Repository Exists

This repository is not only a direct port of older tools. It is also the foundation for long-term maintenance:

- clearer separation of feature logic
- repeatable packaging and release process
- support for multiple Revit versions from one codebase
- a migration path away from ad hoc scripting toward a distributable internal product

## Related Documentation

- `README.md` for the quick start
- `docs/Installer.md` for packaging details
- `docs/WithoutOpenPlan.md` for the batch processing direction
- `docs/PortingPlan.md` for migration context
