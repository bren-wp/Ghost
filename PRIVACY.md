# Ghost FTP Privacy

Ghost FTP **0.1.6 Beta** is designed to operate **without application telemetry**, tracking, advertising or hidden product-network activity. Ghost FTP is developed and published by **BRENDIGO LTD**.

## What Ghost FTP does not do

The shipping application does not include application telemetry, usage analytics, advertising SDKs, user fingerprinting, automatic crash-report upload, cloud profile synchronization, a Ghost FTP account requirement, hidden background update checks, remote translation services, marketing beacons or tracking pixels.

Repository audits reject known telemetry/tracking SDK identifiers in shipping C# source.

## Network behavior

Application network access is limited to user-directed FTP/FTPS activity and explicit user actions. This includes control connections, FTP data channels, keepalive, diagnostics and file metadata commands required for the server the user deliberately selected.

Keepalive is **server-only**. When enabled, `NOOP` is sent only to the active FTP/FTPS server. Ghost FTP does not redirect keepalive, transfer state or resume state through a product cloud service.

The 0.1.6 resume-integrity work uses standard FTP `SIZE`, `MDTM` and `REST` commands against the user's selected server. It introduces no BRENDIGO LTD endpoint or third-party service.

## Download resume metadata

When a server exposes both usable `SIZE` and `MDTM`, Ghost FTP can store a small local sidecar next to an interrupted download:

```text
<destination>.ghostftp.part.meta
```

The sidecar is capped at 16 KiB and contains only the resume identity needed to avoid mixing stale bytes:

- metadata format version;
- selected FTP host and port;
- selected FTP security mode;
- normalized remote path;
- remote size;
- remote modification timestamp.

It does **not** contain the username, password, account token or transferred file content. Resume metadata remains on the local device and is never uploaded to BRENDIGO LTD.

If a partial cannot be tied to a trustworthy server identity, Ghost FTP restarts the download rather than retaining unverified resumable state.

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

Portable mode stores Ghost FTP data under the portable executable directory when the portable marker/name is active. That data is local and is not cloud synchronized. Resume sidecars also remain beside the user-selected local transfer destination rather than becoming cloud profile data.

## Settings

Language, appearance, local path, queue concurrency, retries, timeouts, keepalive, window dimensions and splitter/layout values are local preferences. They are not used for advertising or profiling. Settings recovery uses local atomic replacement and bounded backup fallback.

## Transfer queue

Transfer jobs are local runtime state. Pause/resume dispatch, retry/cancellation, queue-history cleanup and progress reporting do not create a server-side queue or cloud coordination service.

## Transfer-buffer privacy

FTP data paths reuse bounded pooled buffers to reduce allocation pressure. Because a buffer may contain user file bytes, it is cleared before being returned to the shared pool. Buffer pooling remains entirely inside the local process.

## Connection log and diagnostics

The connection log is local session information. It is not automatically uploaded. Diagnostics query the connected FTP/FTPS server for operational protocol information and do not deliberately log credentials or resume sidecar content.

## Windows Setup

Setup remains per-user and local. It sends no install analytics, creates no Ghost FTP account and installs no tracking agent/background telemetry service. The maintained `GhostFTP-Setup.exe` handles install/update/uninstall registration rather than generating a separate uninstaller executable.

## Localization

Ghost FTP ships a local **29-language** catalog. English (`en`) is primary/default/fallback. No translation API receives UI text, filenames, server details or credentials.

## Local regression tests

The built-in Demo profile is entirely local and opens no external FTP, telemetry or analytics connection.

`GhostFTP.HardeningSelfTest` and the new `GhostFTP.ResumeSelfTest` use process-local loopback networking only. The resume suite verifies safe REST offset usage, stale-revision restart and in-flight remote mutation handling without contacting BRENDIGO LTD or a third-party server.

## Live-server smoke testing

**Live-server smoke testing** is an explicit development/release action, not hidden application telemetry. The optional CI harness uses explicitly configured credentials and performs only connect/PWD/LIST/NOOP/disconnect. The password is supplied through protected CI secret storage and is not committed to source.

## Third parties

Ghost FTP does not sell user data to advertisers and contains no advertising-network integration. A third-party FTP/FTPS server naturally receives the protocol traffic, credentials and metadata commands necessary for the user's chosen connection; that server's own privacy/security practices remain outside Ghost FTP's control.

## Release/privacy verification

Before public release, audits check for telemetry SDK references, unsupported platform drift, private signing material and dependency/version inconsistencies. Windows/Linux CI verifies local Demo, transfer queue, protocol/parser/lifecycle hardening, dedicated safe-resume integrity, renderer smoke paths and package integrity.

For transport-security details see [`SECURITY.md`](SECURITY.md). For current implementation details see [`docs/releases/v0.1.6.md`](docs/releases/v0.1.6.md).
