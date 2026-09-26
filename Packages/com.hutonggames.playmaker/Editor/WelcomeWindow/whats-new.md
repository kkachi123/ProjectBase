# :star: What's New

For a full list of changes, see the ChangeLog.

## [2.0.0 beta 85] - 2026-9-4

### CHANGED

- Enum Definitions now generate next to their `.asset` by default instead of under a separate generated enums folder.

### ADDED

- Added LayerMask filter to [GameObjectFindClosest](action:GameObjectFindClosest) actions.

### FIXED

- Fixed Enum Definition editor error when selecting Flags option.
- Fixed Transform-to-GameObject variable conversion for GameObject fields.
- Fixed alignment of Enum flag fields in action editors.
- Fixed Markdown inline code rendering adding extra lines.
- Fixed internal _assetGuid migration issue in beta 84.
- Fixed warnings in Unity 6.6.