<p align="center">
  <img src="assets/readme/ghostftp-client.png" alt="Ghost FTP 0.1.0 Beta — authentic production Windows desktop client" width="100%">
</p>

<p align="center"><strong>Authentic application capture generated from the compiled Ghost FTP desktop client — not a mockup, illustration or generated UI.</strong></p>

# Ghost FTP

**Ghost FTP** (`GhostFTP`) is a privacy-first native FTP/FTPS desktop workstation for Windows and Linux. It combines a dense dual-pane file workflow, local-only configuration, explicit transport-security boundaries, bounded background transfers and a dependency-minimal C#/.NET codebase.

Ghost FTP is developed and published by **BRENDIGO LTD** (Company number **16545639**), 71–75 Shelton Street, Covent Garden, London, WC2H 9JQ, United Kingdom.

- Product: https://ghostftp.com
- Publisher: https://brendigo.com
- GitHub Releases: https://github.com/bren-wp/Ghost/releases
- Current source version: **0.1.0**
- Current release channel: **Beta**
- Informational version: **0.1.0-beta**
- First stable target: **1.0.0**
- Runtime baseline: **.NET 10 / C# 14**
- Desktop targets: **Windows x64 / ARM64 and Linux x64 / ARM64**
- License: proprietary/source-available; see [LICENSE](LICENSE)

## One product, one FTP/FTPS core, two native desktop renderers

```text
src/GhostFTP.Core      shared FTP/FTPS protocol, safety, models and transfer queue
src/GhostFTP.Design    shared brand, localization and reference UI contract
src/GhostFTP.App       Windows WPF desktop renderer; installed and portable modes
src/GhostFTP.Setup     Windows Setup / maintenance application
src/GhostFTP.Linux     Linux X11/XWayland desktop renderer
```

Windows and Linux use the same `GhostFTP.Core` protocol implementation and the same Ghost reference palette, product identity, localization catalog and workstation hierarchy. The native rendering technologies differ — WPF on Windows and X11/XWayland on Linux — so OS font rasterization and window chrome can differ, but the product structure, core actions, colors, privacy policy and transfer semantics are intentionally kept aligned.

The approved workstation structure is:

```text
Permanent product / saved-sites / privacy rail
→ File / View / Sites / Transfers / Tools / Help
→ compact global action toolbar + Remote search
→ Connection Log + Quick Connect
→ Local + Remote file panes
→ Transfers
→ compact connection/privacy status
```

Canonical normal-desktop geometry uses a **292 px** left rail, **38 px** menu, **70 px** toolbar and the shared dark Ghost FTP palette. See [docs/UI-PARITY.md](docs/UI-PARITY.md).

## Windows release files

The official Windows release workflow produces and verifies:

```text
setup.exe
portable.exe
setup-arm64.exe
portable-arm64.exe
GhostFTP-Setup-win-x64.exe
GhostFTP-Portable-win-x64.exe
GhostFTP-Setup-win-arm64.exe
GhostFTP-Portable-win-arm64.exe
SHA256SUMS.txt
SIGNING.txt
```

`portable.exe` and the installed application are the same `GhostFTP.App` renderer. Portable mode changes only where local data is stored; it does not switch to a different FTP engine, UI implementation or privacy policy.

The release workflow validates file versions and SHA-256 manifests after final packaging/signing. Stable releases additionally require a trusted Authenticode signature. A self-signed development certificate is intentionally **not** represented as a SmartScreen or `Unknown Publisher` solution. See [docs/CODE-SIGNING.md](docs/CODE-SIGNING.md).

## Linux release files

The Linux renderer is a real native desktop client in `src/GhostFTP.Linux`. It uses the system X11 client ABI directly and can run on Wayland desktops through XWayland without adding a third-party NuGet GUI framework.

Build it with:

```bash
chmod +x build-linux-release.sh
./build-linux-release.sh
```

Verified release outputs include:

```text
GhostFTP-linux-x64
GhostFTP-linux-arm64
GhostFTP-linux-x64.tar.gz
GhostFTP-linux-arm64.tar.gz
GhostFTP-0.1.0-beta-linux-x64.tar.gz
GhostFTP-0.1.0-beta-linux-arm64.tar.gz
SHA256SUMS-linux.txt
BUILD-INFO.txt
```

Tarballs contain user-local install/uninstall helpers. Normal uninstall preserves profiles/settings; explicit `--purge` removes local Ghost FTP data.

## Authentic application screenshots

Repository UI screenshots are rebuilt from the real compiled Windows client. The capture path is:

```powershell
dotnet run --project src/GhostFTP.App/GhostFTP.App.csproj -c Release --no-build -- --capture-ui assets/readme
```

The production MainWindow capture is deterministic at **1914×907** logical pixels / 96 DPI. Capture mode uses the local-only built-in Demo session and makes no external FTP connection.

<p align="center">
  <img src="assets/readme/ghostftp-client.png" alt="Ghost FTP production workstation" width="100%">
</p>

<p align="center">
  <img src="assets/readme/ghostftp-site-manager.png" alt="Ghost FTP production Site Manager" width="82%">
</p>

The old decorative README hero has been removed from the active documentation so the first image users see is the actual application.

## FTP / FTPS security model

Ghost FTP implements its FTP layer directly on Microsoft .NET networking and cryptography primitives. Shipping projects currently contain zero third-party `<PackageReference>` entries.

Current protocol boundaries include:

- FTP, explicit FTPS (`AUTH TLS`) and implicit FTPS;
- invalid/unknown security enum values fail closed rather than falling through to plain FTP;
- TLS 1.2 / TLS 1.3 with normal certificate-chain and hostname validation;
- no trust-all or certificate-bypass option;
- plain FTP requires an explicit warning/confirmation in both desktop clients;
- explicit TLS requires a positive 2xx `AUTH TLS` response before TLS upgrade;
- protected FTPS data channels use `PROT P`;
- every transfer requires successful binary mode (`TYPE I`) before file data is sent/received;
- EPSV preference with PASV fallback;
- PASV data connects to the authenticated control host instead of trusting an arbitrary PASV redirect host;
- UTF-8 negotiation where supported;
- MLSD with LIST fallback;
- bounded control replies, listing payloads, traversal depth and recursive item counts;
- control-character/CRLF/NUL rejection for FTP command arguments;
- canonical remote-path handling and remote-root deletion protection;
- server-confirmed `CWD` + `PWD` navigation;
- `REST` resume using `.ghostftp.part` files where supported;
- `SIZE`-assisted transfer length verification where available;
- temporary remote upload paths plus rollback backup when replacing a file;
- isolated real transfer sessions rather than sharing the browsing control channel;
- server-only `NOOP` keepalive, configurable or disableable, on Windows and Linux;
- no silent credential-based reconnect after a failed keepalive.

See [SECURITY.md](SECURITY.md).

## Transfer reliability

The queue is bounded and operational rather than decorative:

- concurrent transfers: **1–8**, default **3**;
- automatic retries: **0–5**;
- queue capacity: **4,096** jobs;
- independent cancellation per job;
- transient-only retry policy;
- progress, transferred bytes, speed, ETA, retries and timestamps;
- isolated FTP/FTPS sessions for real queued transfers;
- queue saturation becomes visible failed state rather than an unhandled UI exception.

Authentication, permission, permanent FTP 5xx and TLS/certificate failures are not blindly retried.

## Credential and local-data privacy

Ghost FTP does not require an account and does not synchronize profiles to a cloud service.

**Windows:** saved passwords are opt-in and protected using CurrentUser DPAPI.

**Linux:** saved passwords use AES-256-GCM with a cryptographically random local 256-bit key. The key file is restricted to the current user (`0600`) where Unix permissions are supported. This protects persisted secrets from plaintext disclosure/tampering but is not falsely described as equivalent to a hardware-backed keyring against compromise of the same OS user account.

Session-only Quick Connect entries created by **Keep in this tab** are excluded from persisted profile JSON and disappear with the session.

Ghost FTP contains no application telemetry, analytics SDK, advertising SDK, tracking SDK, automatic crash-report uploader, hidden cloud profile sync or automatic background product-update checker. Normal runtime network activity is limited to the FTP/FTPS server the user explicitly selected, server-only keepalive/diagnostics on that connection and websites the user explicitly opens.

See [PRIVACY.md](PRIVACY.md).

## Installer hardening

The Windows Setup path is per-user and validates an embedded application payload before committing it. Current validation checks minimum size, Windows executable signature, Ghost FTP product identity, **BRENDIGO LTD** publisher identity and exact file version. Existing application replacement keeps a rollback copy until all install stages finish so a later Setup failure does not intentionally strand a half-updated app.

The Installed Apps entry exposes the real interactive uninstall command only. Ghost FTP does not claim a `QuietUninstallString` until a genuine non-interactive uninstall mode exists.

## Localization

English is the primary/default language and guaranteed fallback. The local compiled catalog currently exposes 29 languages:

English, Croatian, German, French, Spanish, Italian, Portuguese, Dutch, Polish, Czech, Slovak, Slovenian, Hungarian, Romanian, Bulgarian, Greek, Turkish, Ukrainian, Russian, Serbian, Bosnian, Swedish, Danish, Norwegian, Finnish, Japanese, Korean, Simplified Chinese and Traditional Chinese.

No online translation service is contacted at runtime. See [docs/LOCALIZATION.md](docs/LOCALIZATION.md).

## Secure live-server smoke testing

Real FTP credentials are **never committed to this repository, README, test fixtures or Actions logs**. The optional live smoke harness reads connection data only from process environment / GitHub Actions secrets and performs a non-destructive connect/PWD/LIST/keepalive/disconnect sequence.

The normal CI suite therefore remains deterministic and credential-free. See [docs/LIVE-SMOKE-TEST.md](docs/LIVE-SMOKE-TEST.md) before testing a real server.

## Build and verification

### Windows

```powershell
dotnet restore GhostFTP.sln
dotnet build GhostFTP.sln -c Release --no-restore
./audit-source.ps1
dotnet run --project tests/GhostFTP.SelfTest/GhostFTP.SelfTest.csproj -c Release --no-build
dotnet run --project tests/GhostFTP.QueueSelfTest/GhostFTP.QueueSelfTest.csproj -c Release --no-build
dotnet run --project tests/GhostFTP.UiSmoke/GhostFTP.UiSmoke.csproj -c Release --no-build
./build-release.ps1
```

### Linux

```bash
dotnet restore src/GhostFTP.Linux/GhostFTP.Linux.csproj
dotnet build src/GhostFTP.Linux/GhostFTP.Linux.csproj -c Release --no-restore
dotnet run --project tests/GhostFTP.SelfTest/GhostFTP.SelfTest.csproj -c Release --no-restore
chmod +x build-linux-release.sh
./build-linux-release.sh
```

CI additionally launches the real Linux renderer under Xvfb and smoke-tests the final packaged Linux x64 binary.

## Release policy

All `0.x.y` builds remain **Beta**. The first stable release is **1.0.0** and must satisfy the stable quality/signing gates. The release workflow publishes Windows x64/ARM64 and Linux x64/ARM64 artifacts only after the required build, audit, self-test, packaging and checksum gates pass.

Current notes: [docs/releases/v0.1.0.md](docs/releases/v0.1.0.md). See also [docs/RELEASE-POLICY.md](docs/RELEASE-POLICY.md), [docs/PLATFORM-SUPPORT.md](docs/PLATFORM-SUPPORT.md) and [docs/VERSIONING.md](docs/VERSIONING.md).

## Security reports

Follow [SECURITY.md](SECURITY.md). Never post real FTP passwords, private keys, access tokens or sensitive server credentials in a public issue.

## Copyright

Copyright © 2026 **BRENDIGO LTD**. All rights reserved.
