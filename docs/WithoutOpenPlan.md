# WithoutOpen Plan

## Scope

`WithoutOpen` is a new tool group for batch work on `.rvt` and `.rfa` files without opening them in the Revit UI.

This group contains two technical modes:

- `Metadata-only`: read or modify file metadata and external references without opening a full Revit document in memory.
- `Background-open`: open files through Revit API in the background, process them, save results, and close them without showing UI documents to the user.

## Planned Tools

1. `Batch File Scan`
   - Input: selected files and later folder recursion.
   - Output: per-file report with file type, version, worksharing flags, cloud/local classification, size, and action eligibility.
   - API: `BasicFileInfo`.
2. `Unload Links`
   - Input: local `.rvt` files.
   - Output: unload selected external references and save updated transmission data.
   - API: `TransmissionData`.
3. `Family Parameter Report`
   - Input: `.rfa` files.
   - Output: parameter/type inventory report.
   - API: `Application.OpenDocumentFile`, `FamilyManager`.
4. `Upgrade And Copy`
   - Input: local `.rvt`/`.rfa` files older than current Revit.
   - Output: upgraded copies saved to target folder.
   - API: `Application.OpenDocumentFile`, `SaveAsOptions`, `WorksharingSaveAsOptions`.
5. `Batch Add Shared Parameters`
   - Input: `.rfa` files and selected shared parameter definitions.
   - Output: updated family copies or in-place updates.
   - API: `FamilyManager.AddParameter`.
6. `Batch Rename Family Parameters And Types`
   - Input: `.rfa` files and rename rules.
   - Output: renamed family parameters and family types.
   - API: `FamilyManager.RenameParameter`, `FamilyManager.Types`.

## Ribbon Placement

Use a new pulldown under `Manage` named `WithoutOpen`.

## Shared Infrastructure

Create reusable services under `Infrastructure/WithoutOpen/`:

- `WithoutOpenFileClassifier`
- `WithoutOpenFileDiscoveryService`
- `WithoutOpenFileMetadataService`
- `WithoutOpenBatchLogService`
- `WithoutOpenDialogService`
- `WithoutOpenDocumentService`
- `WithoutOpenTransmissionDataService`

## Delivery Order

1. `Batch File Scan`
2. `Unload Links`
3. `Family Parameter Report`
4. `Upgrade And Copy`
5. `Batch Add Shared Parameters`
6. `Batch Rename Family Parameters And Types`
