# Ghost FTP changelog

This file tracks the active public Ghost FTP version line. The original pre-reset engineering changelog is preserved verbatim in [`docs/HISTORICAL-CHANGELOG.md`](docs/HISTORICAL-CHANGELOG.md); detailed release bodies remain under [`docs/releases/`](docs/releases/).

## 0.1.5 Beta — 2026-09-06

### Listing/parser hardening

- Added explicit per-line bounds for LIST and MLSD server-controlled input.
- Added a per-entry MLSD fact-count bound to prevent pathological parser work.
- Switched Unix/Windows LIST regular expressions to the .NET non-backtracking regex engine.
- Replaced full listing split/copy processing with incremental `StringReader` line enumeration.
- Fixed safe Unix symlink names being discarded when LIST reports an absolute ` -> target` value.
- Expanded deterministic parser regression coverage for oversized lines, excessive MLSD facts and symlink handling.

### Passive-mode regression coverage

- Added deterministic EPSV coverage using a valid non-default delimiter.
- Added malformed PASV tuple rejection coverage before a data socket can be established.
- Retained strict six-value PASV tuple parsing, authenticated-control-host data-channel routing and bounded port validation from 0.1.4.

### Transfer efficiency and renderer load

- Reused bounded 128 KiB transfer buffers through `ArrayPool<byte>` rather than allocating a new large managed buffer for every data stream.
- Explicitly clears pooled transfer buffers before returning them because they may contain private file contents.
- Removed an unnecessary `Progress<T>` ThreadPool dispatch layer from transfer progress delivery.
- Throttled active transfer renderer progress notifications to approximately 10 Hz while preserving immediate terminal state.
- Removed redundant terminal queue notifications.
- Coalesced burst transfer-completion Local/Remote refreshes so a batch does not issue one FTP LIST for every completed item.

### Workstation quality

- Added a visible **Pause queue / Resume queue** action directly in the Windows Transfers header while keeping the existing context action synchronized.
- Persisted the saved-server sidebar width and Connection Log / Quick Connect height in addition to transfer-panel height, Local/Remote ratio and window state.
- Bounded restored layout dimensions before use.
- Reduced the default connection-area height to give Local/Remote panes more useful first-run workspace.
- Preserved the dark Site Manager segmented General/Advanced design and the approved Ghost reference palette.

### Settings and deterministic hardening tests

- Added corrupted-primary-settings recovery coverage using the atomic `.bak` fallback.
- Added persisted-layout normalization tests for invalid/oversized dimensions.
- Added retry/concurrency bounds coverage.
- Retained concurrent FTP-session and transfer-queue disposal regressions from 0.1.4.
- Kept the hardening suite package-free and cross-platform.

### Security, privacy and scope retained

- Preserved fail-closed FTP security-mode validation, strict Explicit/Implicit FTPS behavior, TLS certificate/hostname validation and no FTPS-to-FTP downgrade.
- Preserved required `TYPE I`, `PBSZ 0`, `PROT P`, command-injection guards, traversal bounds, local path containment and root-delete protection.
- Preserved local-only profiles/settings, session-only Quick Connect by default, current-user DPAPI on Windows and AES-256-GCM local key protection on Linux.
- Preserved zero application telemetry/tracking and zero third-party NuGet `PackageReference` dependencies in shipping/regression projects.
- Preserved Windows/Linux-only shipping scope and the 29-language local catalog with English as primary/default/fallback.

Detailed notes: [`docs/releases/v0.1.5.md`](docs/releases/v0.1.5.md)

## 0.1.4 Beta — 2026-09-06

### FTP protocol hardening

- Added bounded support for valid preliminary FTP greetings such as `120` before the final `220` service-ready response.
- Tightened control-reply parsing to require numeric `100..599` codes and standards-compatible space/hyphen framing.
- Tightened multiline reply accounting so line length, line count and total character limits are enforced before data is accumulated.
- Replaced permissive passive-mode digit extraction with strict EPSV/PASV parsers.
- PASV now consumes exactly the six values inside the passive tuple, validates every byte and derives the port only from `p1,p2`.
- Preserved the authenticated-control-host data-channel rule, preventing PASV responses from redirecting data traffic to an arbitrary third-party host.

### Lifecycle and shutdown stability

- Made `FtpSession.DisposeAsync()` idempotent and race-safe under concurrent callers.
- New FTP operations are rejected as soon as session shutdown begins; concurrent disposal callers await the same completion signal.
- Removed semaphore-disposal races from FTP session teardown.
- Made `TransferQueueService.DisposeAsync()` a coordinated single-owner shutdown operation.
- Queue shutdown now completes dispatch, releases paused waiters, cancels work, waits for workers and only then disposes cancellation resources.
- Enqueue attempts after shutdown begins fail deterministically instead of entering an unusable queue.

### Deterministic regression testing

- Added `GhostFTP.HardeningSelfTest` with no external packages or Internet dependency.
- Added a concurrent FTP-session disposal regression test.
- Added a concurrent transfer-queue disposal regression test.
- Added malformed FTP reply-framing rejection coverage.
- Added an in-process loopback FTP server that exercises `120 -> 220`, USER/PASS, PWD, TYPE I, EPSV fallback, PASV, LIST, a real passive data connection and QUIT.
- The PASV test intentionally appends unrelated numeric diagnostics to prove the client uses the six-value tuple rather than trailing digits.
- Added the hardening suite to both Windows and Linux CI and to the release workflow.

### Security, privacy and product scope retained

- Preserved strict Explicit/Implicit FTPS behavior, TLS certificate/hostname validation, `PBSZ 0`, `PROT P`, required `TYPE I` and no FTPS-to-FTP downgrade.
- Preserved bounded listing/traversal/queue limits, local path containment and command-injection guards.
- Preserved local-only profiles/settings, DPAPI on Windows, AES-256-GCM on Linux, zero application telemetry and zero third-party NuGet `PackageReference` dependencies.
- Preserved Windows/Linux-only shipping scope and 29 local languages with English as default/fallback.
- Preserved the 0.1.3 transfer workstation, UI cleanup and premium Setup behavior.

Detailed notes: [`docs/releases/v0.1.4.md`](docs/releases/v0.1.4.md)

## 0.1.3 Beta — 2026-09-06

### Transfer management

- Added queue-level **Pause queue / Resume queue** control. Pause is deliberately a dispatch pause: already-running FTP data streams continue, while queued and retrying jobs wait asynchronously.
- Added queue-state notifications so desktop renderers can keep their controls synchronized with the actual queue state.
- Added selective cleanup for completed, failed and cancelled transfer history in addition to the existing clear-finished action.
- Added retry-all-failed workflow.
- Windows transfer context actions now include details, pause/resume, retry selected, retry failed, selected/all cancellation, selective cleanup and source/destination path copy.
- Windows queue summary now reports running, retrying, queued, failed, cancelled and completed counts plus aggregate active throughput.
- Linux transfer rows are now selectable, fixing deterministic selected-transfer cancellation in the native renderer.
- Linux transfer header now exposes pause/resume, retry failed, cancellation and cleanup controls at responsive widths.

### UI / UX cleanup

- Unified the dark Windows application and Windows Setup with the shared canonical `GhostReferencePalette` already used by the reference/Linux renderer.
- Tightened card/surface radii, button density, table headers, list rows and badges for a cleaner professional workstation layout.
- Improved keyboard-focus visibility and standardized hover/pressed/disabled button states.
- Improved Linux transfer selection/status coloring and surfaced paused-queue state directly in the transfer summary.

### Windows Setup polish

- Increased usable Setup width for localized copy while retaining resize support.
- Added a step-progress badge and clearer visual hierarchy.
- Added explicit local-only Setup/privacy messaging.
- Clarified that the maintained `GhostFTP-Setup.exe` handles install, update and uninstall; no separate uninstaller executable is generated.
- Clarified staged payload validation, transactional maintenance and rollback behavior on Ready/Progress screens.
- Added a cleaner completion surface and privacy summary.

### Localization

- Preserved all 29 selectable local languages with English (`en`) as primary/default/fallback.
- Added shared `GhostTransferText` for new queue-management labels without an online translation dependency.
- Added explicit Croatian queue-management wording; all other missing new transfer strings safely fall back to English.

### Testing and stability

- Extended `GhostFTP.QueueSelfTest` to verify paused queues do not create transfer sessions before resume.
- Added regression checks for resume, queue-state notification, cancellation while dispatch is paused and selective completed/cancelled/failed cleanup.
- Queue workers remain bounded, cancellation-isolated and asynchronously waiting while paused.
- Queue shutdown releases paused waiters before cancellation to avoid deadlock during application close.

### Security and privacy invariants retained

- Preserved fail-closed security-mode validation, strict `AUTH TLS`, normal TLS certificate/hostname validation and no FTPS-to-FTP downgrade.
- Preserved `PBSZ 0` / `PROT P`, required `TYPE I`, authenticated-control-host passive-data protection, command-injection guards and bounded untrusted input.
- Preserved local-only profile/settings behavior, opt-in saved-password protection, zero telemetry/tracking SDKs and zero third-party NuGet `PackageReference` dependencies in shipping projects.
- Preserved Windows/Linux-only shipping scope; Android/iOS/MacCatalyst targets remain outside the product line and are rejected by source audit.

Detailed notes: [`docs/releases/v0.1.3.md`](docs/releases/v0.1.3.md)

## 0.1.2 Beta — 2026-09-06

### Premium workstation cleanup

- Reworked the Windows workstation shell to remove duplicated global New folder/Rename/Delete actions and keep file operations contextual to Local/Remote panes.
- Added resizable sidebar, connection-log/Quick-Connect area, Local/Remote panes and Transfers panel with persisted layout dimensions.
- Improved compact-window behavior and Quick Connect density while preserving the canonical 1914 × 907 authentic capture contract.
- Improved Site Manager presentation and input validation.
- Kept Windows and Linux aligned through shared `GhostFTP.Core`, `GhostFTP.Design`, reference palette and workflow semantics.

### Security and lifecycle hardening

- Centralized Host/Port/remote-name/path validation through `InputGuard` at UI and protocol boundaries.
- Preserved strict Explicit/Implicit FTPS handling and plain-FTP warning boundaries.
- Hardened Windows DPAPI plaintext-buffer cleanup using explicit zeroing before unmanaged memory release.
- Hardened Linux session lifecycle and keepalive ownership checks to prevent stale-session work after disconnect/reconnect.
- Kept Android/iOS/MacCatalyst source/TFMs out of the desktop product scope through audit rules.

### Setup and release quality

- Kept the transactional Windows Setup architecture with staged application/maintenance candidates, metadata/version validation, downgrade protection and rollback.
- Kept one Setup executable as the installed maintenance/uninstall entry rather than generating a separate uninstaller.
- Synchronized 0.1.2 metadata, manifests, docs, release trigger and Beta tag contract.
- Regenerated authentic README screenshots from the compiled WPF application.
- Verified Windows and Linux build/self-test/package gates before publishing `v0.1.2-beta`.

Detailed notes: [`docs/releases/v0.1.2.md`](docs/releases/v0.1.2.md)

## 0.1.1 Beta — 2026-09-06

### Reliability and Demo regression hardening

- Added a complete local-only `GhostFTP.DemoSelfTest` workflow on both Windows and Linux CI.
- Demo regression covers connect, diagnostics, PWD/CWD, listing, keepalive, download, upload/download byte-for-byte round trip, rename, create/delete directory, recursive directory round trip, cleanup, root-delete protection, disconnect reset and rejection of post-disconnect operations.
- Added conflict protection so file upload cannot replace an existing directory and directory upload cannot replace an existing file in Demo state.
- `DemoFtpSession` exposes local diagnostics/keepalive behavior and resets its working directory on disconnect/disposal.
- Demo mode remains local and opens no external FTP, telemetry or analytics connection.

### Transactional Windows Setup hardening

- Setup stages and validates both the application payload and maintenance `GhostFTP-Setup.exe` candidate before changing an active installation.
- Candidate identity validation requires ProductName **Ghost FTP**, CompanyName **BRENDIGO LTD** and the exact active file version.
- Added downgrade protection and independent rollback copies for application and maintenance Setup binaries.
- Uninstall removes stale transaction files; `QuietUninstallString` remains absent until a genuine silent-uninstall path exists.

### Release and documentation

- Advanced public Beta line to 0.1.1 with assembly/file version 0.1.1.0 and informational version 0.1.1-beta.
- Added detailed release notes and synchronized security/privacy/architecture/platform/install/localization/UI/version/release documentation.
- Preserved authentic application screenshots generated from the real compiled WPF client.

Detailed notes: [`docs/releases/v0.1.1.md`](docs/releases/v0.1.1.md)

## 0.1.0 Beta — 2026-09-05

### Public version-line reset

- Restarted the public Ghost FTP sequence at **0.1.0 Beta** without discarding completed application, protocol, UI, Setup, security, privacy, localization, testing or release-pipeline work.
- Added root `VERSION` / `RELEASE_CHANNEL` metadata and defined the 0.x Beta line with **1.0.0** reserved as the first stable release.
- Synchronized .NET and Windows manifest metadata to the public line.
- Preserved canonical Windows `setup.exe` / `portable.exe` naming.
- Preserved native Windows/Linux desktop renderers, shared FTP/FTPS core, 29-language localization, workstation UI, strict FTPS validation, bounded transfers and privacy-first local persistence.
- Established authentic repository screenshots produced from the compiled application rather than conceptual mockups.

Detailed notes: [`docs/releases/v0.1.0.md`](docs/releases/v0.1.0.md)

For the full engineering history that led to the public 0.x line, see [`docs/HISTORICAL-CHANGELOG.md`](docs/HISTORICAL-CHANGELOG.md).
