# Ghost FTP Privacy

Ghost FTP **0.1.5 Beta** is designed to operate **without application telemetry**, tracking, advertising or hidden product-network activity. Ghost FTP is developed and published by **BRENDIGO LTD**.

## What Ghost FTP does not do

The shipping application does not include application telemetry, usage analytics, advertising SDKs, user fingerprinting, automatic crash-report upload, cloud profile synchronization, a Ghost FTP account requirement, hidden background update checks, remote translation services, marketing beacons or tracking pixels.

Repository audits reject known telemetry/tracking SDK identifiers in shipping C# source.

## Network behavior

Application network access is limited to user-directed FTP/FTPS activity and explicit user actions. This includes control connections, FTP data channels, keepalive and diagnostics required for the server the user deliberately selected.

Keepalive is **server-only**. When enabled, `NOOP` is sent only to the active FTP/FTPS server. Ghost FTP does not redirect keepalive or transfer state through a product cloud service.

The 0.1.5 parser, transfer-buffer and workstation changes introduce no product-side network service. Deterministic hardening tests use process-local loopback listeners only.

## Quick Connect

**Session-only Quick Connect** is the default privacy boundary. Host, port, username, password and security mode can be used for the current desktop session without creating a persistent saved-site record.

The optional “keep in this tab” workflow creates an in-memory/session-only profile. Persistent storage occurs only after an explicit save action.

## Saved profiles and credentials

Saved site information remains local to the current user/device and is not synchronized to BRENDIGO LTD.

Saved-password protection is opt-in:

- Windows uses the current-user DPAPI boundary;
- Linux uses AES-256-GCM with local user-private key material.

Plaintext passwords are not intentionally logged. Sensitive Windows intermediary buffers are cleared where practical before release.

## Portable mode

Portable mode stores Ghost FTP data under the portable executable directory when the portable marker/name is active. That data is local and is not cloud synchronized.

## Settings

Language, appearance, local path, queue concurrency, retries, timeouts, keepalive, window dimensions and splitter/layout values are local preferences. They are not used for advertising or profiling.

0.1.5 adds persistence for additional workstation dimensions and regression-tests recovery from a corrupted primary settings file through the local bounded `.bak` fallback. No settings data leaves the machine as part of that recovery.

## Transfer queue

Transfer jobs are local runtime state. Pause/resume dispatch, retry/cancellation, queue-history cleanup and progress reporting do not create a server-side queue or cloud coordination service.

0.1.5 reduces local renderer scheduling and coalesces burst completion refreshes. This optimization does not add telemetry or third-party endpoints.

## Transfer-buffer privacy

FTP data paths now reuse bounded pooled buffers to reduce allocation pressure. Because a buffer may contain user file bytes, it is cleared before being returned to the shared pool. Buffer pooling remains entirely inside the local process.

## Connection log and diagnostics

The connection log is local session information. It is not automatically uploaded. Diagnostics query the connected FTP/FTPS server for operational protocol information and do not deliberately log credentials.

## Windows Setup

Setup remains per-user and local. It sends no install analytics, creates no Ghost FTP account and installs no tracking agent/background telemetry service. The maintained `GhostFTP-Setup.exe` handles install/update/uninstall registration rather than generating a separate uninstaller executable.

## Localization

Ghost FTP ships a local **29-language** catalog. English (`en`) is primary/default/fallback. No translation API receives UI text, filenames, server details or credentials.

## Demo mode

The built-in Demo profile is entirely local and opens no external FTP, telemetry or analytics connection.

## Local protocol hardening tests

`GhostFTP.HardeningSelfTest` uses loopback networking only. The 0.1.5 additions test pathological LIST/MLSD input, EPSV/PASV parsing and settings recovery without contacting BRENDIGO LTD or a third-party server.

## Live-server smoke testing

**Live-server smoke testing** is an explicit development/release action, not hidden application telemetry. The optional CI harness uses explicitly configured credentials and performs only connect/PWD/LIST/NOOP/disconnect. The password is supplied through protected CI secret storage and is not committed to source.

## Third parties

Ghost FTP does not sell user data to advertisers and contains no advertising-network integration. A third-party FTP/FTPS server naturally receives the protocol traffic and credentials necessary for the user's chosen connection; that server's own privacy/security practices remain outside Ghost FTP's control.

## Release/privacy verification

Before public release, audits check for telemetry SDK references, unsupported platform drift, private signing material and dependency/version inconsistencies. Windows/Linux CI verifies local Demo, transfer queue, parser/protocol/lifecycle hardening, renderer smoke paths and package integrity.

For transport-security details see [`SECURITY.md`](SECURITY.md). For current implementation details see [`docs/releases/v0.1.5.md`](docs/releases/v0.1.5.md).
