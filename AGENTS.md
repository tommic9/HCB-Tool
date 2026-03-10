# Repository Guidelines

## Project Structure & Module Organization
This repository contains a native Revit add-in in C# targeting Revit 2025 and 2026.

- `src/HCB.RevitAddin/` - main add-in project
- `src/HCB.RevitAddin/Features/` - feature folders for individual tools
- `src/HCB.RevitAddin/Features/_Template/` - starter skeleton for new tools
- `src/HCB.RevitAddin/Infrastructure/` - add-in bootstrap, ribbon definitions, ribbon builders, icon resolution
- `src/HCB.RevitAddin/UI/Styles/` - shared WPF design tokens and control styles
- `src/HCB.RevitAddin/UI/Controls/` - reusable WPF controls shared across tools
- `src/HCB.RevitAddin/Resources/` - shared static assets and fallback resources
- `docs/IconWorkflow.md` - workflow for preparing ribbon icons during tool porting
- `HCB_Tools/` - Python/pyRevit source used as behavior reference
- `geeWiz/` - C# Revit add-in used as architecture and UI reference

Keep tools organized by feature, not by ribbon panel. Ribbon panels are presentation metadata defined centrally and should not drive folder structure.

Use this feature layout where applicable:

- `Features/<ToolName>/<ToolName>Command.cs` - Revit entry point
- `Features/<ToolName>/<ToolName>Service.cs` - business logic and Revit API operations
- `Features/<ToolName>/Models/` - feature-specific DTOs/view models
- `Features/<ToolName>/UI/` - feature windows and interaction logic
- `Features/<ToolName>/Resources/` - feature-specific icons and assets

Keep shared visual behavior out of feature folders. Colors, button styles, spacing, common controls, and reusable layouts belong under `UI/`.

## Build, Test, and Development Commands
Use solution-level commands from the repository root:

- `dotnet build .\Revit_Codex_HCB.sln -c "Debug R25"` - build for Revit 2025; currently deploys to the local Revit add-ins folder by default
- `dotnet build .\Revit_Codex_HCB.sln -c "Debug R26"` - build for Revit 2026; currently deploys to the local Revit add-ins folder by default
- `dotnet build .\Revit_Codex_HCB.sln -c "Debug R25 Deploy"` - explicit deploy-oriented build configuration for Revit 2025
- `dotnet build .\Revit_Codex_HCB.sln -c "Debug R26 Deploy"` - explicit deploy-oriented build configuration for Revit 2026
- `dotnet build .\Revit_Codex_HCB.sln -c "Release R25"` - release build for Revit 2025
- `dotnet build .\Revit_Codex_HCB.sln -c "Release R26"` - release build for Revit 2026
- `dotnet build .\Revit_Codex_HCB.sln -c "Debug R25" -p:DeployOnBuild=false` - local-only build for Revit 2025 without deployment
- `dotnet build .\Revit_Codex_HCB.sln -c "Debug R26" -p:DeployOnBuild=false` - local-only build for Revit 2026 without deployment

Visual Studio launch profiles are configuration-based. Prefer `Debug R25 Deploy` or `Debug R26 Deploy` when validating in Revit.

Revit loads the add-in from `%AppData%\Autodesk\Revit\Addins\<version>`, not from `bin`. If changes are not visible, verify deployment timestamps and ensure `Revit.exe` is fully closed before rebuilding.

## Coding Style & Naming Conventions
Use standard C# conventions:

- 4-space indentation
- `PascalCase` for classes, methods, properties
- `_camelCase` for private readonly fields
- one public command per feature folder

Prefer small service classes over large command files. Keep Revit transactions in service methods or narrowly scoped command flows. Use ASCII in source unless a file already contains localized text.

Match filenames to the main type: `CopyFiltersCommand.cs`, `CopyFiltersService.cs`, `CopyFiltersWindow.xaml`.

Start new tools from `Features/_Template/`, then rename the folder, files, namespaces, and types before wiring the command into the ribbon.

## Ribbon and Feature Registration
The add-in is hosted as an `Application` and creates its ribbon during startup.

- define tab, panel, and button metadata in `Infrastructure/RibbonDefinitions.cs`
- keep `Infrastructure/RibbonBuilder.cs` focused on composing Revit ribbon UI from definitions
- use `Infrastructure/RibbonIconResolver.cs` for icon lookup instead of hardcoding paths in commands or ribbon setup
- do not organize source folders by ribbon panel

When adding a new tool, register it once in the ribbon definitions and keep the implementation self-contained inside its feature folder.

## Shared UI System
Shared WPF styling is centralized and should stay centralized.

- `UI/Styles/SharedStyles.xaml` is the primary source of shared colors, brushes, control styles, spacing, and common visual behavior
- `UI/Controls/` contains reusable controls such as `SelectionActionsBar`
- `Features/StylePreview/` contains a designer/reference window for reviewing shared styles and controls outside Revit ribbon integration

When building new tool windows:

1. use `SharedStyles.xaml` before adding local styles
2. prefer reusable controls from `UI/Controls/` over duplicated XAML
3. keep feature-specific layout in the feature window, but keep reusable styling in shared resources
4. add new shared controls only when they are generic enough for multiple tools

Current reusable UI pieces already in use:

- `UI/Controls/SelectionActionsBar` - dual action bar for variants such as `All / None`, `All / Clear`
- `UI/Controls/DialogFooterBar` - shared footer with status text and primary/secondary actions
- `UI/Controls/SelectionListWindow` - reusable searchable multi-select window with initial selection support
- `UI/Controls/RenameOptionsWindow` - shared rename dialog used by view and sheet rename tools

Current multi-step porting pattern:

- prefer sequential shared dialogs over one large custom window when the workflow is naturally step-based
- use `SelectionListWindow` first for picking elements, then a smaller feature window for action options
- reserve custom feature windows for cases where source/target documents, action modes, or previews must be shown together

## Branding and Color Usage
The current shared styling follows `ColorBranding.md`.

- primary background surfaces should remain white: `#FFFFFF`
- structural color is HellCold Navy: `#2E466F`
- primary action color is HellCold Blue: `#1F8ECE`

Use shared style tokens instead of repeating raw color values across feature windows whenever possible.

## Icon Workflow
Do not treat Python icons as final assets.

- use `HCB_Tools/` icons as semantic starting points when porting tools
- prepare separate `icon16.png` and `icon32.png` for each feature
- store them in `Features/<ToolName>/Resources/`
- optimize the 16 px icon for readability instead of downscaling the 32 px icon blindly
- follow `docs/IconWorkflow.md` when evaluating whether to reuse, simplify, or redesign an icon

Fallback shared icons may live under `Resources/`, but feature-specific icons should be preferred.

## Testing Guidelines
There is no automated test project yet. Validate changes by:

- building `Debug R25`, `Debug R26`, or the matching `Deploy` configuration
- launching Revit from Visual Studio or after deployment
- testing against a sample model with the required views, filters, families, or templates
- verifying that the ribbon button appears with correct icon sizing in both large and compact ribbon states when relevant
- checking WPF windows against the shared styles preview when introducing new shared UI patterns

For new features, include at least one manual verification scenario in the PR description.

Known validation note:

- if Revit flickers only when started from Visual Studio, treat that as a debugger/session issue first; normal Revit launch with deployed add-in is the baseline for add-in stability checks

## Commit & Pull Request Guidelines
Root repository Git history is not currently available, so use clear imperative commit messages such as:

- `Add Copy Filters command`
- `Extract shared WPF styles`
- `Add deploy build configurations`

PRs should include:

- short summary of user-facing change
- affected Revit version(s)
- manual test steps
- screenshots for ribbon or WPF UI changes when visual output changed

## Security & Configuration Tips
Do not hardcode user-specific file paths except Revit installation paths that are already configuration-driven. Prefer `$(RevitVersion)` and `$(AppData)` in project settings.

This project currently deploys debug builds by default unless `DeployOnBuild=false` is passed explicitly. Be careful when rebuilding while Revit is running, because locked DLLs can leave the deployed add-in stale.

## Porting Workflow
This repository is intended to port tools from `HCB_Tools` into a native C# Revit add-in.

For each new tool:

1. Identify the source behavior in `HCB_Tools/`.
2. Use `geeWiz/` as the reference for C# add-in structure, ribbon setup, helpers, and UI patterns.
3. Use `D:\Revit 2025.3 SDK\Samples` to verify correct Revit API usage before implementing.
4. Split the implementation into:
   - command entry point
   - service or business logic
   - models or view models when needed
   - WPF UI only if needed
5. Keep the first C# version behaviorally aligned with the Python tool before expanding it.
6. Reuse shared UI styles and controls instead of recreating colors, buttons, and layouts per feature.
7. Prepare feature-level icons as part of the port, not as a final cleanup step.

When a Python script has gaps or rough edges, preserve the intended workflow but fix obvious API or architectural issues in the C# version.

Use `geeWiz` as a structural reference, not as a source to copy blindly. Prefer the simplest implementation that fits the current add-in and keeps future tools easy to add.

## Current Port Status
As of the current porting stage, these tools already exist in the native add-in and should be treated as the active baseline for further refinement:

- `CopyFilters` - WPF window, shared styles, feature icons
- `Levels` - WPF options window, safe naming format for Revit level names
- `Rename Views` - shared rename dialog
- `Rename Sheets` - shared rename dialog with split sheet number and sheet name controls
- `Rename Materials` - selection list plus shared rename dialog
- `FlowChanger` - WPF options window with parameter picker from the active view
- `ColorVentSystems` - preset package coloring for ventilation and piping system sets based on legacy pyRevit presets
- `ColorUniqueSystems` - manual selection of individual ventilation and piping systems in one grouped window
- `SharedParameters` - WPF window for reviewing and loading missing shared parameters
- `SpaceParamLinked` - selectable Revit link list before updating target parameters
- `SpaceToElement` - local MEP Space to element mapping for `LIN_ROOM_*`
- `DuctFittingsArea LIN` and `DuctFittingsArea MC` - command workflows available from the `Area` dropdown
- `FittingsAngle` - category picker plus source angle parameter selection from available parameters
- `LevelFromHVACElements` - copies `Level` or `Reference Level` into selected target parameter
- `SystemAssigner` - propagates `HC_System` from selected mechanical equipment across connected well-connected systems
- `HCWireSize` - generates `HC_WireSize` for electrical power circuits
- `MassOfDuctsFittings` - calculates `HC_Masa` for ducts, fittings, flex ducts, and accessories
- `Estimate` - pricing and cost assignment from CSV catalog for ducts, fittings, accessories, and flex ducts
- `NumberingSystemElements` - MEP traversal-based numbering from a selected mechanical equipment start element
- `PurgeAnnotations` - selection-based cleanup of unused annotation types
- `UnhideAllElements` - active view unhide utility
- `ViewsDuplicate` - multi-view duplicate flow with mode and copy count
- `TransferViewTemplates` - source/target document transfer with optional overwrite
- `ViewFiltersBulkEdit` - shared bulk edit flow for common filters across multiple views

Current ribbon direction:

- `Filters` is a large dropdown intended to grow with more filter-related tools
- `Area` is a large dropdown for fitting area variants
- stacked buttons and stacked pulldowns are preferred over long flat button lists
- `Views` is now a dedicated panel for duplication, template transfer, and view cleanup actions
- `HVAC Tools` is now the catch-all pulldown for helper tools such as angle, level propagation, system assignment, numbering, and mass
- `Spaces` now contains both linked-room propagation and local `SpaceToElement`
- `Manage` now also includes `HC Wire`, `Materials`, and `Estimate`
- `Color Systems` should represent preset packages (`Set Systems` behavior), while `Unique Colors` remains the manual per-system picker
- all active ribbon features should have their own `Resources/icon16.png` and `Resources/icon32.png`; only `StylePreview` and `_Template` are intentionally excluded

Known remaining extension tools not yet ported from the larger pyRevit source:

- `Accessory&Terminals Numbering`
- `Duct&Fitting Numbering`
- `RenumberByCategory`
- `RenumberRoomsWithSpline`
- `RenumberParkingWithSpline`
- revision tools
- sheet duplication
- filter legend generation
- broader find/replace variants for filters, room names, family types

Known current diagnostic note:

- if Revit flickers only when launched under Visual Studio debugging but not when started normally with the deployed add-in, treat it as a Visual Studio/debug-session issue first, not as a normal runtime add-in regression
