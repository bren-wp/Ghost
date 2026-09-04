# Ghost FTP changelog

## 1.3.0 — 2026-09-05

### Brand and product identity

- Standardized every shipping surface on the **Ghost FTP / GhostFTP** identity.
- Added `GhostBrand` as the central product-name, website, repository and vector-icon source used by the desktop app and setup.
- Added the official vector icon at `assets/brand/ghostftp-icon.svg`.
- Added a Ghost FTP-only repository hero at `assets/readme/ghostftp-hero.svg` and integrated it into README.
- Removed legacy alternate brand/author references from UI, installer metadata, privacy/legal documentation and current project metadata.
- Added CI enforcement that rejects a return of the previous alternate brand identity.

### Security and stability

- Installer now validates the embedded application payload before installation, including minimum size and Windows `MZ` executable signature.
- Existing installations are replaced with atomic `File.Replace` semantics and temporary backup cleanup.
- Setup reports a locked/running application as an error instead of claiming a successful update.
- Uninstall verifies required application deletion and reports failure instead of silently ignoring a locked executable.
- Added bounded profile persistence: 8 MiB file limit and a 2,048-profile limit.
- Added bounded settings persistence with a 1 MiB file limit.
- Added backup recovery for settings and retained backup recovery for saved profiles.
- Preserved DPAPI-protected opt-in saved passwords, path canonicalization, command-injection guards, FTPS certificate validation, passive-host hardening and recursive traversal limits.

### Maintainability and release quality

- Removed stale hardcoded version fallbacks from About/setup/Windows uninstall metadata.
- Updated version, assembly metadata and application manifests to 1.3.0.
- Expanded source audit checks for Ghost FTP identity, required visual assets and shared design-system ownership.
- Refreshed README, SECURITY, architecture and UI/UX documentation for the new brand and persistence/installer boundaries.
- Retained dependency-free C# source and mandatory x64/ARM64 `setup.exe` / `portable.exe` release verification.

## 1.2.0 — 2026-09-05

### UI / UX

- Rebuilt the main file workspace around a clearer Windows 11 hierarchy with separate page header, Quick Connect, file panes and transfer queue.
- Added a shared `GhostFTP.Design` project so the desktop application and installer use one palette, typography system, control treatment and Mica/rounded-window integration.
- Removed duplicated legacy app/setup theme and Windows 11 backdrop helpers.
- Replaced anonymous Quick Connect fields with labeled Host, Port, Security, Username and Password fields.
- Added responsive wrapping file toolbars so actions no longer clip at narrower window sizes.
- Replaced default white GridView headers with consistent dark/light themed headers and improved list selection/menu/tooltip styling.
- Widened and reorganized Saved Servers navigation; added a dedicated Connect selected action.
- Added Local/Remote item counts and selected-item summaries.
- Added Local Home, Desktop, Documents and Downloads navigation shortcuts and Remote root navigation.
- Added Copy local/remote path actions and Open in File Explorer for local items.
- Added keyboard shortcuts: F5 Refresh, F2 Rename, Delete, Ctrl+F Filter and Ctrl+L Path.
- Added Enter-to-connect from the Quick Connect password field.
- Added a setting to show or hide hidden/system items in the local pane.
- Improved transfer queue summary states for running, queued, failed and completed jobs.
- Redesigned setup/uninstall into the same Ghost FTP visual language with inline progress and completion actions.

### Correctness / maintainability

- Fixed Quick Connect profile matching so manually changing connection fields cannot accidentally reuse the selected Demo/saved-profile connection mode.
- Consolidated reusable visual primitives, resource colors, control styles and DWM integration into `GhostFTP.Design`.
- Reduced duplicated UI helper code and removed stale theme/backdrop implementations.
- Kept the source C#-only and dependency-free with zero NuGet PackageReference entries.
- Preserved x64/ARM64 self-contained setup/portable packaging and mandatory canonical `setup.exe` / `portable.exe` release assets.

## 1.1.0 — 2026-09-04

- Rebuilt the Windows client as a C#-only, dependency-free desktop application.
- Added Windows 11 Fluent/Mica visual treatment and premium dual-pane file management UX.
- Added local Demo mode with realistic folders and transfer operations without network traffic.
- Added FTP, explicit FTPS and implicit FTPS support using the .NET networking stack directly.
- Added upload/download queues, cancellation, recursive folders, rename, delete, new folder and refresh operations.
- Added per-transfer FTP/FTPS sessions so cancelled transfers cannot corrupt the browser control connection.
- Added strict TLS 1.2/1.3 validation with no certificate-bypass option and offline revocation cache checks.
- Added CR/LF command-injection guards, path canonicalization, PASV host hardening, traversal limits and reply-size limits.
- Added safe partial downloads and temporary remote uploads before atomic rename into the destination.
- Added NTFS reparse-point protection for recursive uploads/deletes.
- Added DPAPI-protected optional saved passwords and atomic profile/settings writes.
- Added a C# per-user installer, Start Menu/Desktop integration and Windows uninstall registration.
- Added x64/ARM64 portable + setup release builds with SHA-256 checksums.
- Added CI self-tests and source audits that reject NuGet PackageReference dependencies and known telemetry/tracking SDKs.
- Synchronized product metadata around the Ghost FTP identity, ghostftp.com and version 1.1.0.
