# Ghost FTP changelog

## 1.3.1 — 2026-09-05

### Critical input fix

- Removed custom TextBox and PasswordBox templates from the shared design layer after they were identified as a risk to native WPF focus/caret/editing behavior.
- Restored native WPF editable-control behavior while retaining Ghost FTP colors, typography, spacing and dark/light resources.
- Explicitly keeps Host, Port, Username, Password, path and filter controls focusable, tab-accessible and writable.
- Added a real WPF editable-input smoke-test project to CI and Release validation.
- The UI smoke test instantiates Ghost FTP TextBox, PasswordBox and ComboBox controls on a Windows STA thread and verifies editable state and value mutation.
- Source audit now rejects reintroduction of fragile TextBox/PasswordBox replacement templates.

### Transfer queue and UI stability

- Added local exception boundaries to synchronous toolbar/context-menu actions so a single failed operation cannot escape directly to the global application handler.
- Replaced normal workflow error/confirmation paths with Ghost FTP-styled dialogs.
- Added transfer queue Retry selected, Cancel selected, Cancel all, Copy source, Copy destination and Clear finished actions.
- Queue saturation now leaves a visible failed transfer instead of throwing an unhandled queue-full exception into the UI.
- CI artifact naming no longer hardcodes a product version and therefore cannot drift from `VERSION`.

### FTP/FTPS hardening

- Added a strict memory limit for directory-listing payloads before LIST/MLSD parsing.
- Removed redundant data-stream disposal paths and kept deterministic socket closure before final FTP replies.
- FTP control-channel failures are no longer swallowed as optional-command failures.
- Ambiguous `550` responses from `MKD` are verified with directory access before they can be treated as an existing directory.
- Both manual remote-folder creation and recursive upload tree creation use the same verified directory-creation path.
- Recursive traversal budgets now count returned entries instead of only recursion depth.
- Aggregate transfer progress uses saturating arithmetic so malicious or nonsensical listing sizes cannot overflow counters.
- Upload replacement now uses a temporary remote file plus rollback backup; the previous destination is restored when finalization fails where the server permits it.

### Profile and persistence hardening

- Saved profile data is normalized before entering application state.
- Duplicate Demo profiles are removed and the surviving Demo record is forced back to canonical Ghost FTP Demo values.
- Invalid security enum values fall back to FTPS Explicit.
- Invalid stored hosts are neutralized, remote paths are canonicalized and oversized profile names/usernames are bounded.
- Oversized or invalid protected-password data is discarded.
- Decrypted saved passwords pass the same FTP command-argument safety guard before use.
- Added Core self-tests for forged/duplicate Demo data, invalid host/security/path state, oversized protected-password data and saved-password CR/LF injection.

### Branding and release discipline

- Product identity remains exclusively **Ghost FTP / GhostFTP** across application, setup, documentation, metadata and repository artwork.
- Source audit now scans both file contents and repository paths for disallowed legacy identity tokens.
- Version, assembly/file metadata and both Windows manifests are synchronized to 1.3.1.
- Release remains gated on Build, source/privacy/brand audit, Core self-tests, WPF input smoke tests, x64/ARM64 packaging and required executable verification.

## 1.3.0 — 2026-09-05

### Brand and product identity

- Standardized every shipping surface on the **Ghost FTP / GhostFTP** identity.
- Added `GhostBrand` as the central product-name, website, repository and vector-icon source used by the desktop app and setup.
- Added the official vector icon at `assets/brand/ghostftp-icon.svg`.
- Added deterministic build-time generation of the Windows `.ico` resource and connected it as the real `ApplicationIcon` for both Ghost FTP and Setup.
- Added a Ghost FTP-only repository hero at `assets/readme/ghostftp-hero.svg` and integrated it into README.
- Removed legacy alternate brand/author references from UI, installer metadata, privacy/legal documentation and current project metadata.
- Added CI enforcement that rejects a return of the previous alternate brand identity.

### UI / UX

- Replaced the remaining Windows-default Security/Appearance ComboBox chrome with the shared `GhostComboBox` C# template.
- Applied the premium dropdown consistently to Quick Connect, server-profile editing and Settings.
- Removed obsolete `GhostTheme.Logo()` and `GhostTheme.ComboBox()` helpers after migrating all callers to the canonical brand/dropdown paths.
- Polished the sidebar brand block, spacing, Ghost FTP naming and About navigation.

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
- Removed obsolete release-trigger files from earlier releases.
- Expanded source audit checks for Ghost FTP identity, required visual assets, executable-icon generation, premium dropdown ownership and shared design-system ownership.
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
