# Ghost FTP Architecture

Ghost FTP **0.1.4 Beta** is one privacy-first FTP/FTPS desktop product with native Windows and Linux renderers, a shared platform-neutral protocol/transfer core, local-only persistence, bounded resource usage and release-time verification.

Ghost FTP is the product. **BRENDIGO LTD** is the developer, publisher and licensor.

## Release identity

The public release identity comes from root files:

```text
VERSION=0.1.4
RELEASE_CHANNEL=beta
```

`Directory.Build.props` synchronizes Version, AssemblyVersion, FileVersion and InformationalVersion. Windows application/Setup manifests use the matching four-part assembly identity.

## Project boundaries

### GhostFTP.Core

Platform-neutral `net10.0` library containing FTP/FTPS protocol sessions, parser/input/path guards, transfer queue, transfer models, profile/settings persistence primitives, Linux saved-secret cryptography and the Demo FTP session. Core contains no WPF dependency and is shared by Windows and Linux.

### GhostFTP.Design

Shared product identity, localization, palette and design semantics. Important contracts include `GhostProduct`, `GhostBrand`, `GhostLocalization`, `GhostReferencePalette`, `GhostReferenceText` and `GhostTransferText`.

### GhostFTP.App

Native Windows WPF renderer. It owns workstation layout, Quick Connect, Local/Remote panes, transfer actions, Site Manager/Settings, the Windows DPAPI adapter and the authentic screenshot capture path.

### Linux X11/XWayland renderer

`GhostFTP.Linux` is a native C# **Linux X11/XWayland renderer** using the system `libX11.so.6` ABI. It is not a browser wrapper and does not ship a second FTP implementation. It shares Core services, settings/profile models, transfer queue, localization and palette with Windows.

### GhostFTP.Setup

Native Windows per-user Setup application. The same maintenance executable is installed as `GhostFTP-Setup.exe` and registered for update/uninstall operations. A separate uninstaller executable is intentionally not generated.

### GhostFTP.HardeningSelfTest

A package-free, cross-platform deterministic regression executable. It owns an in-process loopback fake FTP server and validates real Core protocol behavior without an Internet dependency. It is compiled and executed by Windows/Linux CI and release gates.

## Connection architecture

A user-selected connection becomes `FtpConnectionOptions`, which is validated again inside `FtpSession`. The chosen control mode is plain FTP, Explicit FTPS or Implicit FTPS. No transport fallback silently changes an FTPS request into plain FTP.

Explicit FTPS requires successful `AUTH TLS`; encrypted sessions require `PBSZ 0` and `PROT P`.

### Control reply state machine

0.1.4 accepts a bounded preliminary greeting sequence before requiring a final positive-completion greeting. This permits valid servers that send `120` followed by `220` but caps the sequence so an endpoint cannot hold the client in an unlimited greeting loop.

Reply parsing enforces numeric `100..599` codes, standard space/hyphen framing, per-line size, multiline line count and total multiline character limits. Timeout and cancellation still wrap every control read/write.

## Data-transfer mode integrity

Before upload/download data paths, Ghost FTP requires FTP binary mode (`TYPE I`). The transfer fails if the server refuses the required mode.

Passive data connections prefer EPSV and can fall back to PASV. EPSV delimiter framing and port range are validated. PASV consumes exactly the six tuple bytes and derives the port from `p1,p2`; unrelated trailing digits are ignored. The data socket remains tied to the authenticated control host rather than treating an arbitrary PASV address as trusted routing input.

## Transfer queue architecture

`TransferQueueService` is shared by both renderers. It provides bounded channel capacity, configurable/clamped worker count, isolated transfer sessions, per-job cancellation, bounded transient retries, progress/throughput/ETA state and UI synchronization-context dispatch when supplied.

### Pause/resume model

Pause/resume gates **dispatch**, not the byte stream of transfers already running. `PauseQueue()` creates an asynchronous resume gate. Workers about to start queued/retrying work await that gate. `ResumeQueue()` releases it. Running transfers are not interrupted by a queue pause.

### Coordinated shutdown

0.1.4 adds a single-owner asynchronous disposal contract. The first `DisposeAsync()` caller completes the channel, releases paused dispatch waiters, cancels outstanding work, awaits workers and disposes cancellation resources. Concurrent disposal callers await the same completion task. New enqueue attempts fail once shutdown begins.

This prevents cancellation-token and worker races during application close.

### Queue history

Completed, failed and cancelled history can be removed selectively. `ClearFinished()` remains the aggregate cleanup action.

## FTP session lifecycle architecture

`FtpSession` serializes protocol operations through its gate. 0.1.4 coordinates disposal with an atomic state plus a shared completion task. Once shutdown begins, new operations are rejected; existing serialized work can unwind; transport cleanup runs once; later disposal callers wait for the same result. The gate itself is not disposed while another caller might still be waiting on it.

## Persistence architecture

Installed mode uses the current user's local application-data directory. Portable mode uses a local `Data` directory beside the executable when the portable marker/name is active.

Settings and profiles use bounded JSON files with atomic replacement/backup behavior and best-effort private filesystem permissions. Session-only profiles are explicitly excluded from persistent profile writes.

## Saved-secret architecture

### Windows

The WPF application uses native DPAPI (`CryptProtectData`/`CryptUnprotectData`) with current-user semantics. Sensitive intermediary buffers are zeroed where practical before release.

### Linux

Linux uses AES-256-GCM with a local user-private key file. Encryption/authentication data and file permission hardening remain local to that user profile.

## Local Demo regression architecture

The built-in Demo profile uses `DemoFtpSession`; it performs no external FTP operation. The **Local Demo regression architecture** deterministically tests connection lifecycle, PWD/CWD/listing, upload/download, byte-for-byte round trips, rename/create/delete, recursive transfers, root-delete protection, keepalive and conflict behavior.

## Local protocol hardening architecture

`GhostFTP.HardeningSelfTest` creates only loopback listeners. Its protocol scenario exercises `120 -> 220`, USER/PASS, PWD, TYPE I, EPSV rejection/fallback, strict PASV, LIST over a real passive data socket and QUIT. Additional cases cover malformed reply framing and concurrent session/queue disposal.

The fake PASV response includes valid tuple data followed by extra numbers. This is a deliberate regression trap against permissive “extract all digits” parsing.

## Live real-server smoke architecture

The optional **Live real-server smoke architecture** is separate from Demo/hardening tests and is intentionally non-destructive. It performs connect/PWD/LIST/NOOP/disconnect against explicitly supplied test credentials. The password comes from protected CI secret storage and is redacted from test output.

See `docs/LIVE-SMOKE-TEST.md`.

## Windows/Linux UI parity

Windows and Linux share the workflow hierarchy: product identity/sidebar, top menu/toolbar, Connection Log + Quick Connect, Local + Remote panes, transfer queue, and status/diagnostics/settings/site management. Transfer pause/resume, retry, cancellation and history cleanup are driven by the shared Core queue service.

## Windows Setup transaction architecture

Setup stages application and maintenance binaries before replacing an active installation. Candidate product/company/file-version identity is validated and downgrade protection runs before commit.

Existing application/maintenance files keep independent rollback copies until later stages succeed. A later failure attempts to restore prior files; first-install partial commits are removed where appropriate. Installation remains per-user/as-invoker.

## Privacy boundary

Shipping application code has no telemetry, analytics, advertising, hidden crash upload, cloud profile synchronization or product account requirement. Network traffic is user-directed FTP/FTPS traffic plus explicitly opened links. Repository CI network activity is build/release infrastructure and is not application telemetry.

## Dependency boundary

Shipping and regression-test projects have no third-party NuGet `PackageReference` dependencies. Source audit also rejects known mobile application targets and known telemetry SDK identifiers.

## Release verification

A public **GitHub Release** is produced only after the release workflow verifies the current version source. Required gates include Windows/Linux build, source audit, hardening audit, Core/Demo/queue tests, the protocol/shutdown hardening test, WPF/X11 runtime tests, authentic UI capture, packaging and checksum verification.

The release workflow attaches canonical Windows and Linux assets to the GitHub Release rather than treating an unverified local build as an official binary.

## Canonical screenshots

The README image is generated by the real compiled WPF application at the canonical capture size. Screenshot generation is a build path, not a hand-authored mockup. `assets/readme/ghostftp-client.png` and the Site Manager capture are product documentation artifacts.
