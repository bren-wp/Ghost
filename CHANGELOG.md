# Ghost FTP changelog

This file tracks the active public Ghost FTP version line. The original pre-reset engineering changelog is preserved verbatim in [`docs/HISTORICAL-CHANGELOG.md`](docs/HISTORICAL-CHANGELOG.md); detailed per-release notes remain under [`docs/releases/`](docs/releases/).

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
