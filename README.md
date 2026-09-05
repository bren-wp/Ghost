<p align="center">
  <img src="assets/readme/ghostftp-hero.svg" alt="Ghost FTP — private FTP and FTPS desktop workspace" width="100%">
</p>

# Ghost FTP

**Ghost FTP** (`GhostFTP`) is a privacy-first FTP/FTPS desktop client with a modern dual-pane workstation, local-only configuration, a dependency-free C# codebase and a release pipeline that publishes verified Windows installer and portable editions.

Ghost FTP is developed and published by **BRENDIGO LTD** (Company number **16545639**), registered office **71–75 Shelton Street, Covent Garden, London, WC2H 9JQ, United Kingdom**.

- Product website: https://ghostftp.com
- Developer / publisher website: https://brendigo.com
- Repository: https://github.com/bren-wp/Ghost
- Current source version: **1.6.0**
- Runtime baseline: **.NET 10 / C# 14**
- Production GUI target: **Windows / WPF**
- Shared protocol core: **platform-neutral `net10.0`**
- Product identity: **Ghost FTP / GhostFTP**
- Developer / publisher / licensor: **BRENDIGO LTD**
- License: proprietary/source-available; see [LICENSE](LICENSE)

## Downloads

Every official Windows release is required to publish verified x64 and ARM64 assets:

- [setup.exe](https://github.com/bren-wp/Ghost/releases/latest/download/setup.exe) — Windows x64 installer
- [portable.exe](https://github.com/bren-wp/Ghost/releases/latest/download/portable.exe) — Windows x64 portable build
- [setup-arm64.exe](https://github.com/bren-wp/Ghost/releases/latest/download/setup-arm64.exe) — Windows ARM64 installer
- [portable-arm64.exe](https://github.com/bren-wp/Ghost/releases/latest/download/portable-arm64.exe) — Windows ARM64 portable build
- architecture-explicit GhostFTP copies for x64 and ARM64
- `SHA256SUMS.txt`

CI and Release fail if required executables are missing or empty.

## What changed in 1.6.0

Ghost FTP 1.6.0 focuses on **connection resilience, transfer observability and safer workstation input routing**.

### Connection resilience

- Added configurable FTP control-channel keepalive using standard `NOOP`.
- Keepalive can be disabled with `0`; enabled values are constrained to 15–600 seconds.
- Default keepalive is 60 seconds.
- Keepalive communicates only with the FTP/FTPS server the user already selected.
- Demo mode remains fully local and never runs keepalive.
- Failed keepalive resets the stale control transport instead of leaving a false connected state.
- A lost control connection clears stale Remote data and is shown explicitly as **Connection lost**.
- Connection Diagnostics now applies the same stale-transport reset on real control-channel failure.

### Transfer workstation improvements

- Concurrent transfers are now configurable from 1–8; default remains 3.
- Real queued transfers continue to use isolated FTP/FTPS sessions.
- Queue rows now expose transferred bytes, total size where known, speed, ETA, retry count and timestamps.
- Queue header shows aggregate live throughput across running jobs.
- Double-clicking a queue job opens detailed transfer diagnostics rather than triggering an implicit retry.
- Resumed-transfer speed calculation now establishes a fresh measurement baseline, preventing previously downloaded bytes from inflating the displayed current speed.

### Keyboard and focus safety

- File shortcuts now act only on the file pane that actually owns focus.
- `Delete` while the Transfers queue has focus cancels the selected transfer instead of falling through to Local file deletion.
- `Ctrl+A` selects all in the active Local, Remote or Transfers list.
- `Enter` opens/activates the selected file-pane item.
- `Backspace` navigates to the parent directory in the active Local/Remote pane.
- `F5`, `F2`, `Delete`, `Ctrl+F` and `Ctrl+L` no longer silently default to Local actions when another region has focus.

See [docs/releases/v1.6.0.md](docs/releases/v1.6.0.md).

## 29 languages

English is the primary/default language and guaranteed fallback. Ghost FTP ships selectable language support for:

English, Croatian, German, French, Spanish, Italian, Portuguese, Dutch, Polish, Czech, Slovak, Slovenian, Hungarian, Romanian, Bulgarian, Greek, Turkish, Ukrainian, Russian, Serbian, Bosnian, Swedish, Danish, Norwegian, Finnish, Japanese, Korean, Simplified Chinese and Traditional Chinese.

The application and Setup localization catalogs are local C# data. Ghost FTP does not call an online translation service, download language packs or report the selected language.

See [docs/LOCALIZATION.md](docs/LOCALIZATION.md).

## Premium Setup

`setup.exe` uses the same Ghost FTP design system and follows a guided Windows flow:

1. **Language** — choose Setup and initial client language.
2. **License** — review the embedded repository license and explicitly accept it.
3. **Install options** — install location and optional desktop shortcut.
4. **Ready** — review product, publisher, language and choices.
5. **Install / Update** — validate and replace the embedded application payload safely.
6. **Finish** — launch Ghost FTP or close Setup.

Uninstall uses the installed `GhostFTP-Setup.exe --uninstall`. Ghost FTP does **not** generate a separate uninstaller executable and does not install an updater service, scheduled task or telemetry component.

See [docs/INSTALLATION.md](docs/INSTALLATION.md).

## FTP / FTPS protocol capabilities

- FTP.
- Explicit FTPS (`AUTH TLS`).
- Implicit FTPS.
- TLS 1.2 / TLS 1.3.
- Standard .NET certificate-chain and hostname validation in the production Windows client.
- No certificate-validation bypass setting.
- EPSV with PASV fallback.
- Passive data channels use the authenticated control host instead of trusting server-supplied PASV redirect host data.
- UTF-8 negotiation where supported.
- MLSD parsing with LIST fallback.
- `CWD` + server-confirmed `PWD` remote navigation.
- Download resume through `REST` and `.ghostftp.part` files where supported.
- Remote `SIZE` verification for completed downloads where available.
- Upload through a temporary remote file before commit.
- Pre-commit and post-commit remote-size verification where `SIZE` is available.
- Rollback backup when safely replacing an existing remote file.
- Recursive upload/download, create folder, rename and delete.
- Root-delete protection.
- Directory-listing, reply and recursive-traversal resource limits.
- Isolated browsing/control connection and independent queued transfer sessions.
- Configurable control-channel `NOOP` keepalive.

## Transfer reliability

Ghost FTP separates permanent failures from conditions that may reasonably recover.

- Automatic transfer retries: 0–5.
- Concurrent transfer limit: 1–8.
- Connect timeout: 3–120 seconds.
- Command timeout: 5–300 seconds.
- Transfer idle timeout: 15–3600 seconds.
- Keepalive: disabled (`0`) or 15–600 seconds.
- Automatic retry is limited to transient socket/timeout conditions and FTP 4xx replies.
- Authentication failures, TLS/certificate failures, permission failures and permanent FTP 5xx errors are not blindly retried.
- Cancelling one transfer does not terminate unrelated workers.
- Queue capacity is bounded and saturation becomes a visible failed job rather than an unhandled exception.

Changes to concurrency/retry worker configuration apply after application restart. Timeout changes apply to the next connection. Keepalive interval changes apply while the application is running.

## Transfer observability

The Transfers workspace can show:

- item and direction;
- state;
- percentage progress;
- transferred bytes / known total;
- current speed;
- ETA when calculable;
- retry count;
- source and destination;
- start/finish timestamps in the details dialog;
- error details for failed jobs;
- aggregate live queue throughput.

This information is **local UI state**, not product telemetry. It is not uploaded to Ghost FTP, BRENDIGO LTD or an analytics provider.

## File workstation

Ghost FTP provides a resizable Local / Remote workflow with:

- Saved Servers and Quick Connect;
- Host, Port, Security, Username and Password inputs;
- native WPF TextBox/PasswordBox editing for reliable caret, selection, paste, keyboard and IME behavior;
- resizable Saved Servers sidebar;
- resizable Local/Remote pane split;
- resizable Transfers region;
- double-click splitter reset behavior;
- persistent local workspace geometry;
- Local Home, Desktop, Documents and Downloads shortcuts;
- Remote root and parent navigation;
- responsive wrapping toolbars;
- dynamically resized file and transfer columns;
- extended multi-selection;
- drag-and-drop upload from Windows;
- create folder, rename, refresh and path navigation;
- local and remote filters;
- copy local/remote paths;
- Open in File Explorer for local items;
- hidden/system-item preference;
- item/selection summaries;
- Retry selected, Cancel selected, Cancel all and Clear finished queue actions;
- per-transfer details;
- connection diagnostics from the connection-status badge.

### Keyboard shortcuts

| Shortcut | Active context | Action |
| --- | --- | --- |
| `F5` | Local / Remote | Refresh active file pane |
| `F2` | Local / Remote | Rename selected item |
| `Delete` | Local / Remote | Delete selected file/folder through normal confirmation policy |
| `Delete` | Transfers | Cancel selected active transfer |
| `Ctrl+F` | Local / Remote | Focus active pane filter |
| `Ctrl+L` | Local / Remote | Focus/select active path field |
| `Ctrl+A` | Local / Remote / Transfers | Select all |
| `Enter` | Local / Remote | Open/activate selected item |
| `Backspace` | Local / Remote | Navigate to parent directory |

Shortcut routing is focus-scoped so an inactive pane does not become an accidental destructive-operation target.

## Privacy by design

Ghost FTP contains no application telemetry, analytics SDK, advertising SDK, tracking SDK, crash-report upload, cloud profile sync or automatic background product update checker.

Runtime network traffic is limited to:

1. FTP/FTPS connections and operations against the server selected by the user;
2. optional keepalive `NOOP` requests on that selected server session;
3. Connection Diagnostics against that selected server;
4. product/publisher website links explicitly opened by the user.

Keepalive can be disabled. It does not contact ghostftp.com, brendigo.com, GitHub or a telemetry endpoint.

Demo mode remains completely local.

See [PRIVACY.md](PRIVACY.md).

## Security boundaries

Ghost FTP intentionally avoids “ignore errors” and “trust everything” switches.

- Explicit FTPS is the default for new profiles.
- Plain FTP requires an explicit warning before connection.
- Password persistence is opt-in and protected with Windows DPAPI for the current user.
- FTP command arguments reject CR/LF/NUL control injection.
- Control replies and listing payloads are bounded.
- Remote paths and local extraction destinations are canonicalized.
- Recursive operations use depth and total-entry budgets.
- Local recursive upload does not follow NTFS reparse points.
- Remote root deletion is blocked.
- Ambiguous `MKD 550` is verified rather than automatically treated as “already exists”.
- Malformed control-channel responses propagate as failures.
- Failed keepalive/diagnostics reset stale control-channel state.
- Profiles/settings are size-bounded, normalized and written atomically with recovery paths.
- Queue saturation creates a visible failed job rather than an unhandled UI exception.
- Destructive keyboard actions are scoped to the focused pane.
- Installer application payload and maintenance Setup copy are validated before installation/use.
- Existing installed application replacement uses atomic replacement semantics where possible.

See [SECURITY.md](SECURITY.md) and [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Local data

Installed Ghost FTP stores settings and profiles under the current user's local application-data directory. Portable builds use a `Data` directory next to the executable in portable mode.

Local settings can include language, appearance, last directory, delete confirmation, hidden-file visibility, retry/concurrency/timeouts, keepalive interval and workspace geometry.

Saved passwords are optional. When enabled they are protected through Windows DPAPI and scoped to the current Windows user.

No Ghost FTP cloud settings/profile service is used.

## Zero third-party runtime package dependencies

Shipping source contains **zero NuGet `PackageReference` entries**.

The production Windows application uses:

- C#;
- Microsoft .NET base class libraries;
- Microsoft WPF included in .NET Desktop;
- Windows APIs already available for DPAPI, DWM/Mica, shell shortcuts and Installed Apps registration.

Official Windows releases are self-contained; end users do not need to install .NET separately.

## Platform support

- **Windows x64 / ARM64:** production desktop GUI, portable build and Setup.
- **Linux:** `GhostFTP.Core` is reusable because it targets platform-neutral `net10.0`; a production Linux GUI is not yet claimed because WPF is Windows-only and this repository currently forbids third-party package dependencies.
- **Android / iOS:** not shipping and outside the current desktop product scope. Source audit rejects mobile application targets from shipping source.

Ghost FTP does not relabel a Windows executable as Linux-compatible. A real Linux renderer must satisfy the parity/security/privacy gates in [docs/PLATFORM-SUPPORT.md](docs/PLATFORM-SUPPORT.md).

## Build and validate

Requirements for the production Windows GUI:

- Windows 10 version 2004 or newer; Windows 11 recommended;
- .NET SDK 10.0.x for source builds.

```powershell
dotnet restore GhostFTP.sln
dotnet build GhostFTP.sln -c Release
./audit-source.ps1
dotnet run --project tests/GhostFTP.SelfTest/GhostFTP.SelfTest.csproj -c Release --no-build
dotnet run --project tests/GhostFTP.QueueSelfTest/GhostFTP.QueueSelfTest.csproj -c Release --no-build
dotnet run --project tests/GhostFTP.UiSmoke/GhostFTP.UiSmoke.csproj -c Release --no-build
```

Create verified release packages:

```powershell
./build-release.ps1
```

Expected release output:

```text
setup.exe
portable.exe
setup-arm64.exe
portable-arm64.exe
GhostFTP-Setup-win-x64.exe
GhostFTP-Portable-win-x64.exe
GhostFTP-Setup-win-arm64.exe
GhostFTP-Portable-win-arm64.exe
SHA256SUMS.txt
```

## Quality gates

Every `main` release candidate is required to pass:

1. .NET restore;
2. Release build with warnings treated as errors;
3. dependency/version/privacy/product/publisher/platform source audit;
4. Core security/correctness self-tests;
5. parallel transfer queue concurrency/session-isolation self-test;
6. real Windows/WPF editable-input tests;
7. application localization coverage checks;
8. Setup localization and live language-switch checks;
9. Ghost FTP / BRENDIGO LTD identity checks;
10. x64 and ARM64 self-contained packaging;
11. canonical setup/portable executable verification;
12. SHA-256 release manifest generation;
13. verified artifact upload.

The Release workflow repeats the validation before GitHub Release publication.

## Documentation

- [CHANGELOG.md](CHANGELOG.md) — chronological version history.
- [docs/releases/](docs/releases/) — detailed version-specific release notes.
- [docs/releases/v1.6.0.md](docs/releases/v1.6.0.md) — current release details.
- [docs/INSTALLATION.md](docs/INSTALLATION.md) — install, update and uninstall behavior.
- [docs/LOCALIZATION.md](docs/LOCALIZATION.md) — languages and translation architecture.
- [docs/PLATFORM-SUPPORT.md](docs/PLATFORM-SUPPORT.md) — exact Windows/Linux/mobile support contract.
- [docs/RELEASE-POLICY.md](docs/RELEASE-POLICY.md) — release requirements and quality gates.
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — project and runtime architecture.
- [docs/UI-UX.md](docs/UI-UX.md) — desktop interaction/design rules.
- [SECURITY.md](SECURITY.md) — security boundaries and reporting.
- [PRIVACY.md](PRIVACY.md) — privacy and network behavior.
- [LICENSE](LICENSE) — BRENDIGO LTD Ghost FTP license.

## Project structure

```text
assets/
  brand/               official Ghost FTP vector branding
  readme/              repository artwork
src/
  GhostFTP.Core/       platform-neutral FTP/FTPS engine, parsers, demo session, transfer queue
  GhostFTP.Design/     shared Windows design, product identity and localization
  GhostFTP.App/        Windows desktop application, C# programmatic WPF UI
  GhostFTP.Setup/      guided Windows install/update/uninstall wizard
tests/
  GhostFTP.SelfTest/   Core security and correctness tests
  GhostFTP.QueueSelfTest/ bounded parallel queue/session-isolation tests
  GhostFTP.UiSmoke/    real Windows/WPF input, localization and Setup smoke tests
docs/
  releases/            detailed version-specific release notes
```

Ghost FTP is developed and published by **BRENDIGO LTD** — https://brendigo.com. Copyright © 2026 BRENDIGO LTD. All rights reserved. See [NOTICE.md](NOTICE.md).
