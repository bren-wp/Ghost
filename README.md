<p align="center">
  <img src="assets/readme/ghostftp-client.png" alt="Ghost FTP 0.1.4 Beta — authentic production Windows desktop client" width="100%">
</p>

<p align="center"><strong>Authentic application capture generated from the compiled Ghost FTP desktop client — not a mockup, illustration or generated UI.</strong></p>

# Ghost FTP

**Ghost FTP** (`GhostFTP`) is a privacy-first native FTP/FTPS desktop workstation for **Windows and Linux**. It combines a modern dual-pane workflow, bounded parallel transfers, local-only profiles, strict TLS behavior, a premium resizable workstation UI and a dependency-minimal C#/.NET codebase.

Ghost FTP is developed and published by **BRENDIGO LTD** (Company number **16545639**), 71–75 Shelton Street, Covent Garden, London, WC2H 9JQ, United Kingdom.

- Product: https://ghostftp.com
- Publisher: https://brendigo.com
- GitHub Releases: https://github.com/bren-wp/Ghost/releases
- Current source version: **0.1.4**
- Current release channel: **Beta**
- Informational version: **0.1.4-beta**
- First stable target: **1.0.0**
- Runtime baseline: **.NET 10 / C# 14**
- Detailed release notes: [`docs/releases/v0.1.4.md`](docs/releases/v0.1.4.md)

## What Ghost FTP is built for

Ghost FTP is a real desktop file-transfer client, not a web wrapper. It keeps the mature dual-pane workflow expected from professional FTP clients while using a cleaner contemporary interface and explicit privacy/security boundaries.

The workstation provides:

- saved servers and session-only Quick Connect profiles;
- Local and Remote file panes;
- upload/download queues;
- queue retry, cancellation and selective cleanup controls;
- queue-level pause/resume for **new dispatch**;
- connection log and local diagnostics;
- Site Manager;
- configurable retries, concurrency, timeouts and keepalive;
- 29 selectable local languages;
- native Windows and native Linux renderers sharing the same protocol and transfer core.

## 0.1.4 Beta highlights

### Protocol compatibility without permissive parsing

Ghost FTP 0.1.4 accepts a bounded preliminary FTP greeting sequence such as `120 -> 220`, which improves compatibility with standards-compliant servers that are not immediately ready.

At the same time, control replies are parsed more strictly:

- reply codes must be numeric `100..599` values;
- the reply framing separator must be a space or hyphen when present;
- reply lines, multiline line count and total reply characters remain bounded;
- malformed responses such as `220X ...` are rejected.

### Strict EPSV/PASV handling

Passive-mode parsing no longer extracts arbitrary digits from a server response.

- EPSV validates the delimiter framing and port.
- PASV parses exactly the six comma-separated values inside the passive tuple.
- Each PASV tuple value must fit in `0..255`.
- The data port comes only from the tuple's `p1,p2` values.
- Trailing numeric diagnostics cannot alter the selected data port.
- Data connections remain tied to the authenticated control host instead of trusting a PASV-supplied host.

### Race-safe shutdown

FTP session and transfer-queue disposal are now coordinated single-owner operations. Concurrent `DisposeAsync()` callers wait for the same completion signal, post-shutdown operations are rejected deterministically, queue workers are allowed to stop before cancellation resources are disposed, and paused dispatch waiters are released during shutdown.

### Deterministic protocol hardening tests

`GhostFTP.HardeningSelfTest` runs on Windows and Linux with no external server or package dependency. An in-process loopback FTP server verifies the real control/data path, including USER/PASS, PWD, TYPE I, EPSV fallback, PASV, LIST and QUIT. Separate cases test malformed reply rejection and concurrent session/queue shutdown.

## Transfer management

Ghost FTP retains the workstation transfer improvements from the previous release line:

- Pause queue / Resume queue;
- Retry selected failed or cancelled transfers on Windows;
- Retry all failed transfers on Windows and Linux;
- Cancel selected or all active transfers;
- Clear completed, failed or cancelled history selectively;
- Clear all finished history;
- inspect transfer state and copy source/destination paths on Windows;
- aggregate running throughput and richer queue summaries;
- selectable transfer rows in the Linux X11/XWayland renderer.

**Pause means dispatch pause.** Transfers that are already running continue. Ghost FTP does not claim that arbitrary FTP servers can safely suspend an active byte stream unless resumable-transfer semantics have actually been negotiated.

## Supported protocols

Ghost FTP currently implements:

- FTP;
- Explicit FTPS (`AUTH TLS`);
- Implicit FTPS.

Explicit FTPS is recommended where supported. Ghost FTP does **not** label SFTP as FTP and does not claim SSH/SFTP support in this release line.

## Security model

The shipping client preserves these boundaries:

- fail-closed transport-mode validation;
- TLS 1.2/1.3 certificate and hostname validation;
- no silent FTPS-to-FTP downgrade;
- `PBSZ 0` / `PROT P` encrypted-data protection for FTPS;
- required binary `TYPE I` transfer mode;
- passive data connections tied to the authenticated control host;
- strict EPSV/PASV port parsing;
- CR/LF/NUL command-injection guards;
- bounded control replies, listings, traversal and transfer-queue limits;
- local path containment checks;
- deletion confirmation and root/path protections;
- bounded retry and concurrency behavior;
- isolated transfer sessions;
- cancellation-safe cleanup and coordinated shutdown.

Read [`SECURITY.md`](SECURITY.md) for the threat and hardening model.

## Privacy

Ghost FTP is designed to run **without application telemetry**.

The application contains no analytics SDK, advertising SDK, usage telemetry, user fingerprinting, hidden crash uploader, cloud profile synchronization, hidden product-account requirement or automatic background update checker.

Quick Connect credentials are session-only unless the user explicitly saves a profile. Saved-password protection is opt-in. Windows uses the current-user DPAPI boundary; Linux uses AES-256-GCM with local user-private key material.

Read [`PRIVACY.md`](PRIVACY.md) for the complete privacy contract.

## Windows

The Windows renderer is native WPF and targets modern Windows desktop systems with per-monitor DPI awareness and long-path awareness.

### Windows release files

Canonical and architecture-specific release files include:

- `setup.exe`
- `portable.exe`
- `setup-arm64.exe`
- `portable-arm64.exe`
- `GhostFTP-Setup-win-x64.exe`
- `GhostFTP-Portable-win-x64.exe`
- `GhostFTP-Setup-win-arm64.exe`
- `GhostFTP-Portable-win-arm64.exe`
- `SHA256SUMS.txt`
- `SIGNING.txt`

The same Setup executable is the installed maintenance/uninstall entry. Ghost FTP intentionally does not generate a separate `uninstall.exe`.

## Linux

The Linux renderer is a native C# X11/XWayland desktop client using the system `libX11.so.6` ABI. It shares `GhostFTP.Core` and `GhostFTP.Design` with Windows rather than implementing a separate FTP engine.

### Linux release files

Canonical Linux assets include:

- `GhostFTP-linux-x64`
- `GhostFTP-linux-arm64`
- corresponding `.tar.gz` archives;
- versioned archives;
- `SHA256SUMS-linux.txt`;
- `BUILD-INFO.txt`.

The Linux client follows the same saved-site, Quick Connect, Local/Remote, transfer queue, privacy and transport-security model as Windows.

## Platform scope

Shipping desktop scope is intentionally limited to:

- Windows x64/ARM64;
- Linux x64/ARM64.

Android, iOS, MacCatalyst and web application targets are not part of the shipping product scope.

## Localization

Ghost FTP provides **29 selectable languages** from local application resources. **English (`en`) is the primary language, default language and final fallback.**

No online translation API is used by the desktop client or Setup. Newly introduced technical strings safely fall back to English when a specific localized override is not yet available.

See [`docs/LOCALIZATION.md`](docs/LOCALIZATION.md).

## Transfer queue semantics

The transfer queue is bounded and uses isolated FTP sessions for transfer work where appropriate. It supports configurable concurrency, automatic transient retries, cancellation isolation, queued/running/retrying/completed/failed/cancelled state, progress/speed/ETA reporting, dispatch pause/resume and selective history cleanup.

Queue pause waits asynchronously. It does not spin a polling loop and does not interrupt transfers that were already running.

## Demo mode

The built-in Ghost FTP Demo profile is completely local. It opens no external FTP connection and is used by the regression suite to exercise listing, upload/download, rename, directory traversal, recursive transfer behavior, cleanup and disconnect lifecycle.

## Real-server smoke testing

A separate non-destructive live-server harness can validate connect/PWD/LIST/NOOP/disconnect against explicitly configured credentials without server writes. Credential values are supplied through protected CI secrets and are redacted from output.

See [`docs/LIVE-SMOKE-TEST.md`](docs/LIVE-SMOKE-TEST.md).

## Build from source

Requirements:

- .NET 10 SDK;
- Windows for the real WPF renderer/Setup build;
- Linux with X11/XWayland runtime libraries for the native Linux renderer.

Typical validation:

```text
dotnet restore GhostFTP.sln
dotnet build GhostFTP.sln -c Release
```

Official release builds use the repository release scripts and CI gates rather than relying only on a local compile.

## Dependency policy

Shipping and regression-test projects intentionally contain **zero third-party NuGet `PackageReference` dependencies**. Platform APIs are accessed through the .NET runtime/BCL, WPF on Windows, audited native Windows calls where necessary, and the audited X11 ABI layer on Linux.

Repository audits reject known telemetry/tracking SDK references and private signing-key material.

## CI and release gates

A Ghost FTP Beta source is not treated as release-ready until the relevant Windows/Linux pipeline passes:

- restore/build;
- source/dependency/platform audit;
- final security hardening audit;
- Core self-test;
- complete local Demo workflow self-test;
- parallel transfer queue self-test;
- protocol and shutdown hardening self-test on Windows and Linux;
- Windows WPF editable-input/localization smoke test;
- Linux X11/XWayland runtime smoke test;
- authentic Windows UI capture;
- Windows and Linux packaging;
- required artifact verification;
- SHA-256 generation/verification.

## Documentation

- [`docs/releases/v0.1.4.md`](docs/releases/v0.1.4.md) — detailed 0.1.4 Beta release notes
- [`CHANGELOG.md`](CHANGELOG.md) — cumulative public version history
- [`docs/HISTORICAL-CHANGELOG.md`](docs/HISTORICAL-CHANGELOG.md) — preserved pre-reset engineering history
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — architecture and trust boundaries
- [`docs/UI-UX.md`](docs/UI-UX.md) — UI/UX contract
- [`docs/UI-PARITY.md`](docs/UI-PARITY.md) — Windows/Linux parity contract
- [`docs/INSTALLATION.md`](docs/INSTALLATION.md) — install/update/uninstall model
- [`docs/LOCALIZATION.md`](docs/LOCALIZATION.md) — local language architecture
- [`docs/PLATFORM-SUPPORT.md`](docs/PLATFORM-SUPPORT.md) — shipping platform scope
- [`docs/RELEASE-POLICY.md`](docs/RELEASE-POLICY.md) — release requirements
- [`docs/LIVE-SMOKE-TEST.md`](docs/LIVE-SMOKE-TEST.md) — non-destructive real-server verification
- [`SECURITY.md`](SECURITY.md) — security model
- [`PRIVACY.md`](PRIVACY.md) — privacy model

## Versioning

The public source version is controlled by root `VERSION` and `RELEASE_CHANNEL` files. All 0.x releases remain Beta. Version **1.0.0** is reserved for the first stable public release.

Historical engineering documents are preserved for traceability and do not override the current public version line.

## License

The repository is source-available/proprietary under [`LICENSE`](LICENSE). See [`NOTICE.md`](NOTICE.md) for publisher and legal information.
