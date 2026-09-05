# Ghost FTP Installation, Update and Uninstall Guide

This document describes the Windows installation model used by Ghost FTP 1.4.0 and later.

Ghost FTP is developed and published by **BRENDIGO LTD** (Company number 16545639), registered office 71–75 Shelton Street, Covent Garden, London, WC2H 9JQ, United Kingdom.

## Supported Windows environment

Ghost FTP targets Windows 10 version 2004 / build 19041 or newer. Windows 11 is recommended for the full modern visual treatment. Official release binaries are self-contained and do not require a separate .NET installation.

Official Windows packages are produced for:

- Windows x64;
- Windows ARM64.

## Installer flow

The standard `setup.exe` uses a guided wizard. The normal installation sequence is:

1. **Language** — select the Setup language and initial Ghost FTP client language.
2. **License Agreement** — review the embedded BRENDIGO LTD Ghost FTP license.
3. **Accept license** — the Next action remains disabled until the license is explicitly accepted.
4. **Install options** — review per-user install location and optional desktop shortcut.
5. **Ready** — review product, publisher, language and selected options.
6. **Install / Update** — Setup validates and installs the architecture-matching Ghost FTP payload.
7. **Finish** — launch Ghost FTP or close Setup.

The English license text embedded from the repository `LICENSE` file is the governing license text.

## Language selection

English is the default and fallback language. Setup supports the same 29 language choices validated by the application.

The selected language is written to Ghost FTP's local settings and becomes the initial client language. Existing valid settings are preserved when the language property is added or changed.

Changing a language never contacts a translation service or any other network service.

## Install location

The default install is per-user and does not require administrative privileges:

```text
%LOCALAPPDATA%\Programs\GhostFTP\
```

The directory contains the installed client and the maintenance copy of Setup:

```text
GhostFTP.exe
GhostFTP-Setup.exe
```

There is intentionally **no separate uninstall.exe**.

## User data location

Installed-mode application data is stored under:

```text
%LOCALAPPDATA%\GhostFTP\
```

This includes local settings and saved server profiles. Passwords are not persisted unless the user enables **Remember password**. Persisted passwords are protected with Windows DPAPI and scoped to the current Windows user.

## Update behavior

Running a newer Setup over an existing installation performs an in-place per-user update.

Update safety rules include:

- validate the embedded application payload before replacement;
- require a plausible executable size and Windows `MZ` signature;
- use temporary files rather than writing directly into the active application file;
- use atomic replacement/backup semantics when the target filesystem supports them;
- fail visibly if the installed Ghost FTP executable is locked by a running process;
- validate the installed Setup maintenance copy;
- preserve existing local profiles and settings;
- update the selected client language without discarding unrelated valid settings.

Close Ghost FTP before updating so Windows can replace the executable safely.

## Windows Installed Apps integration

Setup registers Ghost FTP under the current user's Windows uninstall registry location.

The entry includes:

- Display name: Ghost FTP;
- Publisher: BRENDIGO LTD;
- Display version;
- install location;
- Ghost FTP executable icon;
- product/help website;
- uninstall command.

The uninstall command points to:

```text
"%LOCALAPPDATA%\Programs\GhostFTP\GhostFTP-Setup.exe" --uninstall
```

## Uninstall flow

Uninstall uses the same Setup executable and a guided flow:

1. Welcome / language context.
2. Choose whether local settings and saved server profiles should also be removed.
3. Review the uninstall summary.
4. Remove Ghost FTP.
5. Finish.

The uninstall process removes:

- installed `GhostFTP.exe`;
- Start Menu shortcut;
- Desktop shortcut if present;
- Windows Installed Apps registration;
- optional local Ghost FTP data when explicitly selected.

If local data removal is not selected, profiles/settings remain available for a future reinstall.

## Self-removal of Setup

A running executable cannot reliably delete itself immediately on Windows. Ghost FTP therefore uses the same installed Setup binary for uninstall and then:

- schedules Windows delete-on-reboot as a fallback for the maintenance Setup executable; and
- starts a local hidden delayed cleanup attempt to remove `GhostFTP-Setup.exe` and the now-empty install directory after Setup exits.

No additional uninstaller binary is created.

## Portable edition

`portable.exe` does not register Windows Installed Apps and does not run the Setup wizard.

Portable mode stores data in a `Data` directory next to the executable when operating as a portable build. Delete the portable executable and its local `Data` directory manually if you want to remove both the application and its portable data.

## Troubleshooting

### Setup says Ghost FTP is running or locked

Close Ghost FTP, wait for active transfer windows/processes to exit, and run Setup again. Setup intentionally refuses to claim a successful replacement while Windows still has the installed executable locked.

### Setup cannot remove Ghost FTP

Close the client first. If the installed executable remains locked, uninstall reports the failure rather than silently claiming success.

### Settings file is malformed or oversized

Setup does not trust arbitrary local JSON. Invalid/oversized settings can be quarantined before Setup writes the selected language. Ghost FTP itself also applies bounded settings/profile reads and recovery rules.

### Installer language does not match the client after an update

The language selected in Setup is written as the client language. The desktop application applies stored language configuration at startup. Restart the client after changing the language.

## Privacy

Setup performs local installation, update, shortcut and registry operations only. It contains no telemetry, analytics, advertising, crash upload or automatic update checker and does not report installation status to Ghost FTP or BRENDIGO LTD.
