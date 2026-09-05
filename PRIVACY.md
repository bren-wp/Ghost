# Ghost FTP Privacy

Ghost FTP is designed to operate without application telemetry, user tracking or hidden background product network activity.

Ghost FTP is developed and published by **BRENDIGO LTD**. This privacy model describes the official Ghost FTP application and Setup behavior in **version 0.1.0 Beta**.

The public version-number reset does not remove or weaken privacy work completed during the preserved internal development history.

## What Ghost FTP does not do

Ghost FTP does not include:

- application telemetry;
- usage analytics;
- advertising SDKs;
- user fingerprinting or behavioral profiling;
- crash-report upload;
- automatic background update checks;
- cloud synchronization of saved servers, profiles or settings;
- background requests to ghostftp.com;
- background requests to the source repository;
- third-party tracking SDKs.

## Network behavior

Ghost FTP opens network connections for the FTP/FTPS server selected by the user and for links the user explicitly opens.

Examples:

1. connecting to an FTP/FTPS server;
2. browsing, uploading, downloading, renaming or deleting data on that server;
3. running Connection Diagnostics against the server that is already connected;
4. optional FTP control-channel keepalive (`NOOP`) while a real server session remains connected;
5. manually opening a Ghost FTP or BRENDIGO LTD website link.

The keepalive interval is configurable from Settings. `0` disables it. When enabled, Ghost FTP sends `NOOP` only to the FTP/FTPS server already selected by the user; it does not create a connection to Ghost FTP, BRENDIGO LTD, GitHub, analytics or an unrelated endpoint.

Ghost FTP does not send connection diagnostics, server metadata, file lists, transfer details, speed/ETA values, Connection Log entries or error details to BRENDIGO LTD.

## FTPS certificate revocation privacy

FTPS uses normal Windows/.NET certificate-chain and hostname validation. Revocation checking uses the Windows offline revocation cache so Ghost FTP does not initiate hidden online CRL/OCSP requests during certificate validation.

## Demo mode

Demo mode is fully local. It does not open an FTP socket, call ghostftp.com, contact a repository endpoint or send data elsewhere. Keepalive is not used in Demo mode.

Its folders, files and transfer behavior are simulated locally for product testing, demonstrations and authentic documentation capture.

## Local data

Installed Ghost FTP stores settings and saved profiles under the current user's local application-data area. Portable builds use a local `Data` directory next to the portable executable.

Local configuration can include:

- appearance preference;
- selected language;
- last local directory;
- delete-confirmation preference;
- hidden/system item visibility;
- transfer retry count;
- concurrent transfer limit;
- keepalive interval;
- connect/command/transfer timeout values;
- window and pane geometry;
- saved server profiles;
- optional protected password data when Remember password is enabled.

These values are not synchronized by Ghost FTP.

## Session-only Quick Connect

Quick Connect is allowed to remain completely ephemeral. The **Keep in this tab** option retains an ad-hoc connection definition only in the current application process so the connection can remain visible in the sidebar during that session.

A session-only entry:

- is marked as memory-only in the runtime model;
- is excluded from JSON serialization;
- is explicitly filtered by `ProfileStore.SaveAsync` even if a caller passes the whole in-memory profile collection;
- never stores the Quick Connect password;
- disappears when Ghost FTP exits;
- is covered by a Core self-test that verifies its host and runtime flag never reach `profiles.json`.

Selecting **Keep in this tab** is therefore different from saving a server through Site Manager. Only an explicit saved-site action enters the persistent profile store.

## Password handling

Passwords are not persisted unless **Remember password** is enabled for a saved profile.

When enabled, the password is protected with Windows DPAPI using current-user scope. Ghost FTP does not upload saved credentials or provide cloud credential synchronization.

The current Site Manager uses the same existing protection path; it does not introduce a new credential store.

## Connection Log privacy

Ghost FTP includes an in-memory Connection Log for user-visible session activity.

The log may contain:

- local timestamps;
- startup/profile-loading status;
- selected host/port/security connection attempts;
- TLS/plain connection state;
- remote directory-listing counts;
- disconnect/lost-session information;
- user-visible error summaries.

The Connection Log does **not** intentionally record passwords, protected DPAPI blobs or file contents. It is bounded in memory, can be cleared by the user and is not uploaded automatically.

## Localization privacy

All 29 supported languages are compiled into local application/Setup C# source. Changing language does not contact a translation provider, download a language pack or report the selected language.

English is the default/fallback language.

## Connection Diagnostics privacy

Connection Diagnostics runs only on demand against the active FTP/FTPS connection. It can execute `NOOP`, `SYST` and `PWD` and display capabilities already known from `FEAT`.

Results stay inside the application. No diagnostic bundle is uploaded automatically.

## Connection keepalive privacy

Keepalive is connection resilience, not product telemetry.

- It is disabled by setting interval to `0`.
- Enabled values are 15–600 seconds.
- It sends standard FTP `NOOP` only over the selected server session.
- It skips Demo mode.
- It sends no analytics payload, file list, transfer metrics or product profile to BRENDIGO LTD.
- If the control connection is unusable, Ghost FTP marks the session lost and requires explicit reconnect rather than silently opening a replacement connection.

## Transfer information privacy

The queue calculates progress, transferred bytes, speed, ETA, retry count and timestamps locally to help the user manage active work.

Ghost FTP does not upload transfer history or queue metrics and does not use them for analytics/profiling.

## Authentic documentation screenshot privacy

The repository screenshot command `--capture-ui <directory>` is intentionally local-only.

Capture mode:

1. forces deterministic dark theme and English documentation UI;
2. uses the built-in Demo profile;
3. renders the actual production MainWindow and Site Manager using WPF;
4. writes PNG files to the requested local directory;
5. does not open a real FTP socket;
6. does not contact an image-generation service, Ghost FTP website, analytics service or telemetry endpoint.

The GitHub Actions workflow that refreshes repository screenshots is build infrastructure. End-user Ghost FTP installations do not contact GitHub Actions.

## Setup and uninstall privacy

`setup.exe` performs local operations:

- display embedded license;
- store selected language locally;
- validate/install the embedded application;
- create shortcuts;
- register per-user Windows Installed Apps metadata;
- update an existing per-user installation;
- remove application/shortcuts/registration during uninstall;
- optionally remove local Ghost FTP data when explicitly requested.

Setup contains no installation analytics, conversion tracking, crash upload or update-service reporting.

## Version/channel privacy note

The active public line uses `VERSION=0.1.0` and `RELEASE_CHANNEL=beta`. The first stable release is reserved for `1.0.0`.

This version/channel distinction affects release labeling and executable metadata only. It does not enable additional telemetry, analytics, account synchronization or product network traffic in Beta builds.

Canonical `portable.exe` and `setup.exe` filenames remain predictable while their internal version metadata follows the active release version. A stable 1.0.0 package must not be produced from a 0.x Beta metadata state.

## Release/build infrastructure

GitHub Actions and Microsoft .NET SDK infrastructure may access their own build services while compiling official releases. That build infrastructure is separate from runtime behavior of Ghost FTP installed on an end-user system.

Official runtime binaries do not call package feeds or GitHub Actions.

## External services selected by the user

When a user connects to an FTP/FTPS server, that server and surrounding network infrastructure can observe connection data according to their own configuration and policy. Plain FTP does not encrypt credentials/content in transit.

Those servers, networks, proxies, DNS providers and operating-system services are outside Ghost FTP's control.

## Product website links

Opening ghostftp.com or brendigo.com is user-initiated. Ghost FTP does not open either site silently at startup or in the background.

## Summary

Ghost FTP's privacy rule is simple: configuration stays local, Connection Log/diagnostics/transfer metrics stay local, and network traffic is limited to the server session the user selected plus links the user explicitly opens. Quick Connect can be retained for the current tab/session without writing that connection definition to disk. Optional keepalive stays on the selected FTP/FTPS connection and can be disabled. Authentic repository screenshots are rendered from Demo mode without a real FTP connection.

Product: https://ghostftp.com  
Repository: https://github.com/bren-wp/Ghost  
Developer/publisher: BRENDIGO LTD