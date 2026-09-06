# Ghost FTP Privacy

Ghost FTP **0.1.3 Beta** is designed to operate **without application telemetry**, tracking, advertising or hidden product-network activity. Ghost FTP is developed and published by **BRENDIGO LTD**.

## What Ghost FTP does not do

The shipping application does not include:

- application telemetry;
- usage analytics;
- advertising SDKs;
- user fingerprinting or behavioral profiling;
- automatic crash-report upload;
- cloud profile synchronization;
- a Ghost FTP account requirement;
- hidden background update checks;
- remote translation services;
- marketing beacons or tracking pixels.

Source audit rejects known telemetry/tracking SDK identifiers in shipping C# source.

## Network behavior

The application performs network access for user-directed FTP/FTPS activity. This includes control connections, FTP data channels, keepalive and explicit diagnostics required for the connected server.

Keepalive is **server-only**: when enabled, `NOOP` traffic is sent only to the FTP/FTPS server the user deliberately connected to. Ghost FTP does not redirect keepalive through a product cloud service.

Links opened by a user may launch the system browser. The desktop application itself does not need a Ghost FTP cloud account to transfer files.

## Quick Connect

**Session-only Quick Connect** is the default privacy boundary. Host, port, username, password and security mode can be used for the current desktop session without creating a persistent saved-site record.

The optional “keep in this tab” behavior creates only an in-memory/session-only profile. It is not written to persistent profile storage.

A persistent site is created only through an explicit save action.

## Saved profiles

Saved site information is stored locally for the current user/device. Profiles are not uploaded to BRENDIGO LTD or synchronized between devices by Ghost FTP.

A saved password is opt-in:

- Windows protects saved password data with the current-user Windows DPAPI boundary;
- Linux protects saved password data with AES-256-GCM and local user-private key material.

Plaintext passwords are not intentionally written to logs. The Windows protector also zeros sensitive plaintext/intermediate unmanaged buffers where practical before release.

## Portable mode

Portable mode stores its application data under the portable executable directory when the portable marker/name is active. That allows the user to control where portable profiles/settings live. Portable data is still local and is not cloud synchronized.

## Settings

Settings such as language, appearance, local path, queue concurrency, retries, timeouts, keepalive and pane dimensions are local preferences. They are not used to build an advertising profile.

## Transfer queue

Transfer jobs are local runtime state. 0.1.3 adds pause/resume dispatch control and more queue-history operations. These features do not introduce a server-side queue or cloud coordination service.

Queue pause waits locally and asynchronously. Running transfers remain attached only to their user-selected FTP/FTPS server.

## Connection log and diagnostics

The connection log is local session information intended to help the user understand connection/transfer behavior. It is not automatically uploaded.

Diagnostics query the connected FTP/FTPS server for protocol/server information. Credentials are not deliberately logged.

## Windows Setup

Setup is per-user and designed to install/update/uninstall locally. It does not send install analytics. The Setup UI explicitly describes the local-only/no-telemetry behavior.

The installed maintenance `GhostFTP-Setup.exe` handles update/uninstall registration rather than generating a separate background service or tracking agent.

## Localization

Ghost FTP ships a local 29-language catalog. English (`en`) is primary/default/fallback. No translation API receives user UI content or connection details.

## Demo mode

The built-in Demo profile is entirely local and opens no external FTP, telemetry or analytics connection. It exists for user exploration and deterministic regression testing.

## Live-server smoke testing

**Live-server smoke testing** is part of development/release verification, not hidden application telemetry. The optional CI harness uses explicitly configured test credentials and performs a non-destructive connect/PWD/LIST/NOOP/disconnect sequence. The password is supplied through protected CI secret storage and is not committed to the repository.

## Third parties

Ghost FTP does not sell user data to advertisers and the application contains no advertising network integration. When the user connects to a third-party FTP/FTPS server, that server naturally receives the network traffic and credentials required for the chosen protocol. Its own privacy/security practices are outside Ghost FTP's control.

## Release/privacy verification

Before a public release, repository audits check for known telemetry SDK references, mobile-scope drift, private signing material and dependency/version inconsistencies. Windows/Linux tests verify the local Demo workflow and core transfer behavior.

For transport-security details see [`SECURITY.md`](SECURITY.md). For the current release implementation details see [`docs/releases/v0.1.3.md`](docs/releases/v0.1.3.md).
