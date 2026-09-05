# Ghost FTP Security

Ghost FTP 1.7.0 is designed around explicit trust boundaries, conservative FTP/FTPS behavior, bounded untrusted input, local-only persistence, deterministic control-connection state and focus-safe desktop operations.

Ghost FTP is the product. **BRENDIGO LTD** is the developer, publisher and licensor.

## Secure connection defaults

- Explicit FTPS is the default for newly created server profiles.
- TLS 1.2 and TLS 1.3 are supported for FTPS.
- Certificate chain and hostname validation use the normal Windows/.NET validation path.
- There is no "accept any certificate" or trust-all switch.
- Certificate revocation uses the Windows offline cache so Ghost FTP does not create hidden CRL/OCSP web traffic.
- Plain FTP requires an explicit warning confirmation before connection.
- EPSV is preferred with PASV fallback.
- PASV host redirection is not trusted; data channels connect to the authenticated control host.

## FTP command and reply safety

User-controlled FTP command arguments reject CR, LF and NUL characters to prevent command injection.

Control replies are bounded by line and aggregate limits. Malformed replies, socket closure, timeouts and protocol failures propagate as real failures rather than silently becoming unsupported-command results.

Directory listings are bounded before parsing. MLSD is preferred with LIST fallback. Listing entry counts, line lengths, recursive depth and aggregate recursive item counts are constrained.

## Remote path safety

Remote paths are canonicalized before use. Traversal above FTP root is clamped/rejected where appropriate, user-supplied names cannot inject traversal segments, and remote root deletion is blocked.

Ambiguous `MKD 550` responses are verified before being treated as an already-existing directory.

## Server working-directory consistency

Remote navigation uses server `CWD` followed by `PWD`. The visible Remote path is synchronized to the server-confirmed working directory instead of relying on a UI-only assumption.

## Control-channel keepalive and stale state

Ghost FTP can send standard FTP `NOOP` periodically while a real FTP/FTPS server session remains connected.

- Keepalive can be disabled with interval `0`.
- Enabled intervals are constrained to 15–600 seconds.
- It uses only the already selected server session.
- Demo mode does not use network keepalive.
- Failed `NOOP` resets unusable browser transport rather than leaving stale `IsConnected=true` state.
- Genuine control-channel failure during Connection Diagnostics applies the same reset.
- Ghost FTP does not silently reconnect with saved credentials after keepalive failure.

## Download integrity

Downloads use a `.ghostftp.part` file. When server `SIZE` is available, Ghost FTP verifies final partial-file length before promoting it to the requested destination.

A mismatch fails the transfer and leaves the partial file available for later safe resume.

## Upload integrity and replacement

Uploads use a unique temporary remote file. When `SIZE` is available:

1. temporary remote size is checked against local source length;
2. an existing destination can move to rollback backup;
3. temporary upload is renamed into destination;
4. committed destination size is checked again;
5. rollback backup is removed only after successful verification.

If final verification fails, Ghost FTP attempts to remove the invalid destination and restore the previous backup.

This is a byte-length integrity boundary, not a cryptographic checksum claim.

## Transfer retry and concurrency

Automatic retries are configurable from 0–5 and limited to transient conditions such as socket/timeouts and FTP 4xx responses.

Authentication failures, certificate failures, permission problems and permanent FTP 5xx errors are not blindly retried.

Browsing uses the primary FTP/FTPS session. Real queued transfers use independent sessions. Concurrent workers normalize to 1–8 and queue capacity remains bounded at 4,096 jobs.

Cancelling one transfer does not cancel neighboring workers. Queue saturation becomes visible failed state rather than an unhandled WPF exception.

## Transfer measurement safety

Progress, transferred bytes, speed and ETA are local display state. The first current-session progress sample establishes a measurement baseline so resumed partial-file bytes are not misreported as new throughput.

Unknown ETA remains unknown rather than being fabricated.

## Keyboard destructive-action routing

File-operation shortcuts are scoped to the UI region that owns keyboard focus.

- `Delete` on Local/Remote acts only on that active pane.
- `Delete` while Transfers has focus cancels the selected transfer.
- `F2`, `F5`, `Enter`, `Backspace`, `Ctrl+F` and `Ctrl+L` do not silently fall through to Local when a non-file region owns focus.

This focus boundary is treated as a data-safety property.

## Site Manager credential boundary

Ghost FTP 1.7 adds a first-class Site Manager, but it does not introduce a new credential store.

- Site Manager edits cloned profile models until the dialog is accepted.
- Built-in Demo cannot be modified or removed into a real-server profile.
- Saved passwords remain opt-in.
- Password changes pass through the existing `ProfileStore` / Windows DPAPI current-user protection path.
- Passwords are not copied into the Connection Log.
- Invalid security mode, host, port and path data is normalized or rejected by the existing profile/settings boundaries.

Global timeout, retry, keepalive and concurrency policy remains centralized rather than duplicated as unvalidated per-site settings.

## Connection Log boundary

The 1.7 Connection Log is bounded in-memory user-interface state.

It can contain timestamps, host/port/security connection attempts, TLS/plain state, listing counts and visible error summaries. It is not intended to contain passwords, protected password blobs or file contents.

The log can be cleared by the user and is never uploaded as telemetry or a crash report.

## Local filesystem safety

Remote names are sanitized before writing to Windows paths. Local extraction destinations are canonicalized and verified to remain inside the selected destination root.

Recursive local operations do not follow NTFS reparse points/junctions, preventing traversal into an unexpected filesystem tree.

## Profile and settings hardening

Ghost FTP treats local JSON as untrusted input.

- Settings are size-bounded.
- Profile files/backups are size-bounded.
- Saved profile count is bounded.
- Important strings and protected-password blobs are bounded.
- Invalid security enums normalize to FTPS Explicit.
- Invalid hosts are neutralized.
- Stored remote paths are canonicalized.
- Demo profile is canonicalized and duplicate Demo entries are removed.
- Oversized/invalid protected password data is discarded.
- Decrypted saved passwords pass through FTP command-argument guards.
- Concurrent transfers normalize to 1–8.
- Keepalive normalizes to disabled (`0`) or 15–600 seconds.
- Timeout and workspace-geometry bounds remain enforced.

Settings/profile writes use unique temporary files and atomic replacement/backup recovery where supported.

## Password persistence

Password persistence is opt-in through **Remember password**. Saved passwords are protected with Windows DPAPI current-user scope.

Ghost FTP does not implement a cloud credential vault, account synchronization or remote password backup.

## Connection Diagnostics

Diagnostics is user-initiated and communicates only with the selected FTP/FTPS server. It can inspect control-channel health, `SYST`, `PWD`, known `FEAT` capabilities and current TLS/plain transport state.

Results remain local. If diagnostics prove control transport unusable, connection state is reset rather than left stale.

## Authentic documentation capture boundary

`--capture-ui <directory>` is a documentation/build path that renders the actual compiled WPF application.

Security properties:

- capture mode uses built-in Demo only;
- Demo opens no FTP network socket;
- output is limited to PNG files requested by the caller;
- the renderer uses WPF `RenderTargetBitmap` and `PngBitmapEncoder` already present in the Microsoft desktop stack;
- no external image-generation API or tracking endpoint is contacted;
- CI verifies both canonical captures are produced and non-empty.

The canonical repository images are therefore derived from production UI code rather than unrelated mockups.

## Editable-input regression protection

Host, Port, Username, Password, path, filter and dialog fields retain native WPF TextBox/PasswordBox behavior. Shared styling does not replace their editor/content host.

CI runs a live Windows STA smoke test for focusability, tab navigation and value mutation. Source audit rejects known fragile replacement templates.

## Installer/update boundary

The per-user Setup validates the embedded Ghost FTP payload:

- payload must exist;
- payload must exceed a conservative minimum size;
- payload must start with Windows `MZ` signature;
- updates use temporary files and atomic replacement where possible;
- locked/running Ghost FTP causes visible failure;
- installed maintenance Setup copy is validated before use;
- invalid/oversized settings are quarantined or neutralized when Setup persists language.

## Uninstall boundary

The installed `GhostFTP-Setup.exe --uninstall` handles removal. No separate uninstaller executable is generated.

Uninstall verifies required application deletion, removes shortcuts/Installed Apps registration and optionally removes local data when explicitly selected. Setup self-cleanup uses delayed local deletion plus Windows delete-on-reboot fallback.

## No telemetry / tracking runtime

Ghost FTP contains no application telemetry SDK, analytics SDK, advertising SDK, tracking SDK, crash-report upload component, background update checker or cloud profile synchronization component.

Source audit checks for known telemetry/tracking SDK identifiers and zero NuGet `PackageReference` entries in shipping source.

## Reporting security issues

Use the project issue tracker for non-sensitive issues. Do not post passwords, server addresses, private keys, access tokens, session credentials or other secrets publicly.

For sensitive security reports, use a private contact method published by BRENDIGO LTD / Ghost FTP.
