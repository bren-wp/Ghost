# Ghost FTP Security

This document describes the active security model for **Ghost FTP 0.1.5 Beta** developed and published by **BRENDIGO LTD**.

Ghost FTP is a native FTP/FTPS desktop client for Windows and Linux. It does not implement SSH/SFTP and does not present FTP and SFTP as interchangeable protocols.

## Fail-closed transport selection

Supported modes are plain FTP, Explicit FTPS (`AUTH TLS`) and Implicit FTPS. Security-mode values are validated before network setup; undefined values fail closed. A failed TLS request is never silently retried as plain FTP.

Explicit FTPS requires a successful `AUTH TLS` reply before control-channel upgrade. TLS certificate and hostname validation use the platform trust model. When TLS is active Ghost FTP requires `PBSZ 0`, `PROT P` and encrypted FTP data channels.

## Control-channel protocol hardening

FTP control replies are strictly bounded and framed:

- reply codes must be numeric `100..599` values;
- the fourth reply character, when present, must be a space or hyphen;
- reply-line length, multiline count and total characters are bounded;
- command/reply operations retain timeout and cancellation boundaries;
- a bounded preliminary `1xx` greeting sequence is accepted before the final positive-completion greeting;
- malformed responses such as `220X ...` fail closed.

The deterministic loopback hardening suite verifies valid `120 -> 220` interoperability and malformed-reply rejection.

## Data-transfer mode integrity

Ghost FTP explicitly requires binary transfer mode with `TYPE I` before send/receive data paths. Refusal by the server fails the transfer rather than silently changing data semantics.

Passive mode prefers EPSV and can fall back to PASV. EPSV validates its delimiter framing and port; PASV parses exactly six tuple values, validates every value as `0..255`, and derives the data port only from `p1,p2`. Data sockets remain tied to the authenticated control host rather than trusting a PASV-supplied host.

0.1.5 adds deterministic coverage for a valid non-default EPSV delimiter and for malformed PASV tuples that must be rejected before a data connection is attempted.

## Listing parser resource bounds

Server-controlled LIST/MLSD text is treated as untrusted input.

0.1.5 adds additional parser hardening:

- each LIST/MLSD line is bounded;
- MLSD fact count is bounded per entry;
- Unix and Windows LIST patterns use the .NET non-backtracking regex engine;
- listing lines are enumerated incrementally rather than creating another full split/copy of the payload;
- Unix symlink ` -> target` metadata is removed before validating the safe entry name;
- symlink targets are never followed as directories by parser semantics.

These measures reduce CPU/memory exposure to pathological server listings while preserving standards-compatible entries.

## Input and command safety

Untrusted user/server values are bounded and normalized before becoming protocol commands. Protections include host and port validation, CR/LF/NUL command-injection rejection, bounded command arguments, single remote-name validation, remote-path normalization, bounded directory-listing payload size, bounded recursive traversal depth/entry count and local path-containment checks.

## Transfer queue safety

`TransferQueueService` uses a bounded channel, clamped concurrency, isolated transfer sessions where appropriate, per-job cancellation and bounded transient retries.

Queue pause/resume is intentionally a **dispatch pause**. Queued/retrying jobs wait asynchronously, already-running transfers continue, and no claim is made that an arbitrary active FTP data stream can be frozen safely.

Shutdown remains coordinated: a single disposal owner releases paused waiters, stops dispatch, cancels work, waits for workers and then disposes cancellation resources. Concurrent disposal callers await the same completion signal; post-shutdown enqueue fails deterministically.

0.1.5 reduces renderer pressure without weakening transfer state: progress delivery is throttled to an appropriate UI cadence and terminal states remain immediate.

## Transfer-buffer confidentiality

0.1.5 uses bounded pooled 128 KiB buffers for FTP data streams to reduce repeated large-object allocation. Because those buffers may hold user file contents, they are explicitly cleared before returning to the shared pool.

## FTP session lifecycle safety

`FtpSession.DisposeAsync()` remains idempotent and coordinated under concurrent callers. Once disposal begins, new operations fail, existing serialized work can unwind, transport cleanup executes once and concurrent disposal callers wait for the shared completion state.

## Local path and destructive-operation safety

Ghost FTP normalizes and contains local paths before write/delete operations. Remote root protections and bounded recursive operations reduce destructive traversal risk. User-facing destructive operations require confirmation when configured.

Downloads use partial files and size validation where server metadata permits it. Uploads use temporary remote paths, size verification where available and rollback-oriented replacement semantics for an existing destination.

## Credential protection

### Windows

Saved passwords are opt-in and protected by the current-user Windows DPAPI boundary through native `CryptProtectData` / `CryptUnprotectData`. Sensitive plaintext/intermediate buffers are explicitly zeroed where practical before release.

### Linux

Saved passwords are opt-in and protected with AES-256-GCM using local user-private key material. Key/profile files receive best-effort private filesystem permissions.

### Session-only Quick Connect

Quick Connect stays session-only unless the user deliberately saves a site/profile. Passwords are never written to the connection log.

## Installer integrity and rollback

Windows Setup stages and validates the application executable and maintenance Setup candidate before replacing an installation. Validation covers Ghost FTP product/publisher/file-version identity. Setup refuses downgrade of newer installed binaries, retains rollback copies through the transaction and attempts rollback after later-stage failure.

The installed `GhostFTP-Setup.exe` is the maintenance/uninstall entry. A separate uninstaller executable is not generated. `QuietUninstallString` is not advertised until a genuine tested silent-uninstall contract exists.

## Dependency boundary

Shipping and regression-test projects are audited to contain zero third-party NuGet `PackageReference` dependencies. Audits also reject known telemetry/tracking SDK identifiers, private signing-key material and unsupported mobile targets.

## Platform scope

Shipping application targets are Windows and Linux. Android, iOS, MacCatalyst/macOS application targets and a Web/browser client are outside this repository's shipping scope.

## Live-server testing without credential disclosure

The optional real-server smoke harness is non-destructive: connect/PWD/LIST/NOOP/disconnect only. Its password comes from protected `GHOSTFTP_LIVE_PASSWORD` CI secret storage and is not committed to the repository. See `docs/LIVE-SMOKE-TEST.md`.

## Local deterministic hardening test

`GhostFTP.HardeningSelfTest` uses process-local loopback FTP listeners and no Internet dependency. It verifies concurrent session/queue disposal, malformed reply rejection, LIST/MLSD resource bounds, safe symlink parsing, custom-delimiter EPSV, malformed PASV rejection and real control/data flow.

## Demo regression security

The built-in Demo session is local-only and creates no external FTP, analytics or telemetry connection. It exercises file operations, traversal guards, root-delete protection and lifecycle cleanup without exposing a real server.

## Reporting security issues

Include the affected Ghost FTP version, platform, reproducible steps and expected/observed result. Do not place real FTP passwords, private keys or confidential server content in public issues.

## Release gate

Ghost FTP 0.1.5 is release-ready only after exact-head Windows/Linux CI passes build, dependency/source audit, final hardening audit, Core self-test, Demo workflow, transfer queue regression, protocol/parser/settings hardening self-test, renderer smoke tests, authentic UI capture, packaging and checksum/runtime verification.
