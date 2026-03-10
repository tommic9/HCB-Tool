# Icon Workflow

This add-in uses a feature-level icon workflow so new tools can be ported from `HCB_Tools` without repeating ribbon decisions each time.

## Goal

Use the Python tool icon as the starting metaphor, but do not copy it blindly into the C# add-in. Revit ribbon buttons need separate small and large icon behavior, and assets that look acceptable in pyRevit often break down when scaled.

## Folder Convention

Store icon assets inside the feature folder:

```text
src/HCB.RevitAddin/Features/<ToolName>/Resources/
  icon16.png
  icon32.png
```

Temporary fallback during porting:

```text
src/HCB.RevitAddin/Resources/
  icon32.png
```

The code first looks for `Features/<ToolName>/Resources/`, then falls back to the shared `Resources/` folder.

## Porting Rules

1. Find the source icon in `HCB_Tools`.
2. Keep the same meaning if the Python icon is already recognizable for users.
3. Prepare two ribbon variants:
   - `icon16.png` for compact ribbon states
   - `icon32.png` for normal large buttons
4. Use transparent background PNG files.
5. Reduce visual noise for the `16x16` icon. Remove thin strokes, small text, and low-contrast details.
6. Keep one consistent visual family across tools. Similar line weight, shadow treatment, corner radius, and palette.

## Decision Guide

Copy and refine the Python icon when:

- the metaphor is already clear,
- the icon is visually consistent with the rest of the add-in,
- it survives simplification to `16x16`.

Redraw the icon when:

- the Python icon depends on tiny details,
- it looks blurry or muddy at small size,
- it uses pyRevit-era styling that does not fit the new add-in,
- multiple tools start feeling visually inconsistent.

Search or design a new icon only when:

- the source icon is weak conceptually,
- the tool has changed meaning in the C# version,
- the existing icon cannot be made readable after simplification.

## Review Checklist

Before accepting an icon:

- check the `32px` icon on a full-size ribbon button,
- check the `16px` icon on compact ribbon state,
- verify the silhouette is still recognizable,
- verify it works on both light and dark surrounding UI,
- verify the icon still makes sense without reading the button text.

## Implementation Notes

- `RibbonIconResolver` applies both `Image` and `LargeImage`.
- Feature icons are copied to build output and publish output automatically.
- If a feature does not yet have dedicated icons, the add-in falls back to shared icons so development can continue.

## Recommended Workflow Per Tool

1. Port command/service/UI behavior.
2. Copy the source icon from `HCB_Tools` into the feature `Resources` folder as a temporary base.
3. Produce `icon16.png` and `icon32.png`.
4. Verify the icon in Revit after deployment.
5. Only then treat the feature as visually complete.
