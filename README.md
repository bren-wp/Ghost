# GhostFTP

**GhostFTP** is a premium, privacy-first FTP/FTPS client for Windows, authored by **Brendigo**.

- Project: https://ghostftp.com
- Author: https://brendigo.com
- Repository: https://github.com/bren-wp/Ghost
- Version: **1.1.0**
- Runtime baseline: **.NET 10 LTS / C# 14**

## Why GhostFTP

GhostFTP is built for people who want a modern Windows 11 file-transfer experience without analytics, cloud accounts or background services. The desktop app uses a Fluent-inspired dual-pane workflow with a Windows 11 Mica backdrop where supported.

### Core features

- Local / Remote dual-pane file manager.
- Saved FTP/FTPS server profiles.
- Quick Connect.
- FTP, explicit FTPS and implicit FTPS.
- TLS 1.2 / TLS 1.3 with normal certificate validation.
- Upload/download files and complete folders.
- Sequential transfer queue with progress, speed and cancellation.
- Download resume through `.ghostftp.part` where the server supports `REST`.
- Atomic-style uploads through a remote temporary file followed by rename.
- Create, rename and recursively delete remote directories.
- Local filename sanitization and remote-path boundary protection.
- Windows 11 Mica/rounded-corner integration with safe fallback.
- Dark, light and system appearance modes.
- Fully local Demo server with realistic `public_html`, `assets`, `backups` and `logs` data.
- Per-user setup and standalone portable builds.
- x64 and ARM64 Windows releases.

## Privacy by design

GhostFTP has **no telemetry, no analytics, no ads, no tracking SDK, no crash-report upload and no automatic update checker**.

The application creates network traffic only when you explicitly:

1. connect to an FTP/FTPS server; or
2. click a website link in the About dialog.

Demo mode never opens a network connection. See [PRIVACY.md](PRIVACY.md).

## No third-party runtime dependencies

The application source has **zero NuGet `PackageReference` dependencies**. GhostFTP uses only:

- C# and the Microsoft .NET 10 base class libraries;
- Microsoft WPF included with the .NET Desktop runtime;
- Windows APIs already present in Windows for Mica, DPAPI, shortcuts and installer registration.

The release is self-contained so users do not need to install .NET separately.

## Security

- Traversal and resource limits protect recursive operations from malicious or cyclic server listings.
- Local recursive uploads skip NTFS reparse points/junctions so a selected folder cannot silently expand outside its tree.
- Plain FTP always requires an explicit warning confirmation; FTPS Explicit remains the default.

New profiles default to **explicit FTPS**. GhostFTP deliberately does not provide an “accept invalid certificate” switch. See [SECURITY.md](SECURITY.md) for the full security model.

## Build

Requirements for building:

- Windows 11 recommended;
- .NET SDK **10.0.x** (latest stable feature band recommended).

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

or double-click/run:

```text
build-release.bat
```

The `release` directory and every official tagged GitHub Release contain the simple direct-download names:

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

CI and the Release workflow fail if `setup.exe` or `portable.exe` are missing or empty.

## GitHub Actions

- `CI` builds the solution, audits privacy/dependencies, runs self-tests, builds x64 + ARM64 packages and verifies `setup.exe` + `portable.exe` on every push to `main`.
- `Release` repeats those checks and publishes the verified executables plus SHA-256 checksums for version tags.

## Portable vs installed data

`portable.exe` and `GhostFTP-Portable-*.exe` store profiles/settings in a `Data` folder next to the executable. Installed GhostFTP stores them under the current user's local application-data directory.

Passwords are not saved unless **Remember password** is enabled. Saved passwords are protected with Windows DPAPI for the current Windows user.

## Project structure

```text
src/
  GhostFTP.Core/       FTP/FTPS engine, demo session, transfer queue
  GhostFTP.App/        premium Windows desktop app, C# UI, no XAML
  GhostFTP.Setup/      self-contained per-user C# installer

tests/
  GhostFTP.SelfTest/   zero-dependency CI self-tests

docs/
  ARCHITECTURE.md
```

Copyright © 2026 Brendigo. See [NOTICE.md](NOTICE.md).
