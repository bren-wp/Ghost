<p align="center">
  <img src="assets/readme/ghostftp-hero.svg" alt="Ghost FTP — private FTP and FTPS desktop workspace" width="100%">
</p>

# Ghost FTP

**Ghost FTP** (`GhostFTP`) is a privacy-first FTP/FTPS desktop client built as a professional dual-pane workstation. It combines a dense, familiar FTP workflow with a modern native desktop design, local-only configuration, strict transport boundaries and a dependency-minimal C# codebase.

Ghost FTP is developed and published by **BRENDIGO LTD** (Company number **16545639**), registered office **71–75 Shelton Street, Covent Garden, London, WC2H 9JQ, United Kingdom**.

- Product website: https://ghostftp.com
- Developer / publisher website: https://brendigo.com
- Repository: https://github.com/bren-wp/Ghost
- Current source version: **0.1.0**
- Current release channel: **Beta**
- Informational version: **0.1.0-beta**
- First stable release target: **1.0.0**
- Runtime baseline: **.NET 10 / C# 14**
- Desktop targets: **Windows x64 / ARM64 and Linux x64 / ARM64**
- Shared protocol core: **platform-neutral `net10.0`**
- License: proprietary/source-available; see [LICENSE](LICENSE)

## Current 0.1.0 Beta architecture

Ghost FTP uses one shared FTP/FTPS engine with separate native desktop renderers:

```text
src/GhostFTP.Core      shared FTP/FTPS protocol, transfer queue, models and safety
src/GhostFTP.Design    shared identity, localization and reference visual tokens
src/GhostFTP.App       Windows WPF desktop client; installed and portable modes
src/GhostFTP.Setup     Windows installer / maintenance application
src/GhostFTP.Linux     Linux X11/XWayland desktop client
```

The Linux implementation is not a renamed Windows binary and does not contain a second FTP implementation. Both desktop clients use `GhostFTP.Core` for protocol behavior and transfer semantics.

See [docs/PLATFORM-SUPPORT.md](docs/PLATFORM-SUPPORT.md).

## Approved desktop reference and UI parity

Ghost FTP now has one documented desktop visual contract for the Windows installed client, `portable.exe`, Setup and Linux. Canonical palette and geometry tokens live in `src/GhostFTP.Design/GhostReferencePalette.cs`; platform renderers consume those tokens instead of maintaining unrelated visual identities.

The normal desktop reference uses:

- permanent **292 px** product / saved-sites / privacy rail;
- **38 px** menu row;
- **70 px** global action toolbar;
- Connection Log and Quick Connect in the same upper workspace row;
- Local and Remote file panes side by side;
- full-width Transfers area;
- compact local status/privacy state;
- reference dark appearance as the first-run default.

Windows installed mode and `portable.exe` use the same `GhostFTP.App` renderer, so portable mode does not have a separate UI implementation. Setup is a different workflow but uses the same product palette and control language. Linux reproduces the same workstation hierarchy in the native X11/XWayland renderer while continuing to share the FTP/FTPS core.

The authentic Windows reference capture is rendered deterministically at **1914×907** logical pixels so visual-regression output can be compared against the approved desktop viewport without CI-host DPI scaling changing the image dimensions.

See [docs/UI-PARITY.md](docs/UI-PARITY.md).

## Professional FTP workspace

The 0.1.0 Beta workstation is organized around the workflow users expect from established desktop FTP clients while retaining Ghost FTP's own identity:

1. permanent product / saved-site / privacy rail;
2. **File / View / Sites / Transfers / Tools / Help** menu hierarchy;
3. compact global action toolbar and Remote search;
4. local **Connection Log** beside **Quick Connect**;
5. large **Local / Remote** file panes;
6. full transfer queue with state, progress and cancellation;
7. compact connection/privacy status area.

The goal is information density without UI clutter. File browsing and transfer state receive most of the window instead of oversized decorative cards.

### Site Manager

The Site Manager supports:

- site name;
- host / IP / URL;
- port;
- FTP / FTPS security mode;
- username;
- optional locally protected saved password;
- default remote path;
- immediate connect;
- local save/remove;
- protected built-in Demo profile.

Global timeout, retry, keepalive and transfer-concurrency policies remain centralized in Settings rather than being duplicated into every site profile.

### Connection Log

The main workspace includes a bounded local activity log for useful connection events. It can show startup, profile loading, connection attempts, TLS/plain state, listings, disconnects and failures.

Passwords and protected credential blobs are never written to the log. The log is not transmitted to Ghost FTP, BRENDIGO LTD or an analytics provider.

## Windows

The Windows client uses .NET 10 / WPF and ships for:

- Windows x64;
- Windows ARM64.

Expected official Windows release assets:

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

`setup.exe` and `portable.exe` do not contain different FTP clients. The installed and portable packages both launch the same Windows desktop renderer; portable mode changes only the local data location.

### Windows Authenticode signing

Ghost FTP includes a secure SHA-256 Authenticode release-signing pipeline. The private PFX is supplied only through GitHub Actions secrets and is not committed to the repository.

The signing process:

- imports the release certificate temporarily;
- requires the Code Signing EKU;
- signs final Windows executables with SHA-256;
- optionally timestamps signatures;
- verifies signer identity;
- regenerates SHA-256 checksums after signing;
- removes temporary private-key material;
- requires trusted signature validation for stable releases.

A local self-signed RSA-3072 development certificate can be created for testing signing mechanics, but a self-signed certificate does **not** automatically remove Windows `Unknown Publisher` / SmartScreen warnings for end users.

See [docs/CODE-SIGNING.md](docs/CODE-SIGNING.md).

## Linux

Ghost FTP contains a real Linux desktop client at `src/GhostFTP.Linux`.

The Linux renderer uses the standard X11 client ABI directly. It therefore does not add a third-party NuGet GUI framework. On Wayland desktops it can run through XWayland.

Linux release targets:

- `linux-x64`;
- `linux-arm64`.

Build Linux release packages with:

```bash
chmod +x build-linux-release.sh
./build-linux-release.sh
```

Expected output:

```text
release-linux/GhostFTP-linux-x64
release-linux/GhostFTP-linux-arm64
release-linux/GhostFTP-linux-x64.tar.gz
release-linux/GhostFTP-linux-arm64.tar.gz
release-linux/GhostFTP-0.1.0-beta-linux-x64.tar.gz
release-linux/GhostFTP-0.1.0-beta-linux-arm64.tar.gz
release-linux/SHA256SUMS-linux.txt
release-linux/BUILD-INFO.txt
```

The tarballs include user-local `install.sh` and `uninstall.sh` helpers. Uninstall preserves local Ghost FTP profiles/settings by default; explicit `--purge` removes local application data as well.

### Linux credential protection

Linux saved-password support uses AES-256-GCM with a cryptographically random local key. The key file is restricted to the current user (`0600`) where the filesystem supports Unix permissions.

This prevents plaintext password persistence and provides authenticated tamper detection. The project does not falsely describe this file-based Linux protection as equivalent to Windows DPAPI or a hardware-backed secret store.

### Linux UI and runtime validation

The Linux renderer consumes the shared Ghost reference palette, keeps the permanent left rail / menu / toolbar / Log + Quick Connect / Local + Remote / Transfers hierarchy and adapts field/button density for narrower windows.

CI launches the real renderer under Xvfb, runs the shared Core and transfer-queue tests, builds self-contained x64/ARM64 packages, verifies checksums and launches the packaged x64 binary again under Xvfb. This is a runtime gate, not merely a compile-only platform claim.

## Authentic Windows UI screenshots

Repository Windows screenshots are generated from the **real compiled Ghost FTP WPF client** rather than external mockups.

Documentation capture command:

```powershell
dotnet run --project src/GhostFTP.App/GhostFTP.App.csproj -c Release --no-build -- --capture-ui assets/readme
```

It produces:

```text
assets/readme/ghostftp-client.png
assets/readme/ghostftp-site-manager.png
```

The main client capture uses the fixed **1914×907** reference viewport at 96 DPI. The screenshot workflow rebuilds these files from source. Demo capture is local-only and opens no external FTP connection.

<p align="center">
  <img src="assets/readme/ghostftp-client.png" alt="Ghost FTP 0.1.0 Beta real Windows desktop client" width="100%">
</p>

<p align="center">
  <img src="assets/readme/ghostftp-site-manager.png" alt="Ghost FTP 0.1.0 Beta real Site Manager" width="82%">
</p>

## FTP / FTPS engine

Ghost FTP implements its protocol layer directly on Microsoft .NET networking/cryptography primitives and does not depend on an external FTP NuGet library.

Supported behavior includes:

- FTP;
- explicit FTPS (`AUTH TLS`);
- implicit FTPS;
- TLS 1.2 / TLS 1.3;
- normal certificate-chain and hostname validation;
- no trust-all / ignore-certificate switch;
- EPSV preference with PASV fallback;
- passive data channels bound to the authenticated control host instead of blindly trusting a server-supplied PASV redirect host;
- UTF-8 negotiation where supported;
- MLSD with LIST fallback;
- server-confirmed `CWD` + `PWD` navigation;
- `REST` download resume with `.ghostftp.part` files where supported;
- `SIZE`-assisted length verification where available;
- temporary remote upload paths before commit;
- rollback backup when replacing an existing remote file;
- create, rename and delete files/directories;
- recursive upload/download with traversal budgets;
- remote-root deletion protection;
- bounded reply/listing parsing;
- isolated browsing/control connection and independent queued transfer sessions;
- configurable server-only `NOOP` keepalive;
- connection diagnostics against the user-selected server.

## Transfer queue

The transfer queue exposes operational state instead of acting as a decorative progress list.

Configurable bounds:

- concurrent transfers: 1–8, default 3;
- automatic retries: 0–5;
- connect timeout: 3–120 seconds;
- command timeout: 5–300 seconds;
- transfer idle timeout: 15–3600 seconds;
- keepalive: disabled (`0`) or 15–600 seconds.

Transient socket/timeout and FTP 4xx conditions can retry. Authentication, TLS/certificate, permission and permanent FTP 5xx failures are not blindly retried.

Transfer state can include:

- direction;
- queued / running / retrying / completed / cancelled / failed;
- percentage;
- transferred bytes / known total;
- speed;
- ETA;
- retry count;
- source and destination;
- local failure detail.

These values remain local UI state and are not product telemetry.

## Local file safety

Ghost FTP normalizes local download destinations according to the active operating system.

- Windows reserved device names and invalid filename characters are escaped on Windows.
- Linux-valid names are not unnecessarily rewritten with Windows-only rules.
- resolved download targets are checked to remain under the selected local directory.
- traversal-style remote listing entries are rejected/ignored by the shared protocol layer.

## Localization

English is the primary/default language and guaranteed fallback.

The localization catalog currently includes 29 selectable languages:

English, Croatian, German, French, Spanish, Italian, Portuguese, Dutch, Polish, Czech, Slovak, Slovenian, Hungarian, Romanian, Bulgarian, Greek, Turkish, Ukrainian, Russian, Serbian, Bosnian, Swedish, Danish, Norwegian, Finnish, Japanese, Korean, Simplified Chinese and Traditional Chinese.

Localization data is compiled locally into C# source. Ghost FTP does not call an online translation service or report the selected language.

See [docs/LOCALIZATION.md](docs/LOCALIZATION.md).

## Privacy by design

Ghost FTP contains no:

- application telemetry;
- analytics SDK;
- advertising SDK;
- tracking SDK;
- automatic crash-report upload;
- cloud profile synchronization;
- hidden product network requests;
- automatic background product update checker.

Normal network activity is limited to user-requested FTP/FTPS traffic and documented server-only keepalive/diagnostics. Opening a Ghost FTP or BRENDIGO LTD website is an explicit user action.

Settings, saved sites and credentials remain on the user's device. See [PRIVACY.md](PRIVACY.md) and [SECURITY.md](SECURITY.md).

## Zero third-party PackageReference policy

Shipping projects currently contain no third-party `<PackageReference>` entries. The project intentionally prefers .NET and operating-system primitives for protocol, cryptography, persistence and desktop integration.

This policy is checked by the repository source audit.

## Build from source

### Windows

Requirements:

- Windows 10/11 or supported Windows build environment;
- .NET 10 SDK.

```powershell
dotnet restore GhostFTP.sln
dotnet build GhostFTP.sln -c Release --no-restore
dotnet run --project tests/GhostFTP.SelfTest/GhostFTP.SelfTest.csproj -c Release --no-build
dotnet run --project tests/GhostFTP.QueueSelfTest/GhostFTP.QueueSelfTest.csproj -c Release --no-build
./build-release.ps1
```

### Linux

Requirements:

- Linux x64/ARM64 development environment;
- .NET 10 SDK;
- standard X11 development/runtime environment for desktop execution (`libX11.so.6` runtime).

```bash
dotnet restore src/GhostFTP.Linux/GhostFTP.Linux.csproj
dotnet build src/GhostFTP.Linux/GhostFTP.Linux.csproj -c Release --no-restore
dotnet run --project tests/GhostFTP.SelfTest/GhostFTP.SelfTest.csproj -c Release
chmod +x build-linux-release.sh
./build-linux-release.sh
```

## Versioning

The active public line begins at **0.1.0 Beta** and continues through `0.x.y` Beta releases until the stable quality gate is satisfied. The first stable release is **1.0.0**.

The version reset does not erase previous implementation work or historical internal-development records.

See [docs/VERSIONING.md](docs/VERSIONING.md) and [docs/RELEASE-POLICY.md](docs/RELEASE-POLICY.md).

## Release notes

Current release documentation: [docs/releases/v0.1.0.md](docs/releases/v0.1.0.md).

## Security

Security reports should follow [SECURITY.md](SECURITY.md). Do not include real production FTP credentials in public issues, logs, screenshots or test fixtures.

## Copyright

Copyright © 2026 **BRENDIGO LTD**. All rights reserved.
