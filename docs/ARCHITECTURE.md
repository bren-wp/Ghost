# Ghost FTP architecture

Ghost FTP 1.3 is Windows-first, C#-only and intentionally free of third-party runtime package dependencies.

## Projects

- `GhostFTP.Core` — FTP/FTPS protocol engine, listing parsers, transfer queue, demo session and profile persistence abstractions. It has no WPF dependency.
- `GhostFTP.Design` — shared Ghost FTP identity and Windows desktop visual system used by both the app and setup. It owns `GhostBrand`, palette resources, typography, reusable controls/surfaces, list/header/menu styling and Windows DWM/Mica integration.
- `GhostFTP.App` — Windows desktop client written programmatically in C# using WPF. It references Core + Design. No XAML is required.
- `GhostFTP.Setup` — per-user C# installer/uninstaller. It references Design so setup identity and visual language cannot drift from the application. No MSI, WiX, NSIS or Inno Setup dependency is required.
- `GhostFTP.SelfTest` — dependency-free executable self-tests used by CI.

## Dependency direction

```text
GhostFTP.Core      <- GhostFTP.App
GhostFTP.Design    <- GhostFTP.App
GhostFTP.Design    <- GhostFTP.Setup
GhostFTP.Core      <- GhostFTP.SelfTest
```

`GhostFTP.Core` does not depend on UI projects. `GhostFTP.Design` does not depend on Core, App or Setup. Transport/business logic therefore remains isolated from Windows presentation concerns.

## Single product identity

`GhostBrand` is the source of truth for shipping identity values and the programmatic vector icon:

- display name: `Ghost FTP`;
- assembly/product identifier: `GhostFTP`;
- website: `https://ghostftp.com`;
- source repository URL;
- privacy tagline;
- app/setup WPF `ImageSource`.

Repository visual assets live under `assets/brand` and `assets/readme`. CI requires these assets and rejects the former alternate product/author brand if it reappears in current source, configuration, documentation or artwork metadata.

## Shared design system

`GhostFTP.Design` is the only home for reusable presentation primitives:

- dark/light/system color resources;
- Segoe UI Variable typography;
- rounded buttons, text/password fields and shared surfaces;
- GridView/ListView/ListBox/context-menu visual defaults;
- Ghost FTP vector identity primitive;
- Windows dark titlebar, rounded corners and Mica integration with safe fallback.

App- or setup-specific duplicate theme/chrome classes are prohibited by `audit-source.ps1`.

## Main-window composition

The desktop UI is split into partial classes by responsibility:

- `MainWindow.Core.cs` — state and control ownership;
- `MainWindow.Layout.cs` — visual composition and event wiring;
- `MainWindow.Connection.cs` — connection lifecycle and remote listing;
- `MainWindow.Files.cs` — local/remote navigation and file operations;
- `MainWindow.Transfers.cs` — queue, profiles and settings interactions;
- `MainWindow.Helpers.cs` — reusable workspace actions, formatting and shortcuts;
- `MainWindow.Responsive.cs` — adaptive column sizing.

The visual hierarchy is page-based: sidebar → page header → Quick Connect → Local/Remote panes → transfer queue.

## Dependency policy

There are no `PackageReference` entries and no third-party runtime libraries. The self-contained release bundles the Microsoft .NET Desktop runtime needed to execute Ghost FTP.

The application itself never calls package feeds or a remote update service. Build infrastructure may obtain Microsoft SDK/runtime packs, but those are build-time concerns and not runtime network dependencies of the installed application.

## Network boundary

Only `FtpSession` owns FTP/FTPS sockets. Demo mode uses `DemoFtpSession` and does not open sockets. Transfer jobs use dedicated transfer sessions where required so the browser/control connection is not concurrently corrupted by file transfers.

No analytics, telemetry, background update service or cloud synchronization component exists in the application architecture.

## Persistence boundary

Profiles and settings are local. Installed builds use the current user's local application-data directory; portable builds prefer a `Data` directory next to the executable. Password persistence is opt-in and uses Windows DPAPI for the current Windows user.

Persistence inputs are bounded:

- settings: maximum 1 MiB;
- profiles: maximum 8 MiB;
- profiles: maximum 2,048 entries.

Settings and profiles use temporary files plus atomic replacement/backup recovery when replacing existing files. Corrupted or oversized settings do not crash into unbounded deserialization; profile recovery may use the previous backup.

## Installer boundary

Setup is a per-user executable and embeds the architecture-matching Ghost FTP portable payload. Before replacement it verifies the embedded file exists, exceeds a conservative minimum size and starts with the Windows `MZ` signature.

Existing installations are replaced with `File.Replace` and a temporary backup. A locked/running executable is surfaced as an install failure instead of being silently ignored. Required uninstall deletion and requested user-data removal are verified.

## Release architecture

`build-release.ps1` publishes self-contained single-file Windows builds for `win-x64` and `win-arm64`, embeds the corresponding portable payload into each architecture's setup executable and produces canonical assets:

- `setup.exe`
- `portable.exe`
- `setup-arm64.exe`
- `portable-arm64.exe`

Architecture-explicit copies and `SHA256SUMS.txt` are produced alongside them. CI and Release workflows verify required assets before upload/publishing.
