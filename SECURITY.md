# Ghost FTP Security

## Secure defaults

Ghost FTP prioritizes FTPS with valid server certificates. Explicit FTPS is the default for new server profiles.

- TLS 1.2 and TLS 1.3 only for FTPS.
- Standard Windows/.NET certificate-chain and hostname validation.
- Certificate revocation uses the Windows offline cache so FTPS validation does not create hidden CRL/OCSP web requests.
- No "accept any certificate" setting.
- Passive data connections only.
- PASV host values are not trusted; data channels connect to the authenticated control host to reduce FTP bounce/NAT abuse.
- FTP command arguments reject CR, LF and NUL characters to block command injection.
- Control replies have line and total-size limits.
- LIST/MLSD directory-listing payloads are size-bounded before parsing to prevent memory amplification from a malicious or broken server.
- Connection, command and transfer timeouts are enforced.
- Downloads use temporary `.ghostftp.part` files and are promoted only after a successful transfer.
- Uploads use a unique temporary remote name. Existing remote files are moved to a rollback backup before final rename when replacement is required.
- Ambiguous FTP `550` responses from `MKD` are never treated as success without verifying that the target is an accessible existing directory.
- Deleting the FTP root directory is blocked.
- Remote filenames are sanitized before writing to Windows paths and are prevented from escaping the selected local directory.
- Recursive operations enforce both depth and total-entry budgets.
- Aggregate transfer counters use saturating arithmetic so untrusted listing sizes cannot overflow progress totals.
- Local recursive operations protect against NTFS reparse-point/junction expansion.
- Saved passwords are optional and protected using Windows DPAPI.

## Control-channel failure semantics

Optional FTP commands are allowed to return negative FTP reply codes, but transport/protocol failures are not silently converted into an “unsupported command” result.

Malformed replies, unexpected control-socket closure, timeout and other `FtpException` conditions continue to propagate so the caller can transition the session into an error state instead of operating on a desynchronized connection.

## Quick Connect and saved profiles

The Quick Connect form is authoritative. A selected saved profile is reused only while its host, port, username and security mode still match the visible connection fields. Editing those fields therefore cannot silently connect using a different Demo/saved-profile mode.

Plain FTP always requires an explicit warning confirmation before a connection is opened.

## Editable-input safety

Ghost FTP uses native WPF `TextBox` and `PasswordBox` editing behavior for Host, Port, Username, Password, path, filter and dialog fields. The shared visual system styles these controls without replacing their editor/content-host implementation.

This preserves caret movement, focus, selection, Tab navigation, clipboard shortcuts, keyboard layouts and IME behavior. CI includes a real Windows/WPF smoke test that instantiates the shared controls and verifies that editable fields are writable and focusable. Source audit blocks the fragile replacement templates that previously risked breaking input.

## Local persistence hardening

Ghost FTP treats local settings/profile files as untrusted input even though they are stored in the user's profile or portable directory.

- `settings.json` is limited to 1 MiB.
- `profiles.json` and its backup are limited to 8 MiB.
- Saved profiles are limited to 2,048 entries.
- Profile display names, usernames and protected-password blobs are bounded.
- Settings/profile deserialization occurs only after size checks.
- Settings and profiles are written through unique temporary files.
- Existing settings/profile files are atomically replaced with backup recovery where supported by the Windows filesystem.
- Invalid settings fall back to safe defaults or the backup file.
- Invalid/oversized profile data can fall back to the profile backup; it is never silently accepted as valid input.
- Saved profile security enums, host, remote path and credential state are normalized before entering application state.
- Only one Demo record is retained and its connection values are forced to the canonical Ghost FTP Demo values.
- Decrypted saved passwords are passed through the FTP command-argument guard before use.

These limits reduce startup memory amplification and protect recovery behavior from corrupted or manually modified local JSON files.

## Transfer queue boundary

The transfer queue has a bounded capacity. If the queue is saturated, the new transfer remains visible as failed with an explanatory error instead of throwing an unhandled exception into the WPF event pipeline.

Normal queue actions include retry, cancellation of selected jobs, cancel-all and clearing completed/cancelled/failed jobs. Transfer sessions remain isolated from the browser/control session where required so cancellation cannot corrupt an unrelated file-browser command sequence.

## Installer/update boundary

The per-user installer validates its embedded Ghost FTP payload before replacing an installation:

- the embedded payload must exist;
- it must exceed the minimum expected executable size;
- it must start with the Windows `MZ` executable signature;
- updates use `File.Replace` with a temporary backup rather than overwriting the installed executable in-place;
- a running/locked Ghost FTP executable causes setup to fail visibly instead of claiming success;
- uninstall verifies removal of the installed executable and reports failure if the file remains locked;
- optional user-data removal is verified when requested.

The installer does not grant itself administrative privileges and installs to the current user's application directory by default.

## Local file visibility

The **Show hidden and system items** preference only controls which local entries are shown in the file pane. It does not change filesystem permissions or bypass Windows access control. Inaccessible items remain subject to normal Windows permissions.

## UI and installer boundary

`GhostFTP.Design` contains presentation resources, Ghost FTP identity primitives and Windows 11 DWM integration only. It does not own credentials, FTP sockets or server data. The installer uses the same design project but has no access to saved FTP credentials beyond normal filesystem operations performed during install/uninstall.

## Brand integrity

Shipping source, setup, documentation, metadata and artwork use only the Ghost FTP / GhostFTP identity. CI scans both source/document text and repository paths for disallowed legacy identity tokens so another product/vendor identity cannot silently re-enter a release.

## Plain FTP warning

Plain FTP provides no transport encryption. Use it only when required by a trusted server or isolated network. FTPS should be preferred whenever possible.

## Reporting security issues

Use the project's issue tracker for non-sensitive issues. Never post passwords, private server addresses, private keys, session credentials or other secrets in public issues.
