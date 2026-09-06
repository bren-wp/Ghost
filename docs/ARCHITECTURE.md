# Ghost FTP Architecture

Ghost FTP **0.1.2 Beta** is a privacy-first FTP/FTPS desktop product with native Windows and Linux renderers, a shared platform-neutral protocol/transfer core, local-only persistence, bounded resource usage and release-time verification.

Ghost FTP is the product. **BRENDIGO LTD** is the developer, publisher and licensor.

## Release identity

The public version line is defined by root metadata:

```text
VERSION=0.1.2
RELEASE_CHANNEL=beta
```

`Directory.Build.props` synchronizes product, company, assembly, file and informational versions. Windows app and Setup manifests use the corresponding `0.1.2.0` assembly identity.

## Project topology

### `src/GhostFTP.Core`

Platform-neutral .NET 10 code shared by Windows and Linux:

- FTP/FTPS control-session lifecycle;
- TLS transport negotiation;
- FTP command/reply parsing;
- data-channel creation;
- file/directory upload and download;
- recursive traversal limits;
- shared input/path guards;
- transfer queue and retry/cancellation logic;
- profile/settings models and local stores;
- deterministic local Demo session.

The Core layer has no WPF/X11 dependency and no third-party NuGet `PackageReference`.

### `src/GhostFTP.Design`

Shared product and presentation contract:

- product/publisher identity;
- dark premium reference palette;
- common dimensions and visual tokens;
- 29-language local catalog with English fallback;
- shared reference-shell copy.

Windows and Linux intentionally consume this layer so product identity, colors, language selection and user-facing semantics do not drift.

### `src/GhostFTP.App`

Native Windows WPF renderer:

- resizable reference workstation shell;
- saved-server sidebar;
- menu/primary toolbar;
- Connection Log and Quick Connect;
- Local/Remote panes and context actions;
- transfer queue;
- Site Manager, Settings and diagnostics;
- DPAPI saved-password protector;
- authentic production UI capture path.

0.1.2 removes duplicated global create/rename/delete controls and keeps those operations in the Local/Remote pane toolbars.

### `src/GhostFTP.Linux`

The **Linux X11/XWayland renderer** is a real native desktop implementation rather than a browser wrapper or Windows compatibility package. It uses direct audited X11 ABI interop while sharing `GhostFTP.Core` and `GhostFTP.Design`.

Linux owns platform-specific input/window/rendering code but shares FTP/FTPS, transfer, profile, privacy, localization and product semantics with Windows.

### `src/GhostFTP.Setup`

Self-contained native Windows Setup/maintenance executable:

- install/update/uninstall UI;
- local language selector using the same 29-language catalog;
- per-user installation;
- staged candidate validation;
- downgrade protection;
- transactional application/maintenance-Setup rollback;
- registration of the maintenance Setup as the uninstall command.

There is intentionally no separate `uninstall.exe`.

## Runtime trust boundaries

### User input

Host, port, username, password, paths and names are treated as untrusted. Windows 0.1.2 validates connection input before DNS/option construction and Core validates again at the protocol boundary.

### FTP server input

Greetings, replies, feature lists, directory listings and passive-mode addresses are untrusted. Reply size/line counts and traversal budgets are bounded. Passive data connections remain associated with the authenticated control host.

### Filesystem input

Local paths are canonicalized. Recursive/destructive operations validate path relationships instead of trusting textual prefixes.

### Stored local data

Settings/profile files are size-bounded and written atomically where applicable. Session-only profiles are excluded from persistence. Saved passwords are opt-in and encrypted/protected per platform.

## FTPS architecture

Explicit FTPS is fail-closed:

1. connect TCP;
2. read bounded FTP greeting;
3. issue `AUTH TLS` and require 2xx;
4. negotiate TLS with normal certificate-chain/hostname validation;
5. issue and require `PBSZ 0`;
6. issue and require `PROT P`;
7. authenticate;
8. continue with encrypted control and data channels.

Implicit FTPS negotiates TLS before the normal greeting/authentication sequence.

Plain FTP exists for compatibility but is an explicit mode and is warned as unencrypted.

## Data-transfer mode integrity

Ghost FTP requires binary mode with `TYPE I` before receive/send data paths. This is a correctness and security boundary: arbitrary file bytes must not be transformed by text-mode conversions.

Interactive control activity and background transfers are isolated. Real-server background transfers normally create dedicated sessions from the validated active connection options; the Demo session can be safely shared because it is deterministic/local.

The queue bounds concurrent transfers and automatic retries, propagates cancellation and tracks job progress without sending analytics.

## Connection lifecycle

Windows 0.1.2 clears authoritative active-session routing state before waiting for QUIT/disposal. This prevents keepalive or transfer callbacks from treating a disconnecting transport as current.

Linux lifecycle code similarly guards candidate-session replacement, active-session identity and keepalive ownership.

## Workspace architecture

The approved workstation has the same semantic order on both desktop platforms:

1. saved-server navigation;
2. menu and primary connection/transfer actions;
3. Connection Log + Quick Connect;
4. Local + Remote file panes;
5. Transfers queue;
6. local status.

Windows uses WPF `GridSplitter` controls for sidebar, connection area, Local/Remote pane and transfer-queue resizing. Window resize/minimize/maximize/restore remains native operating-system behavior.

The documentation capture fixes the reference Windows render at **1914 × 907** for repeatable visual comparison. Normal users are not locked to that size.

## Persistence architecture

Installed mode stores local data in the user-local Ghost FTP data directory. Portable mode stores under `Data` beside the executable.

`AppSettingsStore` and `ProfileStore` serialize only local settings/profiles. Session-only entries are filtered before persistence.

Windows saved passwords use current-user DPAPI. Linux saved passwords use AES-256-GCM with local per-user key material and private permissions.

## Local Demo regression architecture

`DemoFtpSession` implements the same `IFtpSession` contract as a real FTP session but performs no external network operation. The Demo self-test exercises connection, PWD/CWD, listing, keepalive, file and directory transfers, rename, create/delete, recursive round-trip behavior, conflict protection, disconnect reset and post-disconnect rejection.

CI runs the complete local Demo regression on Windows and Linux, allowing destructive workflow testing without risking a real server.

## Live real-server smoke architecture

The optional live smoke harness is separate from deterministic CI. It reads credentials from environment variables/GitHub secrets, redacts sensitive values and performs only non-destructive operations: connect, PWD/list, keepalive and disconnect.

It deliberately contains no upload/rename/create/delete calls. See `docs/LIVE-SMOKE-TEST.md`.

## Dependency architecture

Shipping projects have zero third-party NuGet `PackageReference` entries. Native Windows functionality comes from WPF/Windows APIs; Linux uses the system `libX11.so.6` ABI through audited interop.

Source audit rejects known telemetry/tracking SDK references, private signing files, unsupported mobile target frameworks and known Android/iOS/mobile source directories.

## Platform scope

Only Windows and Linux desktop applications ship from this line. Android, iOS, MacCatalyst and web/browser client targets are intentionally outside the active product scope.

## Release architecture

CI validates every proposed change. Release publication is a separate gated workflow tied to version/channel metadata and the current release marker.

For 0.1.2 Beta the expected tag is `v0.1.2-beta`. The release workflow builds/tests/audits source, captures authentic UI, builds Windows x64/ARM64 Setup and portable packages, builds Linux x64/ARM64 packages, verifies version/hashes/signing policy, and then creates/synchronizes the public **GitHub Release**.

A release is not considered complete merely because source was merged; canonical downloadable artifacts must be attached to the matching GitHub Release.
