# Ghost FTP desktop UI parity contract

Ghost FTP **0.1.1 Beta** is one desktop product with native Windows and Linux renderers. This document defines the visual/workflow contract that prevents the two implementations from drifting into separate products.

## Authentic reference

The canonical repository image is the real compiled Windows client at **1914 × 907 logical pixels / 96 DPI**:

```text
assets/readme/ghostftp-client.png
```

README must show that authentic capture first. Decorative mockups are not accepted as the primary product image.

The Windows capture path is `--capture-ui <directory>`. It uses the local-only Demo profile and renders the production `MainWindow` visual tree rather than a duplicate/mock shell.

## Shared design authority

The authoritative cross-platform palette/geometry source is:

```text
src/GhostFTP.Design/GhostReferencePalette.cs
```

The normal desktop reference uses:

- left rail: **292 px**;
- top menu: 38 px;
- global toolbar: 70 px;
- normal outer gap: 10 px;
- dark first-run appearance;
- shared Ghost accent/border/surface/text tokens.

Both renderers consume shared `GhostFTP.Design` product/localization/reference definitions.

## Required information hierarchy

At normal desktop width both Windows and Linux must preserve this order:

```text
Product / saved sites / privacy rail
File / View / Sites / Transfers / Tools / Help
Global action toolbar + Remote search
Connection Log + Quick Connect
Local + Remote file panes
Transfers
Connection / privacy status
```

A renderer may compact or hide secondary columns at narrow widths, but it must not replace the desktop workstation with an unrelated mobile/web layout.

## Core control parity

Both desktop clients must expose the same major workflows:

- saved Site Manager profiles;
- host, port, username and password entry;
- FTP / Explicit FTPS / Implicit FTPS selection;
- explicit plain-FTP warning;
- local path navigation/filtering;
- remote path navigation/filtering;
- create folder / rename / delete;
- upload / download;
- transfer queue and cancellation;
- local Connection Log;
- Settings and language selection;
- Connection Diagnostics;
- configurable server-only keepalive;
- session-only **Keep in this tab** privacy behavior.

Windows can additionally expose OS-specific integrations such as Explorer opening, drag/drop and DWM/Mica. Linux can expose Linux-native equivalents where practical. Those are platform integrations, not permission for core FTP behavior to diverge.

## Native renderer rule

Windows uses WPF. Linux uses direct X11 client integration and runs under XWayland on compatible Wayland desktops.

The goal is the same Ghost FTP visual system and workstation behavior. We do **not** claim mathematical pixel identity between different OS font engines/window managers. Native chrome and glyph rasterization can differ. Product colors, hierarchy, action placement and safety semantics should not.

## Responsive behavior

The normal workstation is desktop-first. When width is constrained:

- Quick Connect fields reflow/compact;
- secondary table columns may be hidden before primary file data;
- Local/Remote panes remain usable;
- transfer controls remain reachable;
- modal dialogs stay within the usable desktop bounds;
- destructive actions must remain explicit.

The canonical 1914×907 viewport is a visual comparison target, not a fixed runtime window size.

## Premium-quality rules

Ghost FTP UI should be dense, calm and operational rather than decorative:

- browsing/transfers get more space than branding;
- controls use consistent spacing and hit targets;
- errors are local and actionable;
- no telemetry/marketing banner is injected into the file workspace;
- passwords are never drawn as plaintext;
- destructive operations require the appropriate confirmation/scope;
- Demo status is clearly different from a real server connection;
- TLS/plain status is visible rather than implied.

## Screenshot freshness gate

`Refresh authentic UI screenshots` rebuilds the real WPF client and updates the repository PNGs on `main`. The normal CI also captures the production UI as a verification artifact.

A README image is stale if it no longer comes from the current compiled UI. Any stale decorative image should be removed rather than presented as the current product.

## Security parity

Visual parity also includes safety behavior:

- no silent downgrade from FTPS to FTP;
- same plain-FTP warning expectation;
- same session-only Quick Connect persistence boundary;
- same stale-session state after keepalive failure;
- same shared FTP/FTPS transfer engine;
- no renderer-specific telemetry path.

0.1.1 additionally requires the same complete local Demo workflow regression suite on Windows and Linux so lifecycle, conflict-protection and post-disconnect behavior cannot drift silently between the native renderers.

See `SECURITY.md`, `PRIVACY.md` and `docs/PLATFORM-SUPPORT.md`.
