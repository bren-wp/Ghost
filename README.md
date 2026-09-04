# GhostFTP

**GhostFTP** is a privacy-first FTP/FTPS client for Windows, authored by **Brendigo**.

- Project: https://ghostftp.com
- Author: https://brendigo.com
- Repository: https://github.com/bren-wp/Ghost
- Version: **1.2.0**
- Runtime baseline: **.NET 10 LTS / C# 14**

## What GhostFTP is

GhostFTP is a Windows-first file-transfer workspace for people who want a modern desktop FTP/FTPS client without analytics, cloud accounts, advertisements or background services. The application and installer share the same Windows 11 design system, use a Fluent-inspired visual language and enable Mica/rounded-window integration where Windows supports it.

## Workspace UX

Version 1.2 introduces a complete workspace redesign:

- Clear page hierarchy instead of one oversized toolbar row.
- Labeled Quick Connect fields for host, port, security, username and password.
- FTPS Explicit remains the recommended/default security mode.
- Wider saved-server navigation with dedicated Connect, Edit, Remove and Add actions.
- Responsive `WrapPanel` file toolbars so actions wrap instead of being clipped.
- Consistent dark/light GridView headers, list selection, menus, tooltips and surfaces.
- Local and Remote panes with explicit path navigation, Up and Home/root actions.
- Desktop, Documents and Downloads shortcuts in the local pane.
- Item and selection counts for both panes.
- Context actions for Copy path and Open in File Explorer.
- Local hidden/system item visibility setting.
- Keyboard shortcuts: `F5` refresh, `F2` rename, `Delete` remove, `Ctrl+F` filter and `Ctrl+L` focus path.
- Drag-and-drop uploads from Windows into the remote pane.
- Cleaner transfer queue with running, queued, failed and completed summaries.
- Installer/uninstaller uses the same GhostFTP design system and shows progress/success inline instead of chaining legacy message boxes.

## Core features

- Local / Remote dual-pane file manager.
- Saved FTP/FTPS server profiles.
- Quick Connect.
- FTP, explicit FTPS and implicit FTPS.
- TLS 1.2 / TLS 1.3 with normal certificate validation.
- Upload/download individual files and complete folders.
- Sequential transfer queue with progress, speed and cancellation.
- Separate transfer connections so long transfers do not corrupt the browser control connection.
- Download resume through `.ghostftp.part` where the server supports `REST`.
- Atomic-style uploads through a temporary remote file followed by rename.
- Create, rename and recursively delete remote directories.
- Local filename sanitization and remote-path boundary protection.
- Dark, light and Windows-system appearance modes.
- Fully local Demo server with realistic `public_html`, `assets`, `backups` and `logs` data.
- Per-user setup and standalone portable builds.
- x64 and ARM64 Windows releases.

## Privacy by design

GhostFTP has **no telemetry, no analytics, no ads, no tracking SDK, no crash-report upload and no automatic update checker**.

The application creates network traffic only when you explicitly:

1. connect to an FTP/FTPS server; or
2. open a website link from the About dialog.

Demo mode never opens a network connection. Settings, UI preferences and saved profiles remain local. See [PRIVACY.md](PRIVACY.md).

## No third-party runtime dependencies

The source tree has **zero NuGet `PackageReference` dependencies**. GhostFTP uses only:

- C# and the Microsoft .NET 10 base class libraries;
- Microsoft WPF included with the .NET Desktop runtime;
- Windows APIs already present in Windows for Mica, DPAPI, shortcuts and installer registration.

The release is self-contained, so users do not need to install .NET separately.

## Security

- Explicit FTPS is the default for new server profiles.
- Standard Windows/.NET certificate-chain and hostname validation is used.
- GhostFTP deliberately provides no “accept invalid certificate” switch.
- Plain FTP requires an explicit warning confirmation.
- FTP command arguments reject CR/LF/NUL command-injection characters.
- Remote paths and local extraction destinations are boundary checked.
- Recursive operations use traversal/resource limits.
- Local recursive operations protect against NTFS reparse-point expansion.
- Password persistence is opt-in and protected with Windows DPAPI for the current user.

See [SECURITY.md](SECURITY.md) for the full security model.

## Build

Requirements:

- Windows 11 recommended;
- .NET SDK **10.0.x**.

Build and run self-tests:

```powershell
dotnet restore GhostFTP.sln
dotnet build GhostFTP.sln -c Release
dotnet run --project tests/GhostFTP.SelfTest/GhostFTP.SelfTest.csproj -c Release
```

Create all release packages:

```powershell
./build-release.ps1
```

or run:

```text
build-release.bat
```

The `release` directory and every official tagged GitHub Release contain these simple direct-download names:

```text
setup.exe                 standard Windows x64 installer
portable.exe              standard Windows x64 portable app
setup-arm64.exe           Windows ARM64 installer
portable-arm64.exe        Windows ARM64 portable app
```

Architecture-explicit copies and checksums are published alongside them:

```text
GhostFTP-Setup-win-x64.exe
GhostFTP-Portable-win-x64.exe
GhostFTP-Setup-win-arm64.exe
GhostFTP-Portable-win-arm64.exe
SHA256SUMS.txt
```

CI and Release fail if required executable assets are missing or empty.

## GitHub Actions

- `CI` restores, compiles, performs dependency/privacy/version audits, runs self-tests, publishes x64 + ARM64 packages and verifies canonical `setup.exe` + `portable.exe` on pushes to `main`.
- `Release` repeats those checks before publishing versioned GitHub Release assets.
- Stale CI runs on the same ref are automatically cancelled so only the newest source state consumes release validation time.

## Portable vs installed data

`portable.exe` and `GhostFTP-Portable-*.exe` store profiles/settings in a `Data` directory next to the executable when writable. Installed GhostFTP stores them under the current user's local application-data directory.

Passwords are not saved unless **Remember password** is enabled. Saved passwords are protected by Windows DPAPI for the current Windows user.

## Project structure

```text
src/
  GhostFTP.Core/       FTP/FTPS engine, parsers, demo session, transfer queue
  GhostFTP.Design/     shared Windows 11 visual system and window chrome
  GhostFTP.App/        desktop application, C# programmatic UI, no XAML
  GhostFTP.Setup/      self-contained per-user installer/uninstaller

tests/
  GhostFTP.SelfTest/   zero-dependency CI self-tests

docs/
  ARCHITECTURE.md
  UI-UX.md
```

The application and setup both depend on `GhostFTP.Design`; duplicated app/setup theme and Mica helpers were intentionally removed in 1.2.0.

Copyright © 2026 Brendigo. See [NOTICE.md](NOTICE.md).
