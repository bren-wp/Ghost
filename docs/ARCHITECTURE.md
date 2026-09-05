# Ghost FTP Architecture

Ghost FTP 1.6.0 is a Windows-production, C#-only FTP/FTPS client designed around explicit protocol boundaries, local persistence, bounded parallel transfers, shared UI primitives and zero third-party runtime package dependencies.

Ghost FTP is the product. **BRENDIGO LTD** is the developer, publisher and licensor.

## Solution projects

- `GhostFTP.Core` — platform-neutral `net10.0` protocol engine, FTP/FTPS session, listing parsers, path/input guards, Demo session, transfer queue and storage abstractions. No WPF dependency.
- `GhostFTP.Design` — shared product identity, BRENDIGO LTD publisher metadata, localization, Windows 11 visual resources, GhostComboBox, native editable-control styling, Mica/DWM integration and programmatic icon source.
- `GhostFTP.App` — Windows desktop client written programmatically in C# with WPF. No XAML.
- `GhostFTP.Setup` — self-contained guided per-user installer/update/uninstall wizard. No MSI/WiX/NSIS/Inno dependency.
- `GhostFTP.SelfTest` — dependency-free Core regression/security/correctness executable tests.
- `GhostFTP.QueueSelfTest` — bounded-parallel-queue, session-isolation, cancellation-isolation and lifecycle regression tests.
- `GhostFTP.UiSmoke` — real Windows/WPF smoke tests for editable controls, localization and live Setup language switching.

Dependency direction:

```text
GhostFTP.Core   <- GhostFTP.App
GhostFTP.Design <- GhostFTP.App
GhostFTP.Design <- GhostFTP.Setup
GhostFTP.Core   <- GhostFTP.SelfTest
GhostFTP.Core   <- GhostFTP.QueueSelfTest
GhostFTP.Design <- GhostFTP.UiSmoke
GhostFTP.Setup  <- GhostFTP.UiSmoke
```

Core does not depend on Windows presentation. Design does not own FTP sockets or credentials.

## Product and legal identity

`GhostBrand` is the single runtime source of truth for:

- display name: `Ghost FTP`;
- compact identifier: `GhostFTP`;
- product website;
- publisher website;
- repository;
- shared visual identity/icon;
- publisher: `BRENDIGO LTD`;
- company number: `16545639`;
- registered office;
- copyright notice.

The product name is not replaced by the publisher name. User-facing product surfaces remain Ghost FTP while legal/publisher surfaces identify BRENDIGO LTD where appropriate.

## Platform boundary

The production GUI is currently Windows WPF. `GhostFTP.Core` deliberately targets platform-neutral `net10.0` so protocol behavior can be shared with a future real Linux renderer.

Android and iOS are outside the shipping desktop scope. Source audit rejects mobile application trees/TFMs. A Linux GUI is not claimed until a real renderer exists and passes the same protocol, privacy, security and UX gates.

See `docs/PLATFORM-SUPPORT.md`.

## Shared localization

`GhostLocalization` owns the supported language list, English source strings, core application translations, code normalization and fallback rules.

`GhostSetupLocalization` owns the compact guided-Setup vocabulary. Both operate entirely in-process with no web/localization service.

English is the primary/default language and fallback. Ghost FTP validates 29 selectable languages through `GhostFTP.UiSmoke`.

## Editable-input architecture

TextBox and PasswordBox deliberately retain native WPF editor/content-host behavior. Ghost FTP styles typography, colors, padding and focus visuals without replacing the editor template.

This protects:

- normal text and numeric entry;
- caret placement and selection;
- mouse/keyboard focus;
- Tab navigation;
- clipboard/edit shortcuts;
- IME and alternative keyboard layouts.

A real Windows STA smoke test mutates actual shared controls and blocks a release if editable input regresses.

## Main desktop composition

The main window is split into partial classes by responsibility:

- `MainWindow.Core.cs` — state/control ownership and composition startup;
- `MainWindow.Layout.cs` — workspace composition;
- `MainWindow.Workspace.cs` — resizable/persistent pane geometry;
- `MainWindow.Connection.cs` — connection lifecycle;
- `MainWindow.KeepAlive.cs` — optional control-channel health loop;
- `MainWindow.Diagnostics.cs` — local connection health diagnostics;
- `MainWindow.Files.cs` — local/remote navigation and file operations;
- `MainWindow.Transfers.cs` — transfer/profile/settings interactions;
- `MainWindow.QueueUx.cs` — queue details/context operations;
- `MainWindow.Responsive.cs` — adaptive columns;
- `MainWindow.Helpers.cs` — reusable UI helpers and focus-safe keyboard routing.

The user workflow is:

```text
Saved servers / identity
        ↓
Global connection status
        ↓
Quick Connect
        ↓
Local pane ⇄ Remote pane
        ↓
Transfer queue
```

The sidebar, Local/Remote split and browser/Transfers split are user-resizable. Geometry is stored locally and normalized before use.

## FTP/FTPS control boundary

Only `FtpSession` owns real FTP/FTPS sockets.

Connection capabilities include:

- FTP;
- Explicit FTPS;
- Implicit FTPS;
- TLS 1.2 / TLS 1.3;
- EPSV with PASV fallback;
- MLSD with LIST fallback;
- UTF-8 negotiation where supported;
- REST/SIZE-assisted transfer behavior where supported;
- standard `NOOP` health checking.

Security/correctness boundaries include:

- Windows/.NET certificate and hostname validation in the production Windows client;
- no certificate bypass option;
- offline revocation-cache mode to avoid hidden OCSP/CRL requests initiated by Ghost FTP;
- CR/LF/NUL command-argument rejection;
- command/reply limits;
- bounded LIST/MLSD payload memory;
- passive-host hardening;
- connect/command/transfer-idle timeouts;
- depth and total-entry traversal budgets;
- remote-root delete protection;
- verified ambiguous `MKD 550` handling.

## Server working-directory consistency

The application does not treat the Remote path box as unrelated UI-only state. Remote folder navigation passes through server `CWD`, then reads `PWD` and synchronizes the visible path to the server-confirmed working directory.

This keeps browsing, diagnostics and servers that rely on current-working-directory semantics aligned.

## Browser control-session health

The primary browsing session is explicit state.

`KeepAliveAsync` is part of the `IFtpSession` contract. The real `FtpSession` implementation sends `NOOP` under the same session gate used by control operations.

If `NOOP` or a diagnostic control check proves the transport is unusable:

1. the transport is reset;
2. `IsConnected` becomes false;
3. stale remote path/list state is cleared by the UI;
4. the status becomes Connection lost;
5. reconnection remains an explicit user action.

The keepalive loop is configurable/disableable, skips Demo mode and talks only to the currently selected FTP/FTPS server. It is not a product telemetry channel.

## Transfer architecture

Browsing uses the primary session. Real queued transfers lease independent sessions so a long transfer, retry or cancellation cannot consume replies intended for the browser session.

`TransferQueueService` provides:

- queue capacity bounded at 4,096 jobs;
- bounded parallel workers;
- configurable worker count normalized to 1–8;
- default concurrency of 3;
- per-job cancellation;
- queue-wide cancellation;
- progress, byte, speed, ETA, retry and lifecycle state;
- controlled transient retry;
- failed-job visibility instead of UI-thread exceptions;
- deterministic worker shutdown before queue cancellation resources are released.

Automatic retry is intentionally selective. Socket/timeouts and FTP 4xx transient errors can be retried; authentication, TLS/certificate, permission and permanent 5xx failures are not blindly retried.

## Transfer measurement model

`TransferJob` is local operational state and implements WPF-friendly property change notification.

It tracks:

- state;
- progress percent;
- bytes transferred;
- known total bytes;
- current speed;
- calculated ETA;
- retry count;
- start/finish timestamps;
- source/destination;
- error text.

Speed calculation is session-relative. The first progress callback establishes a baseline before rate calculation, preventing resumed partial-file bytes from being counted as newly transferred throughput.

ETA is only meaningful when both total bytes and current speed are available. Unknown values remain unknown instead of being fabricated.

## Queue cancellation isolation

Each queued item has its own linked cancellation token. Cancelling one job cancels only that job's work.

`GhostFTP.QueueSelfTest` explicitly verifies that survivor jobs complete after a neighboring transfer is cancelled. It also verifies concurrency clamps, actual parallelism, isolated transfer-session instances and lifecycle timestamps.

## Download integrity model

Downloads are written to a `.ghostftp.part` file. When server `SIZE` is available:

1. expected remote size is obtained;
2. resume offset is checked;
3. transfer is performed;
4. final partial-file length must match expected size;
5. only then is the file promoted to the requested destination.

If the size check fails, the partial file remains available for a future safe resume rather than being mislabeled as a successful final file.

## Upload integrity and replacement model

A file upload uses a unique temporary remote path.

When `SIZE` is supported:

1. temporary remote size is checked against local length;
2. an existing destination is moved to a rollback backup;
3. temporary upload is renamed into the destination;
4. final destination size is checked again;
5. backup is removed only after a verified commit.

If final integrity verification fails, Ghost FTP attempts to remove the invalid destination and restore the rollback backup.

This does not claim cryptographic integrity; it is a byte-length integrity boundary based on FTP `SIZE`. Future checksum support would require explicit server capability handling.

## Focus-safety boundary

Global shortcut handling resolves explicit Local, Remote and Transfers focus context.

File operations do not silently default to Local when the queue/sidebar owns focus. In particular, `Delete` while Transfers owns focus cancels the selected transfer; it does not invoke local-file deletion.

This interaction rule is treated as a safety property because incorrect focus routing can make a destructive operation affect the wrong data domain.

## Connection diagnostics

Connection Diagnostics performs user-initiated checks against the already-connected server:

- NOOP control-channel health;
- SYST server identity text;
- PWD current directory;
- known FEAT capabilities;
- TLS/plain transport status.

Results remain local. No diagnostic report is sent to Ghost FTP or BRENDIGO LTD.

## Persistence boundary

Installed mode uses `%LOCALAPPDATA%\GhostFTP`. Portable mode uses a local `Data` directory alongside the portable executable when applicable.

Persistence is treated as untrusted input:

- settings file size bound;
- profiles file size bound;
- profile count bound;
- bounded important strings/blobs;
- normalized enums/paths/host data;
- canonical single Demo profile;
- opt-in DPAPI-protected password storage;
- command-safety revalidation after password decryption;
- bounded retry/concurrency/timeouts/keepalive values;
- bounded window/pane geometry;
- temporary-file/atomic-replacement/backup recovery where supported.

## Setup architecture

Setup is a C# WPF application that embeds:

- the architecture-matching self-contained Ghost FTP executable;
- the repository LICENSE text;
- shared Ghost FTP visual/localization code.

Installation is a wizard:

```text
Language → License → Options → Ready → Install/Update → Finish
```

The license must be accepted before installation proceeds.

Setup installs per-user under `%LOCALAPPDATA%\Programs\GhostFTP` and registers Windows Installed Apps metadata.

There is no separate uninstaller executable. The installed maintenance copy is `GhostFTP-Setup.exe`; Windows calls it with `--uninstall`.

Setup validates the embedded application payload and the maintenance Setup copy as Windows executables before use.

## Setup language rebuild lifecycle

The Setup language selector is stateful WPF UI. Rebuilding immediately inside `SelectionChanged` previously risked reparenting controls that still belonged to the old logical tree.

The stable architecture:

1. closes the language dropdown;
2. defers rendering until the input event unwinds;
3. detaches reusable controls from their existing parent;
4. rebuilds the wizard tree;
5. coalesces repeated render requests;
6. ignores a queued render after window close.

`GhostFTP.UiSmoke` exercises the real Setup window through multiple live language changes plus Next/Back navigation.

## Setup maintenance and self-delete

A running process cannot remove its own executable reliably. During uninstall Ghost FTP removes the client, shortcuts and registry entry immediately, then schedules maintenance-Setup cleanup through:

- a bounded hidden delayed local delete attempt after process exit; and
- Windows delete-on-reboot fallback for the Setup executable.

The local delay loop uses loopback only and is not external product network traffic.

## No telemetry architecture

There is no telemetry service, analytics client, ad component, cloud profile service, automatic update client or crash-upload component in the runtime architecture.

Runtime network behavior is limited to:

- the FTP/FTPS server session selected by the user;
- optional documented keepalive on that selected session;
- user-initiated diagnostics on that selected session;
- website links explicitly opened by the user.

Transfer metrics remain local UI state.

## Build architecture

`build-release.ps1` publishes self-contained single-file builds for:

- `win-x64`;
- `win-arm64`.

Each architecture gets a matching Setup executable with its own embedded portable payload. Canonical and architecture-explicit asset names plus SHA-256 checksums are produced.

## Release gates

The pipeline requires:

1. restore;
2. warning-as-error Release build;
3. dependency/version/privacy/product/publisher/platform audit;
4. Core security/correctness tests;
5. bounded parallel queue/session/cancellation tests;
6. WPF editable-input tests;
7. app localization tests;
8. Setup localization/live-rebuild tests;
9. x64/ARM64 packaging;
10. required EXE verification;
11. SHA-256 manifest generation;
12. artifact/release publication.

See `docs/RELEASE-POLICY.md` for release governance.
