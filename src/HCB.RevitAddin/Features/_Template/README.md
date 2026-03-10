# Feature Template

Copy this folder when starting a new tool and rename the placeholders:

- `_Template` -> `ToolName`
- `FeatureCommand` -> `ToolNameCommand`
- `FeatureService` -> `ToolNameService`
- `FeatureResult` -> tool-specific model name
- `FeatureWindow` -> `ToolNameWindow` if the tool needs WPF

Suggested sequence:

1. Copy the folder.
2. Rename namespaces, files, and types.
3. Wire the command in `Infrastructure/RibbonDefinitions.cs`.
4. If the tool has UI, merge `UI/Styles/SharedStyles.xaml` and prefer reusable controls from `UI/Controls/`.
5. Port behavior from `HCB_Tools`.
6. Verify API usage against the Revit 2025.3 SDK samples.

Delete the WPF files if the tool does not need a dialog.
