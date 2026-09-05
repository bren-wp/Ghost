# Ghost FTP changelog

## 1.5.0 — 2026-09-05

### Professional resizable workspace

- Added real WPF splitters between Saved Servers and the main workspace, between Local and Remote file panes, and between the browser area and Transfers queue.
- Added double-click reset behavior for the three splitter regions.
- Explicitly enabled resize-with-grip behavior and lowered the safe main-window minimum size so the client works better on smaller desktop displays.
- Fixed the existing responsive-column subsystem: `ConfigureResponsiveColumns()` existed but was never connected to MainWindow startup.
- File and transfer GridView columns now resize on both list-size and main-window-size changes.
- Kept the current Ghost FTP design language while moving the interaction model closer to a professional FileZilla-style workstation.

### Parallel transfer engine

- Replaced the single transfer worker with a bounded parallel worker pool.
- Ghost FTP now processes up to three queue jobs concurrently by default, with an internal safety cap of eight workers.
- Real FTP/FTPS jobs continue to create isolated transfer sessions so parallel work does not share or desynchronize the browsing control connection.
- Demo mode remains safe because its local session serializes operations through its own session gate.
- Preserved the 4,096-job queue bound, per-job cancellation, transient-only automatic retries and visible failed-job behavior when the queue is saturated.
- Queue disposal now cancels work and awaits all worker tasks before releasing cancellation resources.

### Security, privacy and dependency discipline

- Preserved strict TLS 1.2/1.3 validation with no certificate-bypass option.
- Preserved authenticated-control-host passive-data hardening, FTP command-injection guards, bounded reply/listing parsing, traversal limits, root-delete blocking and upload/download integrity checks.
- Added no telemetry, analytics, crash upload, advertising, cloud profile synchronization, background updater or tracking SDK.
- Shipping source remains C#-only with zero NuGet `PackageReference` entries.

### Platform contract

- Added `docs/PLATFORM-SUPPORT.md` to distinguish actual shipping support from future platform goals.
- Windows x64/ARM64 remains the production WPF desktop GUI target.
- `GhostFTP.Core` remains platform-neutral `net10.0` and is the required shared FTP/FTPS engine for future desktop renderers.
- Android and iOS are explicitly outside the current desktop release scope; there are no shipping mobile application projects in the source tree.
- Linux GUI parity is not falsely claimed: WPF is Windows-only and the repository currently forbids third-party package dependencies. A future Linux renderer must satisfy the documented parity/security/privacy gates before release.

### Version and documentation discipline

- Synchronized VERSION, assembly/file/informational metadata and Windows manifests to 1.5.0.
- Added dedicated `docs/releases/v1.5.0.md` release notes.
- Refreshed README for 1.5.0 features, 29-language support, parallel transfers, platform scope and workspace behavior.
- Restored the missing 1.4.2 entry in this master changelog so version history matches the already-published detailed 1.4.2 release document.

## 1.4.2 — 2026-09-05

### Setup language-switch stability

- Fixed a fatal WPF Setup reparenting failure that could occur when changing language from the live language ComboBox.
- Reusable wizard controls are now detached from their previous logical parents before a new wizard tree is assembled.
- The language dropdown is closed before rebuild, and rendering is deferred until the SelectionChanged input event has unwound.
- Repeated language changes are coalesced so overlapping rebuild requests cannot accumulate.
- Queued language rebuilds are ignored after the Setup window closes.
- Normal Back/Next navigation continues to preserve wizard state without illegally reusing attached controls.

### Live Setup regression coverage

- The Windows WPF smoke test now opens the real Ghost FTP Setup window rather than validating only localization dictionaries.
- The test switches English → Croatian → German → Japanese → English and pumps the WPF Dispatcher after every change.
- It verifies that the locale remains active, the language selector remains attached, Next reaches License and Back rebuilds Welcome without a crash.
- 1.4.2 intentionally leaves FTP/FTPS protocol behavior, transfer integrity, TLS validation, credential storage and privacy policy unchanged.

## 1.4.1 — 2026-09-05

### Publisher identity hardening

- Added `GhostBrand.PublisherWebsite` as the canonical BRENDIGO LTD website.
- Product website remains `https://ghostftp.com`; developer/publisher website is now explicitly `https://brendigo.com`.
- Expanded source audit so both URLs and the BRENDIGO LTD publisher identity must remain present in the central brand source.
- Expanded the Windows/WPF smoke test to validate Ghost FTP product identity, BRENDIGO LTD publisher identity and both HTTPS website values.
- Updated README/legal/install documentation so the product URL and publisher URL have distinct roles instead of being conflated.

### Uninstall self-cleanup

- Replaced the previous one-shot delayed `GhostFTP-Setup.exe` self-delete attempt with a bounded retry loop.
- The hidden cleanup helper now tolerates a user remaining on the uninstall Finish page for up to several minutes before Setup actually exits.
- Each retry attempts to remove the maintenance Setup executable; after the file unlocks, the helper removes it and then attempts to remove the empty install directory.
- Windows delete-on-reboot remains registered first as an eventual fallback if local cleanup cannot complete.
- The retry delay uses loopback-only traffic and does not contact any external host.
- No separate uninstaller executable, service, scheduled task or background updater was added.

### Version and release discipline

- Synchronized VERSION, assembly/file/informational metadata and both Windows manifests to 1.4.1.
- Added dedicated `docs/releases/v1.4.1.md` release notes before release publication.
- 1.4.1 intentionally does not change the FTP/FTPS protocol engine or transfer-integrity model validated in 1.4.0.

## 1.4.0 — 2026-09-05

### Internationalization

- Added a central dependency-free C# localization system shared by the Windows client and Setup.
- English remains the primary/default language and the guaranteed fallback for technical text that does not yet have a localized override.
- Added selectable support for 29 languages: English, Croatian, German, French, Spanish, Italian, Portuguese, Dutch, Polish, Czech, Slovak, Slovenian, Hungarian, Romanian, Bulgarian, Greek, Turkish, Ukrainian, Russian, Serbian, Bosnian, Swedish, Danish, Norwegian, Finnish, Japanese, Korean, Simplified Chinese and Traditional Chinese.
- Added a dedicated Setup wizard catalog for Welcome, License Agreement, license acceptance, Back/Next navigation, Install options, Ready, Finish and client-language selection.
- Added WPF smoke-test coverage for all 29 application locales and all 29 Setup locales.
- Unknown/invalid stored language codes normalize back to English instead of breaking application startup.
- Language selection is stored locally only; no online translation or language lookup service is used.

### Connection and transfer reliability

- Added configurable automatic transfer retries from 0–5 attempts.
- Retries are limited to transient socket/timeout conditions and FTP 4xx failures.
- Authentication failures, TLS/certificate failures, permission failures and FTP 5xx permanent errors are not blindly retried.
- Added `Retrying` transfer state and retry-count visibility.
- Fixed cancellation during retry backoff so cancelling one job cannot terminate the queue worker.
- Fixed retry-count property notification so the UI updates immediately.
- Added configurable connect, command and transfer-idle timeout settings and wired them into browsing and per-transfer FTP sessions.
- Preserved one independent FTP/FTPS session per queued transfer so transfer cancellation/failure cannot desynchronize the browser control connection.

### FTP integrity and protocol correctness

- Added local Connection Diagnostics using `NOOP`, `SYST`, `PWD` and the server capability set already obtained through `FEAT`.
- Diagnostics communicate only with the server the user explicitly connected to and are never uploaded to Ghost FTP or BRENDIGO LTD.
- Remote file-pane navigation now synchronizes path state through server `CWD` and `PWD`, preventing UI path state from diverging from the server working directory.
- Download completion now verifies `.ghostftp.part` length against server `SIZE` when the server supports it before promoting the partial file to the final local destination.
- Upload completion verifies the temporary remote file size with `SIZE` when available before replacing the destination.
- Upload completion re-verifies the committed destination size when available.
- A failed post-commit upload integrity check attempts to remove the invalid new destination and restore the previous rollback backup.
- Preserved strict certificate validation, passive-host hardening, command-injection guards, reply/listing bounds, traversal limits, root-delete blocking and verified `MKD 550` handling.

### Guided premium Setup

- Rebuilt Setup as a multi-step Windows 11-style wizard instead of a single action screen.
- Install flow is now: **Language → License → Install options → Ready → Install/Update → Finish**.
- The license displayed by Setup is the same `LICENSE` file stored in the repository and embedded into the Setup build.
- The user must explicitly accept the license before the installer enables progression beyond the License step.
- The selected Setup language is also stored as the initial Ghost FTP client language.
- Existing valid settings are preserved when Setup writes the initial language.
- Malformed or oversized settings data is quarantined/neutralized rather than trusted.
- Setup validates both the embedded Ghost FTP application payload and its installed maintenance copy as Windows executables before use.
- Setup reports install/update/uninstall failures on the Ready page instead of silently returning without a visible reason.
- Installed updates retain atomic replacement behavior for the application payload.

### Uninstall architecture

- Removed the separate generated uninstaller executable model.
- The installed maintenance copy is `GhostFTP-Setup.exe` and the Windows Installed Apps uninstall command invokes the same executable with `--uninstall`.
- Uninstall removes the application, shortcuts and Installed Apps registration immediately and optionally removes local Ghost FTP settings/profiles.
- When Setup is uninstalling itself, Windows delete-on-reboot is registered as a fallback while a local delayed cleanup attempt tries to remove the maintenance Setup and empty install directory after process exit.
- Preserved the choice to keep local profiles/settings for a future reinstall.

### Product and legal identity

- Product identity remains **Ghost FTP / GhostFTP**.
- Developer, publisher and licensor metadata is now **BRENDIGO LTD**.
- Added Company number **16545639** and registered office **71–75 Shelton Street, Covent Garden, London, WC2H 9JQ, United Kingdom** to the shared legal identity model.
- Windows assembly metadata, Setup publisher display, Installed Apps publisher and About dialog use the same BRENDIGO LTD publisher identity.
- Replaced the previous license text with an English-first Ghost FTP proprietary/source-available license naming BRENDIGO LTD as licensor.
- The repository audit continues to reject legacy non-Ghost FTP product identifiers while allowing the legitimate BRENDIGO LTD publisher identity.

### Dependency, privacy and build guarantees

- Source remains C#-only for shipping application code and contains zero NuGet `PackageReference` entries.
- No application telemetry, analytics, advertising, tracking SDK, crash-upload service or background update checker was introduced.
- Refined telemetry source scanning to detect actual SDK identifiers/namespaces without false positives from ordinary WPF symbols such as `ScrollBarVisibility`.
- Synchronized VERSION, assembly/file/informational metadata and both Windows manifests to 1.4.0.
- Release remains blocked unless Build, source/privacy/product/publisher audit, Core self-tests, WPF editable-input tests, application/Setup localization tests, x64/ARM64 packaging and required release-executable verification all pass.

### Documentation and release discipline

- Expanded README for 1.4.0 features, publisher identity, languages, Setup, FTP integrity and validation rules.
- Added dedicated installation, localization and release-policy documentation.
- Expanded security, privacy, architecture and UI/UX documentation.
- Added version-specific release-note documents under `docs/releases/` so every published release has a maintainable detailed historical record.
- Release workflow now uses the version-specific release-note document instead of generic generated notes for new releases.

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
- Source audit scans both file contents and repository paths for disallowed legacy product identity tokens.
- Version, assembly/file metadata and both Windows manifests are synchronized to 1.3.1.
- Release remains gated on Build, source/privacy/brand audit, Core self-tests, WPF input smoke tests, x64/ARM64 packaging and required executable verification.

## 1.3.0 — 2026-09-05

### Brand and product identity

- Standardized every shipping product surface on **Ghost FTP / GhostFTP**.
- Added `GhostBrand` as the central product-name, website, repository and vector-icon source used by the desktop app and Setup.
- Added the official vector icon at `assets/brand/ghostftp-icon.svg`.
- Added deterministic build-time generation of the Windows `.ico` resource and connected it as the real `ApplicationIcon` for both Ghost FTP and Setup.
- Added a Ghost FTP-only repository hero at `assets/readme/ghostftp-hero.svg` and integrated it into README.
- Added CI enforcement that rejects a return of previous alternate product identities.

### UI / UX

- Replaced remaining Windows-default Security/Appearance ComboBox chrome with the shared `GhostComboBox` C# template.
- Applied the premium dropdown consistently to Quick Connect, server-profile editing and Settings.
- Removed obsolete `GhostTheme.Logo()` and `GhostTheme.ComboBox()` helpers after migrating all callers to canonical identity/dropdown paths.
- Polished the sidebar brand block, spacing, Ghost FTP naming and About navigation.

### Security and stability

- Installer validates the embedded application payload before installation, including minimum size and Windows `MZ` executable signature.
- Existing installations use atomic `File.Replace` semantics and temporary backup cleanup.
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
- Refreshed README, SECURITY, architecture and UI/UX documentation for persistence/installer boundaries.
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
