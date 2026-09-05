# Ghost FTP Architecture

Ghost FTP **0.1.1 Beta** is one privacy-first FTP/FTPS desktop product with native Windows and Linux renderers, a shared platform-neutral protocol/transfer core, local-only persistence, bounded resource usage and release-time verification.

Ghost FTP is the product. **BRENDIGO LTD** is the developer, publisher and licensor.

## Release identity

Root files define the public line:

```text
VERSION=0.1.1
RELEASE_CHANNEL=beta
```

`Directory.Build.props` synchronizes assembly/file/informational version metadata. All 0.x builds remain Beta; the first stable target is **1.0.0**.

## Project map

```text
src/GhostFTP.Core        shared net10.0 FTP/FTPS engine, safety, persistence and queue
src/GhostFTP.Design      shared product identity, localization and visual contract
src/GhostFTP.App         Windows WPF renderer
src/GhostFTP.Setup       Windows per-user Setup / maintenance application
src/GhostFTP.Linux       Linux X11/XWayland renderer

tests/GhostFTP.SelfTest      shared security/correctness regression executable
tests/GhostFTP.DemoSelfTest  complete local-only Demo workflow regression executable
tests/GhostFTP.QueueSelfTest transfer concurrency/cancellation/session tests
tests/GhostFTP.UiSmoke       Windows WPF input/localization smoke tests
tests/GhostFTP.LiveSmoke     optional credential-safe real-server smoke harness
```

Dependency direction keeps FTP sockets and credentials out of the design layer. Both desktop renderers use `GhostFTP.Core`; both consume shared `GhostFTP.Design` identity/localization/reference definitions.

## Platform boundary

### Windows

`GhostFTP.App` is a C# WPF desktop application. `GhostFTP.Setup` is a separate installation/maintenance UI. Official Windows builds are self-contained x64/ARM64 packages.

### Linux

`GhostFTP.Linux` is a real native desktop renderer that calls the system X11 client ABI (`libX11.so.6`) directly and can run on Wayland desktops through XWayland. It is not a browser wrapper, WPF shim or web runtime. Official Linux builds are self-contained x64/ARM64 packages except for the normal system X11 library dependency.

Android, iOS, MacCatalyst/macOS native and Web/browser clients are outside the current shipping line.

## Shared visual/product contract

`src/GhostFTP.Design/GhostReferencePalette.cs` defines shared reference geometry and palette tokens. The normal desktop hierarchy is:

```text
Product / saved-sites / privacy rail
→ File / View / Sites / Transfers / Tools / Help
→ compact global toolbar + Remote search
→ Connection Log + Quick Connect
→ Local + Remote panes
→ Transfers
→ status
```

The canonical repository image is the real Windows application rendered at **1914×907 / 96 DPI**. Windows and Linux are required to preserve product identity, action hierarchy, colors, core workflows and safety semantics. Native WPF/X11 font rasterization and OS window chrome can differ; literal byte-identical pixels are not claimed.

See `docs/UI-PARITY.md`.

## Product identity

`GhostBrand` / shared design metadata defines:

- display name `Ghost FTP`;
- compact identifier `GhostFTP`;
- product/publisher websites;
- repository identity;
- publisher `BRENDIGO LTD`;
- company number and registered-office metadata;
- copyright and visual identity.

Product identity is not replaced by publisher identity on normal user surfaces.

## Localization

`GhostLocalization` owns the application language catalog, English source strings, language normalization and fallback. `GhostSetupLocalization` owns Setup vocabulary.

The current catalog exposes **29 languages**. English is primary/default and guaranteed fallback. Translation resources are local; runtime does not call an online translation service.

## FTP/FTPS ownership

Only `FtpSession` owns real FTP/FTPS sockets. The shared engine supports:

- FTP;
- Explicit FTPS;
- Implicit FTPS;
- TLS 1.2 / TLS 1.3;
- EPSV with PASV fallback;
- MLSD with LIST fallback;
- UTF-8 negotiation where supported;
- SIZE / REST-assisted transfers;
- standard FTP `NOOP` keepalive.

### Fail-closed transport selection

`FtpConnectionOptions.Security` is validated when a real session is constructed. Undefined enum values fail immediately rather than falling through to plain FTP.

For Explicit FTPS, `AUTH TLS` must return a positive 2xx reply before TLS upgrade. Ghost FTP does not silently downgrade failed FTPS to FTP.

Both desktop renderers require explicit confirmation before real plain FTP because it sends credentials/content without TLS.

### FTPS data protection

After TLS is active, Ghost FTP requires:

```text
PBSZ 0
PROT P
```

The data channel therefore remains protected or the connection fails.

### Certificate validation

FTPS uses normal .NET chain and hostname validation. No trust-all/certificate-bypass switch exists. Revocation uses the operating-system offline cache so Ghost FTP does not intentionally create hidden online CRL/OCSP requests.

## Data-transfer mode integrity

Before any listing, upload or download data channel is consumed, the engine requires successful FTP binary mode:

```text
TYPE I
```

The old best-effort behavior is not used; a server refusing binary mode fails the data operation instead of allowing an unknown prior mode to corrupt content.

## Passive-mode hardening

EPSV is preferred. PASV is used as compatibility fallback. PASV supplies a port, but Ghost FTP deliberately connects the data socket to the **authenticated control host** rather than trusting an arbitrary host embedded in a PASV reply.

## Untrusted server-input bounds

Core bounds include:

- control reply lines: 256;
- control reply text: 1 MiB;
- one reply line: 64 KiB;
- listing payload: 16 MiB;
- recursive traversal depth: 64;
- recursive traversal entries: 100,000;
- transfer queue capacity: 4,096;
- concurrent transfers: 1–8.

FTP command arguments reject unsafe control characters including CR/LF/NUL. Remote paths/names are canonicalized/validated, and root deletion is guarded.

## Remote working-directory consistency

Remote navigation uses server `CWD` followed by `PWD`. The UI's visible path therefore comes from the server-confirmed working directory rather than a client-only assumption.

Late Linux listing results are ignored when they belong to a session that has already been replaced.

## Browser-session health and keepalive

`IFtpSession.KeepAliveAsync` maps to standard server-only FTP `NOOP` for real sessions.

Keepalive:

- defaults to 60 seconds;
- accepts 15–600 seconds;
- `0` disables it;
- skips automatic keepalive in Demo mode;
- exists on Windows and Linux;
- never silently reconnects using saved credentials.

If keepalive proves the current control channel unusable, stale connected state is invalidated. The renderer clears stale remote state and reports connection loss. A late error from an obsolete session must not overwrite a newer session.

## Windows MainWindow composition

The WPF renderer is split into focused partial classes including connection, files, transfer queue, diagnostics, keepalive, responsive layout, workspace actions, helpers and deterministic documentation capture.

`MainWindow.Helpers.cs` owns focus-safe shortcut routing. Destructive file operations require explicit Local/Remote context. Transfer focus routes Delete to transfer cancellation rather than local deletion.

`SiteManagerDialog` is the first-class saved-site editor.

## Linux renderer composition

`LinuxMainWindow` is split across native layout/drawing/input/testing/core/keepalive partials. The renderer shares the same protocol/core model but owns X11 event processing and drawing.

Linux lifecycle rules include:

- successful session assignment only after authentication;
- disposal of failed candidate sessions;
- explicit cancellation handling during initial navigation;
- clearing `_activeOptions` on failure/disconnect;
- ignoring late listing results from replaced sessions;
- server-only keepalive with stale-state invalidation;
- focus-safe destructive shortcuts: transfer selection routes Delete to cancellation, and Local/Remote selection clears transfer selection.

## Transfer queue architecture

Browsing uses the primary control session. Real queued transfers create independent sessions from the current connection options so transfer work cannot consume browser-session replies.

`TransferQueueService` provides:

- queue capacity 4,096;
- 1–8 workers, default 3;
- per-job cancellation;
- queue-wide cancellation;
- retry count and selective transient retry;
- progress/bytes/speed/ETA/start/finish state;
- controlled worker shutdown.

Authentication, certificate, permission and permanent 5xx errors are not blindly retried.

## Download integrity

Downloads use `.ghostftp.part` files. When server `SIZE` is available, resume offsets and final byte length are validated before the partial file is promoted to the requested destination.

A byte-count mismatch remains failed/resumable state rather than being reported as success. These checks are byte-length integrity, not cryptographic file hashing.

## Upload replacement integrity

Uploads use a unique temporary remote path. When replacing a destination, the old destination can be moved to a rollback backup before the temporary upload is renamed into place. Server `SIZE` is used when available to validate temporary/final length. Failure after replacement attempts to restore the backup.

## Local path safety

Downloaded names are sanitized and containment-checked under the selected local root. Windows-only reserved/invalid filename rules are applied on Windows without unnecessarily rewriting Linux-valid names. Recursive uploads skip reparse points/symlinks to avoid unintentional traversal.

## Persistence boundary

Profile/settings files are treated as untrusted local input:

- file-size bounds;
- profile-count/string/blob bounds;
- host/port/enum/path normalization;
- canonical single Demo profile;
- retry/concurrency/timeout/keepalive normalization;
- workspace-geometry normalization;
- atomic temp/replace/backup recovery where implemented.

### Windows secrets

Saved passwords are opt-in and protected by CurrentUser DPAPI.

### Linux secrets

Saved passwords are opt-in and protected by AES-256-GCM using a cryptographically random local 256-bit key. The key file is restricted to the current user (`0600`) where Unix permissions are supported.

This is documented as local file-based protection, not a claim of protection against compromise of the same OS account.

## Session-only Quick Connect

**Keep in this tab** produces a runtime-only profile. Session-only state is `[JsonIgnore]`, filtered again before `ProfileStore.SaveAsync`, never persists the Quick Connect password and disappears when the process exits.

Core self-tests verify that the session-only host/runtime marker do not reach `profiles.json`.

## Local Demo regression architecture

`DemoFtpSession` is a deterministic local-only implementation used for UI exploration and cross-platform regression testing. It never opens a real FTP socket.

`tests/GhostFTP.DemoSelfTest` validates the complete local workflow on Windows and Linux:

```text
connect → diagnostics → PWD/CWD → LIST → NOOP
→ file download/upload round trip → rename
→ create/delete directory → recursive directory round trip
→ conflict protection → cleanup → root-delete protection
→ disconnect reset → reject post-disconnect operations
```

File-vs-directory conflicts are rejected rather than silently replacing one node type with another. The test is intentionally separate from the secret-backed live-server harness.

## Connection Log and diagnostics

Connection Log state is bounded/local. It can show timestamps, host/port/security state, list counts and visible errors but never intentionally records passwords/protected blobs/file contents.

User-initiated diagnostics can issue `NOOP`, `SYST` and `PWD` against the current server and display known `FEAT` capability state. Results are local.

## Windows Setup architecture

Setup is a separate C# WPF maintenance application embedding the architecture-matching Ghost FTP client payload and license text.

Install flow remains:

```text
Language → License → Options → Ready → Install/Update → Finish
```

The license must be accepted before installation. Installation is per-user under `%LOCALAPPDATA%\Programs\GhostFTP` and registers normal Windows Installed Apps metadata.

### Candidate validation

Before active installation changes, Setup stages and validates the application payload and, when required, the maintenance Setup candidate:

- minimum size;
- `MZ` executable signature;
- ProductName = Ghost FTP;
- CompanyName = BRENDIGO LTD;
- exact file version matching Setup;
- candidate file version must not be older than the corresponding installed binary.

### Transactional binary rollback

When updating an existing installation, both active binaries keep independent rollback copies until all later install stages have succeeded. If shortcut/settings/registry or another later stage fails, Setup attempts to restore the previous application and previous maintenance Setup copy. A brand-new partial installation removes newly committed binaries during rollback.

Temporary application/Setup transaction files are cleaned after completion/failure and stale transaction files are removed during uninstall.

### Uninstall metadata

`UninstallString` points to the real interactive maintenance Setup. `QuietUninstallString` is deliberately not advertised until a true non-interactive uninstall mode exists.

## Authentic documentation capture

`GhostFTP.App` recognizes:

```text
--capture-ui <output-directory>
```

Capture mode uses the real compiled `MainWindow`, local profile/settings infrastructure and built-in local Demo session. It renders the production MainWindow and Site Manager to PNG, disposes runtime state and exits.

The canonical MainWindow PNG is **1914×907**. `.github/workflows/capture-ui.yml` rebuilds and refreshes the repository PNGs. The stale decorative hero is not the primary README image.

## Live real-server smoke architecture

`tests/GhostFTP.LiveSmoke` is intentionally separate from deterministic CI. It reads connection values only from environment variables / GitHub Actions secrets and performs:

```text
connect → PWD → optional CWD → LIST → NOOP → disconnect
```

It performs no upload/download/rename/delete/create operation. Plain FTP requires explicit `GHOSTFTP_LIVE_ALLOW_PLAIN=1`. Its own error output redacts configured host/username/password values.

The manual workflow is `.github/workflows/live-smoke.yml`. Real credentials must never be committed to source or workflow text.

## Privacy/network architecture

Ghost FTP has no application telemetry service, analytics client, ad component, crash uploader, cloud profile service or automatic background product-update client.

Normal runtime traffic is limited to:

- operations against the FTP/FTPS server selected by the user;
- optional `NOOP` keepalive on that server session;
- user-initiated diagnostics on that server;
- website links explicitly opened by the user.

Transfer metrics, logs and screenshots remain local application/build state.

## Build/release architecture

Windows packaging creates self-contained x64/ARM64 portable + Setup executables. Linux packaging creates self-contained x64/ARM64 binaries/tarballs. SHA-256 manifests are verified before publication.

The official release workflow creates GitHub Release tag `v0.1.1-beta` and uploads canonical release assets only after its build/test/package gates pass. Stable Windows publication additionally requires trusted Authenticode validation.

## Validation gates

The exact release source is expected to pass:

1. version/channel/product/publisher synchronization;
2. Windows warning-as-error solution build;
3. Linux renderer build;
4. dependency/privacy/platform/security source audit;
5. Core self-tests on Windows and Linux;
6. complete local Demo workflow tests on Windows and Linux;
7. transfer queue tests on Windows and Linux;
8. WPF editable-input/localization smoke tests;
9. authentic production Windows UI capture and 1914×907 validation;
10. real Linux renderer X11/XWayland smoke under Xvfb;
11. Windows x64/ARM64 packaging and executable-version verification;
12. Linux x64/ARM64 packaging and packaged-x64 runtime smoke;
13. SHA-256 verification;
14. trusted Authenticode verification for stable publication;
15. GitHub Release asset publication.

See `SECURITY.md`, `PRIVACY.md`, `docs/UI-PARITY.md`, `docs/PLATFORM-SUPPORT.md`, `docs/LIVE-SMOKE-TEST.md` and `docs/RELEASE-POLICY.md`.
