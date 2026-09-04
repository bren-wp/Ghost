# Ghost FTP UI / UX guidelines

This document defines the Windows desktop visual, interaction and branding rules for Ghost FTP 1.3.1. The desktop app and setup/uninstall experience must behave as one product, not as separate visual systems.

## One brand only

Every user-visible product surface uses **Ghost FTP** or the compact identifier **GhostFTP**. Do not add secondary product, agency, author or vendor branding to:

- application chrome;
- navigation;
- About/settings/profile dialogs;
- setup/uninstall UI;
- Windows uninstall metadata;
- icons, screenshots or README artwork;
- shipping documentation;
- repository paths or source identifiers intended for the product identity.

Shared identity values and the programmatic icon live in `GhostBrand`. Repository artwork lives under `assets/brand` and `assets/readme`. CI scans both text and repository paths for disallowed legacy identity tokens.

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
- Dark mode must never expose unintended white WPF list/header/dropdown surfaces.
- Spacing should use a small consistent rhythm rather than ad-hoc per-control gaps.

## Editable controls

Every connection field requires a visible label. Placeholder-only forms are not allowed for Host, Port, Security, Username or Password.

Text input is a functional requirement, not a styling detail. Shared Ghost FTP text/password controls must retain **native WPF editing behavior**.

Do not replace the TextBox or PasswordBox editor/content-host template merely to obtain rounded corners. Custom replacement templates can break caret rendering, mouse focus, IME, keyboard layouts, selection or text entry while still compiling successfully.

The shared controls may set:

- foreground/background resources;
- border colors;
- typography;
- padding and minimum height;
- caret/highlight colors;
- focusability and tab-stop behavior;
- maximum input length where a protocol field has a meaningful bound.

They must preserve:

- normal text and numeric entry;
- caret placement;
- mouse/keyboard focus;
- selection;
- Tab navigation;
- Ctrl+C / Ctrl+V / Ctrl+X / Ctrl+Z;
- keyboard layouts and IME input.

Current practical limits include a 253-character Host field, 5-character Port field and bounded Username input. Semantic validation still occurs when a connection/profile is submitted rather than blocking ordinary typing/paste at the keystroke level.

`GhostFTP.UiSmoke` is part of CI and Release validation. It creates the real shared WPF controls on a Windows STA thread and verifies that text/password values can be mutated and editable state remains enabled.

## Quick Connect

- Quick Connect is optimized for repeated use.
- Host, Port, Security, Username and Password remain visibly labeled.
- FTPS Explicit remains the recommended/default security choice.
- Enter from Password may initiate Connect.
- Password persistence remains opt-in through saved profiles; Quick Connect itself does not silently persist credentials.
- Validation errors appear in Ghost FTP dialogs and must not terminate the application.

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

Remote folder creation must report server failures accurately. An FTP `550` is not assumed to mean “already exists”; Ghost FTP verifies that the directory actually exists and is accessible before reporting success.

## Transfer queue

The Transfers area is an operational workspace, not only a progress display.

Supported queue actions include:

- retry selected failed/cancelled transfers;
- cancel selected;
- cancel all active/queued transfers;
- clear finished/cancelled/failed transfers;
- copy source path;
- copy destination path.

Queue selection supports multiple jobs. Queue-capacity failures remain visible as failed jobs with an error instead of escaping as an unhandled UI exception.

## Keyboard shortcuts

- `F5` — refresh active pane.
- `F2` — rename selected item in active pane.
- `Delete` — delete selected item(s) in active pane.
- `Ctrl+F` — focus filter for active pane.
- `Ctrl+L` — focus/select path for active pane.
- `Enter` in the Quick Connect password field — connect.

Destructive shortcuts must continue to honor the user's delete-confirmation setting.

## Dialogs and error boundaries

Normal user-facing errors, confirmations and destructive-operation prompts use Ghost FTP-styled dialogs rather than default platform MessageBox chrome.

Synchronous toolbar/context-menu actions and asynchronous operations both need local exception boundaries. A failed filesystem operation, queue action or server command should result in actionable feedback while preserving the main window whenever recovery is possible.

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

The shared `GhostBrand.IconSource` is applied to app, setup and dialogs so titlebar/taskbar identity remains consistent without an external icon package. Build-time icon generation separately embeds the Ghost FTP icon in the real executable resource.

## Accessibility and maintainability

- Text remains readable without relying on color alone for meaning.
- Interactive elements use reasonable minimum hit targets.
- Avoid tiny unlabeled icon-only controls for primary workflows.
- Selection and disabled states remain visually distinct.
- Buttons retain text labels even when an icon/glyph is present.
- Editable controls remain reachable by keyboard.
- Shared UI code stays centralized; duplicated theme/chrome/identity implementations are technical debt and rejected by audit where possible.
- New UI work must pass CI compilation, brand/privacy/dependency audit, Core self-tests and WPF editable-input smoke tests before release packaging.
