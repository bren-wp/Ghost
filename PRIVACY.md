# Ghost FTP Privacy

Ghost FTP is designed to operate without application telemetry, user tracking or hidden background product network activity.

Ghost FTP is developed and published by **BRENDIGO LTD**. The privacy model below describes the official Ghost FTP application and Setup behavior in version 1.4.0.

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

The application opens network connections only when the user explicitly performs an action that requires them.

Examples:

1. connecting to an FTP/FTPS server;
2. browsing, uploading, downloading, renaming or deleting data on that server;
3. running Connection Diagnostics against the server that is already connected;
4. manually opening a Ghost FTP website link from the application.

Ghost FTP does not send connection diagnostics, server metadata, file lists or transfer details to BRENDIGO LTD.

## FTPS certificate revocation privacy

FTPS uses normal Windows/.NET certificate-chain and hostname validation. Revocation checking is configured to use the Windows offline revocation cache, avoiding hidden online CRL/OCSP requests initiated by Ghost FTP during certificate validation.

This is a privacy-oriented tradeoff: Ghost FTP does not silently contact certificate-authority revocation services in the background.

## Demo mode

Demo mode is fully local. It does not open an FTP socket, call ghostftp.com, contact a repository endpoint or send any data elsewhere.

Its folders, files and transfer behavior are simulated in memory/local application state.

## Local data

Installed Ghost FTP stores local settings and saved profiles under the current user's local application-data area. Portable builds use a local `Data` directory next to the portable executable when operating in portable mode.

Local configuration can include:

- appearance preference;
- selected language;
- last local directory;
- delete-confirmation preference;
- hidden/system item visibility;
- transfer retry count;
- connect/command/transfer timeout values;
- saved server profiles;
- optional protected password data when Remember password is enabled.

These values are not synchronized by Ghost FTP.

## Password handling

Passwords are not persisted unless **Remember password** is enabled for a saved server profile.

When enabled, the password is protected with Windows DPAPI using the current-user scope. Ghost FTP does not upload saved credentials or provide a cloud credential synchronization service.

## Localization privacy

All 29 supported languages are compiled into the application/Setup C# source. Changing language does not contact an online translation provider, download a language pack or report the selected language to BRENDIGO LTD.

English is the default/fallback language.

## Connection Diagnostics privacy

Connection Diagnostics runs only on demand against the user's active FTP/FTPS connection. It can execute server commands such as `NOOP`, `SYST` and `PWD`, and display capabilities already known from `FEAT`.

Results stay inside the application. No diagnostic bundle is generated or uploaded automatically.

## Setup and uninstall privacy

`setup.exe` performs local operations only:

- displays the embedded license;
- writes the selected language to local settings;
- validates and installs the embedded Ghost FTP executable;
- creates local shortcuts;
- writes per-user Windows Installed Apps registration;
- updates an existing per-user installation;
- removes the application/shortcuts/registration during uninstall;
- optionally removes local Ghost FTP data if the user explicitly chooses that option.

Setup contains no installation analytics, conversion tracking, crash upload or update-service reporting.

## Release/build infrastructure

GitHub Actions and Microsoft .NET SDK infrastructure may access their own build services while compiling official releases. That build-time infrastructure is separate from the runtime behavior of Ghost FTP installed on an end-user system.

Official Ghost FTP runtime binaries are designed not to call package feeds or GitHub Actions.

## External services selected by the user

When a user connects to an FTP/FTPS server, that server and the surrounding network infrastructure can observe connection data according to their own configuration and policies. Plain FTP does not encrypt credentials or content in transit.

Those third-party servers, networks, proxies, DNS providers and operating-system services are outside Ghost FTP's control.

## Product website links

Opening ghostftp.com is a user-initiated action. Ghost FTP does not open the site silently at startup or in the background.

## Summary

Ghost FTP's privacy rule is simple: local configuration stays local, diagnostics stay local, and network traffic is created only for actions the user deliberately initiates.

Product: https://ghostftp.com  
Repository: https://github.com/bren-wp/Ghost  
Developer/publisher: BRENDIGO LTD
