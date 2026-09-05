# Ghost FTP Architecture

Ghost FTP 1.4.0 is a Windows-first, C#-only FTP/FTPS client designed around explicit protocol boundaries, local persistence, shared UI primitives and zero third-party runtime package dependencies.

Ghost FTP is the product. **BRENDIGO LTD** is the developer, publisher and licensor.

## Solution projects

- `GhostFTP.Core` — protocol engine, FTP/FTPS session, listing parsers, path/input guards, Demo session, transfer queue and storage abstractions. No WPF dependency.
- `GhostFTP.Design` — shared product identity, BRENDIGO LTD publisher metadata, localization, Windows 11 visual resources, GhostComboBox, native editable-control styling, Mica/DWM integration and programmatic icon source.
- `GhostFTP.App` — Windows desktop client written programmatically in C# with WPF. No XAML.
- `GhostFTP.Setup` — self-contained guided per-user installer/update/uninstall wizard. No MSI/WiX/NSIS/Inno dependency.
- `GhostFTP.SelfTest` — dependency-free Core regression/security/correctness executable tests.
- `GhostFTP.UiSmoke` — real Windows/WPF smoke tests for editable controls and localization coverage.

Dependency direction:

```text
GhostFTP.Core   <- GhostFTP.App
GhostFTP.Design <- GhostFTP.App
GhostFTP.Design <- GhostFTP.Setup
GhostFTP.Core   <- GhostFTP.SelfTest
GhostFTP.Design <- GhostFTP.UiSmoke
```

Core does not depend on Windows presentation. Design does not depend on FTP sockets or credentials.

## Product and legal identity

`GhostBrand` is the single runtime source of truth for:

- display name: `Ghost FTP`;
- compact identifier: `GhostFTP`;
- website and repository;
- shared visual identity/icon;
- publisher: `BRENDIGO LTD`;
- company number: `16545639`;
- registered office;
- copyright notice.

The product name is not replaced by the publisher name. User-facing product surfaces remain Ghost FTP while legal/publisher surfaces identify BRENDIGO LTD where appropriate.

## Shared localization

`GhostLocalization` owns the supported language list, English source strings, core application translations, code normalization and fallback rules.

`GhostSetupLocalization` owns the compact guided-Setup vocabulary. Both operate entirely in-process with no web/localization service.

English is the primary/default language and fallback. 1.4.0 validates 29 selectable languages through `GhostFTP.UiSmoke`.

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

- `MainWindow.Core.cs` — state/control ownership;
- `MainWindow.Layout.cs` — composition;
- `MainWindow.Connection.cs` — connection lifecycle;
- `MainWindow.Diagnostics.cs` — local connection health diagnostics;
- `MainWindow.Files.cs` — local/remote navigation and file operations;
- `MainWindow.Transfers.cs` — transfer/profile/settings interactions;
- `MainWindow.QueueUx.cs` — queue context operations;
- `MainWindow.Responsive.cs` — adaptive columns;
- `MainWindow.Helpers.cs` — reusable UI helpers and shortcuts.

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
- REST/SIZE-assisted transfer behavior where supported.

Security/correctness boundaries include:

- Windows/.NET certificate and hostname validation;
- no certificate bypass option;
- offline revocation-cache mode to avoid hidden OCSP/CRL network calls;
- CR/LF/NUL command-argument rejection;
- command/reply limits;
- bounded LIST/MLSD payload memory;
- passive-host hardening;
- connect/command/transfer idle timeouts;
- depth and total-entry traversal budgets;
- remote-root delete protection;
- verified ambiguous `MKD 550` handling.

## Server working-directory consistency

The application no longer treats the Remote path box as an unrelated UI-only state. Remote folder navigation passes through server `CWD`, then reads `PWD` and synchronizes the visible path to the server-confirmed working directory.

This keeps browsing, diagnostics and servers that rely on current working directory semantics aligned.

## Transfer architecture

Browsing uses the primary session. Queued transfers lease independent sessions so a long transfer, retry or cancellation cannot consume replies intended for the browser session.

`TransferQueueService` provides:

- bounded queue capacity;
- sequential processing;
- per-job cancellation;
- queue-wide cancellation;
- progress/speed/retry state;
- controlled transient retry;
- failed-job visibility instead of UI-thread exceptions.

Automatic retry is intentionally selective. Socket/timeouts and FTP 4xx transient errors can be retried; authentication, TLS/certificate, permission and permanent 5xx failures are not blindly retried.

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

## Setup maintenance and self-delete

A running process cannot remove its own executable reliably. During uninstall Ghost FTP removes the client, shortcuts and registry entry immediately, then schedules maintenance-Setup cleanup through:

- a hidden delayed local delete attempt after process exit; and
- Windows delete-on-reboot fallback for the Setup executable.

## No telemetry architecture

There is no telemetry service, analytics client, ad component, cloud profile service, automatic update client or crash-upload component in the runtime architecture.

Network traffic originates only from explicit FTP/FTPS operations or user-opened website links.

## Build architecture

`build-release.ps1` publishes self-contained single-file builds for:

- `win-x64`;
- `win-arm64`.

Each architecture gets a matching Setup executable with its own embedded portable payload. Canonical and architecture-explicit asset names plus SHA-256 checksums are produced.

## Release gates

The pipeline requires:

1. restore;
2. warning-as-error Release build;
3. dependency/version/privacy/product/publisher audit;
4. Core tests;
5. WPF editable-input tests;
6. app localization tests;
7. Setup localization tests;
8. x64/ARM64 packaging;
9. required EXE verification;
10. artifact/release publication.

See `docs/RELEASE-POLICY.md` for release governance.
