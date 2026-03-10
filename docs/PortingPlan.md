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
- `NumberingSystemElements`
- `PurgeAnnotations`
- `UnhideAllElements`
- `ViewsDuplicate`
- `TransferViewTemplates`
- `ViewFiltersBulkEdit`

## Validated Build Baseline
- `dotnet build .\Revit_Codex_HCB.sln -c "Debug R25" -p:DeployOnBuild=false`
- latest known result before next session: `0 Warning(s), 0 Error(s)`

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
- active ribbon features now have feature-level `icon16.png` and `icon32.png`; only `StylePreview` and `_Template` remain intentionally without icons

## Remaining
- `Accessory&Terminals Numbering`
- `Duct&Fitting Numbering`
- `RenumberByCategory`
- `RenumberRoomsWithSpline`
- `RenumberParkingWithSpline`
- `ViewFiltersLegend`
- revision tools
- sheet duplication
- broader find/replace variants for filters, room names, family types

## Notes For Next Session
- `Estimate` and `NumberingSystemElements` are now implemented and need runtime validation in Revit
- skip `TextTransform`, `Revision` tools, and `Duplicate Sheets` for now
- Visual Studio-only Revit flicker remains a debugger/session issue; normal Revit launch with deployed add-in is the runtime baseline
