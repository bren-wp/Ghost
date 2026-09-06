# Ghost FTP changelog

This file tracks the active public Ghost FTP version line. The original pre-reset engineering changelog is preserved verbatim in [`docs/HISTORICAL-CHANGELOG.md`](docs/HISTORICAL-CHANGELOG.md); detailed per-release notes remain under [`docs/releases/`](docs/releases/).

## 0.1.2 Beta — 2026-09-06

### Premium workstation cleanup

- Reworked the Windows reference shell around a cleaner professional FTP workstation hierarchy while preserving the authentic compiled application capture path.
- Removed duplicated global **New folder**, **Rename** and **Delete** controls. Those context-sensitive operations now remain in the Local and Remote pane toolbars where the target is explicit.
- Added a real resizable saved-server sidebar with double-click reset.
- Added a vertical splitter between Connection Log / Quick Connect and the file panes.
- Preserved and refined the Local / Remote pane splitter and Transfers queue splitter, with double-click reset behavior.
- Reduced the minimum Windows desktop window size while retaining native minimize, maximize, restore and resize behavior.
- Rebuilt Quick Connect spacing so Host, Port, Security, Username and Password remain aligned without the cramped field collisions visible in earlier Beta captures.
- Compact Windows layouts hide the optional language/search overlay before it can collide with primary toolbar controls.

### Connection stability and security hardening

- Windows now validates host, port, username and password input through the shared `InputGuard` before DNS resolution or FTP command construction; the protocol engine validates the values again at its trust boundary.
- Disconnect teardown clears authoritative `_session` and `_activeOptions` state before QUIT/disposal so keepalive and transfer workers cannot route through a stale session during shutdown.
- Preserved fail-closed FTP security-mode selection, strict explicit TLS negotiation, normal TLS certificate/hostname validation, `PBSZ 0` / `PROT P`, required binary transfer mode and passive-data authenticated-control-host protection.
- Preserved bounded untrusted server input, recursive traversal limits, local path safety checks, cancellation and transfer retry bounds.
- Preserved local-only profile/settings storage, session-only Quick Connect entries and opt-in protected saved passwords.

### Platform, dependency and privacy scope

- Windows and Linux remain the only shipping application platforms.
- Source audit now explicitly rejects Android, iOS, MacCatalyst and known mobile source directories if they are reintroduced.
- Shipping projects continue to contain zero third-party NuGet `PackageReference` entries.
- Source audit continues to reject known telemetry, analytics, tracking and automatic crash-upload SDKs as well as tracked private signing material.
- Linux remains a real native X11/XWayland renderer sharing `GhostFTP.Core`, `GhostFTP.Design`, the same transfer queue, profile model, security semantics and 29-language catalog.

### Localization, Setup and documentation

- English remains the primary language and fallback.
- The local catalog remains **29 selectable languages** across Windows, Linux and Windows Setup; no online translation service is used.
- Windows Setup remains self-contained and per-user with update/uninstall handled by the maintenance Setup executable rather than a separate `uninstall.exe`.
- Advanced the public Beta line to **0.1.2** with file/assembly version **0.1.2.0** and informational version **0.1.2-beta**.
- Added `docs/releases/v0.1.2.md` with the detailed release contract.
- Rewrote the active README and synchronized security, privacy, architecture, installation, localization, UI/UX, UI parity, platform-support and release-policy documentation for 0.1.2.
- The expected public tag is **`v0.1.2-beta`**.

## 0.1.1 Beta — 2026-09-06

### Reliability and Demo regression hardening

- Added a complete local-only `GhostFTP.DemoSelfTest` workflow on both Windows and Linux CI.
- The Demo regression covers connect, diagnostics, PWD/CWD, listing, keepalive, download, upload/download byte-for-byte round trip, rename, create/delete directory, recursive directory round trip, cleanup, root-delete protection, disconnect reset and rejection of post-disconnect operations.
- Added conflict protection so a file upload cannot replace an existing directory and a directory upload cannot replace an existing file in Demo state.
- `DemoFtpSession` now explicitly exposes local diagnostics/keepalive behavior and resets its working directory on disconnect/disposal.
- Demo mode remains fully local and opens no external FTP, telemetry or analytics connection.

### Transactional Windows Setup hardening

- Setup now stages and validates both the application payload and maintenance `GhostFTP-Setup.exe` candidate before changing an active installation.
- Candidate identity validation requires a Windows executable, ProductName **Ghost FTP**, CompanyName **BRENDIGO LTD** and the exact active file version.
- Added file-version downgrade protection for both the application and maintenance Setup paths.
- Existing application and maintenance Setup binaries keep independent rollback copies until all later install stages succeed.
- A later install failure attempts to restore both previous binaries; a first-time partial installation removes newly committed binaries during rollback.
- Uninstall removes stale application/Setup transaction files.
- `QuietUninstallString` remains intentionally absent until a genuine silent uninstall path exists.

### Release, documentation and repository cleanup

- Advanced the public Beta line to **0.1.1** with assembly/file version **0.1.1.0** and informational version **0.1.1-beta**.
- Added `docs/releases/v0.1.1.md` as the authoritative detailed release body.
- Updated README, SECURITY, PRIVACY, architecture, installation, localization, UI/UX, UI parity, platform support, versioning, release policy and legal notice for the 0.1.1 line.
- Kept the authentic README product image generated from the real compiled WPF application; no mock/generated product screenshot is substituted for the canonical app capture.
- Made the hardening audit derive the required release marker/tag from active `VERSION` and `RELEASE_CHANNEL` instead of hardcoding 0.1.0.
- Removed obsolete release-trigger marker files from older public/internal releases while preserving their engineering documentation/history.
- Moved the original cumulative pre-reset changelog content verbatim to `docs/HISTORICAL-CHANGELOG.md` so current public history stays readable without deleting prior work.

### Security and privacy invariants retained

- Preserved fail-closed FTP security-mode selection, strict 2xx `AUTH TLS`, normal TLS certificate/hostname validation and no FTPS-to-FTP downgrade.
- Preserved `PBSZ 0` / `PROT P`, required `TYPE I`, passive-data authenticated-control-host protection, CR/LF/NUL command guards and bounded untrusted server input.
- Preserved bounded isolated transfer sessions, cancellation/retry safety, root/path protections and server-only `NOOP` keepalive.
- Preserved zero third-party NuGet `PackageReference` entries in shipping projects.
- Preserved no application telemetry, analytics, advertising, tracking SDK, automatic crash upload, cloud profile sync or hidden background update checks.
- Windows saved passwords remain opt-in DPAPI; Linux saved passwords remain opt-in AES-256-GCM with local per-user key material.

### Platform and release gates

- Windows remains the native WPF renderer with x64/ARM64 Setup and portable packages.
- Linux remains the native X11/XWayland renderer with x64/ARM64 self-contained packages and system `libX11.so.6` ABI.
- Both platforms continue to share `GhostFTP.Core` and `GhostFTP.Design` and the same premium workstation hierarchy/security/privacy semantics.
- The official Beta tag is `v0.1.1-beta`.
- Public release is complete only after the exact version source passes Windows/Linux build, audit, Core, Demo, queue, WPF/X11 runtime, authentic capture, packaging and checksum gates and the expected assets are attached to the GitHub Release.

## 0.1.0 Beta — 2026-09-05

### Public version-line reset

- Restarted the public Ghost FTP version sequence at **0.1.0 Beta** without removing completed application, protocol, UI, Setup, localization, transfer, security, privacy, testing or screenshot work.
- Added root `VERSION` / `RELEASE_CHANNEL` metadata and defined the 0.x Beta line with **1.0.0** reserved as the first stable release.
- Synchronized .NET and Windows manifest metadata to the new public line.
- Preserved canonical Windows `setup.exe` / `portable.exe` naming and introduced the public `v0.1.0-beta` GitHub prerelease.
- Preserved Windows/Linux native desktop renderers, shared FTP/FTPS core, 29-language localization, premium workstation UI, strict FTPS validation, bounded transfers and privacy-first local persistence.
- Established authentic repository screenshots produced from the real compiled application rather than conceptual mockups.

For the full original engineering history that led to the public 0.x line, see [`docs/HISTORICAL-CHANGELOG.md`](docs/HISTORICAL-CHANGELOG.md).
