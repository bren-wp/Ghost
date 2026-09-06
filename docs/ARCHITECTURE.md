# Ghost FTP Architecture

Ghost FTP **0.1.5 Beta** is one privacy-first FTP/FTPS desktop product with native Windows and Linux renderers, a shared platform-neutral protocol/transfer core, local-only persistence, bounded resource usage and release-time verification.

Ghost FTP is the product. **BRENDIGO LTD** is the developer, publisher and licensor.

## Release identity

The public release identity comes from root files:

```text
VERSION=0.1.5
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

Package-free, cross-platform deterministic regression executable. It owns in-process loopback FTP servers and validates real Core protocol behavior without an Internet dependency. Windows/Linux CI and release gates execute it.

## Connection architecture

A user-selected connection becomes `FtpConnectionOptions`, which is validated again inside `FtpSession`. The control mode is plain FTP, Explicit FTPS or Implicit FTPS. No transport fallback silently changes an FTPS request into plain FTP.

Explicit FTPS requires successful `AUTH TLS`; encrypted sessions require `PBSZ 0` and `PROT P`.

### Control reply state machine

The bounded preliminary-greeting support introduced in 0.1.4 remains active: valid `120 -> 220` sequences interoperate, while repeated preliminary replies are capped. Reply parsing enforces numeric `100..599` codes, standard space/hyphen framing, per-line size, multiline line count and total multiline character limits.

## Data-transfer mode integrity

Before upload/download data paths, Ghost FTP requires FTP binary mode (`TYPE I`). Passive connections prefer EPSV and can fall back to PASV. EPSV delimiter framing and port range are validated. PASV consumes exactly six tuple bytes and derives the port from `p1,p2`; the data socket remains tied to the authenticated control host.

0.1.5 adds deterministic coverage for alternate valid EPSV delimiters and malformed PASV tuples that must fail before socket connection.

## Directory-listing parser architecture

LIST/MLSD payloads are server-controlled and therefore bounded before expensive parsing work.

0.1.5 adds:

- a 64 KiB per-line ceiling;
- a bounded MLSD fact count per entry;
- non-backtracking Unix/Windows LIST regexes;
- incremental line enumeration through `StringReader` instead of a second full split/copy;
- symlink metadata stripping before safe remote-name validation.

The total listing data stream remains bounded independently. Safe symlink names are retained, but symlink targets are not treated as recursively traversable directories.

## Transfer queue architecture

`TransferQueueService` is shared by both renderers. It provides bounded channel capacity, configurable/clamped workers, isolated transfer sessions, per-job cancellation, bounded transient retries, progress/throughput/ETA state and synchronization-context dispatch when supplied.

### Pause/resume model

Pause/resume gates **dispatch**, not a byte stream already in progress. `PauseQueue()` creates an asynchronous resume gate. Workers about to start queued/retrying work await that gate. `ResumeQueue()` releases it. Running transfers continue.

### Progress-delivery model

0.1.5 removes the redundant `Progress<T>` ThreadPool hop from Core transfer progress. Transfer work reports through an inline progress adapter; renderer marshaling remains the one deliberate UI boundary. Active progress notifications are throttled to a bounded UI cadence while final terminal state is immediate.

### Coordinated shutdown

The single-owner disposal contract remains: complete dispatch, release paused waiters, cancel outstanding work, await workers, then dispose cancellation resources. Concurrent disposal callers await the same completion task and new enqueue attempts fail once shutdown begins.

### Queue history

Completed, failed and cancelled history can be removed selectively. `ClearFinished()` remains the aggregate cleanup action.

## Transfer data-buffer architecture

FTP data send/receive paths use pooled 128 KiB byte buffers to avoid repeated large managed allocations. Each rented buffer is cleared before being returned to `ArrayPool<byte>` because it may contain private transferred data.

## FTP session lifecycle architecture

`FtpSession` serializes protocol operations through its gate. Disposal uses an atomic state plus shared completion task. New operations are rejected once shutdown begins, transport cleanup runs once and concurrent disposers synchronize on the same completion path.

## Persistence architecture

Installed mode uses the current user's local application-data directory. Portable mode uses a local `Data` directory beside the executable when the portable marker/name is active.

Settings and profiles use bounded JSON files with atomic replacement/backup behavior and best-effort private filesystem permissions. Session-only profiles are explicitly excluded from persistent profile writes.

0.1.5 expands persisted workstation state to include sidebar width and Connection Log / Quick Connect height. All persisted dimensions, retry values and concurrency are normalized before use. Corrupted primary settings can recover from the bounded local backup.

## Saved-secret architecture

### Windows

The WPF application uses native DPAPI (`CryptProtectData`/`CryptUnprotectData`) with current-user semantics. Sensitive intermediary buffers are zeroed where practical before release.

### Linux

Linux uses AES-256-GCM with a local user-private key file and best-effort private permissions.

## Completion refresh coalescing

Windows transfer completion no longer triggers one immediate Local/Remote refresh for every completed job. A short cancellation-based debounce coalesces bursts into a single refresh cycle, reducing repeated FTP LIST commands and UI churn after batch transfers.

## Local Demo regression architecture

The built-in Demo profile uses `DemoFtpSession`; it performs no external FTP operation. The **Local Demo regression architecture** deterministically tests connection lifecycle, PWD/CWD/listing, upload/download, byte-for-byte round trips, rename/create/delete, recursive transfers, root-delete protection, keepalive and conflict behavior.

## Local protocol hardening architecture

`GhostFTP.HardeningSelfTest` creates only loopback listeners. Coverage now includes concurrent session/queue disposal, malformed reply framing, bounded preliminary greetings, strict EPSV/PASV, real passive LIST flow, pathological LIST/MLSD parser input, safe symlink parsing and settings backup/normalization behavior.

## Live real-server smoke architecture

The optional **Live real-server smoke architecture** is separate from Demo/hardening tests and intentionally non-destructive. It performs connect/PWD/LIST/NOOP/disconnect against explicitly supplied test credentials. See `docs/LIVE-SMOKE-TEST.md`.

## Windows/Linux UI parity

Windows and Linux share product identity/sidebar, top menu/toolbar, Connection Log + Quick Connect, Local + Remote panes, transfer queue, status/diagnostics/settings/site management and the same Core queue/protocol semantics.

## Windows Setup transaction architecture

Setup stages application and maintenance binaries before replacing an active installation. Candidate identity and downgrade checks run before commit. Existing application/maintenance files keep rollback copies until later stages succeed; installation remains per-user/as-invoker.

## Privacy boundary

Shipping application code has no telemetry, analytics, advertising, hidden crash upload, cloud profile synchronization or product account requirement. Network traffic is user-directed FTP/FTPS traffic plus explicitly opened links.

## Dependency boundary

Shipping and regression-test projects have no third-party NuGet `PackageReference` dependencies. Audits reject known mobile targets, telemetry SDK identifiers and private signing material.

## Release verification

A public **GitHub Release** is produced only after exact-version source passes Windows/Linux build, source audit, hardening audit, Core/Demo/queue tests, protocol/parser/settings hardening tests, WPF/X11 runtime tests, authentic UI capture, packaging and checksum/runtime verification.

## Canonical screenshots

The README image is generated by the real compiled WPF application at the canonical capture size. Screenshot generation is a build path, not a hand-authored mockup. `assets/readme/ghostftp-client.png` and the Site Manager capture are product documentation artifacts.
