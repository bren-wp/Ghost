<p align="center">
  <img src="assets/readme/ghostftp-client.png" alt="Ghost FTP 0.1.2 Beta — authentic production Windows desktop client" width="100%">
</p>

<p align="center"><strong>Authentic application capture generated from the compiled Ghost FTP desktop client — not a mockup, illustration or generated UI.</strong></p>

# Ghost FTP

**Ghost FTP** is a privacy-first native FTP/FTPS desktop workstation for Windows and Linux. It is designed around a professional dual-pane file workflow, strict transport-security boundaries, local-only profile storage, bounded parallel transfers and a dependency-minimal C#/.NET codebase.

Ghost FTP is developed and published by **BRENDIGO LTD** (Company number **16545639**), 71–75 Shelton Street, Covent Garden, London, WC2H 9JQ, United Kingdom.

Current source version: **0.1.2**  
Release channel: **Beta**  
Informational version: **0.1.2-beta**  
Runtime baseline: **.NET 10 / C# 14**

Detailed release notes: [`docs/releases/v0.1.2.md`](docs/releases/v0.1.2.md)

## What Ghost FTP is

Ghost FTP is a real desktop FTP client, not a web wrapper. The shipping application supports normal server credentials and the core FTP/FTPS workflow expected from a serious workstation client:

- saved servers and Quick Connect;
- FTP, explicit FTPS and implicit FTPS;
- Local and Remote directory browsing;
- upload and download of files and directories;
- create folder, rename and delete operations;
- background transfer queue with cancellation, retry and progress;
- persistent Local/Remote pane ratio and transfer-panel height;
- local filtering and remote search/filter;
- server diagnostics and connection log;
- session keepalive;
- built-in local Demo mode for deterministic testing without network access.

Context-specific file actions live in the Local and Remote pane toolbars. Ghost FTP 0.1.2 intentionally removes duplicate global **New folder**, **Rename** and **Delete** buttons because duplicate target-sensitive actions made the workstation harder to understand and easier to misuse.

## 0.1.2 workstation design

The desktop layout is built around the same hierarchy on Windows and Linux:

1. saved-server sidebar;
2. menu and primary connection/transfer toolbar;
3. Connection Log and Quick Connect;
4. Local and Remote file panes;
5. Transfers queue;
6. local connection status.

On Windows, the sidebar, Connection Log / Quick Connect area, Local/Remote split and Transfers queue are draggable. Double-clicking a splitter restores its default size. The normal window can be resized, minimized, maximized and restored using the operating system window controls.

Quick Connect keeps **Host**, **Port**, **Security**, **Username** and **Password** aligned on one compact row at normal workstation sizes. Session-only privacy controls and Connect/Disconnect are separated from credential fields. On compact windows the optional language/search overlay hides before it can overlap primary toolbar actions; language remains available in Settings and the Remote pane still has its own filter.

The canonical documentation capture remains **1914 × 907** so visual regressions can be compared against a stable production-rendered reference.

## Security model

Ghost FTP treats FTP servers, directory listings, paths and credentials as untrusted input. Important invariants include:

- explicit FTPS must successfully negotiate `AUTH TLS`; a refused TLS request is not downgraded to FTP;
- normal TLS certificate-chain and hostname validation is retained;
- encrypted sessions require `PBSZ 0` and `PROT P` so data channels are protected;
- transfer paths require binary mode (`TYPE I`);
- passive data connections are constrained to the authenticated control host rather than blindly trusting an arbitrary server-advertised address;
- host, port, username, password, remote path and remote name values are bounded and command-control characters are rejected;
- recursive traversal has depth/entry limits;
- local path operations are canonicalized and checked before destructive work;
- plain FTP remains available for compatibility but requires an explicit warning because credentials and file data are not encrypted.

Windows connection teardown clears authoritative session routing state before QUIT/disposal, reducing the chance that a keepalive or transfer worker observes a stale connection during disconnect.

See [`SECURITY.md`](SECURITY.md) for the security contract.

## Privacy

Ghost FTP does not require a Ghost FTP account. Application configuration and server profiles stay local to the device/user context.

The shipping application contains:

- no telemetry SDK;
- no analytics SDK;
- no advertising SDK;
- no tracking SDK;
- no automatic crash-report upload;
- no cloud profile synchronization;
- no hidden background update check.

Session-only Quick Connect entries are kept in memory only and never persist passwords. Saved passwords are opt-in: Windows uses current-user DPAPI protection; Linux uses local AES-256-GCM protection with per-user key material.

See [`PRIVACY.md`](PRIVACY.md).

## 29 local interface languages

English (`en`) is the primary language and fallback. Ghost FTP ships **29 selectable languages** from local source data:

English, Croatian, German, French, Spanish, Italian, Portuguese, Dutch, Polish, Czech, Slovak, Slovenian, Hungarian, Romanian, Bulgarian, Greek, Turkish, Ukrainian, Russian, Serbian, Bosnian, Swedish, Danish, Norwegian, Finnish, Japanese, Korean, Chinese (Simplified), and Chinese (Traditional).

The same local catalog is consumed by Windows, Linux and Windows Setup. Ghost FTP does not call an online translation service. Technical text that does not yet have a locale override falls back to English rather than failing startup or contacting a network service.

See [`docs/LOCALIZATION.md`](docs/LOCALIZATION.md).

## Windows release files

The GitHub Release workflow publishes self-contained Windows x64 and ARM64 builds. Canonical names are retained for easy download:

- `setup.exe` — x64 Windows Setup and maintenance executable;
- `portable.exe` — x64 Windows portable executable;
- `setup-arm64.exe` — ARM64 Setup;
- `portable-arm64.exe` — ARM64 portable executable;
- `GhostFTP-Setup-win-x64.exe` / `GhostFTP-Portable-win-x64.exe`;
- `GhostFTP-Setup-win-arm64.exe` / `GhostFTP-Portable-win-arm64.exe`;
- `SHA256SUMS.txt` and `SIGNING.txt`.

`setup.exe` installs per-user without requiring a separate uninstaller executable. The installed maintenance copy `GhostFTP-Setup.exe` handles update and uninstall. Setup validates staged application and maintenance executables, rejects version downgrades, and keeps rollback copies until later installation steps complete.

Portable builds are self-contained and detect portable mode from the executable identity or `portable.flag`; portable profile/settings data lives beside the executable in `Data` rather than being written to the normal installed profile location.

See [`docs/INSTALLATION.md`](docs/INSTALLATION.md).

## Linux release files

Linux is a real native X11/XWayland renderer sharing the same `GhostFTP.Core` protocol engine and `GhostFTP.Design` product/localization layer. The release workflow publishes:

- `GhostFTP-linux-x64`;
- `GhostFTP-linux-arm64`;
- x64 and ARM64 `.tar.gz` archives;
- versioned archive aliases and release hashes.

The Linux executable is self-contained for .NET runtime purposes and uses the system `libX11.so.6` ABI for its native window integration. No third-party GUI package is added as a project dependency.

See [`docs/PLATFORM-SUPPORT.md`](docs/PLATFORM-SUPPORT.md) and [`docs/UI-PARITY.md`](docs/UI-PARITY.md).

## Windows + Linux only

The active product line intentionally ships only native desktop applications for **Windows and Linux**. Android, iOS and MacCatalyst application targets are not part of this repository's shipping scope. The source audit rejects mobile target frameworks and known mobile source directories if they are reintroduced.

There is no web/browser client in the shipping product line.

## Dependency policy

Shipping projects intentionally contain **zero third-party NuGet `PackageReference` entries**. Core functionality uses the .NET runtime/framework APIs plus audited operating-system facilities needed by the native renderers and credential stores.

Repository audits fail if a third-party `PackageReference`, known telemetry/tracking SDK, tracked private signing key, unsupported mobile target, or forbidden non-C# shipping source is introduced.

## Build from source

Prerequisite: .NET 10 SDK.

Windows PowerShell:

```powershell
dotnet restore GhostFTP.sln
dotnet build GhostFTP.sln -c Release
```

Run Windows client:

```powershell
dotnet run --project src/GhostFTP.App/GhostFTP.App.csproj -c Release
```

Run Linux client on a Linux/X11 or XWayland session:

```bash
dotnet run --project src/GhostFTP.Linux/GhostFTP.Linux.csproj -c Release
```

Windows release packages are produced by `build-release.ps1`; Linux packages are produced by `build-linux-release.sh`. Release publication itself is intentionally gated through GitHub Actions so the exact tagged/source version is built, tested, captured and checksummed in controlled runners.

## Testing

The repository includes deterministic test executables and CI gates for:

- Core FTP/FTPS behavior and security boundaries;
- complete local Demo workflow on Windows and Linux;
- parallel transfer queue behavior;
- WPF editable controls and localization smoke coverage;
- Linux renderer/runtime behavior;
- source/dependency/privacy/platform audit;
- final hardening audit;
- authentic production UI capture.

Real-server testing is optional and deliberately non-destructive. Credentials are supplied through secrets/environment variables and must never be committed or printed. The harness performs connection, PWD/listing and keepalive checks without uploads, renames or deletes. See [`docs/LIVE-SMOKE-TEST.md`](docs/LIVE-SMOKE-TEST.md).

## Authentic screenshots

`assets/readme/ghostftp-client.png` and `assets/readme/ghostftp-site-manager.png` are produced from the compiled Windows application through the documentation-capture path. Release CI verifies the main capture dimensions and minimum file size so a conceptual mockup cannot silently replace the canonical product screenshot.

After a verified UI change, the repository screenshot refresh workflow updates these assets from the production renderer.

## Documentation

- [`docs/releases/v0.1.2.md`](docs/releases/v0.1.2.md) — current detailed release notes
- [`CHANGELOG.md`](CHANGELOG.md) — active public version history
- [`docs/HISTORICAL-CHANGELOG.md`](docs/HISTORICAL-CHANGELOG.md) — preserved pre-reset engineering history
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — architecture and trust boundaries
- [`docs/UI-UX.md`](docs/UI-UX.md) — workstation interaction model
- [`docs/UI-PARITY.md`](docs/UI-PARITY.md) — Windows/Linux parity contract
- [`docs/LOCALIZATION.md`](docs/LOCALIZATION.md) — 29-language catalog/fallback rules
- [`docs/INSTALLATION.md`](docs/INSTALLATION.md) — Setup, portable and uninstall behavior
- [`docs/PLATFORM-SUPPORT.md`](docs/PLATFORM-SUPPORT.md) — supported desktop platforms
- [`docs/RELEASE-POLICY.md`](docs/RELEASE-POLICY.md) — publication gates and canonical assets
- [`SECURITY.md`](SECURITY.md) — security policy
- [`PRIVACY.md`](PRIVACY.md) — privacy/data handling

## Release status

Ghost FTP 0.x remains Beta. The first stable target is **1.0.0**. A GitHub prerelease is considered complete only when the exact source version passes Windows/Linux build, test, audit, authentic capture, packaging and checksum gates and the expected release assets are attached to the matching tag.

Expected 0.1.2 Beta tag: **`v0.1.2-beta`**.
