<p align="center">
  <img src="assets/readme/ghostftp-client.png" alt="Ghost FTP 0.1.5 Beta — authentic production Windows desktop client" width="100%">
</p>

<p align="center"><strong>Authentic application capture generated from the compiled Ghost FTP desktop client — not a mockup, illustration or generated UI.</strong></p>

# Ghost FTP

**Ghost FTP** (`GhostFTP`) is a privacy-first native FTP/FTPS desktop workstation for **Windows and Linux**. It combines a modern dual-pane workflow, bounded parallel transfers, local-only profiles, strict TLS behavior, a resizable professional UI and a dependency-minimal C#/.NET codebase.

Ghost FTP is developed and published by **BRENDIGO LTD** (Company number **16545639**), 71–75 Shelton Street, Covent Garden, London, WC2H 9JQ, United Kingdom.

- Product: https://ghostftp.com
- Publisher: https://brendigo.com
- GitHub Releases: https://github.com/bren-wp/Ghost/releases
- Current source version: **0.1.5**
- Current release channel: **Beta**
- Informational version: **0.1.5-beta**
- First stable target: **1.0.0**
- Runtime baseline: **.NET 10 / C# 14**
- Detailed release notes: [`docs/releases/v0.1.5.md`](docs/releases/v0.1.5.md)

## What Ghost FTP is built for

Ghost FTP is a real desktop file-transfer client, not a web wrapper. It keeps the familiar dual-pane workflow expected from professional FTP clients while using a cleaner contemporary interface and explicit privacy/security boundaries.

The workstation provides saved servers, session-only Quick Connect, Local and Remote file panes, upload/download queues, retry/cancellation/history cleanup, queue dispatch pause/resume, connection logs, local diagnostics, Site Manager, configurable retries/concurrency/timeouts/keepalive, 29 local languages, and native Windows/Linux renderers sharing the same protocol and transfer core.

## 0.1.5 Beta highlights

### Safer LIST / MLSD parsing

0.1.5 places tighter resource limits around untrusted directory-listing text. Individual LIST/MLSD lines are bounded, MLSD fact count is bounded per entry, and LIST regexes use the .NET non-backtracking engine. Listing text is enumerated incrementally rather than creating a second full split/copy.

Unix symlink output is handled more accurately: the reported ` -> target` metadata is stripped before validating the safe link name, so a valid symlink is not discarded merely because its target is absolute. Symlinks remain non-directory entries and are not recursively followed.

### Lower transfer overhead

FTP data streams now use pooled 128 KiB buffers. Buffers are explicitly cleared before being returned to the shared pool because they may contain private user file data.

Transfer progress uses one deliberate renderer-marshaling boundary rather than an extra ThreadPool hop and is throttled to a practical UI rate during active transfers. Terminal queue notifications are no longer duplicated.

Fast batches also no longer force one remote LIST refresh for every completed item; post-transfer Local/Remote refreshes are coalesced into a short batch refresh.

### Cleaner workstation layout

The first-run Connection Log / Quick Connect area is shorter, leaving more space for Local/Remote file work. Ghost FTP now persists sidebar width and connection-panel height in addition to window size, transfer-panel height and Local/Remote ratio.

Windows Transfers now exposes a visible **Pause queue / Resume queue** action in the header. This remains a truthful **dispatch pause**: running transfers continue; new queued/retrying work waits.

### Expanded deterministic hardening tests

The package-free `GhostFTP.HardeningSelfTest` suite now covers concurrent disposal, malformed control replies, pathological LIST/MLSD input, valid EPSV custom delimiters, malformed PASV tuples, preliminary `120 -> 220` greetings, settings backup recovery and persisted-layout bounds.

## Supported protocols

Ghost FTP currently implements:

- FTP;
- Explicit FTPS (`AUTH TLS`);
- Implicit FTPS.

Explicit FTPS is recommended where supported. Ghost FTP does **not** label SFTP as FTP and does not claim SSH/SFTP support in this release line.

## Security model

The shipping client preserves these boundaries:

- fail-closed transport-mode validation;
- TLS 1.2/1.3 certificate and hostname validation;
- no silent FTPS-to-FTP downgrade;
- `PBSZ 0` / `PROT P` encrypted-data protection for FTPS;
- required binary `TYPE I` transfer mode;
- passive data connections tied to the authenticated control host;
- strict EPSV/PASV tuple and port parsing;
- CR/LF/NUL command-injection guards;
- bounded control replies, directory listings, traversal and transfer-queue resources;
- non-backtracking LIST parsing;
- local path containment checks;
- deletion confirmation and root/path protections;
- bounded retry and concurrency behavior;
- isolated transfer sessions;
- cancellation-safe coordinated shutdown;
- clearing of pooled buffers that may contain transferred file data.

Read [`SECURITY.md`](SECURITY.md) for the complete hardening model.

## Privacy

Ghost FTP is designed to run **without application telemetry**. The application contains no analytics SDK, advertising SDK, usage telemetry, user fingerprinting, hidden crash uploader, cloud profile synchronization, hidden product-account requirement or automatic background update checker.

Quick Connect credentials are session-only unless the user explicitly saves a profile. Saved-password protection is opt-in. Windows uses the current-user DPAPI boundary; Linux uses AES-256-GCM with local user-private key material.

Read [`PRIVACY.md`](PRIVACY.md).

## Transfer queue semantics

The shared bounded queue supports configurable concurrency, bounded transient retries, cancellation isolation, queued/running/retrying/completed/failed/cancelled state, progress/speed/ETA reporting, dispatch pause/resume and selective history cleanup.

**Pause queue** pauses dispatch only. Transfers already running continue. Ghost FTP does not claim arbitrary active FTP byte streams can be frozen and resumed when the server has not negotiated such semantics.

## Windows

The Windows renderer is native WPF with per-monitor DPI awareness, long-path awareness and resizable/persisted workstation panes.

### Windows release files

Canonical and architecture-specific release files include:

- `setup.exe`
- `portable.exe`
- `setup-arm64.exe`
- `portable-arm64.exe`
- `GhostFTP-Setup-win-x64.exe`
- `GhostFTP-Portable-win-x64.exe`
- `GhostFTP-Setup-win-arm64.exe`
- `GhostFTP-Portable-win-arm64.exe`
- `SHA256SUMS.txt`
- `SIGNING.txt`

The same Setup executable is the installed maintenance/uninstall entry. Ghost FTP intentionally does not generate a separate `uninstall.exe`.

## Linux

The Linux renderer is a native C# X11/XWayland desktop client using the system `libX11.so.6` ABI. It shares `GhostFTP.Core` and `GhostFTP.Design` with Windows rather than implementing a separate FTP engine.

### Linux release files

Canonical Linux assets include:

- `GhostFTP-linux-x64`
- `GhostFTP-linux-arm64`
- corresponding `.tar.gz` archives;
- versioned archives;
- `SHA256SUMS-linux.txt`;
- `BUILD-INFO.txt`.

Linux consumes the same FTP/FTPS parser, transfer queue, bounded resource model, localization and local persistence primitives.

## Platform scope

Shipping desktop scope is intentionally limited to Windows x64/ARM64 and Linux x64/ARM64. Android, iOS, macOS/MacCatalyst and web application targets are not part of this repository's shipping product scope.

## Localization

Ghost FTP provides **29 selectable languages** from local application resources. **English (`en`) is the primary language, default language and final fallback.** No online translation API is used by the client or Setup.

See [`docs/LOCALIZATION.md`](docs/LOCALIZATION.md).

## Demo mode

The built-in Ghost FTP Demo profile is completely local. It opens no external FTP connection and is used by the regression suite to exercise listing, upload/download, rename, recursive directory behavior, cleanup and disconnect lifecycle.

## Real-server smoke testing

A separate non-destructive live-server harness can validate connect/PWD/LIST/NOOP/disconnect against explicitly configured credentials without server writes. Credential values are supplied through protected CI secrets and are redacted from output.

See [`docs/LIVE-SMOKE-TEST.md`](docs/LIVE-SMOKE-TEST.md).

## Build from source

Requirements are .NET 10 SDK, Windows for the real WPF renderer/Setup build, and Linux with X11/XWayland runtime libraries for the native Linux renderer.

```text
dotnet restore GhostFTP.sln
dotnet build GhostFTP.sln -c Release
```

Official releases use repository CI/release gates rather than relying only on a local compile.

## Dependency policy

Shipping and regression-test projects intentionally contain **zero third-party NuGet `PackageReference` dependencies**. Platform APIs are accessed through the .NET runtime/BCL, WPF on Windows, audited native Windows calls where required, and the audited X11 ABI layer on Linux.

Repository audits reject known telemetry/tracking SDK references, mobile-scope drift and private signing-key material.

## CI and release gates

A Ghost FTP Beta source is not release-ready until the relevant Windows/Linux pipeline passes:

- restore/build with warnings treated as errors;
- source/dependency/platform/privacy audit;
- final security hardening audit;
- Core self-test;
- complete local Demo workflow self-test;
- parallel transfer queue self-test;
- protocol/shutdown/parser/settings hardening self-test on Windows and Linux;
- Windows WPF editable-input/localization smoke test;
- Linux X11/XWayland runtime smoke test;
- authentic Windows UI capture;
- Windows and Linux packaging;
- required artifact verification;
- SHA-256 generation/verification.

## Documentation

- [`docs/releases/v0.1.5.md`](docs/releases/v0.1.5.md) — detailed 0.1.5 Beta release notes
- [`CHANGELOG.md`](CHANGELOG.md) — cumulative public version history
- [`docs/HISTORICAL-CHANGELOG.md`](docs/HISTORICAL-CHANGELOG.md) — preserved pre-reset engineering history
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — architecture and trust boundaries
- [`docs/UI-UX.md`](docs/UI-UX.md) — UI/UX contract
- [`docs/UI-PARITY.md`](docs/UI-PARITY.md) — Windows/Linux parity contract
- [`docs/INSTALLATION.md`](docs/INSTALLATION.md) — install/update/uninstall model
- [`docs/LOCALIZATION.md`](docs/LOCALIZATION.md) — local language architecture
- [`docs/PLATFORM-SUPPORT.md`](docs/PLATFORM-SUPPORT.md) — shipping platform scope
- [`docs/RELEASE-POLICY.md`](docs/RELEASE-POLICY.md) — release requirements
- [`docs/LIVE-SMOKE-TEST.md`](docs/LIVE-SMOKE-TEST.md) — non-destructive real-server verification
- [`SECURITY.md`](SECURITY.md) — security model
- [`PRIVACY.md`](PRIVACY.md) — privacy model

## Versioning

The public source version is controlled by root `VERSION` and `RELEASE_CHANNEL`. All 0.x releases remain Beta. Version **1.0.0** is reserved for the first stable public release. Historical engineering documents are preserved for traceability and do not override the current public version line.

## License

The repository is source-available/proprietary under [`LICENSE`](LICENSE). See [`NOTICE.md`](NOTICE.md) for publisher and legal information.
