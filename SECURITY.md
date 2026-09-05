# Ghost FTP Security

Ghost FTP 1.6.0 is designed around explicit trust boundaries, conservative FTP/FTPS behavior, bounded untrusted input, local-only persistence and deterministic control-connection state.

Ghost FTP is the product. **BRENDIGO LTD** is the developer, publisher and licensor.

## Secure connection defaults

- Explicit FTPS is the default for newly created server profiles.
- TLS 1.2 and TLS 1.3 are supported for FTPS.
- Certificate chain and hostname validation use the normal Windows/.NET validation path.
- There is no "accept any certificate" or trust-all switch.
- Certificate revocation is configured to use the Windows offline cache so Ghost FTP does not create hidden CRL/OCSP web traffic.
- Plain FTP requires an explicit warning confirmation before the connection is opened.
- Passive data connections are used.
- EPSV is preferred with PASV fallback.
- PASV host redirection is not trusted; data channels connect to the authenticated control host.

## FTP command and reply safety

User-controlled FTP command arguments reject CR, LF and NUL characters to prevent command injection.

Control replies are bounded by line and aggregate limits. Malformed replies, unexpected socket closure, timeouts and protocol failures are propagated as real failures instead of being silently treated as unsupported optional commands.

Directory listings are bounded before parsing. MLSD is preferred when available, with LIST fallback. Listing entry counts, line lengths, recursive depth and total recursive item counts are constrained.

## Remote path safety

Remote paths are canonicalized before use. Path traversal above the FTP root is clamped or rejected where appropriate, user-supplied names cannot contain traversal segments, and remote root deletion is blocked.

Ambiguous `MKD 550` responses are never assumed to mean "already exists". Ghost FTP verifies that the target directory is accessible before treating the operation as successful.

## Server working-directory consistency

Remote navigation uses server `CWD` and then `PWD`. The visible Remote path is synchronized to the server-confirmed working directory instead of relying on a UI-only path assumption.

This reduces path drift on servers that implement relative working-directory semantics differently from a purely client-side path model.

## Control-channel keepalive and stale-state handling

Ghost FTP can send the standard FTP `NOOP` command periodically while a real FTP/FTPS server session remains connected.

- Keepalive is configurable and can be disabled with an interval of `0`.
- Enabled intervals are constrained to 15–600 seconds.
- Keepalive uses the existing user-selected server session only; it does not contact a product or analytics endpoint.
- Demo mode does not use keepalive.
- A failed `NOOP` resets the unusable browser transport instead of leaving a stale `IsConnected=true` state.
- A genuine control-channel failure during Connection Diagnostics applies the same transport reset.
- Ghost FTP does not silently reconnect with saved credentials after a keepalive failure. Reconnection remains an explicit user action.

This prevents later file operations from being issued against a connection the application already knows is no longer trustworthy.

## Download integrity

Downloads use a `.ghostftp.part` file. When the server supports `SIZE`, Ghost FTP verifies the final partial-file length against the expected remote length before promoting the partial file to the requested destination.

A mismatch is treated as a failed transfer. The partial file remains available for a later resume instead of being renamed into a misleading successful destination file.

## Upload integrity and replacement

Uploads use a unique temporary remote file. When `SIZE` is available:

1. the temporary remote size is checked against the local source length;
2. an existing destination is moved to a rollback backup;
3. the temporary upload is renamed into the destination;
4. the committed destination size is checked again;
5. the rollback backup is removed only after the final size check succeeds.

If final verification fails, Ghost FTP attempts to remove the invalid destination and restore the previous backup.

This is a byte-length integrity check based on FTP `SIZE`; it is not a cryptographic checksum claim.

## Transfer retry policy

Automatic retries are configurable from 0 to 5 attempts and are limited to transient failures such as socket/timeouts and FTP 4xx replies.

Authentication failures, TLS/certificate failures, permission problems and permanent FTP 5xx errors are not blindly retried. Cancellation during retry backoff is scoped to the affected transfer and must not terminate other queue workers.

## Transfer isolation and concurrency

Browsing uses the primary FTP/FTPS session. Queued transfers use independent sessions where required so a long upload/download, cancellation or retry cannot consume control replies intended for the browser session.

The concurrent transfer limit is configurable from 1–8 and normalized before use. The queue remains bounded at 4,096 jobs. Queue saturation becomes a visible failed job rather than an unhandled WPF exception.

Transfer progress, speed and ETA are display-only local state. Speed measurement establishes a baseline from the first current-session sample so bytes from an existing resumed partial transfer are not incorrectly counted as newly transferred throughput.

## Keyboard destructive-action routing

File-operation shortcuts are scoped to the UI pane that actually owns keyboard focus.

- `Delete` on Local/Remote acts only on that active file pane.
- `Delete` while Transfers has focus cancels the selected transfer instead of falling through to a local delete action.
- `F2`, `F5`, `Enter`, `Backspace`, `Ctrl+F` and `Ctrl+L` do not silently route to the Local pane when a non-file region has focus.

This removes ambiguous focus fallback from destructive operations and is part of the desktop safety boundary.

## Local filesystem safety

Remote names are sanitized before being written to Windows paths. Local extraction destinations are canonicalized and checked to remain inside the selected destination root.

Recursive local operations do not follow NTFS reparse points/junctions. This prevents recursive upload/delete from unexpectedly escaping into another filesystem tree.

## Profile and settings hardening

Ghost FTP treats local JSON as untrusted input.

- Settings are size-bounded.
- Profile files and backups are size-bounded.
- Saved profile count is bounded.
- Important profile strings and protected-password blobs are bounded.
- Invalid security enum values normalize to FTPS Explicit.
- Invalid stored hosts are neutralized.
- Stored remote paths are canonicalized.
- The Demo profile is canonicalized and duplicate Demo entries are removed.
- Oversized or invalid protected-password data is discarded.
- Decrypted saved passwords pass through the FTP command-argument guard before use.
- Concurrent transfers normalize to 1–8.
- Keepalive normalizes to disabled (`0`) or 15–600 seconds.
- Existing timeout and workspace-geometry bounds remain enforced.

Settings/profile writes use unique temporary files and atomic replacement/backup recovery where supported.

## Password persistence

Password persistence is opt-in through **Remember password**. Saved passwords are protected with Windows DPAPI using the current-user scope.

Ghost FTP does not implement a cloud credential vault, account synchronization or remote password backup.

## Connection Diagnostics

Connection Diagnostics is user-initiated and communicates only with the FTP/FTPS server already selected by the user. It can inspect control-channel health, `SYST`, `PWD`, known `FEAT` capabilities and current TLS/plain transport state.

Diagnostic results remain local and are not uploaded to Ghost FTP or BRENDIGO LTD. If diagnostics prove the control channel is unusable, the connection state is reset rather than left stale.

## Editable-input regression protection

Host, Port, Username, Password, path, filter and dialog fields retain native WPF TextBox/PasswordBox editing behavior. The shared design layer styles these controls without replacing their editor/content host.

CI runs a real Windows STA smoke test that verifies focusability, tab navigation and value mutation for shared editable controls. The source audit rejects fragile replacement templates known to risk caret/focus/input regressions.

## Installer and update boundary

The per-user Setup validates the embedded Ghost FTP payload before installation:

- embedded payload must exist;
- payload must exceed a conservative minimum size;
- payload must start with the Windows `MZ` signature;
- updates use temporary files and atomic replacement semantics where possible;
- a locked/running Ghost FTP executable causes Setup to fail visibly;
- the installed maintenance Setup copy is validated before use;
- invalid or oversized settings are quarantined/neutralized rather than trusted when Setup persists the selected language.

## Uninstall boundary

The same installed `GhostFTP-Setup.exe --uninstall` handles uninstall. No separate uninstaller executable is generated.

Uninstall verifies required application deletion, removes shortcuts and Installed Apps registration, and optionally removes local data when explicitly selected. Because a running process cannot reliably delete its own executable immediately, Setup registers Windows delete-on-reboot as a fallback while also attempting delayed local self-cleanup after process exit.

## No telemetry / tracking runtime

Ghost FTP contains no application telemetry SDK, analytics SDK, advertising SDK, tracking SDK, crash-report upload component, background update checker or cloud profile synchronization component.

The source audit checks for known telemetry/tracking SDK identifiers and zero NuGet `PackageReference` entries in shipping source.

## Reporting security issues

Use the project issue tracker for non-sensitive issues. Do not post passwords, server addresses, private keys, access tokens, session credentials or other secrets in public issues.

For sensitive security reports, use a private contact method published by BRENDIGO LTD / Ghost FTP rather than a public issue.
