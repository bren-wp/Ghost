<p align="center">
  <img src="assets/readme/ghostftp-hero.svg" alt="Ghost FTP — private FTP and FTPS workspace for Windows" width="100%">
</p>

# Ghost FTP

**Ghost FTP** (`GhostFTP`) is a privacy-first FTP/FTPS client for Windows with a modern dual-pane workspace, local-only settings and a dependency-free C# codebase.

- Website: https://ghostftp.com
- Repository: https://github.com/bren-wp/Ghost
- Current source version: **1.3.1**
- Runtime baseline: **.NET 10 LTS / C# 14**
- License: proprietary/source-available; see [LICENSE](LICENSE)

## Download

Official Windows releases publish canonical direct-download assets:

- [setup.exe](https://github.com/bren-wp/Ghost/releases/latest/download/setup.exe) — Windows x64 installer
- [portable.exe](https://github.com/bren-wp/Ghost/releases/latest/download/portable.exe) — Windows x64 portable build
- [setup-arm64.exe](https://github.com/bren-wp/Ghost/releases/latest/download/setup-arm64.exe) — Windows ARM64 installer
- [portable-arm64.exe](https://github.com/bren-wp/Ghost/releases/latest/download/portable-arm64.exe) — Windows ARM64 portable build

Architecture-explicit copies and `SHA256SUMS.txt` are published alongside the canonical names.

## Ghost FTP identity

The application, installer, About dialog, Windows uninstall metadata, documentation, icons and repository artwork use **Ghost FTP / GhostFTP** as the only product identity.

The source audit checks both text content and repository paths for disallowed legacy identity tokens. This prevents another product, author or vendor brand from silently returning to shipping source or documentation.

The official vector icon lives at [`assets/brand/ghostftp-icon.svg`](assets/brand/ghostftp-icon.svg). The app and installer share the same programmatic vector identity from `GhostFTP.Design`; build-time icon generation also embeds the Ghost FTP icon into the actual Windows executables.

## Workspace UX

Ghost FTP 1.3.1 keeps the premium Windows 11 workspace and fixes the editable-input path so forms remain fully usable:

- clearly labeled Host, Port, Security, Username and Password fields;
- native WPF TextBox/PasswordBox editing for reliable caret, focus, selection, keyboard layout, IME and clipboard behavior;
- dedicated WPF editable-input smoke tests in CI;
- saved-server navigation with Add, Connect, Edit and Remove actions;
- responsive file toolbars and dynamically sized GridView columns;
- consistent dark/light surfaces, selections, menus, dropdowns and tooltips;
- Local and Remote panes with explicit path navigation and Up/Home/root actions;
- Desktop, Documents and Downloads shortcuts in the local pane;
- item and selection summaries;
- Copy path and Open in File Explorer actions;
- optional hidden/system item visibility;
- drag-and-drop upload into the remote pane;
- transfer queue with retry, cancel selected, cancel all, clear finished and path-copy actions;
- keyboard shortcuts: `F5`, `F2`, `Delete`, `Ctrl+F`, `Ctrl+L`;
- installer and desktop app use one shared Ghost FTP design system.

See [docs/UI-UX.md](docs/UI-UX.md).

## Core features

- Local / Remote dual-pane file manager.
- Saved FTP/FTPS server profiles.
- Quick Connect.
- FTP, explicit FTPS and implicit FTPS.
- TLS 1.2 / TLS 1.3 with normal Windows/.NET certificate validation.
- Upload/download individual files and complete folders.
- Sequential transfer queue with progress, speed, cancellation and retry.
- Separate transfer connections so long transfers do not desynchronize the browser control connection.
- Download resume through `.ghostftp.part` when the server supports `REST`.
- Safe upload replacement through temporary remote files, rollback backup and final rename.
- Create, rename and recursively delete remote directories.
- Local filename sanitization and remote-path boundary protection.
- Dark, light and Windows-system appearance modes.
- Fully local Demo server with realistic `public_html`, `assets`, `backups` and `logs` data.
- Per-user setup and standalone portable builds.
- x64 and ARM64 releases.

## Privacy by design

Ghost FTP contains **no telemetry, analytics, advertising SDK, tracking SDK, crash-report upload or automatic update checker**.

The application creates network traffic only when you explicitly:

1. connect to an FTP/FTPS server; or
2. open the Ghost FTP website from the About dialog.

Demo mode never opens a network connection. Settings, UI preferences and saved profiles remain local. See [PRIVACY.md](PRIVACY.md).

## No third-party runtime dependencies

The source tree has **zero NuGet `PackageReference` dependencies**. Ghost FTP uses only:

- C# and Microsoft .NET 10 base class libraries;
- Microsoft WPF included with the .NET Desktop runtime;
- Windows APIs already present in Windows for Mica, DPAPI, shortcuts and per-user uninstall registration.

Releases are self-contained, so end users do not need to install .NET separately.

## Security and stability

Security is based on explicit boundaries rather than trust-all or silent-failure switches:

- Explicit FTPS is the default for new server profiles.
- Standard Windows/.NET certificate-chain and hostname validation.
- No “accept invalid certificate” option.
- Plain FTP requires an explicit warning confirmation.
- FTP command arguments reject CR/LF/NUL command-injection characters.
- Control replies and directory-listing payloads are size-bounded.
- Remote paths and local extraction destinations are canonicalized and boundary checked.
- Passive data connections reuse the authenticated control host instead of trusting PASV host redirection.
- Recursive operations use depth and total-entry budgets.
- Local recursive operations protect against NTFS reparse-point expansion.
- Ambiguous FTP `550` results from `MKD` are verified before they can be treated as “already exists”.
- Existing remote files use rollback-safe replacement during upload.
- FTP control-channel protocol/transport failures are not swallowed as optional-command failures.
- Password persistence is opt-in and protected with Windows DPAPI.
- Settings/profile JSON reads are size-bounded; profile count and important string/blob sizes are bounded.
- Saved profile data is normalized before entering application state; the Demo profile is canonicalized and duplicate Demo entries are removed.
- Profile and settings storage use atomic replacement plus backup recovery.
- Transfer queue saturation becomes a visible failed job instead of an unhandled UI exception.
- Installer payloads are checked for minimum size and Windows `MZ` signature before installation.
- Installer updates use atomic `File.Replace` when replacing an existing app.
- Uninstall no longer reports success when the installed executable could not actually be removed.

See [SECURITY.md](SECURITY.md) for the complete security model.

## Build

Requirements:

- Windows 11 recommended;
- .NET SDK **10.0.x**.

Build and run all validation tests:

```powershell
dotnet restore GhostFTP.sln
dotnet build GhostFTP.sln -c Release
dotnet run --project tests/GhostFTP.SelfTest/GhostFTP.SelfTest.csproj -c Release --no-build
dotnet run --project tests/GhostFTP.UiSmoke/GhostFTP.UiSmoke.csproj -c Release --no-build
```

Create all release packages:

```powershell
./build-release.ps1
```

or:

```text
build-release.bat
```

Release output:

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
```

CI and Release fail if required executables are missing or empty.

## GitHub Actions quality gates

`CI` validates every `main` update by performing:

1. restore and Release build;
2. dependency/version/privacy/brand source audit;
3. Core security/correctness self-tests;
4. WPF editable-input smoke tests;
5. x64 + ARM64 portable/setup packaging;
6. canonical `setup.exe` + `portable.exe` verification;
7. artifact upload.

The `Release` workflow repeats the same validation before publishing versioned release assets.

## Portable vs installed data

`portable.exe` and `GhostFTP-Portable-*.exe` store profiles/settings in a `Data` directory next to the executable when writable. Installed Ghost FTP stores data under the current user's local application-data directory.

Passwords are not saved unless **Remember password** is enabled. Saved passwords are protected by Windows DPAPI for the current Windows user.

## Project structure

```text
assets/
  brand/               official Ghost FTP vector branding
  readme/              repository artwork
src/
  GhostFTP.Core/       FTP/FTPS engine, parsers, demo session, transfer queue
  GhostFTP.Design/     shared Ghost FTP visual system, identity and window chrome
  GhostFTP.App/        desktop application, C# programmatic UI, no XAML
  GhostFTP.Setup/      self-contained per-user installer/uninstaller
tests/
  GhostFTP.SelfTest/   Core security and correctness self-tests
  GhostFTP.UiSmoke/    real WPF editable-input regression smoke tests
docs/
  ARCHITECTURE.md
  UI-UX.md
```

Copyright © 2026 Ghost FTP. See [NOTICE.md](NOTICE.md).
