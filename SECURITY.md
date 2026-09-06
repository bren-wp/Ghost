# Ghost FTP Security

This document describes the active security model for **Ghost FTP 0.1.4 Beta** developed and published by **BRENDIGO LTD**.

Ghost FTP is a native FTP/FTPS desktop client for Windows and Linux. It does not implement SSH/SFTP and does not present FTP and SFTP as interchangeable protocols.

## Fail-closed transport selection

Supported modes are:

- plain FTP;
- Explicit FTPS (`AUTH TLS`);
- Implicit FTPS.

Security mode values are validated before network setup. Unsupported enum values fail closed. Plain FTP is never selected as a hidden fallback from a failed TLS connection.

Explicit FTPS requires a successful `AUTH TLS` response before the control channel is upgraded. TLS certificate and hostname validation use the platform trust model; invalid certificates are not silently accepted.

When TLS is active Ghost FTP requires:

- `PBSZ 0`;
- `PROT P`;
- encrypted FTP data channels.

## Control-channel protocol hardening

Ghost FTP 0.1.4 tightens server-reply parsing while retaining standards-compatible behavior.

- FTP reply codes must be numeric values in the `100..599` range.
- When a fourth reply character is present it must use valid single-line (`space`) or multiline (`-`) framing.
- Individual reply lines, multiline line count and total multiline characters are bounded.
- Command/reply reads retain explicit timeout and cancellation boundaries.
- A bounded sequence of preliminary `1xx` greetings is accepted before the required final positive-completion greeting. This supports valid `120 -> 220` servers without permitting an unbounded greeting loop.
- Malformed replies such as `220X ...` fail closed.

The deterministic hardening regression suite exercises both valid preliminary greeting behavior and malformed reply rejection on an in-process loopback FTP server.

## Data-transfer mode integrity

Ghost FTP explicitly requests binary transfer mode with `TYPE I` before send/receive data paths. A server that refuses the required binary mode causes the transfer to fail rather than silently changing data semantics.

Passive-mode handling prefers EPSV and can fall back to PASV. 0.1.4 uses strict passive-response parsing:

- EPSV validates delimiter framing and port range;
- PASV parses exactly the six comma-separated values in the passive tuple;
- every PASV tuple value must be `0..255`;
- only `p1,p2` determine the passive data port;
- unrelated trailing numeric diagnostics cannot alter the selected port.

PASV host redirection is not trusted as an arbitrary network destination: the data connection remains tied to the authenticated control host. This preserves the FTP bounce/redirection defense while still using the server-provided passive port.

## Input and command safety

Untrusted user/server values are bounded and normalized before they become protocol commands.

Protections include:

- host validation;
- port range validation;
- CR/LF/NUL command-injection rejection;
- bounded command arguments;
- single remote-name validation;
- remote-path normalization;
- bounded reply lines/characters;
- bounded directory-listing payload size;
- bounded recursive traversal depth and entry count;
- local path-containment checks.

## Transfer queue safety

The transfer service uses bounded concurrency and isolated transfer sessions where required. The queue has a hard capacity and does not allow unbounded job accumulation.

Queue pause/resume is intentionally a **dispatch pause**:

- queued/retrying jobs wait asynchronously;
- already-running transfers continue;
- cancellation remains per-job;
- no claim is made that a live FTP data stream can be arbitrarily frozen and resumed safely.

0.1.4 hardens queue shutdown. A single disposal owner completes dispatch, releases paused waiters, cancels outstanding work, waits for worker termination, and only then disposes cancellation resources. Concurrent disposal callers await the same completion signal, and enqueue attempts made after shutdown starts fail deterministically.

Retries are bounded and limited to failures classified as transient. Authentication and other permanent failures do not enter an uncontrolled retry loop.

## FTP session lifecycle safety

`FtpSession.DisposeAsync()` is idempotent and coordinated under concurrent callers. Once disposal begins:

- new operations fail with the disposed-session boundary;
- existing serialized work is allowed to leave its gate;
- transport cleanup executes once;
- concurrent disposal callers wait for completion;
- the synchronization gate is not disposed out from under a waiting operation.

This removes teardown races that could otherwise surface nondeterministic `SemaphoreSlim`/transport exceptions during application close or reconnect workflows.

## Local path and delete safety

Ghost FTP normalizes and contains local paths before write/delete operations. Remote root/path protections and bounded recursive operations reduce accidental destructive traversal. User-facing destructive actions require explicit confirmation where configured.

Downloads use partial files and size checks where the server exposes size information. Uploads use temporary remote paths, size verification where available, and rollback-oriented replacement semantics for an existing destination.

## Credential protection

### Windows

Saved passwords are opt-in and protected with the Windows current-user DPAPI security boundary through native `CryptProtectData` / `CryptUnprotectData` calls. Plaintext byte buffers and DPAPI output buffers are explicitly zeroed before release where practical.

### Linux

Saved passwords are opt-in and protected with AES-256-GCM using local per-user key material. Key/profile files receive best-effort private filesystem permissions.

### Session-only Quick Connect

A Quick Connect credential remains session-only unless the user deliberately saves a site/profile. Credentials are never written to the connection log.

## Logging

Connection logging is local application state. Passwords are not logged. Diagnostics expose protocol/server state without intentionally echoing secrets.

## Installer integrity and rollback

Windows Setup stages and validates both the application executable and maintenance Setup candidate before replacing an existing installation.

Validation covers expected Ghost FTP product/publisher/file-version identity. Setup refuses to downgrade an existing newer binary. Existing application and maintenance executables retain independent rollback copies until later install/registration stages succeed. On failure, Setup attempts local rollback and removes first-install partial commits where applicable.

The installed `GhostFTP-Setup.exe` is the maintenance/uninstall entry. A separate uninstaller executable is not generated. `QuietUninstallString` is intentionally not advertised until a genuine silent-uninstall contract exists.

## Dependency boundary

Shipping and regression-test projects are audited to contain zero third-party NuGet `PackageReference` dependencies. Source audits also reject known telemetry/tracking SDK identifiers and private signing-key material committed in source paths.

Native platform integration is limited to audited Windows APIs and the Linux X11 ABI layer used by the native renderer.

## Platform scope

Shipping application targets are Windows and Linux. Source audit rejects Android/iOS/MacCatalyst target frameworks and known mobile source directories. A Web/browser application is not part of the shipping product scope.

## Live-server testing without credential disclosure

The optional real-server smoke harness is non-destructive. It performs connect/PWD/LIST/NOOP/disconnect only and obtains its password through the protected `GHOSTFTP_LIVE_PASSWORD` CI secret. Output is designed to use redacted credential handling. The live test is documented in `docs/LIVE-SMOKE-TEST.md`.

## Local deterministic hardening test

`GhostFTP.HardeningSelfTest` uses a process-local loopback FTP server and no external Internet dependency. It verifies concurrent session disposal, concurrent queue disposal, malformed reply rejection and a real control/data flow with `120 -> 220`, USER/PASS, PWD, TYPE I, EPSV fallback, PASV, LIST and QUIT.

The PASV regression intentionally appends unrelated numeric diagnostics after the valid tuple so the test fails if passive parsing ever regresses to permissive digit extraction.

## Demo regression security

The built-in Demo session is local-only and does not create external FTP, analytics or telemetry connections. Its regression suite exercises file operations, traversal guards, root-delete protection and lifecycle cleanup without exposing a real server.

## Reporting security issues

When reporting a vulnerability, include the affected Ghost FTP version, platform, reproducible steps and expected/observed result. Do not include real FTP passwords, private keys or confidential server content in public issues.

## Release gate

Ghost FTP 0.1.4 is release-ready only after the relevant Windows/Linux CI passes build, dependency/source audit, final hardening audit, Core self-test, Demo workflow where applicable, transfer queue regression, protocol/shutdown hardening self-test, renderer smoke tests, authentic UI capture, packaging and checksum verification.
