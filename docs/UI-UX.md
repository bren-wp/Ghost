# Ghost FTP UI / UX Guidelines

This document defines the desktop interaction rules for Ghost FTP **0.1.3 Beta** and the Windows Setup experience. The goal is a clean, information-dense professional FTP workstation rendered in Ghost FTP's own modern visual language.

Ghost FTP is the product. **BRENDIGO LTD** is the developer/publisher shown on legal and publisher surfaces.

## Core UX principles

1. **The transfer target must be obvious.** Local and Remote operations belong with the pane they affect.
2. **Security state must be understandable.** Explicit FTPS is recommended; plaintext FTP warns before use.
3. **Credentials remain visually private.** Password values are masked and never echoed into the connection log.
4. **Desktop space is user-controlled.** Users can resize the main window and important workstation regions.
5. **Primary work remains visible.** Optional controls yield before they overlap connection or transfer actions.
6. **No account wall.** Users connect to their own FTP/FTPS server without creating a Ghost FTP account.
7. **No decorative fake functionality.** Canonical screenshots are generated from the compiled application.
8. **Queue state must be truthful.** “Pause queue” pauses dispatch of new work; it does not falsely claim to freeze an already-running FTP byte stream.

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

Context-sensitive New folder, Rename and Delete actions remain inside Local/Remote panes. Do not reintroduce ambiguous global duplicates just to fill toolbar space.

## Quick Connect

Normal workstation layout keeps these credential controls compact and aligned:

- Host;
- Port;
- Security;
- Username;
- Password.

Connect/Disconnect and session-only privacy controls use the secondary row so credential fields retain useful width.

Rules:

- Host receives the largest flexible width.
- Port stays compact.
- Security shows the selected mode without clipping normal labels.
- Username/Password remain usable at the supported minimum window width.
- **Keep in this tab** is session-only and must not imply persistent password storage.
- Plain FTP approval must be explicit for a real non-Demo server.

## Resizing model

Windows supports:

- native window resize/minimize/maximize/restore;
- draggable saved-server sidebar width;
- draggable Connection Log / Quick Connect split;
- draggable Local / Remote split;
- draggable Transfers queue height.

Splitters use restrained shared border tokens and correct resize cursors. Double-click restores a sensible reference split where implemented.

Local/Remote ratio, Transfers height and maximized state persist in local settings.

Linux reacts to native X11/XWayland window resizing and adapts toolbar/queue controls according to available width.

## Compact window behavior

The main desktop must remain usable at smaller supported sizes. Optional language/search or secondary queue controls hide/condense before primary connect/file-transfer actions collide.

Do not solve compact layout by horizontal overflow. Prefer:

- hiding optional duplication;
- ellipsizing secondary text;
- wrapping explanatory copy in dialogs;
- preserving Local/Remote and transfer actions.

## Local / Remote panes

Each pane shows title, current path, navigation, contextual actions, filter and file list.

Local actions:

- Upload;
- Refresh;
- New folder;
- Rename;
- Delete.

Remote actions:

- Download;
- Refresh;
- New folder;
- Rename;
- Delete.

Destructive actions use the danger token. Primary transfer actions use the accent token. Disabled commands must look disabled.

## File-list behavior

Lists prioritize Name, then Type, Size, Modified and Remote Permissions where available. Columns resize with the pane instead of forcing a permanent horizontal scrollbar.

Folders sort before files. Names use case-insensitive normal workstation ordering.

Double-click behavior must remain predictable: folders navigate; Local files open through OS behavior; Remote files queue a download where established by the renderer.

## Transfers queue

The queue communicates:

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

0.1.3 adds a richer transfer-management contract.

### Pause / resume

**Pause queue** gates new queued/retrying dispatch. Transfers already running continue. The UI should make this distinction clear rather than imply byte-stream suspension.

**Resume queue** releases the dispatch gate and allows waiting jobs to start.

### Retry

Windows exposes Retry selected plus Retry failed. Linux exposes retry-failed as a primary queue-management action at supported widths. Automatic transient retry remains separately controlled by Settings.

### Cancellation

Both platforms preserve selected/all cancellation. Linux transfer rows are selectable so selected cancellation targets the actual highlighted job rather than an implicit last-item fallback.

### Cleanup

Windows exposes selective Clear completed / Clear failed / Clear cancelled in addition to Clear finished. Linux exposes the most important cleanup actions in the native transfer header and shares the same underlying selective Core operations.

### Queue summary

Where room permits, show useful operational state such as running, retrying, queued, failed, cancelled and completed counts. Windows also surfaces aggregate active throughput.

Background queue work must never block the UI thread.

## Connection Log

The log is a local operational surface, not a credential/debug dump. It may show:

- startup privacy status;
- profile load count;
- connect/disconnect state;
- security mode/TLS state;
- directory-list completion;
- transfer queue transitions;
- operation errors.

Passwords must never be logged. Errors should be actionable without exposing secrets.

## Visual language

The canonical palette is `GhostReferencePalette`. Windows WPF, Windows Setup and Linux should remain aligned with its dark navy surfaces, restrained blue borders and violet accent.

Visual hierarchy comes from:

- surface depth;
- compact radii;
- thin borders;
- typography;
- selected/hover/focus states;
- consistent spacing.

Avoid oversized dashboard cards, unnecessary gradients and decorative animations that reduce workstation clarity.

## Density

Ghost FTP is an information-dense desktop utility. Controls should not consume more vertical space than needed.

0.1.3 intentionally tightens:

- button height/padding;
- card padding/radius;
- list row padding;
- table headers;
- badges.

Density must not come at the expense of readable focus states or input hit targets.

## Typography

Windows uses Segoe UI Variable / Segoe UI fallback. Linux uses its native locale-aware X11 font-set path. External font downloads are not permitted at runtime.

## Accessibility and keyboard behavior

Editable controls remain genuinely focusable/editable. Buttons display a visible accent focus border. Tab order follows the visible Quick Connect flow. Destructive confirmation remains visually distinct.

Keyboard shortcuts such as F2/F5/Delete must continue to map to the current selected context rather than an unrelated pane.

## Localization layout

English is the primary/default/fallback language and the product exposes 29 local selectable languages.

Longer translated labels must not overlap controls. A missing localized technical string falls back to English; no online localization service is permitted.

`GhostTransferText` provides local shared queue-management copy introduced in 0.1.3, with explicit Croatian overrides and English fallback for the rest of the catalog where a new native translation is not yet present.

## Windows / Linux parity

**Windows / Linux parity** means the same product workflow, security, privacy and transfer semantics—not pixel-identical use of incompatible native primitives.

Both expose:

- saved sites and Quick Connect;
- FTP / Explicit FTPS / Implicit FTPS;
- Local/Remote browsing and file operations;
- transfer queue, cancellation, retry and dispatch pause/resume;
- local diagnostics/logging;
- keepalive;
- 29-language local catalog;
- local-only profile settings;
- Demo mode.

See `docs/UI-PARITY.md` for the detailed parity contract.

## Windows Setup UX

Setup must look like a premium Ghost FTP surface, not a generic bootstrapper.

0.1.3 Setup includes:

- Ghost FTP icon/product identity;
- canonical dark palette/accent;
- clear numbered step progress;
- shared 29-language selector;
- local-only/privacy messaging;
- explicit license acceptance;
- concise install/update/uninstall summary;
- transactional validation/rollback messaging;
- resizable window with sensible minimum dimensions;
- clear completion/launch state.

The same installed `GhostFTP-Setup.exe` handles future maintenance/uninstall. UX must not imply a separate `uninstall.exe` exists.

Busy installation stages block accidental close.

## Error UX

User-facing errors state the operation and useful underlying reason without leaking credentials.

Examples:

- invalid host/port before network attempt;
- TLS refusal/certificate failure;
- unavailable saved initial path;
- remote permission failure;
- local filesystem error;
- cancelled connection/transfer;
- package validation or Setup rollback failure.

The application should recover to a stable Offline/connected state after failures rather than leave controls in an ambiguous busy state.

## Local Demo regression UX gate

The built-in Demo profile is part of release UX validation. The **Local Demo regression UX gate** verifies a complete local workflow can connect, navigate, list, transfer, rename, create/delete and disconnect without external network activity.

This catches regressions where an action remains visible while its underlying session/transfer behavior no longer works.

## Transfer regression UX gate

Because 0.1.3 exposes pause/resume, CI must prove that:

- paused queued work does not start early;
- Resume releases the work;
- cancellation while paused remains responsive;
- selective queue cleanup does not remove unrelated history.

The UI should not ship a transfer-management command that is only decorative.

## Authentic screenshot gate

The canonical main screenshot is captured from the compiled Windows application at **1914 × 907**. Product documentation must refresh authentic captures after verified release-quality visual changes rather than manually editing screenshots or substituting conceptual art.

## Performance

UI handlers stay short. Network/transfer work uses async/core queue paths. Paused workers wait asynchronously rather than polling. Avoid full-list rebuilds or blocking network calls on the UI thread.

Stable interaction under large listings and concurrent transfers takes priority over visual effects.

## Definition of done for a UI change

A UI change is complete when:

- target builds successfully;
- core commands remain reachable at supported sizes;
- text/fields do not overlap;
- target-sensitive file actions are not duplicated ambiguously;
- keyboard/mouse input works;
- selection maps to the intended operation;
- destructive actions remain explicit;
- localization fallback remains valid;
- security/privacy messaging stays accurate;
- Demo/queue/UI smoke gates pass;
- authentic captures are refreshed for release-quality visual changes.
