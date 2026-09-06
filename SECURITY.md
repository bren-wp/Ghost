# Ghost FTP Security

This document describes the active security model for **Ghost FTP 0.1.3 Beta** developed and published by **BRENDIGO LTD**.

Ghost FTP is a native FTP/FTPS desktop client for Windows and Linux. It does not implement SSH/SFTP and does not present FTP and SFTP as interchangeable protocols.

## Fail-closed transport selection

Supported modes are:

- plain FTP;
- Explicit FTPS (`AUTH TLS`);
- Implicit FTPS.

Security mode values are validated before network setup. Unsupported enum values fail closed. Plain FTP is never selected as a hidden fallback from a failed TLS connection.

Explicit FTPS requires a successful `AUTH TLS` response before the control channel is upgraded. TLS certificate and hostname validation use the platform's normal trust model; invalid certificates are not silently accepted.

When TLS is active Ghost FTP requires:

- `PBSZ 0`;
- `PROT P`;
- encrypted FTP data channels.

## Data-transfer mode integrity

Ghost FTP explicitly requests binary transfer mode with `TYPE I` before send/receive data paths. A server that refuses the required binary mode causes the transfer to fail rather than silently changing data semantics.

Passive-mode handling prefers EPSV and can fall back to PASV. PASV host redirection is not trusted as an arbitrary network destination: the data connection remains tied to the authenticated control host.

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
- bounded recursive traversal depth and entry count;
- local path-containment checks.

## Transfer queue safety

The transfer service uses bounded concurrency and isolated transfer sessions where required. The queue has a hard capacity and does not allow unbounded job accumulation.

0.1.3 adds queue pause/resume. This is intentionally a **dispatch pause**:

- queued/retrying jobs wait asynchronously;
- already-running transfers continue;
- cancellation remains per-job;
- disposing the queue releases paused waiters before shutdown;
- no claim is made that a live FTP data stream can be arbitrarily frozen and resumed safely.

Retries are bounded and limited to failures classified as transient. Authentication and other permanent failures do not enter an uncontrolled retry loop.

## Local path and delete safety

Ghost FTP normalizes and contains local paths before write/delete operations. Remote root/path protections and bounded recursive operations reduce accidental destructive traversal. User-facing destructive actions require explicit confirmation where configured.

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

Shipping projects are audited to contain zero third-party NuGet `PackageReference` dependencies. Source audits also reject known telemetry/tracking SDK identifiers and private signing-key material committed in source paths.

Native platform integration is limited to audited Windows APIs and the Linux X11 ABI layer used by the native renderer.

## Platform scope

Shipping application targets are Windows and Linux. Source audit rejects Android/iOS/MacCatalyst target frameworks and known mobile source directories.

## Live-server testing without credential disclosure

The optional real-server smoke harness is non-destructive. It performs connect/PWD/LIST/NOOP/disconnect only and obtains its password through the protected `GHOSTFTP_LIVE_PASSWORD` CI secret. Output is designed to use redacted credential handling. The live test is documented in `docs/LIVE-SMOKE-TEST.md`.

## Demo regression security

The built-in Demo session is local-only and does not create external FTP, analytics or telemetry connections. Its regression suite exercises file operations, traversal guards, root-delete protection and lifecycle cleanup without exposing a real server.

## Reporting security issues

When reporting a vulnerability, include the affected Ghost FTP version, platform, reproducible steps and expected/observed result. Do not include real FTP passwords, private keys or confidential server content in public issues.

## Release gate

Ghost FTP 0.1.3 is release-ready only after the relevant Windows/Linux CI passes build, dependency/source audit, final hardening audit, Core self-test, Demo workflow, transfer queue regression, renderer smoke tests, authentic UI capture, packaging and checksum verification.
