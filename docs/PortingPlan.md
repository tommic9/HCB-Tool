# Porting Plan

## Implemented
- `CopyFilters`
- `Levels`
- `Rename Views`
- `Rename Sheets`
- `Rename Materials`
- `FlowChanger`
- `ColorVentSystems`
- `ColorUniqueSystems`
- `SharedParameters`
- `SpaceParamLinked`
- `SpaceToElement`
- `DuctFittingsArea LIN`
- `DuctFittingsArea MC`
- `FittingsAngle`
- `LevelFromHVACElements`
- `SystemAssigner`
- `HCWireSize`
- `MassOfDuctsFittings`
- `Estimate`
- `ManualNumbering`
- `NumberingSystemElements`
- `Duct&Fitting Numbering`
- `Accessory&Terminals Numbering`
- `PurgeAnnotations`
- `UnhideAllElements`
- `ViewsDuplicate`
- `TransferViewTemplates`
- `ViewFiltersBulkEdit`
- `ViewFiltersLegend`
- `WithoutOpen / Scan Files`
- `WithoutOpen / Unload Links`
- `WithoutOpen / Family Report`
- `WithoutOpen / Upgrade Copy`
- `WithoutOpen / Add Shared`
- `WithoutOpen / Rename Family`

## Validated Build Baseline
- `dotnet build .\Revit_Codex_HCB.sln -c "Debug R25" -p:DeployOnBuild=false`
- latest known result before next session: `Build succeeded`, with only expected `CA1416` warnings from `WithoutOpenDialogService`

## Important Completed Refinements
- `Area` now stores real calculated values without legacy scaling and also copies area for `Ducts`
- `Area` now has a user option to clamp values below `1 m2` to supplier minimum
- color tools now show color swatches instead of raw RGB text and can optionally force black projection lines with weight `1`
- `ColorVentSystems` now follows legacy `Color Set Systems` semantics with preset packages from pyRevit
- `ColorUniqueSystems` now uses a single grouped window for individual ventilation and piping systems
- `PurgeAnnotations` has search-by-label plus category dropdown filtering
- `Bulk Edit` skips views that do not support `Visibility/Graphics Overrides`
- `SystemAssigner` now uses well-connected systems and expands target elements using `system.Elements`, `BaseEquipment`, and network collections
- `FittingsAngle` now offers additional source-angle parameter selection from available parameters instead of blind text input
- all `WithoutOpen` tools now use in-app preview windows with optional CSV export instead of summary-only `TaskDialog`
- `Upgrade Copy` now names outputs with `_Rxx` suffixes and replaces existing trailing Revit suffixes instead of appending duplicates
- `Add Shared` and `Rename Family` now support `copy-first` safety mode
- active ribbon features now have feature-level `icon16.png` and `icon32.png`, synchronized from the current `HCB_Tools` set where Python equivalents exist

## Remaining
- runtime validation in Revit for the latest `WithoutOpen` flows and recently finished numbering / filter legend tools
- nullable cleanup in `UI/Controls/ReportPreviewWindow.xaml.cs` if stricter warning hygiene is needed

## Notes For Next Session
- `WithoutOpen` implementation details and commit trail live in `docs/WithoutOpenPlan.md`
- branch used for this work: `feature/without-open-tools`
- `WithoutOpen` tools have no direct Python counterparts, so their icons are native to the C# add-in
- Python-equivalent feature icons were refreshed from the updated `HCB_Tools` folder
- revision tools, sheet duplication, `RenumberRoomsWithSpline`, `RenumberParkingWithSpline`, and broader find/replace variants remain outside the currently finished scope
- Visual Studio-only Revit flicker remains a debugger/session issue; normal Revit launch with deployed add-in is the runtime baseline
