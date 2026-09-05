# Ghost FTP UI / UX Guidelines

This document defines the Windows desktop and Setup interaction rules for Ghost FTP 1.4.0. The desktop application, dialogs, Setup wizard and uninstall flow must feel like one coherent Windows 11 product.

Ghost FTP is the product. **BRENDIGO LTD** is the developer and publisher shown on legal/publisher surfaces.

## Product identity

User-facing product surfaces use **Ghost FTP** or the compact identifier **GhostFTP**. Legal/publisher surfaces may identify BRENDIGO LTD, but the publisher name must not replace the product name.

Shared identity values live in `GhostBrand`. Shared visual primitives live in `GhostFTP.Design`.

## Windows 11 visual direction

The visual system prioritizes:

- clear hierarchy rather than decorative density;
- Mica/rounded-window integration when supported;
- Segoe UI Variable / Segoe UI typography;
- consistent dark/light/system appearance;
- rounded cards and restrained borders;
- accent treatment for primary actions;
- danger treatment for destructive actions;
- readable disabled, hover, selected and focus states;
- no accidental white legacy WPF surfaces in dark mode.

Mica and DWM enhancements are optional. Failure of a Windows visual API must never prevent the application from running.

## Main workspace hierarchy

The desktop client follows this hierarchy:

1. Ghost FTP identity and Saved Servers navigation.
2. Page header and global connection state.
3. Quick Connect.
4. Local / Remote file workspace.
5. Transfer queue.

Local and Remote panes should have equal visual weight.

## Responsive layout

The declared minimum window size must remain usable. Toolbars should wrap instead of clipping actions, path fields should receive flexible width, and long lists should scroll inside their pane.

GridView columns should scale with available pane width. Avoid fixed widths that cause unnecessary horizontal scrolling at the supported minimum size.

## Editable controls are functional infrastructure

Host, Port, Username, Password, path, filter and dialog fields must keep native WPF editing semantics.

Do not replace the TextBox or PasswordBox editor/content host merely to obtain rounded visuals. Shared styling may change colors, typography, padding, border presentation, caret/highlight resources, max length and focus visuals, but it must preserve:

- caret placement;
- mouse and keyboard focus;
- text selection;
- Tab navigation;
- Ctrl+C / Ctrl+V / Ctrl+X / Ctrl+Z;
- keyboard layouts;
- IME behavior;
- PasswordBox editing behavior.

`GhostFTP.UiSmoke` validates real editable controls on a Windows STA thread and blocks release packaging if core input behavior regresses.

## Quick Connect

Quick Connect must show explicit labels for:

- Host;
- Port;
- Security;
- Username;
- Password.

Placeholder-only connection forms are not acceptable.

FTPS Explicit remains the recommended/default profile security mode. Plain FTP requires an explicit warning confirmation before connection. Pressing Enter from the password field may initiate Connect.

The visible Quick Connect values are authoritative. Editing them must not silently reuse incompatible connection data from a selected saved profile or Demo profile.

## Saved Servers

Saved server navigation must support clear Add, Connect, Edit and Remove actions. Profile editing should show the same connection fields and security selector used by Quick Connect.

Remember password remains opt-in and should clearly explain that saved passwords are protected locally with Windows DPAPI.

## File panes

Both panes support consistent core actions:

- Up;
- Home/root navigation;
- Refresh;
- New folder;
- Rename;
- Delete;
- filter/search;
- transfer action;
- item/selection summary;
- path copying.

Local-specific conveniences include Desktop, Documents, Downloads and Open in File Explorer.

Remote actions must remain disabled or fail clearly while disconnected.

## Remote working directory

Remote folder navigation is synchronized with server `CWD` and `PWD`. The UI path should display the server-confirmed working directory, not an unverified client-side guess.

This rule applies to path-bar navigation and folder traversal so the user sees the directory the server actually accepted.

## Transfer queue

The transfer queue is an operational control surface rather than a passive status list.

It supports:

- progress and speed;
- queued/running/retrying/completed/failed/cancelled state;
- retry count;
- Retry selected;
- Cancel selected;
- Cancel all;
- Clear finished;
- copy source/destination path.

Queue-capacity failures remain visible as failed jobs. A queue or transfer failure must not crash the WPF event loop.

## Retry UX

Automatic transfer retries are configurable from 0 to 5. The UI should make retry behavior visible enough that a user can distinguish a currently retrying transfer from a frozen one.

Only transient failures are retried automatically. Permanent authentication, permission or certificate failures should surface immediately with actionable text.

## Connection Diagnostics

Connection Diagnostics is user-initiated. Its dialog should distinguish:

- control-channel health;
- server system text;
- current remote directory;
- known server capabilities;
- TLS/plain transport status.

The dialog should state that diagnostics remain local and communicate only with the already-connected server.

## Error boundaries

Normal user-facing workflow errors should use Ghost FTP-styled dialogs or inline error cards rather than raw unhandled exceptions.

Synchronous toolbar/context-menu actions and asynchronous network/filesystem operations require local exception boundaries. Recovery should preserve the main window whenever possible.

## Setup wizard

Setup is a guided Windows 11-style wizard, not a single-action utility screen.

The install flow is:

```text
Language → License → Install options → Ready → Install/Update → Finish
```

The wizard must:

- show Ghost FTP product identity;
- show BRENDIGO LTD publisher identity in legal/publisher context;
- allow language selection;
- display the embedded repository license;
- require explicit license acceptance before continuing;
- review install location and desktop-shortcut choice;
- show a Ready summary;
- show inline progress;
- show a visible inline failure message on Ready if install/update fails;
- offer Launch Ghost FTP after success.

## Uninstall UX

Uninstall uses the same `GhostFTP-Setup.exe --uninstall` maintenance application.

The flow should:

- clearly state that Ghost FTP will be removed;
- allow the user to preserve or remove local profiles/settings;
- summarize that choice before removal;
- display failures rather than claiming success;
- finish with a Close action.

There is no separate uninstaller executable.

## Localization UX

English is the primary/default language and fallback. Ghost FTP 1.4.0 validates 29 selectable languages for core application and Setup vocabulary.

Language switching is local-only. Missing non-critical technical text may fall back to English rather than presenting a misleading translation.

Product/legal proper names must remain semantically correct across languages: **Ghost FTP** for the product and **BRENDIGO LTD** for the publisher where publisher identity is shown.

## Keyboard shortcuts

- `F5` — refresh active pane.
- `F2` — rename selected item.
- `Delete` — delete selected item(s), honoring confirmation settings.
- `Ctrl+F` — focus active-pane filter.
- `Ctrl+L` — focus/select active-pane path.
- Enter in Quick Connect password field — connect.

## Accessibility and maintainability

- Primary actions retain text labels.
- Avoid tiny unlabeled icon-only controls for important workflows.
- Selection, focus and disabled states must remain visually distinct.
- Text must not rely on color alone to communicate meaning.
- Interactive controls should maintain reasonable hit targets.
- Editable fields remain keyboard reachable.
- Shared visual primitives belong in `GhostFTP.Design`, not duplicate app/setup theme classes.

## Release requirement

UI changes are not release-ready until the exact source commit passes compilation, source/privacy/product/publisher audit, Core self-tests, WPF editable-input tests, localization coverage tests and packaging validation.
