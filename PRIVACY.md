# Ghost FTP Privacy

Ghost FTP **0.1.2 Beta** is designed to operate without application telemetry, analytics, advertising, behavioral tracking or hidden product-network activity. Ghost FTP is developed and published by **BRENDIGO LTD**.

## Local-first product model

Ghost FTP does not require an account. The normal product workflow is server-only: network connections are made to the FTP/FTPS server explicitly supplied by the user. Ghost FTP does not route transfers through a Ghost FTP cloud service.

The application does not include:

- application telemetry;
- usage analytics;
- advertising SDKs;
- user fingerprinting or behavioral profiling;
- automatic crash-report upload;
- cloud profile synchronization;
- cloud credential storage;
- hidden background update checks;
- remote feature flags or experimentation services.

Repository source audits reject known telemetry/tracking SDK references in shipping C# source and reject third-party NuGet `PackageReference` entries.

## Profiles and settings

Settings and saved server profiles are stored locally for the current user or, in portable mode, next to the portable application under its local `Data` directory.

Stored profile data can include:

- display name;
- host and port;
- username;
- selected FTP/FTPS security mode;
- initial remote path;
- user preference to remember a password.

No profile is uploaded to BRENDIGO LTD or a Ghost FTP account.

## Session-only Quick Connect

Quick Connect does not persist credentials by default. If **Keep in this tab** is selected, Ghost FTP may create a session-only entry so the connection remains visible during the current application session. Session-only Quick Connect entries are excluded from the saved profile store and do not persist the password.

A password is stored only when the user explicitly configures a saved Site Manager profile to remember it.

## Saved passwords

Saved passwords are opt-in and protected locally:

- Windows: DPAPI scoped to the current Windows user;
- Linux: AES-256-GCM with local per-user key material and private file permissions where supported.

Protection is intended to prevent casual/plaintext disclosure in profile files. It does not protect a user from malicious software already executing with the same account privileges.

## Connection logs and diagnostics

The Connection Log records local session events such as connect/disconnect state, directory-list completion, transfer status and errors. Password values are not intentionally logged.

Diagnostics are produced locally. Ghost FTP does not automatically upload diagnostics or crash data.

## Network behavior

Application-initiated network activity is limited to explicit FTP/FTPS operations against servers the user chooses. Keepalive uses server-only FTP commands such as `NOOP` against the already connected server.

Demo mode is fully local and performs no external FTP connection.

## Live-server smoke testing

Live-server smoke testing is an optional developer/release workflow documented in `docs/LIVE-SMOKE-TEST.md`. Credentials are supplied through environment variables or GitHub secrets and are not stored in the repository. The harness redacts secrets and is non-destructive: it performs connection, PWD/listing, keepalive and disconnect checks without remote writes.

## Portable mode

Portable mode stores Ghost FTP data under the portable executable directory rather than the normal installed per-user application data location. Users are responsible for protecting the media/folder that contains a portable build and its `Data` directory.

## Setup privacy

Windows Setup is a local installer/maintenance application. It does not require registration or analytics. Installation metadata is written locally for per-user Windows application registration and uninstall support.

Setup and the installed app share the same local language catalog. No online translation service is contacted.

## Languages

Ghost FTP contains 29 local interface languages. English is the primary fallback. Language selection is stored locally and does not create a network request.

## Deleting local data

The Windows maintenance Setup uninstall flow can remove the installed application. When offered, removal of local Ghost FTP data is a separate user choice so uninstalling the binary does not silently destroy saved profiles/settings unless requested.

Portable local data can be removed by deleting the portable `Data` directory after closing Ghost FTP.

## Third parties

Because the shipping app has no analytics/telemetry/advertising SDK, Ghost FTP does not send application-usage data to advertising or analytics providers. When the user connects to an FTP/FTPS server, that server naturally receives connection information necessary to provide the FTP service; its operator controls its own server logs and privacy practices.

## Changes

Privacy-affecting product changes must be reflected in this document and pass the repository privacy/source audits before a public release is considered complete.
