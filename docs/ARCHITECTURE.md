# Architecture

GhostFTP 1.2 is Windows-first, C#-only and intentionally dependency-light.

## Projects

- `GhostFTP.Core` — FTP/FTPS protocol engine, listing parsers, transfer queue, demo session and persistence abstractions. It has no WPF dependency.
- `GhostFTP.Design` — shared Windows desktop visual system used by both the app and setup. It owns palette resources, typography, reusable controls/surfaces, list/header/menu styling and Windows 11 DWM/Mica integration.
- `GhostFTP.App` — Windows desktop client written programmatically in C# using WPF. It references Core + Design. No XAML is required.
- `GhostFTP.Setup` — per-user C# installer/uninstaller. It references Design so installation UI cannot drift from the application visual language. No MSI, WiX, NSIS or Inno Setup dependency is required.
- `GhostFTP.SelfTest` — dependency-free executable self-tests used by CI.

## Dependency direction

```text
GhostFTP.Core      <- GhostFTP.App
GhostFTP.Design    <- GhostFTP.App
GhostFTP.Design    <- GhostFTP.Setup
GhostFTP.Core      <- GhostFTP.SelfTest
```

`GhostFTP.Core` does not depend on the UI projects. `GhostFTP.Design` does not depend on Core, App or Setup. This keeps transport/business logic isolated from Windows presentation concerns.

## Shared design system

Version 1.2 removes the former duplicated App `Theme`/`Win11` and Setup `Win11Backdrop` implementations. `GhostFTP.Design` is the single source of truth for:

- dark/light/system color resources;
- Segoe UI Variable typography;
- rounded buttons, text/password fields and shared surfaces;
- GridView/ListView/ListBox/context-menu visual defaults;
- GhostFTP logo primitive;
- Windows 11 dark titlebar, rounded corners and Mica integration with safe fallback.

The goal is consistency and lower maintenance cost: a visual-system change should be made once and consumed by both executable applications.

## Main-window composition

The desktop UI is split into partial classes by responsibility:

- `MainWindow.Core.cs` — state and control ownership;
- `MainWindow.Layout.cs` — visual composition and event wiring;
- `MainWindow.Connection.cs` — connection lifecycle and remote listing;
- `MainWindow.Files.cs` — local/remote navigation and file operations;
- `MainWindow.Transfers.cs` — queue, profiles and settings interactions;
- `MainWindow.Helpers.cs` — reusable workspace actions, formatting and shortcuts.

The visual hierarchy is intentionally page-based: sidebar → page header → Quick Connect → Local/Remote panes → transfer queue.

## Dependency policy

There are no `PackageReference` entries and no third-party runtime libraries. The self-contained release bundles the Microsoft .NET Desktop runtime required to execute GhostFTP.

The application itself never calls GitHub or package feeds. Build infrastructure may obtain Microsoft SDK/runtime packs, but those are build-time concerns and are not runtime network dependencies of the installed application.

## Network boundary

Only `FtpSession` owns FTP/FTPS sockets. Demo mode uses `DemoFtpSession` and does not open sockets. Transfer jobs use dedicated transfer sessions where required so the browser/control connection is not concurrently corrupted by file transfers.

No analytics, telemetry, background update service or cloud synchronization component exists in the application architecture.

## Persistence boundary

Profiles and settings are local. Installed builds use the current user's local application-data directory; portable builds prefer a `Data` directory next to the executable. Password persistence is opt-in and uses Windows DPAPI for the current Windows user.

UI-only preferences such as appearance, delete confirmation and hidden/system-file visibility are stored with the same local settings model and do not cause network activity.

## Release architecture

`build-release.ps1` publishes self-contained single-file Windows builds for `win-x64` and `win-arm64`, embeds the corresponding portable payload into each architecture's setup executable and produces canonical assets:

- `setup.exe`
- `portable.exe`
- `setup-arm64.exe`
- `portable-arm64.exe`

Architecture-explicit copies and `SHA256SUMS.txt` are produced alongside them. CI and Release workflows verify required assets before upload/publishing.
