# GhostFTP UI / UX guidelines

This document defines the Windows desktop visual and interaction rules introduced with GhostFTP 1.2. The goal is consistency across the main application and setup/uninstall experience.

## Single design source

All shared presentation primitives live in `src/GhostFTP.Design`.

Do not create a second app-specific or setup-specific theme class for colors, typography, generic buttons, cards, form fields or Windows 11 window chrome. If a visual primitive is reusable, add it to `GhostTheme` or `GhostWindowChrome` and consume it from both applications.

## Visual hierarchy

The main application uses this hierarchy:

1. Saved-server navigation.
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
- Avoid fixed column widths that force horizontal scrolling at the minimum supported window size.
- Avoid default WPF white surfaces inside a dark GhostFTP window.

## Forms

Every connection field requires a visible label. Placeholder-only forms are not allowed for Host, Port, Security, Username or Password.

- Quick Connect is optimized for repeated use.
- Saved-profile editing may include explanatory hints.
- FTPS Explicit remains the recommended/default security choice.
- Password persistence must remain opt-in.

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

Setup/uninstall uses inline status and progress. Successful setup should offer `Launch GhostFTP`; successful uninstall should offer `Close`. Avoid chaining modal success prompts when the same state can be communicated in the current window.

## Dark/light behavior

`GhostTheme.Apply` owns the application resource palette. Controls must reference palette resources instead of hardcoded local colors except for intentional brand gradients.

The design system must cover at least:

- cards/surfaces;
- text and muted text;
- buttons;
- text/password fields;
- combo-box items;
- GridView headers;
- ListView/ListBox rows and selection;
- context menus;
- tooltips;
- status badges.

## Windows 11 integration

`GhostWindowChrome` is the only DWM integration helper. Mica and rounded corners are enhancements, not hard dependencies. The applications must continue to run when those calls are unavailable or fail.

## Accessibility and maintainability

- Text must remain readable without relying on color alone for meaning.
- Interactive elements require reasonable minimum hit targets.
- Avoid tiny unlabeled icon-only controls for primary workflows.
- Selection and disabled states must remain visually distinct.
- Keep shared UI code centralized; duplicated theme/chrome implementations are considered technical debt.
- New UI work must pass normal CI compilation, privacy/dependency audit and self-tests before release packaging.
