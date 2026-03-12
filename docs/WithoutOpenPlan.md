# WithoutOpen Plan

## Status

`WithoutOpen` is implemented and available as a dedicated ribbon panel with a `WithoutOpen` pulldown.

The current toolset is intended for batch work on `.rvt` and `.rfa` files without opening them in the visible Revit UI. Some operations are truly metadata-only, while others open documents in the background through Revit API.

## Implemented Tools

1. `Batch File Scan`
   - Purpose: scan selected `.rvt` and `.rfa` files and classify them before processing.
   - API: `BasicFileInfo`.
   - Output: in-app report preview plus optional CSV export.
   - Key data: file type, version flags, worksharing flags, local/cloud classification, size, eligibility for `TransmissionData` and background-open workflows.

2. `Unload Links`
   - Purpose: unload external references from local `.rvt` projects without opening them in UI.
   - API: `TransmissionData`.
   - Output: in-app report preview plus optional CSV export.
   - Notes: skips non-project files and cloud paths.

3. `Family Parameter Report`
   - Purpose: inspect `.rfa` parameters and type metadata in background.
   - API: `Application.OpenDocumentFile`, `FamilyManager`.
   - Output: in-app report preview plus optional CSV export.
   - Key data: parameter name, shared/family, instance/type, group, spec type, formula, family/type counts.

4. `Upgrade And Copy`
   - Purpose: create upgraded copies of local `.rvt` and `.rfa` files for the currently running Revit version.
   - API: `Application.OpenDocumentFile`, `SaveAsOptions`, `WorksharingSaveAsOptions`.
   - Output: in-app report preview plus optional CSV export.
   - Naming: output files use a Revit suffix such as `_R25` / `_R26`.
   - Important behavior: if the file name already ends with a Revit suffix like `_R23`, `-R24`, ` R24`, or `R23`, that ending is replaced instead of appending another suffix.

5. `Batch Add Shared Parameters`
   - Purpose: add selected shared parameters from a chosen shared parameters file to `.rfa` files.
   - API: `FamilyManager.AddParameter`.
   - UI: dedicated options window for picking definitions, parameter group, instance/type, and save mode.
   - Output: in-app report preview plus optional CSV export.
   - Safety: supports `copy-first` mode to save modified families into a target folder.

6. `Batch Rename Family Parameters And Types`
   - Purpose: rename family parameters and family types in `.rfa` files using shared rename rules.
   - API: `FamilyManager.RenameParameter`, `FamilyManager.RenameCurrentType`.
   - UI: dedicated options window for find/replace, prefix, suffix, and save mode.
   - Output: in-app report preview plus optional CSV export.
   - Safety: supports `copy-first` mode to save modified families into a target folder.
   - Notes: skips shared parameters and name collisions.

## Current UX Pattern

All `WithoutOpen` tools now follow the same reporting pattern:

- operation window or file selection
- processing in background / metadata mode
- shared in-app `ReportPreviewWindow`
- optional `Export CSV`

Shared report preview files:
- `src/HCB.RevitAddin/UI/Controls/ReportPreviewColumn.cs`
- `src/HCB.RevitAddin/UI/Controls/ReportPreviewWindow.xaml`
- `src/HCB.RevitAddin/UI/Controls/ReportPreviewWindow.xaml.cs`

## Shared Infrastructure

Reusable services under `Infrastructure/WithoutOpen/`:

- `WithoutOpenFileClassifier`
- `WithoutOpenFileDiscoveryService`
- `WithoutOpenFileMetadataService`
- `WithoutOpenBatchLogService`
- `WithoutOpenDialogService`
- `WithoutOpenDocumentService`
- `WithoutOpenTransmissionDataService`

Supporting models under `Infrastructure/WithoutOpen/Models/`:

- `WithoutOpenFileKind`
- `WithoutOpenFileScanItem`
- `WithoutOpenOperationLogEntry`
- `WithoutOpenOperationStatus`

## Important Constraints

- Revit cloud / ACC / BIM 360 paths are detected and skipped where unsupported.
- Revit API work is single-threaded and currently executed sequentially.
- Metadata-only mode is used only where the API allows it; family inspection and edits still require background document open.
- `Upgrade And Copy` does not process files already saved in a later Revit version.
- `Batch Rename Family Parameters And Types` intentionally skips shared parameters.
- `FolderBrowserDialog` warnings `CA1416` are expected because this add-in is Windows/Revit-only.

## Recent Decisions

- Branch used for this work: `feature/without-open-tools`.
- Tool group name remains `WithoutOpen`.
- `Upgrade And Copy` naming uses `_Rxx` suffixes aligned with running Revit.
- Report previews were preferred over immediate CSV-only output.
- `WithoutOpen` icons were added, but these tools have no direct Python equivalents in `HCB_Tools`.

## Commit Trail

Main `WithoutOpen` implementation commits on this branch:

- `ce1035d` `Add WithoutOpen batch file scan`
- `7666e8b` `Add WithoutOpen unload links tool`
- `6a5a4d0` `Add WithoutOpen family parameter report`
- `6847fbd` `Add WithoutOpen upgrade and copy tool`
- `ad06fe8` `Add WithoutOpen batch shared family parameters`
- `2a19842` `Add WithoutOpen family rename tool`
- `0086366` `Add copy-first safety for WithoutOpen family tools`
- `52c2c02` `Add icons for WithoutOpen tools`
- `b39f432` `Fix WithoutOpen rename init and upgrade naming`
- `ac04131` `Add WithoutOpen report preview windows`
- `de6c4ab` `Add WithoutOpen operation previews and suffix replacement`
- `eb90a2e` `Add previews for WithoutOpen unload and upgrade`

## Next Validation

Recommended manual Revit checks:

- run all 6 tools from the `WithoutOpen` pulldown
- verify preview windows open and CSV export works
- verify `Upgrade And Copy` renames `*_R23` to `*_R25` instead of creating `*_R23_R25`
- verify `copy-first` mode for `Add Shared` and `Rename Family`
- verify cloud files are skipped with clear messages
