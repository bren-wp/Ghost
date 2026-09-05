# Ghost FTP Privacy

Ghost FTP **0.1.0 Beta** is designed to operate without application telemetry, tracking, advertising or hidden product-network activity. Ghost FTP is developed and published by **BRENDIGO LTD**.

## What Ghost FTP does not do

Ghost FTP does not include:

- application telemetry;
- usage analytics;
- advertising SDKs;
- user fingerprinting or behavioral profiling;
- automatic crash-report upload;
- cloud synchronization of saved sites, credentials or settings;
- automatic background product-update checks;
- background requests to ghostftp.com or brendigo.com;
- background requests to the GitHub repository;
- third-party tracking SDKs.

Transfer speed, ETA, retry state, Connection Log entries and server diagnostics remain local application state.

## Runtime network behavior

Ghost FTP opens network connections only when required by an explicit user workflow:

1. FTP/FTPS traffic to the server the user selected;
2. browsing/transfers/rename/delete/create operations requested on that server;
3. Connection Diagnostics against the already selected server;
4. optional server-session keepalive using FTP `NOOP`;
5. a website link the user explicitly chooses to open.

The application does not send server metadata, directory listings, transfer history, error details, saved profiles or settings to BRENDIGO LTD.

## Keepalive privacy

Keepalive is a connection-resilience feature, not telemetry.

- default interval: 60 seconds;
- configurable interval: 15–600 seconds;
- setting `0` disables it;
- the command is standard FTP `NOOP`;
- it is sent only to the FTP/FTPS server already selected by the user;
- Demo mode is skipped;
- Windows and Linux use the same server-only keepalive contract;
- a failed keepalive marks stale session state lost rather than silently creating a replacement connection.

No Ghost FTP, BRENDIGO LTD, GitHub, analytics or advertising endpoint is contacted by keepalive.

## FTPS certificate-validation privacy

FTPS uses normal .NET certificate-chain and hostname validation. Revocation checking uses the operating-system offline revocation cache so Ghost FTP itself does not create hidden online CRL/OCSP requests during certificate validation.

Ghost FTP does not provide a trust-all certificate switch.

## Plain FTP warning

Plain FTP sends credentials and file data without TLS. Both Windows and Linux require explicit user confirmation before opening a real plain-FTP session. Ghost FTP does not silently downgrade a failed FTPS request to plain FTP.

## Demo mode

The built-in `GhostFTP Demo` profile is fully local. It does not open an FTP socket, contact ghostftp.com, call GitHub or send data to another service.

Demo mode is used for local product exploration, deterministic tests and authentic repository UI capture.

## Local data locations

Installed Windows builds store settings/profiles under the current user's local application-data area. Windows portable builds store local data next to the portable executable in their `Data` directory.

Linux uses the current user's Ghost FTP data directory. User-local install/uninstall scripts do not create a Ghost FTP cloud account or background daemon.

Local data can contain:

- language and appearance preferences;
- last local directory;
- hidden/system-file preference;
- transfer retry/concurrency settings;
- keepalive and timeout settings;
- window/pane geometry;
- saved server profiles;
- optional protected saved-password data.

These values are not synchronized by Ghost FTP.

## Session-only Quick Connect

**Session-only Quick Connect** is the privacy boundary behind **Keep in this tab**.

When enabled, the ad-hoc connection definition remains in application memory for the current process only. It is **excluded from JSON** serialization and is also explicitly filtered by `ProfileStore.SaveAsync`.

A session-only entry:

- never persists its runtime-only marker;
- never persists the Quick Connect password;
- never survives application exit;
- is separate from an explicit Site Manager save action;
- is covered by a Core self-test that verifies the session-only host does not reach `profiles.json`.

## Saved password protection

Saving a password is opt-in.

### Windows

Saved passwords use Windows CurrentUser DPAPI. Ghost FTP does not upload or synchronize DPAPI-protected values.

### Linux

Saved passwords use AES-256-GCM with a cryptographically random per-user key file. Authenticated encryption detects ciphertext tampering. The local key file is restricted to the current user (`0600`) where Unix permissions are supported.

This is local file-based protection and is not misrepresented as a hardware-backed secret store or protection against compromise of the same OS user account.

## Connection Log

The Connection Log is local and bounded. It may display timestamps, selected host/port/security mode, directory-listing counts, connection state and user-visible errors.

It does not intentionally log passwords, protected credential blobs or file contents. It is not uploaded automatically.

## Connection Diagnostics

Diagnostics are user initiated and run against the active server connection. They can issue `NOOP`, `SYST` and `PWD` and display already-known `FEAT` capabilities. Results stay in the application.

## Transfer metrics

Queue progress, transferred bytes, speed, ETA, retry count and timestamps are calculated locally. Ghost FTP does not use them for analytics or profiling.

## Localization privacy

All 29 supported language resources are compiled locally. Changing language does not call an online translation service, download a runtime language pack or report the chosen language.

English is the primary/default language and guaranteed fallback.

## Authentic README screenshot generation

`--capture-ui <directory>` runs the real compiled Windows desktop renderer in deterministic local Demo mode and writes the production MainWindow/Site Manager PNG files.

Capture mode:

- does not use real FTP credentials;
- does not connect to an FTP server;
- does not contact an image-generation service;
- does not upload screenshots automatically from an end-user installation.

GitHub Actions used to refresh repository screenshots is release/build infrastructure and is separate from end-user runtime behavior.

## Live-server smoke testing

The optional `tests/GhostFTP.LiveSmoke` harness is deliberately separate from normal CI.

Real server values are read only from environment variables or GitHub Actions repository secrets. They are not stored in repository test fixtures or workflow YAML. The harness performs only non-destructive connect/PWD/LIST/NOOP/disconnect operations and redacts configured host/username/password values from its own exception output.

Plain FTP live testing requires an explicit opt-in. See `docs/LIVE-SMOKE-TEST.md`.

## Windows Setup privacy

`setup.exe` performs local installation/maintenance work: embedded license display, local language preference, payload validation, per-user file installation, shortcuts, Installed Apps registration and uninstall.

Setup contains no installation analytics, conversion tracking or crash-report uploader. Its self-delete helper uses only the local loopback address as a delay mechanism; it does not send data off the machine.

## Build and release infrastructure

GitHub Actions and the Microsoft .NET SDK can contact their own infrastructure while official binaries are built. That build activity is not runtime behavior of Ghost FTP installed on an end-user machine.

Authenticode signing is also a release-time operation. It does not create an end-user signing-service dependency.

## External server visibility

The FTP/FTPS server selected by the user, and the surrounding network/DNS infrastructure, can observe connection data according to their own policies. Plain FTP provides no transport encryption. FTPS provides TLS subject to normal certificate validation and the server's TLS configuration.

## Summary

Ghost FTP's privacy rule is: **local configuration stays local; runtime metrics/logs stay local; network traffic is limited to the server the user selected and links the user explicitly opens.**

Optional `NOOP` keepalive stays on the selected server session and can be disabled with `0`. **Keep in this tab** remains Session-only Quick Connect and is excluded from JSON persistence. Real-server tests require secrets outside source control.

Product: https://ghostftp.com  
Repository: https://github.com/bren-wp/Ghost  
Developer/publisher: BRENDIGO LTD
