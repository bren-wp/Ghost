# Ghost FTP Architecture

Ghost FTP **0.1.3 Beta** is one privacy-first FTP/FTPS desktop product with native Windows and Linux renderers, a shared platform-neutral protocol/transfer core, local-only persistence, bounded resource usage and release-time verification.

Ghost FTP is the product. **BRENDIGO LTD** is the developer, publisher and licensor.

## Release identity

The public release identity comes from root files:

```text
VERSION=0.1.3
RELEASE_CHANNEL=beta
```

`Directory.Build.props` synchronizes Version, AssemblyVersion, FileVersion and InformationalVersion. Windows application/Setup manifests use the matching four-part assembly identity.

## Project boundaries

### GhostFTP.Core

Platform-neutral `net10.0` library containing:

- FTP/FTPS protocol session;
- parser/input/path guards;
- transfer queue;
- transfer models;
- profile/settings persistence primitives;
- Linux saved-secret cryptography;
- Demo FTP session.

Core contains no WPF dependency and is shared by Windows and Linux.

### GhostFTP.Design

Shared product identity, localization, palette and design semantics. It multi-targets platform-neutral .NET plus the Windows target needed by WPF-specific helpers.

Important shared contracts include:

- `GhostProduct`;
- `GhostBrand`;
- `GhostLocalization`;
- `GhostReferencePalette`;
- `GhostReferenceText`;
- `GhostTransferText`.

### GhostFTP.App

Native Windows WPF renderer. It owns:

- workstation layout;
- Quick Connect;
- Local/Remote panes;
- transfer list and context actions;
- Site Manager/Settings/dialogs;
- Windows DPAPI saved-secret adapter;
- authentic screenshot capture path.

### Linux X11/XWayland renderer

`GhostFTP.Linux` is a native C# **Linux X11/XWayland renderer** using the system `libX11.so.6` ABI. It is not a browser wrapper and does not ship a second FTP implementation.

It uses the same Core services, settings/profile models, transfer queue, localization and canonical palette as Windows.

### GhostFTP.Setup

Native Windows per-user Setup application. The same maintenance executable is installed as `GhostFTP-Setup.exe` and registered for future uninstall/update operations. A separate uninstaller executable is intentionally not generated.

## Connection architecture

A user-selected connection becomes `FtpConnectionOptions`, which is validated again inside `FtpSession`. The control connection is established with the chosen mode:

- plain FTP;
- Explicit FTPS;
- Implicit FTPS.

No transport fallback silently changes an FTPS request into plain FTP.

Explicit FTPS requires successful `AUTH TLS`; encrypted sessions require `PBSZ 0` and `PROT P`.

## Data-transfer mode integrity

Before upload/download data paths, Ghost FTP requires FTP binary mode (`TYPE I`). The transfer fails if the server refuses the required mode.

Passive data connections prefer EPSV and can fall back to PASV. The data socket remains tied to the authenticated control host rather than treating an arbitrary PASV address as trusted routing input.

## Transfer queue architecture

`TransferQueueService` is shared by both renderers.

Properties:

- bounded channel capacity;
- configurable worker count clamped to a safe maximum;
- isolated transfer sessions when the active protocol session requires them;
- per-job cancellation token;
- bounded transient retries;
- progress, throughput and ETA state;
- UI synchronization-context dispatch when a renderer supplies one.

### 0.1.3 pause/resume model

Pause/resume gates **dispatch**, not the byte stream of transfers already running.

`PauseQueue()` creates an asynchronous resume gate. Workers that are about to start queued/retrying work await that gate. `ResumeQueue()` releases it. Running transfers are not interrupted by a queue pause.

This avoids claiming unsupported FTP resume semantics and avoids a polling/spin loop.

### Queue history

Completed, failed and cancelled history can be removed selectively. `ClearFinished()` remains the aggregate cleanup action.

## Persistence architecture

Installed mode uses the current user's local application-data directory. Portable mode uses a local `Data` directory beside the executable when the portable marker/name is active.

Settings and profiles use bounded JSON files with atomic replacement/backup behavior and best-effort private filesystem permissions.

Session-only profiles are explicitly excluded from persistent profile writes.

## Saved-secret architecture

### Windows

The WPF application uses native DPAPI (`CryptProtectData`/`CryptUnprotectData`) with current-user semantics. Sensitive intermediary buffers are zeroed where practical before release.

### Linux

Linux uses AES-256-GCM with a local user-private key file. Encryption/authentication data and file permission hardening remain local to that user profile.

## Local Demo regression architecture

The built-in Demo profile uses `DemoFtpSession`; it performs no external FTP operation. The **Local Demo regression architecture** allows deterministic testing of:

- connect/disconnect lifecycle;
- PWD/CWD/listing;
- upload/download;
- byte-for-byte round trip;
- rename/create/delete;
- recursive directory transfers;
- root-delete protection;
- keepalive;
- conflict behavior.

## Live real-server smoke architecture

The optional **Live real-server smoke architecture** is separate from Demo and is intentionally non-destructive. It performs connect/PWD/LIST/NOOP/disconnect against explicitly supplied test credentials. The password comes from protected CI secret storage and is redacted from test output.

See `docs/LIVE-SMOKE-TEST.md`.

## Windows/Linux UI parity

Windows and Linux share the same workflow hierarchy:

1. product identity/sidebar;
2. top menu/toolbar;
3. Connection Log + Quick Connect;
4. Local + Remote panes;
5. transfer queue;
6. status/diagnostics/settings/site management.

0.1.3 also aligns transfer management semantics: pause/resume dispatch, failed retry, cancellation and queue-history cleanup are driven by the same Core queue service.

## Windows Setup transaction architecture

Setup stages application and maintenance binaries before replacing an active installation. Candidate product/company/file-version identity is validated and downgrade protection runs before commit.

Existing application/maintenance files keep independent rollback copies until later stages succeed. A later failure attempts to restore prior files; first-install partial commits are removed where appropriate.

The 0.1.3 Setup UI surfaces these transaction boundaries but does not change the underlying privilege model: installation remains per-user/as-invoker.

## Privacy boundary

Shipping application code has no telemetry, analytics, advertising, hidden crash upload, cloud profile synchronization or product account requirement. Network traffic is user-directed FTP/FTPS traffic plus explicitly opened links.

Repository CI network activity is build/release infrastructure and is not application telemetry.

## Dependency boundary

Shipping projects have no third-party NuGet `PackageReference` dependencies. Source audit also rejects known mobile application targets and known telemetry SDK identifiers.

## Release verification

A public **GitHub Release** is produced only after the release workflow verifies the current version source. Required gates include Windows/Linux build, source audit, hardening audit, Core/Demo/queue tests, WPF/X11 runtime tests, authentic UI capture, packaging and checksum verification.

The release workflow attaches canonical Windows and Linux assets to the GitHub Release rather than treating an unverified local build as an official binary.

## Canonical screenshots

The README image is generated by the real compiled WPF application at the canonical capture size. Screenshot generation is a build path, not a hand-authored mockup. `assets/readme/ghostftp-client.png` and Site Manager capture are treated as product documentation artifacts.
