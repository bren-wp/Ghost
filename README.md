<p align="center">
  <img src="assets/readme/ghostftp-client.png" alt="Ghost FTP 0.1.3 Beta — authentic production Windows desktop client" width="100%">
</p>

<p align="center"><strong>Authentic application capture generated from the compiled Ghost FTP desktop client — not a mockup, illustration or generated UI.</strong></p>

# Ghost FTP

**Ghost FTP** (`GhostFTP`) is a privacy-first native FTP/FTPS desktop workstation for **Windows and Linux**. It combines a modern dual-pane file workflow, bounded parallel transfers, local-only profiles, strict TLS behavior, a premium resizable workstation UI and a dependency-minimal C#/.NET codebase.

Ghost FTP is developed and published by **BRENDIGO LTD** (Company number **16545639**), 71–75 Shelton Street, Covent Garden, London, WC2H 9JQ, United Kingdom.

- Product: https://ghostftp.com
- Publisher: https://brendigo.com
- GitHub Releases: https://github.com/bren-wp/Ghost/releases
- Current source version: **0.1.3**
- Current release channel: **Beta**
- Informational version: **0.1.3-beta**
- First stable target: **1.0.0**
- Runtime baseline: **.NET 10 / C# 14**
- Detailed release notes: [`docs/releases/v0.1.3.md`](docs/releases/v0.1.3.md)

## What Ghost FTP is built for

Ghost FTP is designed as a real desktop file-transfer client rather than a web wrapper. The application keeps the mature dual-pane workflow expected from professional FTP clients while using a cleaner contemporary interface and explicit privacy/security boundaries.

The workstation contains:

- saved servers and session-only Quick Connect profiles;
- Local and Remote file panes;
- upload/download queues;
- queue retry, cancellation and cleanup controls;
- queue-level pause/resume for **new dispatch**;
- connection log and local diagnostics;
- Site Manager;
- configurable retries, concurrency, timeouts and keepalive;
- 29 selectable local languages;
- native Windows and native Linux renderers that share the same protocol/transfer core.

## 0.1.3 Beta highlights

### Transfer management

Ghost FTP 0.1.3 expands the queue from a simple list into a more complete workstation tool.

- Pause queue / Resume queue.
- Retry selected failed or cancelled transfers on Windows.
- Retry all failed transfers on Windows and Linux.
- Cancel selected or all active transfers.
- Clear completed, failed or cancelled history selectively.
- Clear all finished history when one-click cleanup is preferred.
- Inspect detailed transfer state on Windows.
- Copy transfer source or destination paths on Windows.
- Aggregate running throughput and richer queue-state summaries.
- Selectable transfer rows in the Linux X11/XWayland renderer.

**Pause means dispatch pause.** Transfers that are already running continue. Ghost FTP does not claim that arbitrary FTP servers can safely suspend an active byte stream unless resumable-transfer semantics have actually been negotiated.

### UI cleanup

The Windows application, Windows Setup and Linux renderer now use the same canonical Ghost FTP dark-palette contract. Density, radii, borders, hover/focus states and transfer selection are more consistent. The goal is a compact professional file-transfer workstation rather than a collection of oversized dashboard cards.

### Premium Setup

The Windows Setup wizard now has:

- clearer step progress;
- consistent Ghost FTP palette and control density;
- explicit local-only/privacy messaging;
- clearer install/update/uninstall maintenance semantics;
- transactional payload/maintenance-binary language;
- rollback messaging;
- one maintained `GhostFTP-Setup.exe` for install/update/uninstall instead of a generated separate uninstaller executable.

## Supported protocols

Ghost FTP currently implements:

- FTP;
- Explicit FTPS (`AUTH TLS`);
- Implicit FTPS.

Explicit FTPS is the recommended mode where supported.

Ghost FTP does **not** label SFTP as FTP and does not claim SSH/SFTP support in this release line.

## Security model

The shipping client preserves these boundaries:

- fail-closed transport-mode validation;
- strict TLS certificate and hostname validation;
- no silent FTPS-to-FTP downgrade;
- `PBSZ 0` / `PROT P` encrypted-data protection for FTPS;
- required binary `TYPE I` transfer mode;
- passive data connections remain tied to the authenticated control host rather than trusting arbitrary PASV host redirection;
- CR/LF/NUL command-injection guards;
- bounded reply, traversal and transfer-queue limits;
- local path containment checks;
- deletion confirmation and root/path protections;
- bounded retry and concurrency behavior;
- isolated transfer sessions;
- cancellation-safe cleanup.

Read [`SECURITY.md`](SECURITY.md) for the threat and hardening model.

## Privacy

Ghost FTP is designed to run **without application telemetry**.

The application contains no:

- analytics SDK;
- advertising SDK;
- usage telemetry;
- user fingerprinting;
- hidden crash uploader;
- cloud profile synchronization;
- background advertising service;
- hidden product-account requirement;
- automatic background update checker.

Quick Connect credentials are session-only unless the user explicitly saves a profile. Saved-password protection is opt-in. Windows uses the current-user DPAPI boundary; Linux uses AES-256-GCM with local user-private key material.

Read [`PRIVACY.md`](PRIVACY.md) for the complete privacy contract.

## Windows

The Windows renderer is native WPF and targets modern Windows desktop systems with per-monitor DPI awareness and long-path awareness.

### Windows release files

Canonical release names include:

- `setup.exe`
- `portable.exe`

Architecture-specific release assets also include Windows x64 and ARM64 Setup/Portable variants. The release pipeline verifies expected product metadata, version metadata, package identity and SHA-256 outputs before publishing.

The same Setup executable is registered for uninstall/maintenance. Ghost FTP intentionally does not generate a separate `uninstall.exe`.

## Linux

The Linux renderer is a native C# X11/XWayland desktop client using the system `libX11.so.6` ABI. It shares `GhostFTP.Core` and `GhostFTP.Design` with Windows rather than implementing a separate FTP engine.

### Linux release files

Canonical Linux assets include:

- `GhostFTP-linux-x64`
- `GhostFTP-linux-arm64`
- corresponding `.tar.gz` archives;
- versioned archives;
- SHA-256/build information emitted by the release pipeline.

The Linux client follows the same saved-site, Quick Connect, Local/Remote, transfer queue, privacy and transport-security model as Windows.

## Platform scope

Shipping desktop scope is intentionally limited to:

- Windows x64/ARM64;
- Linux x64/ARM64.

Android, iOS and MacCatalyst application targets are not part of this repository's shipping source. The source audit rejects mobile target frameworks/directories so desktop scope cannot drift silently.

## Localization

Ghost FTP provides **29 selectable languages** from local application resources. **English (`en`) is the primary language, default language and final fallback.**

No online translation API is used by the desktop client or Setup. Newly introduced technical strings safely fall back to English when a specific localized override is not yet available.

See [`docs/LOCALIZATION.md`](docs/LOCALIZATION.md).

## Transfer queue semantics

The transfer queue is bounded and uses isolated FTP sessions for transfer work where appropriate.

The queue supports:

- configurable concurrency;
- automatic transient retries;
- cancellation isolation;
- queued/running/retrying/completed/failed/cancelled state;
- progress, speed and ETA reporting;
- queue dispatch pause/resume;
- selective history cleanup.

The 0.1.3 queue pause mechanism waits asynchronously. It does not spin a polling loop and does not interrupt transfers that were already running.

## Demo mode

The built-in Ghost FTP Demo profile is completely local. It opens no external FTP connection and is used by the regression suite to exercise a full file workflow including listing, upload/download, rename, directory traversal, recursive transfer behavior, cleanup and disconnect lifecycle.

## Real-server smoke testing

A separate non-destructive live-server harness can validate connect/PWD/LIST/NOOP/disconnect against explicitly configured credentials without performing server writes. Credential values are supplied through protected CI secrets and are redacted from output.

See [`docs/LIVE-SMOKE-TEST.md`](docs/LIVE-SMOKE-TEST.md).

## Build from source

Requirements:

- .NET 10 SDK;
- Windows for the real WPF renderer/Setup build;
- Linux with X11/XWayland runtime libraries for the native Linux renderer.

Typical validation starts with:

```text
dotnet restore GhostFTP.sln
dotnet build GhostFTP.sln -c Release
```

Official release builds use the repository release scripts and CI gates rather than relying only on a local compile.

## Dependency policy

Shipping projects intentionally contain **zero third-party NuGet `PackageReference` dependencies**. Platform APIs are accessed through the .NET runtime/BCL, WPF on Windows, audited native Windows calls where necessary, and the audited X11 ABI layer on Linux.

Repository audits reject known telemetry/tracking SDK references and private signing-key material.

## CI and release gates

A Ghost FTP Beta source is not treated as release-ready until the relevant Windows/Linux pipeline passes:

- restore/build;
- source/dependency/platform audit;
- final security hardening audit;
- Core self-test;
- complete local Demo workflow self-test;
- transfer queue self-test;
- Windows WPF editable-input/localization smoke test;
- Linux X11/XWayland runtime smoke test;
- authentic Windows UI capture;
- Windows and Linux packaging;
- required artifact verification;
- SHA-256 generation/verification.

## Documentation

- [`docs/releases/v0.1.3.md`](docs/releases/v0.1.3.md) — detailed 0.1.3 Beta release notes
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

The source repository may contain historical engineering documents with older internal version numbers. They are preserved for traceability and do not override the current public version line.

## License

The repository is source-available/proprietary under [`LICENSE`](LICENSE). See [`NOTICE.md`](NOTICE.md) for publisher and legal information.
