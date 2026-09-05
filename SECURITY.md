# Ghost FTP Security

This document describes the active security model for **Ghost FTP 0.1.1 Beta** developed and published by **BRENDIGO LTD**.

## Supported security scope

Ghost FTP is a native FTP/FTPS desktop client. It does not implement SSH/SFTP and does not pretend that FTP and SFTP are interchangeable protocols.

Supported transport modes:

- plain FTP;
- Explicit FTPS (`AUTH TLS`);
- Implicit FTPS;
- TLS 1.2 and TLS 1.3 for FTPS control/data channels.

Explicit FTPS is the recommended default when the server supports it.

## Fail-closed transport selection

`FtpConnectionOptions.Security` is validated when a real `FtpSession` is created. An undefined enum value is rejected with an exception. It cannot silently fall through to plain FTP.

Plain FTP is intentionally treated as a high-risk compatibility mode. Both Windows and Linux clients require an explicit warning/confirmation before credentials are sent without TLS.

For explicit FTPS, `AUTH TLS` must complete with a positive **2xx** reply before Ghost FTP upgrades the control stream. A 3xx intermediate response is not treated as successful TLS negotiation.

## Certificate validation

FTPS uses the normal .NET `SslStream` trust and hostname-validation path.

Ghost FTP deliberately has no:

- trust-all certificate switch;
- `ServerCertificateCustomValidationCallback` that returns `true` unconditionally;
- UI option to bypass hostname validation;
- automatic fallback from failed FTPS to plain FTP.

Certificate revocation uses the operating-system offline cache to avoid introducing hidden online CRL/OCSP traffic from the application itself.

## Protected FTPS data channels

After a TLS control connection is established, Ghost FTP requires:

```text
PBSZ 0
PROT P
```

A server that refuses protected data channels causes the FTPS connection to fail rather than silently transferring files in cleartext.

## Binary transfer integrity

Before any upload, download or listing data channel is used, the shared engine requires successful FTP binary mode (`TYPE I`). A rejected binary-mode command now fails the transfer instead of continuing with an unknown/previous transfer type.

Downloads use `.ghostftp.part` partial files and use server `SIZE` when available to detect a byte-count mismatch before finalization. Uploads use a unique temporary remote filename and, when replacing an existing remote file, a rollback backup before committing the new destination. Server-reported size is checked before and after the upload commit when `SIZE` is available.

These length checks are not cryptographic hashes. A server-side hash capability can be added later only with conservative capability negotiation and explicit interoperability handling.

## FTP command injection protection

User-controlled FTP command arguments pass through `InputGuard`, which rejects CR, LF, NUL and other unsafe command content. Host, port, remote paths and remote single-name operations are validated separately.

Remote paths are canonicalized before use. Traversal-style names received from a directory listing are rejected/ignored by the listing parser rather than being used as local filesystem paths.

## Passive data-connection hardening

Ghost FTP prefers EPSV and falls back to PASV for compatibility. For PASV, the application derives the port from the reply but deliberately connects the data socket to the **authenticated control host**, not to an arbitrary host address supplied inside a PASV response. This reduces passive-mode redirect/bounce exposure.

## Resource-exhaustion boundaries

Untrusted server input is bounded:

- maximum control reply lines: 256;
- maximum control reply characters: 1 MiB;
- maximum individual reply line: 64 KiB;
- maximum directory-listing payload: 16 MiB;
- recursive traversal depth: 64;
- recursive traversal items: 100,000;
- transfer queue capacity: 4,096;
- concurrent real transfers: **1–8**.

Exceeding a limit becomes an explicit failure rather than unbounded memory/recursion growth.

## Transfer-session isolation

Queued real transfers use isolated FTP/FTPS sessions created from the currently authenticated connection options. Browsing/control traffic is not multiplexed onto the same data-transfer control session.

Cancelling one job does not intentionally cancel unrelated queue work. Authentication, permission, permanent FTP 5xx and TLS/certificate failures are not blindly retried.

## Keepalive and stale connection state

Keepalive is supported on Windows and Linux and consists only of standard FTP `NOOP` on the currently selected server connection.

- default: 60 seconds;
- configurable: 15–600 seconds;
- `0` disables keepalive;
- Demo mode is skipped;
- no silent reconnect is performed.

If a Keepalive or connection diagnostic proves the control channel unusable, Ghost FTP invalidates stale connection state and marks the UI offline/lost. A late failure from an obsolete/replaced session is ignored instead of overwriting the state of a newer session.

## Linux connection-lifecycle hardening

The native Linux renderer shares the same `FtpSession` and follows the same security boundaries as Windows:

- explicit warning before plain FTP;
- successful sessions are assigned only after authentication completes;
- candidate sessions are disposed after failed connection attempts;
- cancellation during initial navigation is not misreported as a harmless path fallback;
- active transfer options are cleared on disconnect/failure;
- late directory-listing results from a replaced session are discarded;
- server-only keepalive invalidates stale state without silent reconnect.

## Local path safety

Downloads derive local names through `LocalPathSafety` and containment checks. Resolved destinations must remain under the selected local root.

Windows reserved device names and Windows-invalid characters are escaped on Windows. Linux-valid filenames are not unnecessarily rewritten with Windows-only rules. Recursive local uploads skip reparse points/symlinks to avoid unintended traversal through linked trees.

## Saved credentials

### Windows

Saved passwords are opt-in and protected with CurrentUser DPAPI. Decryption is tied to the same Windows user context.

### Linux

Saved passwords are opt-in and protected with AES-256-GCM using a cryptographically random local 256-bit key. Authenticated encryption detects ciphertext tampering. The key file is restricted to the current user (`0600`) where Unix filesystem permissions are available.

This is local file-based credential protection. It does not claim to protect secrets from a full compromise of the same OS user account and is not represented as a hardware-backed keyring.

## Session-only Quick Connect

**Keep in this tab** creates a memory-only runtime profile. Session-only profiles are marked `[JsonIgnore]`, explicitly filtered from `ProfileStore.SaveAsync`, never persist the Quick Connect password and disappear when the application exits.

A dedicated Core self-test checks that a session-only host and runtime flag do not reach `profiles.json`.

## Local Demo regression boundary

The built-in Demo session is deliberately local-only and is used as a deterministic cross-platform regression target. The 0.1.1 test exercises connect, diagnostics, PWD/CWD, LIST, keepalive, file and recursive-directory round trips, rename, create/delete, conflict protection, cleanup, root-delete protection, disconnect reset and rejection of operations after disconnect.

The Demo test does not contact a real FTP server or any analytics endpoint. File-vs-directory replacement conflicts are rejected so a local regression test cannot silently mutate the Demo tree into an invalid shape.

## Installer integrity and rollback

Windows Setup does not treat an `MZ` header as sufficient payload identity. Before committing an embedded/copied executable it checks:

- minimum expected payload size;
- Windows `MZ` executable signature;
- ProductName = **Ghost FTP**;
- CompanyName = **BRENDIGO LTD**;
- exact file version matching the Setup assembly.

The application payload and, when applicable, the maintenance `GhostFTP-Setup.exe` copy are staged and validated **before** the active installation is modified. Existing client and maintenance-Setup binaries keep independent rollback copies until shortcuts, local language settings and Installed Apps registration have completed. If a later install stage fails, Setup attempts to restore both previous binaries; a first-time installation removes newly committed binaries instead of intentionally leaving a half-installed pair.

Setup also compares the installed and candidate file versions and refuses an older candidate. This prevents a newer local installation from being silently replaced with an older package while still allowing same-version repair/update execution.

Temporary application and Setup staging/rollback files are cleaned after success or failure and stale transaction files are removed during uninstall.

The installed-app registry entry advertises the real interactive uninstall command only. `QuietUninstallString` is removed until a genuine non-interactive uninstall implementation exists.

## Code-signing boundary

Official Windows release packaging supports Authenticode using a PFX supplied only through GitHub Actions secrets:

```text
GHOSTFTP_SIGNING_PFX_BASE64
GHOSTFTP_SIGNING_PFX_PASSWORD
```

Private signing keys must never be committed. A self-signed RSA-3072 development certificate is only for local signing-mechanics tests. It is **not** a SmartScreen/Unknown Publisher solution.

Stable Windows releases require a trusted CA-issued code-signing identity for **BRENDIGO LTD** and the release pipeline rejects stable publication when trusted Authenticode validation is unavailable.

## Live-server testing without credential disclosure

The optional `tests/GhostFTP.LiveSmoke` harness is non-destructive. It reads server information only from environment variables/GitHub Actions secrets and performs connect → PWD → LIST → NOOP → disconnect. It never stores real credentials in repository fixtures.

Plain FTP live testing is disabled unless explicitly opted in with `GHOSTFTP_LIVE_ALLOW_PLAIN=1`.

See `docs/LIVE-SMOKE-TEST.md`.

## Dependency and telemetry policy

Shipping projects contain zero third-party NuGet `PackageReference` entries. Windows uses .NET/WPF; Linux uses the system `libX11.so.6` ABI directly.

The source audit rejects known telemetry/tracking SDK references and tracked private signing-key file types.

Ghost FTP has no application telemetry, analytics SDK, advertising SDK, crash uploader or cloud profile sync.

## Reporting a vulnerability

Do not publish real FTP passwords, private signing material, access tokens, sensitive server listings or other secrets in a public GitHub issue.

When reporting a vulnerability, include the affected Ghost FTP version, platform, reproduction steps that do not disclose production credentials, expected vs actual behavior and whether FTP, Explicit FTPS or Implicit FTPS is involved.

## Release gate

A 0.1.1 Beta release is not considered verified until the exact source commit passes the Windows and Linux build/audit/Core/Demo/queue/UI/runtime/package/checksum gates and the expected assets are published to the matching GitHub Release. Stable 1.0.0 additionally requires trusted Windows Authenticode validation.
