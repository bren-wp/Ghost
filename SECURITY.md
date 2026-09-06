# Ghost FTP Security

This document defines the active security model for **Ghost FTP 0.1.2 Beta**, developed and published by **BRENDIGO LTD**.

## Scope

Ghost FTP is a native FTP/FTPS desktop client for Windows and Linux. FTP, explicit FTPS and implicit FTPS are supported. SSH/SFTP is a different protocol and is not presented as FTP functionality.

The security goal is not to make plaintext FTP safe; it is to make transport selection explicit, prevent accidental security downgrade, validate untrusted server input, isolate transfer work, and keep credentials under the user's local control.

## Fail-closed transport selection

Security mode is an explicit enum validated before a session is created. Unknown enum values are rejected. Plain FTP is not silently selected when an invalid or unsupported mode is supplied.

For explicit FTPS:

1. TCP control transport connects to the requested host/port.
2. Ghost FTP reads a bounded server greeting.
3. `AUTH TLS` must return a successful 2xx reply.
4. TLS is negotiated with normal platform certificate-chain and hostname validation.
5. Ghost FTP sends `PBSZ 0` and requires success.
6. Ghost FTP sends `PROT P` and requires success so data channels are protected.

If TLS negotiation or any required FTPS protection command fails, the connection fails. Ghost FTP does not fall back to plaintext FTP.

Implicit FTPS enters TLS before normal FTP greeting/authentication processing and uses the same certificate validation behavior.

## Plain FTP warning

Plain FTP remains available for compatibility with legacy servers and isolated trusted networks. Before Windows connects in plaintext mode, the application warns that username, password and file data are not TLS protected. Linux follows the same explicit-approval principle.

Users should prefer explicit FTPS when the server supports it.

## FTP command/input boundaries

The shared `InputGuard` validates values used at the protocol boundary:

- host names/IP addresses are length-bounded and syntax-checked;
- ports must be in the range 1–65535;
- command arguments reject CR, LF and NUL control characters;
- remote paths are normalized and bounded;
- remote names are bounded and cannot contain path separators or `.` / `..` traversal names.

Windows 0.1.2 additionally validates Host, Port, Username and Password before DNS resolution or construction of `FtpConnectionOptions`; `FtpSession` validates them again before network use.

Server replies are bounded by maximum line, reply-line count and total reply size. Recursive traversal is bounded by depth and total entry budget.

## Data-transfer protection

Before file send/receive paths, Ghost FTP requires binary transfer mode with `TYPE I`. This avoids platform text conversion/corruption for arbitrary binary payloads.

Passive-data connection handling is tied to the authenticated control host. A server cannot redirect a passive data socket to an arbitrary third-party host merely by advertising a different address in its passive reply.

Encrypted control sessions require encrypted data channels. There is no silent FTPS control-only mode.

## Transfer isolation and lifecycle

Background transfer jobs use bounded concurrency and retry counts. A real-server transfer normally receives its own FTP session created from the active validated options, which limits command interleaving on the interactive control session.

Cancellation is propagated through transfer operations. Connection teardown in the Windows renderer clears authoritative `_session` and `_activeOptions` state before QUIT/disposal so keepalive callbacks and transfer workers cannot observe stale active routing while shutdown is in progress.

Demo mode is local-only and uses no external network connection.

## Local filesystem safety

Local paths are canonicalized before filesystem work. Recursive operations enforce local-root/path relationships rather than trusting textual prefixes. Destructive operations retain confirmation behavior where configured.

Temporary/settings/profile writes use bounded files and atomic replacement patterns where applicable. Sensitive local directories/files receive best-effort private permissions on supported platforms.

## Credential handling

Ghost FTP never writes passwords to the connection log.

Quick Connect credentials remain in the desktop session unless the user explicitly saves a Site Manager profile. **Session-only Quick Connect** entries never persist a password.

Saved passwords are opt-in:

- Windows uses DPAPI with `DataProtectionScope.CurrentUser`;
- Linux uses AES-256-GCM with locally generated per-user key material protected with private file permissions.

There is no cloud credential vault or Ghost FTP account requirement.

## No telemetry/tracking dependency surface

Shipping projects have zero third-party NuGet `PackageReference` entries. Source audit rejects known telemetry, analytics, tracking and automatic crash-upload SDKs. The application has no automatic crash-report upload, advertising service, cloud profile synchronization or hidden background update check.

## Installer integrity and rollback

Windows Setup is self-contained and per-user. It stages application and maintenance Setup candidates before replacing an installed binary. Candidate identity/version checks verify Ghost FTP product/publisher metadata and reject downgrades.

Existing application and maintenance Setup binaries retain independent rollback copies until later install stages succeed. A failure after commit attempts to restore the prior binaries. Uninstall is handled by the maintenance Setup executable; Ghost FTP does not install a separate `uninstall.exe`.

Stable releases require valid trusted Authenticode signatures under the release policy. Beta signing policy is reported in release artifacts even when a trusted production certificate is unavailable.

## Live-server testing without credential disclosure

The optional real-server smoke harness obtains host/user/password from environment variables or GitHub secrets. Password values are redacted and must never be committed or printed.

The live harness is deliberately non-destructive: it connects, verifies working directory/listing and keepalive behavior, then disconnects. It does not upload, rename, create or delete remote data. See `docs/LIVE-SMOKE-TEST.md`.

## Release security gates

A public release must pass:

- Windows and Linux builds;
- source/dependency/platform/privacy audit;
- final hardening audit;
- Core security self-test;
- local Demo end-to-end self-test;
- transfer queue self-test;
- Windows UI smoke test;
- Linux renderer/runtime checks;
- authentic production UI capture;
- package/version/hash verification.

## Reporting a vulnerability

Please report security issues privately to the publisher rather than placing credentials, exploit material or sensitive server details in a public issue. Include the Ghost FTP version, platform, security mode, reproducible steps and the minimum logs necessary to demonstrate the issue; redact secrets.
