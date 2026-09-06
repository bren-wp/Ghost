# Ghost FTP UI / UX Guidelines

This document defines the desktop interaction rules for Ghost FTP **0.1.2 Beta** and the Windows Setup experience. The goal is a clear professional FTP workstation with the information density expected from mature dual-pane clients, rendered in Ghost FTP's own modern visual language rather than copying a legacy client UI.

Ghost FTP is the product. **BRENDIGO LTD** is the developer/publisher shown on legal and publisher surfaces.

## Core UX principles

1. **The transfer target must be obvious.** Local and Remote operations belong with the pane they affect.
2. **Security state must be understandable.** Explicit FTPS is recommended; plaintext FTP warns before use.
3. **Credentials remain visually private.** Password values are masked and never echoed into the connection log.
4. **Desktop space is user-controlled.** Users can resize the window and important workstation regions.
5. **Primary work remains visible.** Optional controls must yield before they overlap connection or transfer actions.
6. **No account wall.** A user can connect to their own FTP/FTPS server without registering for a Ghost FTP account.
7. **No decorative UI that pretends to be functional.** Canonical screenshots are generated from the compiled application.

## Workstation hierarchy

The approved desktop information architecture is:

- saved-server sidebar;
- menu and primary toolbar;
- Connection Log + Quick Connect;
- Local + Remote file panes;
- Transfers queue;
- status area.

This hierarchy is the visual contract on both desktop platforms even when WPF and X11 require different native rendering primitives.

## Clean toolbar rule

The global toolbar is reserved for application-level actions:

- Connect;
- Disconnect;
- Upload;
- Download;
- Refresh;
- Site Manager;
- Settings;
- Diagnostics where layout permits.

Ghost FTP 0.1.2 removes the duplicate global **New folder**, **Rename**, and **Delete** buttons from the Windows reference shell. These operations remain in both file-pane toolbars, making it clear whether the operation affects Local or Remote content.

Do not reintroduce target-sensitive duplicates simply to fill toolbar space.

## Quick Connect

Normal workstation layout keeps the following aligned on one compact field row:

- Host;
- Port;
- Security;
- Username;
- Password.

Connect/Disconnect and session-only privacy controls are placed on the secondary row so credential fields do not become unusably narrow.

Rules:

- Host should receive the largest share of flexible width.
- Port stays compact but readable.
- Security must show the selected mode without clipping ordinary labels.
- Username/Password must remain usable at the supported minimum window width.
- **Keep in this tab** is session-only and must communicate that it does not save credentials to disk.

## Resizing model

Windows 0.1.2 supports:

- native overall window resize/minimize/maximize/restore;
- draggable saved-server sidebar width;
- draggable Connection Log / Quick Connect height;
- draggable Local / Remote split;
- draggable Transfers queue height.

Splitters use a visible but restrained border token and a correct resize cursor. Double-click resets the related split to a sensible reference value.

The Local/Remote ratio, Transfers height and maximized state continue to persist through local settings. Other reference shell splits may return to product defaults on restart unless explicitly added to the settings contract later.

## Compact window behavior

The main desktop must remain usable on smaller supported windows. The optional top language/search overlay hides before it can collide with primary toolbar commands. This is not loss of functionality:

- language selection remains in Settings;
- Remote filtering remains in the Remote pane;
- primary connect/transfer actions stay accessible.

Avoid horizontal overflow that forces core controls outside the window. Prefer hiding optional duplication, using ellipsis for secondary text, and preserving the functional pane layout.

## Local / Remote panes

Each pane must clearly show title, location/path controls, contextual actions, filter and list.

Local contextual actions:

- Upload;
- Refresh;
- New folder;
- Rename;
- Delete.

Remote contextual actions:

- Download;
- Refresh;
- New folder;
- Rename;
- Delete.

Destructive actions use the danger visual token. Primary transfer actions use the accent token. Disabled commands must not appear equally actionable to enabled ones.

## File-list behavior

Lists prioritize Name, then Type, Size, Modified, and Remote Permissions where available. Columns resize with the available pane so long paths/listings do not force a permanent horizontal scrollbar.

Folders sort ahead of files and names use case-insensitive ordering for the normal workstation view.

Double-click behavior must be predictable: folders navigate, Local files open through the operating system behavior defined by the app, and Remote files queue a download where that is the established interaction.

## Transfers queue

The queue must communicate:

- item;
- direction;
- state;
- progress;
- transferred bytes;
- speed;
- ETA;
- retry count;
- source;
- destination.

Queue controls include retry selected, cancel selected, cancel all and clear finished. Background work must never freeze the UI thread.

## Connection Log

The log is a local operational surface, not a debugging dump of credentials. It may record:

- startup privacy status;
- profile load count;
- connect/disconnect state;
- security mode/TLS state;
- directory-list completion;
- transfer and operation errors.

Passwords must never be logged. Error messages should help the user correct host, port, TLS mode or permissions without exposing secrets.

## Visual language

The reference palette uses dark navy surfaces and controlled contrast rather than flat pure black. Visual hierarchy comes from surface depth, borders, typography and spacing, not excessive gradients/animations.

Primary visual tokens:

- background / menu / sidebar / toolbar surfaces;
- Surface, Surface2 and hover layers;
- readable light text and muted secondary text;
- violet Accent for primary actions;
- green Success;
- red Danger;
- amber Warning.

Do not add decorative animations that reduce transfer-workstation clarity or increase input latency.

## Typography

Windows uses Segoe UI Variable/Segoe UI fallbacks. Linux uses its native X11 font-set path. Controls should use compact professional sizes suitable for information-dense desktop software.

Never bundle/download external font files at runtime just to make the UI render.

## Accessibility and keyboard behavior

Editable controls must remain genuinely focusable/editable. Do not create a visual TextBox that swallows mouse focus or prevents keyboard input.

Tab order should follow the visible Quick Connect field order. Dialogs must keep primary/secondary actions reachable without requiring mouse-only interaction.

Dangerous confirmation dialogs should visually distinguish destructive confirmation from normal primary actions.

## Localization layout

English is the primary/fallback language and the product exposes 29 local selectable languages. Layout must tolerate longer translated labels.

Toolbar labels should remain concise. Secondary copy may use wrapping/ellipsis when needed. A missing translation falls back to English instead of creating a blank button.

No online localization service is permitted.

## Windows / Linux parity

Windows / Linux parity means the same product workflow/security/privacy semantics, not pixel-identical use of incompatible window-system primitives.

Both must expose:

- saved sites and Quick Connect;
- FTP/explicit FTPS/implicit FTPS;
- Local/Remote browsing and file operations;
- transfer queue/cancellation/retry;
- local diagnostics/logging;
- session keepalive;
- 29-language local catalog;
- local-only profile settings;
- Demo mode.

See `docs/UI-PARITY.md` for the complete parity contract.

## Windows Setup UX

Setup should look like a premium Ghost FTP product surface, not a generic console bootstrapper. It uses:

- Ghost FTP icon/product identity;
- dark theme and accent tokens;
- clear wizard progress;
- language selector from the shared 29-language catalog;
- visible privacy/security benefits;
- explicit license acceptance;
- clear install/update/uninstall messaging.

The Setup window is resizable within sensible minimum dimensions. Busy installation stages block accidental close. Language changes safely rebuild reusable controls after the selection event unwinds.

Uninstall is part of the same maintenance Setup executable; UX/documentation must not imply a separate `uninstall.exe` exists.

## Error UX

User-facing errors should state what operation failed and include a useful underlying message without leaking credentials.

Examples:

- invalid host/port before network attempt;
- TLS refusal/certificate failure;
- unavailable saved initial path;
- remote permission failure;
- local filesystem error;
- cancelled connection/transfer.

The app should recover to a stable Offline/connected state after an operation failure rather than leaving controls in an ambiguous busy state.

## Local Demo regression UX gate

The built-in Demo profile is part of release UX validation, not just protocol testing. The **Local Demo regression UX gate** verifies that a complete local workflow can connect, navigate, list, transfer, rename, create/delete and disconnect without external network activity.

This catches regressions where a UI command still exists visually but its underlying session/transfer behavior is broken.

## Authentic screenshot gate

The canonical main screenshot is captured from the compiled Windows application at 1914 × 907. Release/capture workflows validate the image. Product documentation should update these captures after verified UI changes rather than manually editing screenshots or substituting conceptual art.

## Performance

UI handlers should remain short and asynchronous work should use existing task/queue paths. Avoid rebuilding entire file lists or performing blocking network operations on the UI thread.

The design should prioritize stable interaction under large listings and multiple concurrent transfers over decorative effects.

## Definition of done for a UI change

A UI change is complete when:

- it compiles on its target platform;
- core commands remain reachable at supported window sizes;
- text/fields do not overlap;
- target-sensitive actions are not duplicated ambiguously;
- keyboard/mouse input works;
- destructive actions remain explicit;
- localization fallback remains valid;
- security/privacy messaging remains accurate;
- Demo/UI smoke gates pass;
- authentic captures are refreshed for release-quality visual changes.
