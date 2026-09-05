# Ghost FTP UI / UX Guidelines

This document defines the cross-platform desktop interaction rules for Ghost FTP **0.1.1 Beta** and the Windows Setup experience. The goal is a clear professional FTP workstation with the information density users expect from mature dual-pane clients, rendered entirely in Ghost FTP's own modern visual language.

Public patch releases do not remove or revert the workstation and interaction work completed during the preserved internal development history.

Ghost FTP is the product. **BRENDIGO LTD** is the developer and publisher shown on legal/publisher surfaces.

## Product identity and Beta status

User-facing product surfaces use **Ghost FTP** or the compact identifier **GhostFTP**. Legal/publisher surfaces may identify BRENDIGO LTD, but the publisher name must not replace the product name.

During the public `0.x.y` line the product is **Beta**. About, Setup and release/download documentation should make the current pre-1.0 status understandable rather than implying a finished stable 1.0 product.

The first stable product version is **1.0.0**. When that transition is made, Beta labeling is removed and `portable.exe` / `setup.exe` carry matching stable 1.0.0 metadata.

Shared identity values live in `GhostFTP.Design`. Shared visual primitives and the cross-platform reference palette also live in `GhostFTP.Design`. Version/channel rules live in `docs/VERSIONING.md` and root `VERSION` / `RELEASE_CHANNEL` files.

## Visual direction

Ghost FTP is intentionally not a visual clone of a legacy FTP client. It borrows proven information architecture—menu, compact actions, connection activity, two file tables and a transfer queue—while keeping its own premium dark/light design.

The visual system prioritizes:

- immediate scanability;
- practical information density;
- large usable file-list regions;
- compact action surfaces rather than oversized dashboard cards;
- consistent typography appropriate to each native desktop renderer;
- Windows Mica/rounded-window integration when supported;
- consistent Ghost FTP dark/light/system appearance where supported;
- restrained borders and section separation;
- accent treatment only for primary actions and selection;
- clear danger treatment for destructive actions;
- readable disabled, hover, selected and focus states;
- no accidental white legacy surfaces in dark mode.

Windows Mica/DWM enhancements are optional. Failure of a Windows visual API must never prevent Ghost FTP from running. Linux must remain usable on X11/XWayland without requiring a third-party GUI framework.

## Windows / Linux parity

Windows WPF and Linux X11/XWayland are native renderers of one Ghost FTP product. They share the same `GhostFTP.Core` protocol/transfer engine and `GhostFTP.Design` identity, localization and reference palette.

The parity contract requires the same major information hierarchy, product colors, core actions, connection/security meaning, transfer semantics, privacy boundaries and destructive-action scope. Native OS font rasterization, window decorations and platform-specific shell integrations can differ. We do not claim byte-identical pixels between WPF and X11.

The canonical visual comparison target is the authentic Windows capture at **1914×907 / 96 DPI**, and the normal layout reference uses a **292 px** left rail, **38 px** menu and **70 px** toolbar. See `docs/UI-PARITY.md`.

## Professional workstation hierarchy

The main desktop window has seven predictable layers:

1. **Menu bar** — File, View, Sites, Transfers, Tools, Help.
2. **Global toolbar** — Connect, Disconnect, Upload, Download, Refresh, New Folder, Rename, Delete, Site Manager, Settings, Diagnostics.
3. **Saved Sites sidebar** — fast profile selection and Site Manager entry.
4. **Connection strip** — Connection Log on the left and Quick Connect on the right.
5. **Local / Remote browser** — equal-weight dual file tables.
6. **Transfers queue** — full-width operational queue below browsing.
7. **Status bar** — privacy reminder and current connection state.

The layout should feel familiar to experienced FTP users without looking like an old Win32 application.

## Resizable professional workspace

Ghost FTP behaves as a workstation rather than a fixed dashboard.

Users can resize:

- Saved Sites versus the main workspace;
- Local versus Remote file panes;
- file browsing versus Transfers.

Windows supports persisted splitter geometry and double-click resets. Linux should preserve equivalent usable proportions and responsive behavior through its native renderer. Persisted geometry must be normalized before restoration.

Malformed persisted dimensions must never produce an unusable zero-sized or effectively off-screen layout.

## Menu and toolbar rules

The menu is permanent and text-first. Important workflows must remain discoverable even when users do not know icons or shortcuts.

The toolbar is compact and action-oriented:

- Connect/Disconnect are globally visible;
- upload/download remain distinct directional actions;
- Refresh is available globally and in each pane;
- file mutation actions stay close together;
- Site Manager, Settings and Diagnostics are directly reachable;
- important actions retain text labels rather than relying on unlabeled glyphs.

Toolbar items may compact or wrap at smaller supported widths rather than disappear or overlap.

## Connection Log

The Connection Log is an operational aid, not telemetry.

It may display:

- local startup state;
- local profile loading;
- explicit connection attempts;
- TLS/plain connection state;
- server working-directory/listing completion;
- disconnects;
- local errors already relevant to the user.

It must not display:

- passwords;
- protected password blobs;
- secret tokens;
- file contents;
- hidden product-network traffic, because Ghost FTP has none.

The log is bounded in memory, clearable by the user and never uploaded automatically.

## Quick Connect

Quick Connect always shows explicit labels for:

- Host;
- Port;
- Security;
- Username;
- Password.

Placeholder-only connection forms are not acceptable.

Explicit FTPS remains the recommended/default security mode. Plain FTP requires an explicit warning confirmation. Pressing Enter from Password may initiate Connect.

Visible Quick Connect values are authoritative. Editing them must not silently reuse incompatible connection state from a selected saved profile or Demo profile.

**Keep in this tab** is session-only. It may retain the ad-hoc connection in current-process UI state but must not persist that runtime profile or Quick Connect password to profile JSON.

## Saved Sites and Site Manager

Saved Sites provides compact access to known profiles. Full profile management belongs in Site Manager.

The Site Manager uses a familiar master/detail layout:

- saved-site list on the left;
- connection editor on the right;
- clear General and Advanced areas;
- Save, Connect and Cancel actions at the bottom.

Supported per-site fields are limited to real implemented product behavior:

- Site name;
- Host / IP / URL;
- Port;
- FTP/FTPS security mode;
- Username;
- Password;
- Remember password;
- default remote path.

Do not expose fake proxy, retry, TLS or protocol options merely because another FTP client has them. Global retry/concurrency/timeouts/keepalive remain centralized in Settings until Ghost FTP implements a real per-site override model.

The built-in Demo profile remains visible and useful but cannot be modified into a misleading real-server profile.

Remember password is opt-in. Windows uses CurrentUser DPAPI and Linux uses the documented local AES-256-GCM protector; neither renderer may silently store plaintext credentials.

## File panes

Local and Remote panes have equal default visual weight and are independently scannable.

Both panes support:

- Up;
- Home/root navigation;
- editable path;
- Refresh;
- New folder;
- Rename;
- Delete;
- filter/search;
- transfer action;
- item/selection summary;
- path copying where implemented.

Windows local conveniences can include Desktop, Documents, Downloads and Open in File Explorer. Linux can expose platform-appropriate local navigation rather than fake Windows shell actions.

Remote table columns include Name, Type, Size, Modified and server-provided Permissions when available. Local must not invent remote-style permissions data it cannot meaningfully represent through the current model.

Remote actions must remain disabled or fail clearly while disconnected.

## Responsive file tables

Columns scale with available pane width rather than forcing unnecessary horizontal scrolling.

Column priority is:

1. Name receives most flexible space;
2. Type remains compact;
3. Size remains compact;
4. Modified retains enough space for a readable timestamp;
5. Remote Permissions retains a bounded operational width.

At narrow widths, secondary columns may hide before primary file identity/actions. The declared minimum window size must still allow useful browsing and access to essential actions.

## Remote working directory

Remote navigation uses server `CWD` followed by `PWD`. The visible path represents the server-confirmed working directory, not an unverified client-side guess.

This keeps path-bar navigation, folder traversal, transfer destinations and diagnostics aligned.

## Connection state and keepalive

Connection status is operational state, not decoration.

- Connected state differentiates TLS from plain FTP.
- Lost control-channel state must not remain visually Connected.
- Keepalive may use FTP `NOOP` only against the currently selected real server session.
- Failed keepalive/diagnostic control checks reset stale transport state and surface Connection lost.
- Ghost FTP does not silently reconnect with saved credentials after a failure.
- Demo mode remains local and does not run automatic network keepalive.

The status surface remains an entry point for Connection Diagnostics where the native renderer supports it.

## Transfer queue

Transfers is an operational control surface rather than a passive status list.

It supports:

- queued/running/retrying/completed/failed/cancelled state;
- percentage progress;
- transferred bytes and known total;
- current speed;
- ETA when calculable;
- retry count;
- source and destination;
- start/finish details;
- Retry selected;
- Cancel selected;
- Cancel all;
- Clear finished;
- copy source/destination path where available;
- aggregate live throughput where available.

Double-clicking/activating a transfer opens details where implemented; it must not silently retry the job.

Queue capacity failures remain visible as failed jobs and must not crash the native event loop.

## Retry, concurrency, speed and ETA

Automatic retries are configurable from 0–5. Concurrent transfers are configurable from 1–8.

Real parallel transfers use isolated FTP/FTPS sessions. The browser control connection is never reused as a transfer worker.

Speed and ETA are current-session estimates. Resumed downloads establish a new measurement baseline so bytes already present in the partial file are not counted as current throughput.

Unknown ETA should display a neutral placeholder rather than an invented value.

## Editable controls are infrastructure

Host, Port, Username, Password, path, filter and dialog fields must retain native editing semantics.

On Windows, do not replace the WPF TextBox or PasswordBox editor/content host merely to obtain rounded visuals. Styling may change colors, typography, padding, borders, caret/highlight resources and focus visuals, but must preserve:

- caret placement;
- mouse and keyboard focus;
- text selection;
- Tab navigation;
- clipboard/edit shortcuts;
- keyboard layouts;
- IME behavior;
- PasswordBox behavior.

`GhostFTP.UiSmoke` validates live editable WPF controls on a Windows STA thread. Linux input handling must preserve equivalent keyboard/mouse usability in its X11 event model.

## Focus-safe keyboard routing

Keyboard shortcuts act on the context that owns focus. A sidebar, menu, toolbar or queue must never silently default destructive actions to Local.

- `F5` — refresh active Local/Remote pane.
- `F2` — rename in active Local/Remote pane.
- `Delete` — delete selected Local/Remote item(s), honoring confirmation settings.
- `Delete` while Transfers has focus — cancel selected active transfer.
- `Ctrl+F` — focus active Local/Remote filter.
- `Ctrl+L` — focus/select active Local/Remote path.
- `Ctrl+A` — select all in Local, Remote or Transfers.
- `Enter` — open selected Local/Remote item.
- `Backspace` — navigate to parent directory in Local/Remote.
- Enter in Quick Connect Password — connect.

Native renderers may map platform-specific equivalents, but destructive scope must remain explicit.

## Connection Diagnostics

Connection Diagnostics is user-initiated and clearly distinguishes:

- control-channel health;
- server system text;
- current remote directory;
- known server capabilities;
- TLS/plain transport state.

Diagnostics stay local and communicate only with the already selected server.

## Error boundaries

Normal workflow errors use Ghost FTP-styled dialogs, native equivalents or visible inline state, not raw unhandled exceptions.

Synchronous toolbar/context-menu actions and asynchronous network/filesystem operations require local exception boundaries. Recovery should preserve the main window whenever possible.

## Local Demo regression UX gate

The built-in Demo is not just a static mock table. The cross-platform `GhostFTP.DemoSelfTest` verifies the local-only workflow used to exercise product behavior without a server:

```text
connect → diagnostics → PWD/CWD → LIST → keepalive
→ download → upload/download byte round trip → rename
→ create/delete directory → recursive directory round trip
→ conflict protection → cleanup → root-delete protection
→ disconnect reset → reject post-disconnect operations
```

A file upload must not overwrite a directory and a directory upload must not overwrite a file. Demo failures must never create external network traffic.

## Authentic documentation screenshots

Repository screenshots must represent the actual application, not conceptual mockups.

The supported documentation path is:

```text
GhostFTP.App --capture-ui <directory>
```

It must:

- run the real compiled MainWindow;
- force deterministic English + dark presentation for documentation;
- connect only to built-in Demo mode;
- render the real MainWindow with WPF `RenderTargetBitmap`;
- render the real Site Manager;
- write `ghostftp-client.png` and `ghostftp-site-manager.png`;
- make no FTP network connection, telemetry request or external image-generation call.

CI validates that both captures can be produced and are non-empty. A separate repository workflow refreshes the checked-in images from the same production code.

## Setup wizard

Setup remains a guided Windows 11-style wizard:

```text
Language → License → Install options → Ready → Install/Update → Finish
```

The wizard must show Ghost FTP identity, BRENDIGO LTD legal/publisher identity where appropriate, language selection, embedded license, explicit license acceptance, install choices, visible progress/errors and a clear Finish action.

During the 0.x public line, Setup should clearly present the build as Beta in version-oriented surfaces. It must not claim that `setup.exe` or the embedded client is stable 1.0.0 until `VERSION=1.0.0` and `RELEASE_CHANNEL=stable`.

Language switching must not rebuild the WPF logical tree unsafely.

For updates, Setup stages and validates both application and maintenance-Setup binaries before changing the active installation, rejects an older candidate version and retains rollback copies until later install stages finish. UX must report a failed/rolled-back update rather than presenting partial success.

## Uninstall UX

Uninstall uses the installed `GhostFTP-Setup.exe --uninstall` maintenance application.

The flow must clearly state what is removed, allow local profiles/settings to be preserved or removed, report failures, and finish with Close. There is no separate generated uninstaller executable.

## Localization UX

English is primary/default and the guaranteed fallback. Ghost FTP validates 29 selectable application/Setup languages locally.

Missing non-critical technical text may fall back to English rather than display a misleading translation. Product/legal proper names remain Ghost FTP and BRENDIGO LTD where appropriate.

## Accessibility and maintainability

- Primary actions retain text labels.
- Selection, focus and disabled states remain visually distinct.
- Text does not rely on color alone.
- Important controls retain reasonable hit targets.
- Editable fields remain keyboard reachable.
- Resizable/compact layout rules preserve usable minimum pane sizes.
- Shared visual primitives belong in `GhostFTP.Design`.
- Windows UI source remains C#-only under the current zero-XAML product policy.
- Linux UI remains dependency-minimal and uses the documented system X11 ABI rather than a hidden browser/runtime framework.

## Release requirement

UI changes are not release-ready until the exact source commit passes compilation, source/version/channel/privacy/product/publisher/platform audit, Core tests, complete local Demo workflow tests on Windows and Linux, parallel queue tests, WPF input/localization/Setup tests, authentic production UI capture, native Linux X11/XWayland smoke tests and required packaging validation.

Passing those gates keeps a 0.x build eligible for Beta release; it does not make the product stable. Stable status begins only at the explicit **1.0.0** transition defined in `docs/VERSIONING.md`.
