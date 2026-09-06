# Ghost FTP Architecture

Ghost FTP **0.1.6 Beta** is one privacy-first FTP/FTPS desktop product with native Windows and Linux renderers, a shared platform-neutral protocol/transfer core, local-only persistence, bounded resource usage and release-time verification.

Ghost FTP is the product. **BRENDIGO LTD** is the developer, publisher and licensor.

## Release identity

The public release identity comes from root files:

```text
VERSION=0.1.6
RELEASE_CHANNEL=beta
```

`Directory.Build.props` synchronizes Version, AssemblyVersion, FileVersion and InformationalVersion. Windows application/Setup manifests use the matching `0.1.6.0` assembly identity.

## Project boundaries

### GhostFTP.Core

Platform-neutral `net10.0` library containing FTP/FTPS protocol sessions, parser/input/path guards, safe-download-resume logic, transfer queue, transfer models, profile/settings persistence primitives, Linux saved-secret cryptography and the Demo FTP session. Core contains no WPF dependency and is shared by Windows and Linux.

### GhostFTP.Design

Shared product identity, localization, palette and design semantics. Important contracts include `GhostProduct`, `GhostBrand`, `GhostLocalization`, `GhostReferencePalette`, `GhostReferenceText` and `GhostTransferText`.

### GhostFTP.App

Native Windows WPF renderer. It owns workstation layout, Quick Connect, Local/Remote panes, transfer actions, Site Manager/Settings, the Windows DPAPI adapter and the authentic screenshot capture path.

### GhostFTP.Linux

Native C# X11/XWayland renderer using the system `libX11.so.6` ABI. It is not a browser wrapper and does not ship a second FTP implementation. It shares Core services, settings/profile models, transfer queue, localization and palette with Windows.

### GhostFTP.Setup

Native Windows per-user Setup application. The same maintenance executable is installed as `GhostFTP-Setup.exe` and registered for update/uninstall operations. A separate uninstaller executable is intentionally not generated.

### GhostFTP.HardeningSelfTest

Package-free cross-platform deterministic regression executable. It owns in-process loopback FTP servers and validates Core protocol/parser/lifecycle behavior without an Internet dependency.

### GhostFTP.ResumeSelfTest

Package-free cross-platform deterministic regression executable dedicated to safe download resume. Its loopback FTP server verifies exact REST-offset resume, stale remote-identity restart and in-flight remote-revision rejection independently of the general hardening suite.

## Connection architecture

A user-selected connection becomes `FtpConnectionOptions`, which is validated again inside `FtpSession`. The control mode is plain FTP, Explicit FTPS or Implicit FTPS. No transport fallback silently changes an FTPS request into plain FTP.

Explicit FTPS requires successful `AUTH TLS`; encrypted sessions require `PBSZ 0` and `PROT P`.

### Control reply state machine

Valid bounded preliminary greetings such as `120 -> 220` interoperate while repeated preliminary replies are capped. Reply parsing enforces numeric `100..599` codes, standard space/hyphen framing, per-line size, multiline line count and total multiline character limits.

## Data-transfer mode integrity

Before upload/download data paths, Ghost FTP requires FTP binary mode (`TYPE I`). Passive connections prefer EPSV and can fall back to PASV. EPSV delimiter framing and port range are validated. PASV consumes exactly six tuple values and derives the port from `p1,p2`; the data socket remains tied to the authenticated control host.

## Safe download resume architecture

0.1.6 places an identity boundary around FTP REST resume.

The byte payload remains in:

```text
<destination>.ghostftp.part
```

When the server provides usable `SIZE` and `MDTM`, a bounded local identity sidecar is stored at:

```text
<destination>.ghostftp.part.meta
```

The versioned sidecar is capped at 16 KiB and stores only host, port, security mode, normalized remote path, remote size and remote modification timestamp. It never stores the username, password, account token or transferred content.

A partial is eligible for REST resume only if its length is positive and not greater than the current remote size and every sidecar identity field matches the current connection/server state. Missing, malformed, oversized, legacy or stale sidecars fail closed: Ghost FTP deletes the stale partial state and restarts from byte zero.

Servers without both usable `SIZE` and `MDTM` can still perform a fresh download, but an interrupted unverified partial is not preserved as safely resumable state.

### In-flight revision check

When a transfer begins with a verifiable `SIZE` + `MDTM` identity, Ghost FTP queries those values again after the data transfer. If either changed, the completed local result is discarded and the transfer fails rather than presenting a potentially mixed remote revision as successful.

FTP metadata is not a cryptographic content identity; the implementation deliberately does not claim stronger guarantees than the server metadata can provide.

### Recursive directory downloads

The recursive download planner keeps the existing bounded traversal model, but each planned file now routes through the same identity-aware resume method. Safe-resume semantics are therefore identical for single-file and directory downloads.

## Directory-listing parser architecture

LIST/MLSD payloads are server-controlled and bounded before expensive parsing work. The parser uses a 64 KiB per-line ceiling, bounded MLSD facts, non-backtracking Unix/Windows LIST regexes, incremental `StringReader` enumeration and safe symlink-name normalization. Total listing payload is bounded independently.

## Transfer queue architecture

`TransferQueueService` is shared by both renderers. It provides bounded channel capacity, configurable/clamped workers, isolated transfer sessions, per-job cancellation, bounded transient retries, progress/throughput/ETA state and synchronization-context dispatch when supplied.

Pause/resume gates **dispatch**, not an already-running byte stream. Workers about to start queued/retrying work wait asynchronously; running transfers continue.

Progress delivery uses one deliberate renderer-marshaling boundary and bounded UI cadence. Shutdown is coordinated: dispatch is completed, paused waiters released, work cancelled, workers awaited and cancellation resources disposed only after work has unwound.

## Transfer data-buffer architecture

FTP send/receive paths use pooled 128 KiB byte buffers to avoid repeated large managed allocations. Each rented buffer is cleared before returning to `ArrayPool<byte>` because it may contain private transferred data.

## FTP session lifecycle architecture

`FtpSession` serializes protocol operations through its gate. Disposal uses an atomic state plus shared completion task. New operations are rejected once shutdown begins, transport cleanup runs once and concurrent disposers synchronize on the same completion path.

## Persistence architecture

Installed mode uses the current user's local application-data directory. Portable mode uses a local `Data` directory beside the executable when the portable marker/name is active.

Settings and profiles use bounded JSON files with atomic replacement/backup behavior and best-effort private filesystem permissions. Session-only profiles are explicitly excluded from persistent profile writes. Window/splitter dimensions, retry values and concurrency are normalized before use.

Resume metadata is separate from profile/settings persistence and stays beside the selected local transfer destination. It is bounded and used only to prove partial-file identity.

## Saved-secret architecture

Windows uses current-user DPAPI through native APIs; sensitive intermediary buffers are zeroed where practical. Linux uses AES-256-GCM with a local user-private key file and best-effort private permissions.

## Completion refresh coalescing

Windows transfer completion uses a short cancellation-based debounce so a batch of completed jobs produces one Local/Remote refresh cycle instead of one remote LIST for every file.

## Regression architecture

The local Demo suite exercises normal file-management workflows without external networking. `GhostFTP.HardeningSelfTest` exercises protocol/parser/settings/lifecycle boundaries through loopback. `GhostFTP.ResumeSelfTest` separately exercises resume identity and remote-mutation safety through loopback.

The optional live-server smoke architecture remains non-destructive and performs connect/PWD/LIST/NOOP/disconnect against explicitly supplied credentials.

## Windows/Linux parity

Windows and Linux share product identity, connection model, Local/Remote workflow, transfer queue, protocol parser, safe download-resume logic, security/privacy semantics, settings/profile models and localization. Renderer-specific native controls may differ without creating a second product behavior.

## Windows Setup transaction architecture

Setup stages application and maintenance binaries before replacing an active installation. Candidate identity and downgrade checks run before commit. Existing application/maintenance files keep rollback copies until later stages succeed; installation remains per-user/as-invoker.

## Privacy and dependency boundaries

Shipping application code has no telemetry, analytics, advertising, hidden crash upload, cloud profile synchronization or product account requirement. Shipping and regression-test projects have no third-party NuGet `PackageReference` dependencies. Audits reject known mobile targets, telemetry SDK identifiers and private signing material.

## Release verification

A public GitHub Release is produced only after exact-version source passes Windows/Linux build, source/hardening audits, Core/Demo/Queue tests, protocol/parser/settings/lifecycle hardening, the dedicated safe-resume integrity suite, WPF/X11 runtime tests, authentic UI capture, packaging and checksum/runtime verification.

## Canonical screenshots

README product images are generated by the real compiled WPF application. The main capture remains 1914 × 907 logical pixels. Screenshot generation is a build path, not a hand-authored mockup.
