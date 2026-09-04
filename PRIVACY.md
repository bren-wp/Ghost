# Ghost FTP Privacy

Ghost FTP is designed to operate without telemetry, tracking or hidden background network activity.

## What Ghost FTP does not do

- No telemetry or analytics.
- No advertising SDKs.
- No crash-report upload.
- No fingerprinting or user profiling.
- No automatic update checks.
- No background requests to ghostftp.com, the source repository, or unrelated services.
- No cloud synchronization of saved servers, settings or UI preferences.

## Network behavior

Ghost FTP opens network connections only when the user explicitly connects to an FTP/FTPS server or manually opens the Ghost FTP website from the About window. Demo mode is entirely local and generates no network traffic.

UI actions such as changing appearance, filtering files, showing hidden/system items, copying paths, browsing local folders or opening File Explorer are local-only operations.

## Local data

Profiles and settings are stored locally. In installed mode they are stored under the current user's local application-data directory. Portable builds store data next to the executable in a `Data` directory when possible.

Local settings can include:

- appearance preference;
- last local directory;
- delete-confirmation preference;
- hidden/system-file visibility preference;
- saved server profiles.

These settings are never synchronized by Ghost FTP.

Passwords are never stored unless **Remember password** is enabled. When enabled, the password is protected using Windows DPAPI and can only be decrypted in the same Windows user context.

## Installer privacy

The Ghost FTP installer performs local file installation, shortcut creation and per-user uninstall registration. It does not report installation success/failure, collect usage information or contact an update service.

Project: https://ghostftp.com  
Repository: https://github.com/bren-wp/Ghost
