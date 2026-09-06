# Ghost FTP desktop UI parity contract

Ghost FTP **0.1.2 Beta** is one desktop product with native Windows and Linux renderers. This contract prevents platform-specific window-system code from drifting into separate products with different FTP behavior or information architecture.

## Authentic reference

The canonical repository image is the real compiled Windows client captured at **1914 × 907 logical pixels / 96 DPI**:

```text
assets/readme/ghostftp-client.png
```

`assets/readme/ghostftp-site-manager.png` is the canonical saved-site manager capture. These files are produced through the application's documentation-capture path; conceptual mockups do not replace them.

The fixed capture size is a regression reference, not a requirement for normal users. Runtime windows remain resizable.

## Shared workstation hierarchy

**Windows and Linux** must expose the same primary desktop workflow in this order:

1. Ghost FTP identity and saved-server navigation;
2. menu/primary connection-transfer actions;
3. Connection Log and Quick Connect;
4. Local and Remote file panes;
5. Transfers queue;
6. local status/connection state.

The visual language uses the shared `GhostReferencePalette`: dark navy surfaces, restrained borders, bright readable text, violet primary accents, green success and red destructive actions.

## Global vs contextual actions

The main toolbar is for actions whose target is obvious at application scope:

- Connect;
- Disconnect;
- Upload;
- Download;
- Refresh;
- Site Manager;
- Settings;
- Diagnostics when space permits.

Create folder, rename and delete are contextual file-pane operations. They belong in the Local and Remote pane toolbars and must not be duplicated globally in the Windows reference shell. This reduces clutter and prevents ambiguous target selection.

Linux should follow the same semantic rule as renderer cleanup proceeds; platform-native layout can hide optional commands when insufficient width exists rather than overlapping controls.

## Quick Connect

Quick Connect provides the same semantic fields on both platforms:

- Host;
- Port;
- Security mode;
- Username;
- Password;
- Connect / Disconnect;
- session-only **Keep in this tab** behavior.

Explicit FTPS is the recommended/default security choice. Plain FTP remains explicit and warns before connecting.

Credential values remain local. A session-only Quick Connect entry never stores its password.

## Saved servers

Both renderers consume the same `ServerProfile` model and `ProfileStore` behavior. Saved profiles expose equivalent fields and initial remote path semantics. The left navigation should make the active/saved site obvious and keep Site Manager reachable without requiring an account.

## File panes

Local and Remote panes use the same conceptual columns:

- name;
- type;
- size;
- modified time;
- remote permissions where available.

Expected operations:

- folder navigation/up/home;
- upload from Local;
- download from Remote;
- refresh;
- create folder;
- rename;
- delete;
- filter/search;
- double-click navigation and file transfer/open semantics appropriate to the side.

## Resizing contract

Windows 0.1.2 exposes drag resizing for:

- saved-server sidebar;
- Connection Log / Quick Connect height;
- Local / Remote horizontal split;
- Transfers queue height.

Double-click resets splitters to their reference defaults. Native minimize, maximize, restore and overall window resizing remain enabled.

The compact Windows shell hides the optional language/search overlay before it can collide with primary toolbar commands. Core functionality remains accessible through Settings and the Remote pane filter.

Linux uses native X11 resize events and a responsive renderer. Platform-native implementation details can differ, but shrinking the window must not change protocol semantics or hide the only path to a core operation.

## Transfer queue and cancellation

Both platforms share the same transfer queue and cancellation semantics from `GhostFTP.Core`:

- bounded concurrent jobs;
- progress/transferred bytes/speed/ETA where available;
- retry of failed jobs;
- selected-job cancellation;
- cancel all;
- clear finished;
- bounded automatic retries;
- per-transfer session isolation for real servers.

A renderer may draw controls differently, but it must not implement a second incompatible transfer engine.

## Connection Log and privacy copy

Connection diagnostics/logging remain local. Passwords are never intentionally logged. Both platforms show the no-account/local-data privacy principle and use localized copy from the shared design/localization layer where applicable.

## Localization parity

Windows, Linux and Setup use `GhostLocalization.SupportedLanguages`. English is the primary fallback and the catalog contains 29 selectable languages. No platform should add an online translation dependency.

Renderer-specific strings without a translation may fall back to English; they must not fail startup or trigger network access.

## Security parity

Feature parity does not permit weaker transport rules on one platform. Both use the same `FtpSession` for real FTP/FTPS work and therefore inherit:

- fail-closed security mode validation;
- strict `AUTH TLS` for explicit FTPS;
- normal TLS certificate/hostname validation;
- `PBSZ 0` / `PROT P` for encrypted data channels;
- binary transfer mode;
- passive-data authenticated-host protection;
- shared input/path guards;
- bounded traversal/reply handling.

Platform-specific credential stores differ by necessity: Windows uses DPAPI, Linux uses local AES-256-GCM key material.

## Setup parity boundary

Windows Setup is Windows-only. It should visually use the same product identity, dark premium theme and 29-language catalog, but it is not part of the Linux application UI. Linux packaging does not emulate Windows Setup.

## Capture and regression gate

The Windows release workflow launches the compiled application with the capture switch and verifies the canonical 1914 × 907 main-window capture plus Site Manager capture. The release fails if required images are missing or unexpectedly small.

UI work should be evaluated against:

- hierarchy and spacing;
- field clipping/overlap;
- contextual action duplication;
- compact-window behavior;
- readable local/remote lists;
- transfer queue usability;
- native resize behavior;
- localization rendering.

## Allowed platform differences

Differences are acceptable where they reflect WPF versus X11/XWayland primitives, native font metrics, system titlebar behavior or platform credential protection. They are not acceptable when they change the user-visible FTP/FTPS feature contract, privacy guarantees or security rules.
