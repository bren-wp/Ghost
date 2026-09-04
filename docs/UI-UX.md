# Ghost FTP UI / UX guidelines

This document defines the Windows desktop visual, interaction and branding rules for Ghost FTP 1.3. The desktop app and setup/uninstall experience must behave as one product, not as separate visual systems.

## One brand only

Every user-visible product surface uses **Ghost FTP** or the compact identifier **GhostFTP**. Do not add secondary product, agency, author or vendor branding to:

- application chrome;
- navigation;
- About/settings/profile dialogs;
- setup/uninstall UI;
- Windows uninstall metadata;
- icons, screenshots or README artwork;
- shipping documentation.

Shared identity values and the programmatic icon live in `GhostBrand`. Repository artwork lives under `assets/brand` and `assets/readme`. CI rejects the previous alternate brand identity if it returns.

## Single design source

All shared presentation primitives live in `src/GhostFTP.Design`.

Do not create a second app-specific or setup-specific theme class for colors, typography, generic buttons, cards, form fields, product identity or Windows window chrome. If a visual primitive is reusable, add it to `GhostTheme`, `GhostBrand` or `GhostWindowChrome` and consume it from both applications.

## Visual hierarchy

The main application uses this hierarchy:

1. Ghost FTP identity and saved-server navigation.
2. Page header and global connection status.
3. Quick Connect card.
4. Local / Remote file workspace.
5. Transfer queue.

Primary actions must be visually stronger than maintenance/destructive actions. Upload, Download and Connect use the accent treatment. Delete/Remove/Uninstall use the danger treatment. Secondary navigation uses neutral or subtle treatments.

## Layout rules

- Main content must remain usable at the declared minimum window size.
- Toolbars containing file actions use wrapping layout instead of fixed horizontal rows that clip controls.
- Path fields receive the remaining horizontal space; navigation buttons use intrinsic width.
- Local and Remote panes have equal visual weight.
- Long lists scroll internally instead of growing the whole window.
- Grid columns scale with panel width and must not force unnecessary horizontal scrolling at the supported minimum window size.
- Dark mode must never expose default white WPF input/list/header surfaces.
- Spacing should use a small consistent rhythm rather than ad-hoc per-control gaps.

## Forms

Every connection field requires a visible label. Placeholder-only forms are not allowed for Host, Port, Security, Username or Password.

- Quick Connect is optimized for repeated use.
- Saved-profile editing may include explanatory hints.
- FTPS Explicit remains the recommended/default security choice.
- Password persistence remains opt-in.
- Controls must use the shared Ghost FTP dark/light treatment instead of platform-default white chrome.

## File panes

Both file panes provide consistent interaction:

- Up navigation.
- Home/root navigation.
- Refresh.
- New folder.
- Transfer action (Upload or Download).
- Rename.
- Delete.
- Filter.
- Item/selection summary.
- Context menu with relevant path actions.

Local-only conveniences include Desktop, Documents, Downloads and Open in File Explorer. Remote-only actions must not appear enabled while disconnected.

## Keyboard shortcuts

- `F5` — refresh active pane.
- `F2` — rename selected item in active pane.
- `Delete` — delete selected item(s) in active pane.
- `Ctrl+F` — focus filter for active pane.
- `Ctrl+L` — focus/select path for active pane.
- `Enter` in the Quick Connect password field — connect.

Destructive shortcuts must continue to honor the user's delete-confirmation setting.

## Status and feedback

Connection state is shown globally as a compact badge. Transfer state is summarized next to the Transfers heading. Long operations must never make success/failure dependent on hidden telemetry or network reporting.

Setup/uninstall uses inline status and progress. Successful setup offers `Launch Ghost FTP`; successful uninstall offers `Close`. A failed required file operation must never be translated into a success state.

## Dark/light behavior

`GhostTheme.Apply` owns the application resource palette. Controls must reference palette resources instead of hardcoded local colors except for intentional Ghost FTP brand gradients.

The design system covers at least:

- cards/surfaces;
- text and muted text;
- buttons;
- text/password fields;
- combo boxes and combo-box items;
- GridView headers;
- ListView/ListBox rows and selection;
- context menus;
- tooltips;
- status badges.

## Windows integration

`GhostWindowChrome` is the only DWM integration helper. Mica and rounded corners are enhancements, not hard dependencies. The applications continue to run when those calls are unavailable or fail.

The shared `GhostBrand.IconSource` is applied to app, setup and dialogs so titlebar/taskbar identity remains consistent without an external icon package.

## Accessibility and maintainability

- Text remains readable without relying on color alone for meaning.
- Interactive elements use reasonable minimum hit targets.
- Avoid tiny unlabeled icon-only controls for primary workflows.
- Selection and disabled states remain visually distinct.
- Buttons retain text labels even when an icon/glyph is present.
- Shared UI code stays centralized; duplicated theme/chrome/identity implementations are technical debt and rejected by audit where possible.
- New UI work must pass CI compilation, brand/privacy/dependency audit and self-tests before release packaging.
