# Ghost FTP architecture

Ghost FTP 1.3.1 is Windows-first, C#-only and intentionally free of third-party runtime package dependencies.

## Projects

- `GhostFTP.Core` — FTP/FTPS protocol engine, listing parsers, transfer queue, Demo session and profile persistence abstractions. It has no WPF dependency.
- `GhostFTP.Design` — shared Ghost FTP identity and Windows desktop visual system used by both the app and setup. It owns `GhostBrand`, `GhostComboBox`, palette resources, typography, reusable controls/surfaces, list/header/menu styling and Windows DWM/Mica integration.
- `GhostFTP.App` — Windows desktop client written programmatically in C# using WPF. It references Core + Design. No XAML is required.
- `GhostFTP.Setup` — per-user C# installer/uninstaller. It references Design so setup identity and visual language cannot drift from the application. No MSI, WiX, NSIS or Inno Setup dependency is required.
- `GhostFTP.SelfTest` — dependency-free Core security/correctness executable tests used by CI.
- `GhostFTP.UiSmoke` — Windows/WPF smoke tests for real editable-control behavior.

## Dependency direction

```text
GhostFTP.Core      <- GhostFTP.App
GhostFTP.Design    <- GhostFTP.App
GhostFTP.Design    <- GhostFTP.Setup
GhostFTP.Core      <- GhostFTP.SelfTest
GhostFTP.Design    <- GhostFTP.UiSmoke
```

`GhostFTP.Core` does not depend on UI projects. `GhostFTP.Design` does not depend on Core, App or Setup. Transport/business logic therefore remains isolated from Windows presentation concerns.

## Single product identity

`GhostBrand` is the source of truth for shipping identity values and the programmatic vector icon:

- display name: `Ghost FTP`;
- assembly/product identifier: `GhostFTP`;
- website: `https://ghostftp.com`;
- source repository URL;
- privacy tagline;
- app/setup/dialog WPF `ImageSource`.

Repository visual assets live under `assets/brand` and `assets/readme`. CI requires these assets and scans both repository text and repository paths for disallowed legacy identity tokens.

## Windows executable icon

A WPF window icon alone does not brand the actual `.exe` file. `tools/generate-ghostftp-icon.ps1` therefore renders a deterministic Ghost FTP 256×256 PNG-backed ICO using only Windows/.NET drawing APIs.

`Directory.Build.targets` runs this generator independently in each executable project's intermediate output directory before compilation and assigns the result to `ApplicationIcon`. App and Setup builds can therefore run in parallel without racing on a shared generated file, while `portable.exe`, `setup.exe`, Start Menu shortcuts and Explorer receive the Ghost FTP executable icon.

The generator is build-time only and adds no runtime dependency.

## Shared design system

`GhostFTP.Design` is the only home for reusable presentation primitives:

- dark/light/system color resources;
- Segoe UI Variable typography;
- rounded buttons and shared surfaces;
- native WPF TextBox/PasswordBox editing with Ghost FTP visual resources layered around the platform editor;
- `GhostComboBox`, which replaces Windows-default dropdown chrome in Quick Connect, profile editing and Settings;
- GridView/ListView/ListBox/context-menu visual defaults;
- Ghost FTP vector identity primitive;
- Windows dark titlebar, rounded corners and Mica integration with safe fallback.

App- or setup-specific duplicate theme/chrome classes are prohibited by `audit-source.ps1`. Obsolete shared helper paths and fragile TextBox/PasswordBox replacement templates are also rejected.

## Editable-input architecture

Editable fields deliberately retain the native WPF editor/content host. Ghost FTP sets typography, foreground/background resources, padding, caret/highlight colors and focusability without replacing the TextBox or PasswordBox content template.

This keeps normal Windows behavior for:

- caret placement and selection;
- mouse focus and keyboard focus;
- Tab navigation;
- Ctrl+C / Ctrl+V / Ctrl+X / Ctrl+Z;
- keyboard layouts and IME input;
- accessibility behavior inherited from WPF.

`GhostFTP.UiSmoke` runs on the Windows CI runner and verifies that shared TextBox, PasswordBox and ComboBox controls can be instantiated and that editable values can actually change.

## Main-window composition

The desktop UI is split into partial classes by responsibility:

- `MainWindow.Core.cs` — state and control ownership;
- `MainWindow.Layout.cs` — visual composition and event wiring;
- `MainWindow.Connection.cs` — connection lifecycle and remote listing;
- `MainWindow.Files.cs` — local/remote navigation and file operations;
- `MainWindow.Transfers.cs` — queue, profiles and settings interactions;
- `MainWindow.Helpers.cs` — reusable workspace actions, formatting and shortcuts;
- `MainWindow.Responsive.cs` — adaptive column sizing;
- `MainWindow.QueueUx.cs` — transfer queue interaction helpers and context actions.

The visual hierarchy is page-based: sidebar → page header → Quick Connect → Local/Remote panes → transfer queue.

## Dependency policy

There are no `PackageReference` entries and no third-party runtime libraries. The self-contained release bundles the Microsoft .NET Desktop runtime needed to execute Ghost FTP.

The application itself never calls package feeds or a remote update service. Build infrastructure may obtain Microsoft SDK/runtime packs, but those are build-time concerns and not runtime network dependencies of the installed application.

## Network boundary

Only `FtpSession` owns real FTP/FTPS sockets. Demo mode uses `DemoFtpSession` and does not open sockets. Transfer jobs use dedicated transfer sessions where required so the browser/control connection is not concurrently corrupted by file transfers.

Protocol boundaries include:

- TLS certificate/hostname validation;
- command-argument CR/LF/NUL rejection;
- control reply line/total limits;
- bounded LIST/MLSD payload memory;
- command/connect/transfer timeouts;
- PASV host hardening;
- depth and total-entry traversal budgets;
- explicit verification of ambiguous `MKD 550` results;
- rollback-safe remote file replacement;
- propagation of real control-channel protocol/transport failures.

No analytics, telemetry, background update service or cloud synchronization component exists in the application architecture.

## Transfer queue boundary

`TransferQueueService` owns sequential transfer scheduling and cancellation state. Its bounded channel prevents unbounded queued work.

Queue saturation is represented as a failed `TransferJob` rather than an exception escaping a UI event handler. The UI can retry failed/cancelled jobs, cancel selected/all active work, copy paths and clear terminal jobs.

Transfer progress uses saturating arithmetic to avoid overflow from untrusted or nonsensical remote sizes.

## Persistence boundary

Profiles and settings are local. Installed builds use the current user's local application-data directory; portable builds prefer a `Data` directory next to the executable. Password persistence is opt-in and uses Windows DPAPI for the current Windows user.

Persistence inputs are bounded:

- settings: maximum 1 MiB;
- profiles: maximum 8 MiB;
- profiles: maximum 2,048 entries;
- important profile strings and protected-password blobs: bounded before use.

Settings and profiles use temporary files plus atomic replacement/backup recovery when replacing existing files. Corrupted or oversized settings do not enter unbounded deserialization; profile recovery may use the previous backup.

Saved profile objects are normalized before entering application state. Security mode, host, port, username, initial remote path and protected-password state are checked. The Demo entry is canonicalized and duplicate Demo records are removed.

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

Release validation order is:

1. restore;
2. Release build with warnings-as-errors;
3. source/dependency/version/privacy/brand audit;
4. Core self-tests;
5. WPF editable-input smoke tests;
6. x64/ARM64 packaging;
7. canonical executable verification;
8. artifact/release publication.
