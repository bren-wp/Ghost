# Ghost FTP Security

This document describes the active security model for **Ghost FTP 0.1.7 Beta**, developed and published by **BRENDIGO LTD**.

Ghost FTP is a native FTP/FTPS desktop client for Windows and Linux. It supports plain FTP, Explicit FTPS (`AUTH TLS`) and Implicit FTPS. It does not implement SSH/SFTP and does not present SFTP as an FTP security mode.

## Fail-closed transport selection

Security-mode values are validated before network setup. Undefined modes fail closed, and a failed TLS request is never silently retried as plain FTP. Explicit FTPS requires successful `AUTH TLS`; encrypted sessions require `PBSZ 0`, `PROT P`, certificate validation and hostname validation through the platform trust model.

## Control-channel protocol hardening

FTP replies are bounded and framed before use. Reply codes must be numeric `100..599`, the fourth character must use standard space/hyphen framing, line length/multiline count/total characters are bounded, and command/reply work retains cancellation and timeout boundaries. A bounded preliminary `1xx` greeting sequence is accepted before the final service-ready reply; malformed framing fails closed.

## Data-channel integrity

Ghost FTP requires binary mode (`TYPE I`) before upload/download data paths. Passive mode prefers EPSV and can fall back to PASV. EPSV delimiter/port framing and PASV's exact six-byte tuple are validated. Passive data sockets stay tied to the authenticated control host rather than trusting a PASV-supplied host address.

## Safe download resume integrity

The **safe download resume model** introduced in 0.1.6 remains mandatory in 0.1.7. A `.ghostftp.part` file can use REST resume only when a bounded `.ghostftp.part.meta` sidecar proves the same host, port, FTP security mode, normalized remote path, server `SIZE` and server `MDTM` revision.

Resume metadata is versioned, capped at **16 KiB** before deserialization, and contains no username, password, token or transferred file contents. Missing, malformed, oversized, legacy or stale metadata does not authorize resume.

Untrusted staged bytes must be removed before a fresh transfer can continue. If deletion cannot be proven successful, Ghost FTP aborts before `REST` or `RETR` rather than falling back to length-only reuse.

### Staged destination commit

A verifiable download remains in `.ghostftp.part` after data transfer. Ghost FTP rechecks `SIZE` and `MDTM` while any existing destination remains untouched. Only a matching remote revision allows `File.Move(..., overwrite: true)` to commit the staged file. If the remote object changed in flight, staged state is discarded and the previous destination remains byte-for-byte intact.

FTP `SIZE`/`MDTM` metadata is not a cryptographic hash. Ghost FTP does not claim stronger identity guarantees than the selected FTP server provides.

## Parser, input and resource bounds

Server-controlled LIST/MLSD text is bounded per payload, line and MLSD fact count. Unix/Windows LIST parsing uses non-backtracking regular expressions and incremental line enumeration. Command arguments reject CR/LF/NUL injection, host/port/path/name values are normalized through shared guards, directory traversal is bounded, local paths are contained, and transfer queue capacity/concurrency/retries are clamped.

## Transfer queue and lifecycle safety

`TransferQueueService` uses a bounded channel, isolated transfer sessions where appropriate, cancellation isolation and bounded retry. Queue pause/resume gates **dispatch only**; running FTP streams continue. Shutdown releases paused waiters, completes dispatch, cancels work and awaits workers before disposing resources.

`FtpSession.DisposeAsync()` is coordinated and idempotent for concurrent callers. New operations are rejected once shutdown begins and transport cleanup executes once.

## Transfer-buffer confidentiality

FTP data paths rent bounded 128 KiB buffers from `ArrayPool<byte>`. Rented buffers are cleared before pool return because they may contain private file content.

## Local path and destructive-operation safety

Downloads use staged local files and identity validation where server metadata permits it. Uploads use temporary remote paths, size verification where available and rollback-oriented replacement behavior. Root/path protections and explicit destructive confirmations reduce accidental deletion risk.

## Credential protection

Saved passwords are opt-in. Windows uses the current-user DPAPI boundary; sensitive intermediary buffers are zeroed where practical. Linux uses AES-256-GCM with local user-private key material and best-effort private filesystem permissions. Session-only Quick Connect is not persisted unless the user explicitly saves a site.

### Saved-site input boundary

0.1.7 aligns saved-site validation more tightly across renderers. Linux validates profile name, host, port, username and initial remote path before a newly created site is persisted; `ProfileStore` normalizes again at the persistence boundary. Windows Site Manager retains the same shared `InputGuard` boundary. Credentials are never written to the connection log or resume metadata.

## Desktop/UI hardening relevant to security

0.1.7 UI polish does not weaken protocol or storage rules. Windows keeps native WPF editor templates while making focus/selection states clearer. Linux Light theme/window-state corrections affect only local rendering/settings. X11 minimum-window hints prevent a geometry mismatch from shrinking the supported workstation below 980×680; they do not add network privileges or background services.

## Installer integrity and rollback

Windows Setup stages and validates application and maintenance candidates before replacing an installation. Product/publisher/version identity is checked, downgrade is refused, rollback copies are retained through the transaction, and the same installed `GhostFTP-Setup.exe` handles maintenance/uninstall. No separate `uninstall.exe` is generated.

## Dependency, privacy and platform boundary

Shipping/regression projects are audited to contain zero third-party NuGet `PackageReference` dependencies. Audits reject known telemetry/tracking SDK identifiers, private signing material and unsupported mobile targets. Shipping scope is Windows and Linux only; Android, iOS, MacCatalyst/macOS application targets and a Web/browser client are outside this repository's product scope.

## Deterministic regression suites

`GhostFTP.HardeningSelfTest` uses process-local loopback FTP listeners to verify session/queue disposal, malformed replies, parser bounds, EPSV/PASV behavior and real control/data flow. `GhostFTP.ResumeSelfTest` independently verifies exact safe REST resume, stale-identity restart, in-flight remote-revision rejection, preservation of an existing destination and fail-closed stale-partial cleanup before REST/RETR. The Demo suite remains local-only.

The Windows UI smoke suite additionally verifies native editable controls, all 29 application/Setup languages, Setup live language switching, product identity and 0.1.7 shared reference-shell English/Croatian fallback behavior.

## Live-server testing

The optional live-server smoke harness is non-destructive: connect/PWD/LIST/NOOP/disconnect only. Passwords come from protected CI secret storage and are not committed to the repository. See `docs/LIVE-SMOKE-TEST.md`.

## Reporting security issues

Include affected version, platform, reproducible steps and expected/observed result. Never place real FTP passwords, private keys or confidential server content in public issues.

## Release gate

**Ghost FTP 0.1.7 Beta** is release-ready only after exact-head Windows/Linux CI passes build, dependency/source audit, final hardening audit, Core/Demo/Queue/protocol/parser/lifecycle tests, safe download resume-integrity tests, Windows UI smoke, Linux X11/XWayland runtime smoke, authentic UI capture, packaging and checksum/runtime verification. The publication workflow independently reruns the integrity gates before official Windows/Linux assets are complete.
