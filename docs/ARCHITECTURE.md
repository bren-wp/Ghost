# Ghost FTP Architecture

Ghost FTP **0.1.0 Beta** is a Windows-production, C#-only FTP/FTPS client organized around a platform-neutral protocol core, local persistence, bounded parallel transfers, a dense professional WPF workstation, authentic UI documentation capture and zero third-party runtime package dependencies.

The public version-line reset does not remove architectural work completed during the preserved internal 1.x development history. The architecture below describes the current source tree and current 0.1.0 Beta product line.

Ghost FTP is the product. **BRENDIGO LTD** is the developer, publisher and licensor.

## Version and release-channel architecture

Release identity is defined by two root files:

```text
VERSION
RELEASE_CHANNEL
```

The current values are:

```text
VERSION=0.1.0
RELEASE_CHANNEL=beta
```

`Directory.Build.props` synchronizes the numeric assembly/file version and the Beta informational version. The current build therefore uses `0.1.0.0` assembly/file metadata and `0.1.0-beta` informational metadata.

All pre-1.0 releases remain Beta. The first stable release is **1.0.0**. Canonical package filenames stay stable while their internal version metadata advances. This keeps download URLs predictable without allowing a 0.x Beta package to claim stable 1.0.0 metadata.

See `docs/VERSIONING.md`.

## Solution projects

- `GhostFTP.Core` — platform-neutral `net10.0` FTP/FTPS engine, parsers, path/input guards, Demo session, transfer queue and storage abstractions. No WPF dependency.
- `GhostFTP.Design` — shared Ghost FTP identity, BRENDIGO LTD metadata, localization, Windows visual resources, GhostComboBox, native editable-control styling, Mica/DWM integration and programmatic icon source.
- `GhostFTP.App` — production Windows desktop client written programmatically in C# with WPF. No XAML.
- `GhostFTP.Setup` — self-contained guided per-user installer/update/uninstall wizard. No MSI/WiX/NSIS/Inno dependency.
- `GhostFTP.SelfTest` — dependency-free Core security/correctness regression executable.
- `GhostFTP.QueueSelfTest` — bounded parallelism, session isolation, cancellation isolation and lifecycle regression executable.
- `GhostFTP.UiSmoke` — live Windows/WPF editable-input, localization and Setup language-switch smoke tests.

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

## Platform boundary

The production GUI is Windows WPF. `GhostFTP.Core` deliberately targets platform-neutral `net10.0` so a future Linux renderer can reuse the same protocol and transfer engine.

Android and iOS are outside the shipping desktop scope and are rejected by source audit. A Linux GUI is not claimed until a real renderer exists and passes the same protocol, privacy, security, localization and workstation-parity gates.

See `docs/PLATFORM-SUPPORT.md`.

## Product and legal identity

`GhostBrand` is the runtime source of truth for:

- display name `Ghost FTP`;
- compact identifier `GhostFTP`;
- product website;
- publisher website;
- repository;
- shared icon/visual identity;
- publisher `BRENDIGO LTD`;
- company number `16545639`;
- registered office;
- copyright notice.

Product identity is never replaced by publisher identity on normal application surfaces.

## Localization architecture

`GhostLocalization` owns the supported application language list, English source strings, core translations, language-code normalization and fallback behavior.

`GhostSetupLocalization` owns Setup vocabulary. Both operate entirely in-process. English is primary/default and guaranteed fallback. Ghost FTP validates 29 selectable application/Setup languages without a translation web service.

## Native editable-input boundary

TextBox and PasswordBox retain native WPF editor/content-host behavior. Ghost FTP styles colors, typography, padding and focus resources without replacing the editor host.

This protects caret placement, selection, clipboard shortcuts, Tab navigation, IME input and normal password editing. `GhostFTP.UiSmoke` exercises actual controls on a Windows STA thread.

## Main desktop composition

The application is split into responsibility-specific partial classes:

- `MainWindow.Core.cs` — control/state ownership and startup composition;
- `MainWindow.Layout.cs` — menu, toolbar, sidebar, Connection Log, Quick Connect, file panes, Transfers and status-bar composition;
- `MainWindow.Connection.cs` — browser connection lifecycle and visible connection activity;
- `MainWindow.WorkspaceActions.cs` — bounded local Connection Log and Site Manager orchestration;
- `MainWindow.KeepAlive.cs` — optional selected-server control-channel health loop;
- `MainWindow.Diagnostics.cs` — user-initiated connection diagnostics;
- `MainWindow.Files.cs` — Local/Remote navigation and filesystem operations;
- `MainWindow.Transfers.cs` — transfer/profile/settings workflows;
- `MainWindow.QueueUx.cs` — transfer queue context/details operations;
- `MainWindow.Responsive.cs` — resizable/persistent pane geometry and adaptive columns;
- `MainWindow.Helpers.cs` — reusable UI helpers and focus-safe keyboard routing;
- `MainWindow.DocumentationCapture.cs` — deterministic rendering of the actual production windows to repository PNGs.

`SiteManagerDialog.cs` is the first-class saved-site master/detail editor.

The current workstation hierarchy is:

```text
Menu bar
   ↓
Global action toolbar
   ↓
Saved Sites │ Connection Log + Quick Connect
            │
            └──────── Local ⇄ Remote
                       ↓
                    Transfers
                       ↓
                    Status bar
```

The Saved Sites/main split, Local/Remote split and browser/Transfers split are user-resizable. Geometry is stored locally and normalized before restoration.

## Site Manager boundary

Site Manager operates on cloned `ServerProfile` models until the dialog is accepted.

Implemented per-site state is intentionally limited to supported behavior:

- site name;
- host;
- port;
- security mode;
- username;
- optional password;
- Remember password;
- default remote path.

Passwords are never exposed as plain persisted JSON. Accepted password changes pass through the existing `ProfileStore` / DPAPI protection path.

The built-in Demo profile is visible but protected from modification/removal. Global retry, timeout, keepalive and concurrency policy remains in `AppSettings` rather than being duplicated as fake per-site state.

## Local Connection Log architecture

The main-window Connection Log is an in-memory bounded collection used only for user-visible session activity.

It may contain timestamps and non-secret events such as:

- application startup;
- local profile count;
- selected host/port/security connection attempt;
- TLS/plain connection state;
- remote directory listing count;
- disconnect/loss/failure summaries.

It never records passwords, DPAPI blobs or file contents. It is not written to an analytics service or uploaded automatically. The UI bounds the collection to prevent unbounded memory growth.

## FTP/FTPS control boundary

Only `FtpSession` owns real FTP/FTPS sockets.

Capabilities include:

- FTP;
- Explicit FTPS;
- Implicit FTPS;
- TLS 1.2 / TLS 1.3;
- EPSV with PASV fallback;
- MLSD with LIST fallback;
- UTF-8 negotiation where supported;
- REST/SIZE-assisted transfer behavior;
- standard `NOOP` health checking.

Security/correctness boundaries include:

- .NET certificate-chain and hostname validation;
- no certificate bypass option;
- offline revocation-cache mode to avoid hidden Ghost FTP-triggered OCSP/CRL traffic;
- CR/LF/NUL command-argument rejection;
- bounded command/reply and listing payloads;
- passive-host redirection hardening;
- connect/command/transfer-idle timeouts;
- depth/entry traversal budgets;
- remote-root deletion protection;
- verified ambiguous `MKD 550` handling.

## Working-directory consistency

Remote navigation sends server `CWD` and then reads `PWD`. The visible Remote path is the server-confirmed working directory rather than a client-only assumption.

This aligns browsing, transfer destinations and diagnostics.

## Browser session health

`KeepAliveAsync` is part of `IFtpSession`. The real implementation sends `NOOP` under the control-session gate.

If keepalive or diagnostics proves the control transport is unusable:

1. the transport resets;
2. `IsConnected` becomes false;
3. stale Remote state is cleared by the UI;
4. status becomes Connection lost;
5. reconnection remains explicit.

Keepalive is configurable/disableable, skips Demo mode and communicates only with the selected FTP/FTPS server. It is not telemetry.

## Transfer architecture

Browsing uses the primary session. Real queued transfers lease independent sessions so a long transfer, retry or cancellation cannot consume browser-session replies.

`TransferQueueService` provides:

- capacity bounded at 4,096 jobs;
- parallel workers normalized to 1–8;
- default concurrency 3;
- per-job cancellation;
- queue-wide cancellation;
- progress, byte, speed, ETA, retry and lifecycle state;
- controlled transient retry;
- visible failed-job state;
- deterministic worker shutdown before cancellation resources are released.

Automatic retry is selective: socket/timeouts and FTP 4xx transient errors may retry; authentication, certificate, permission and permanent 5xx failures do not blindly retry.

## Transfer measurement model

`TransferJob` tracks local operational state:

- lifecycle state;
- progress percentage;
- bytes transferred;
- known total;
- current speed;
- ETA;
- retry count;
- start/finish timestamps;
- source/destination;
- local error text.

The first progress callback establishes a measurement baseline, preventing already-present resumed bytes from being counted as current-session throughput.

ETA remains unknown unless both usable current speed and known total are available.

## Cancellation and session isolation

Each queued item has its own linked cancellation token. Cancelling one transfer must not cancel neighboring jobs.

`GhostFTP.QueueSelfTest` verifies actual parallelism, 1–8 clamps, isolated transfer-session instances, cancellation isolation and lifecycle state.

## Download integrity

Downloads target `.ghostftp.part`. When `SIZE` is available:

1. expected remote size is obtained;
2. resume offset is validated;
3. transfer runs;
4. final partial-file length must match expected size;
5. only then is the partial file promoted to the requested destination.

A mismatch remains a failed resumable partial rather than a misleading successful file.

## Upload integrity and replacement

Uploads use a unique temporary remote path. When `SIZE` is available:

1. temporary upload length is checked;
2. existing destination can be moved to rollback backup;
3. temporary upload is renamed into destination;
4. final destination length is verified;
5. rollback backup is removed only after verified commit.

A failed final check attempts to remove the invalid destination and restore the backup. This is byte-length integrity, not a cryptographic checksum claim.

## Focus-safety boundary

Global shortcuts resolve explicit Local, Remote and Transfers focus context. File actions never silently default to Local when another region owns focus.

`Delete` while Transfers owns focus cancels the selected transfer rather than deleting local data. This is treated as a safety invariant because incorrect focus routing can mutate the wrong storage domain.

## Connection Diagnostics

Diagnostics is user-initiated against the already-connected server and can inspect:

- `NOOP` control health;
- `SYST` text;
- `PWD`;
- known `FEAT` capabilities;
- TLS/plain transport state.

Results remain local.

## Persistence boundary

Installed mode stores data under `%LOCALAPPDATA%\GhostFTP`. Portable mode uses a local `Data` directory next to the executable where applicable.

Persistence is treated as untrusted input:

- settings/profile file size bounds;
- profile count bounds;
- important string/blob bounds;
- enum/path/host normalization;
- canonical single Demo profile;
- opt-in DPAPI password protection;
- command-safety revalidation after decryption;
- bounded retry/concurrency/timeouts/keepalive;
- bounded window/pane geometry;
- temp-file/atomic-replacement/backup recovery where supported.

## Authentic documentation capture

The current Beta line treats repository screenshots as build artifacts derived from real UI source.

`Program.cs` recognizes:

```text
--capture-ui <output-directory>
```

Capture mode:

1. forces deterministic dark theme and English locale;
2. launches the real production `MainWindow`;
3. loads local profile/settings infrastructure;
4. selects the built-in local Demo profile;
5. opens the Demo session without a network socket;
6. renders the real MainWindow through WPF `RenderTargetBitmap`;
7. opens and renders the real `SiteManagerDialog`;
8. writes `ghostftp-client.png` and `ghostftp-site-manager.png`;
9. disposes queue/session state and exits.

`.github/workflows/capture-ui.yml` rebuilds the client and refreshes those images from source. The regular CI workflow independently regenerates captures into an artifact and validates that both are non-empty.

This design prevents repository marketing images from drifting into unrelated mockups: screenshots are a rendering of production UI code.

## Setup architecture

Setup is a C# WPF application embedding:

- architecture-matching self-contained Ghost FTP executable;
- repository LICENSE text;
- shared Ghost FTP design/localization code.

Install flow:

```text
Language → License → Options → Ready → Install/Update → Finish
```

The license must be accepted before installation. Setup installs per-user under `%LOCALAPPDATA%\Programs\GhostFTP` and registers Windows Installed Apps metadata.

No separate uninstaller executable is generated. The installed maintenance copy is `GhostFTP-Setup.exe`, invoked with `--uninstall`.

## Setup language lifecycle

Live Setup language changes avoid unsafe WPF logical-tree reparenting by closing the dropdown, deferring render until the input event unwinds, detaching reusable controls, coalescing repeated requests and ignoring queued rebuilds after close.

`GhostFTP.UiSmoke` exercises actual language switching plus wizard Next/Back navigation.

## Setup self-cleanup

A running process cannot reliably delete itself. During uninstall Ghost FTP removes the client, shortcuts and registry entry, then uses:

- a bounded hidden delayed local delete attempt after process exit;
- Windows delete-on-reboot fallback for maintenance Setup.

The delay loop is local and not external product network traffic.

## No telemetry architecture

Ghost FTP has no telemetry service, analytics client, ad component, cloud profile service, automatic updater client or crash-upload component.

Network behavior is limited to:

- selected FTP/FTPS server operations;
- optional keepalive on that selected session;
- user-initiated diagnostics on that selected session;
- website links explicitly opened by the user.

Transfer metrics, Connection Log entries and documentation capture remain local.

## Build architecture

`build-release.ps1` publishes self-contained single-file Windows builds for `win-x64` and `win-arm64`. Each architecture receives a matching Setup payload. Canonical and architecture-explicit names plus SHA-256 checksums are produced.

The release workflow verifies that the resulting executable file versions match the active numeric `VERSION`. During the Beta line this means 0.x.y.0 Windows file versions. When Ghost FTP is promoted to stable 1.0.0, the canonical `portable.exe` and `setup.exe` family must verify as 1.0.0.0 binaries.

## Release gates

The pipeline requires:

1. restore;
2. warning-as-error Release build;
3. dependency/version/channel/privacy/product/publisher/platform audit;
4. Core security/correctness tests;
5. bounded parallel queue/session/cancellation tests;
6. WPF editable-input tests;
7. app localization tests;
8. Setup localization/live-rebuild tests;
9. authentic compiled MainWindow + Site Manager capture;
10. x64/ARM64 packaging for official release publication;
11. required executable and executable-version verification;
12. SHA-256 manifest generation;
13. verified artifact/release publication.

See `docs/RELEASE-POLICY.md` for release governance.
