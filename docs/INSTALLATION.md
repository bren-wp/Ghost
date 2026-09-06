# Ghost FTP Installation, Update and Uninstall Guide

This document describes the Windows and Linux installation model used by the current **Ghost FTP 0.1.4 Beta** source line. Ghost FTP is developed and published by **BRENDIGO LTD**.

## Release identity

```text
VERSION=0.1.4
RELEASE_CHANNEL=beta
```

Every public `0.x.y` package is Beta. Version 1.0.0 is reserved for the first stable release.

## Windows package choices

### Setup

Use `setup.exe` (or the architecture-specific Setup asset) for a normal per-user installation.

Setup:

- runs as the current user (`asInvoker`);
- installs under the current-user application location;
- can create a desktop shortcut when selected;
- writes the Windows Apps & features/uninstall registration;
- stores a maintained `GhostFTP-Setup.exe` alongside the installation for future update/uninstall operations;
- does not generate a separate `uninstall.exe`.

### Portable

Use `portable.exe` (or an architecture-specific portable asset) when installed Windows registration is not desired. Portable mode stores local Ghost FTP data under a `Data` directory beside the executable.

## Premium Setup workflow

0.1.4 retains the canonical Ghost FTP dark Setup workflow:

1. Welcome / language selection;
2. license review;
3. install options;
4. Ready summary;
5. transactional progress;
6. Finish / launch.

A compact progress badge indicates the active wizard step. Setup explains the local-only/no-telemetry model and that one maintained Setup executable handles installation, update and uninstall.

## Install location and local data

Ghost FTP uses a per-user install directory. No administrator-level machine-wide installation is claimed by the current Setup contract.

Installed application data is stored in the current user's local Ghost FTP data directory. It can contain settings, saved site profiles and protected saved-password data when explicitly enabled. Uninstall allows the user to choose whether local profiles/settings should also be removed.

## Windows saved passwords

Saved passwords are optional. Windows protects them with the current-user DPAPI boundary. A reinstall under another Windows account does not imply that another user can decrypt the previous user's saved password data.

## Update transaction

Setup does not blindly overwrite the active application. The installer stages and validates the application candidate and maintenance Setup candidate.

Candidate validation checks expected executable/product/company/file-version identity. Setup **refuses to downgrade** an existing newer installation/maintenance binary.

Before replacing an existing executable, Setup keeps an independent local backup. If a later stage fails, the installer attempts **rollback** of both application and maintenance binaries. During a first-time failed installation, newly committed partial binaries are removed where applicable.

## Uninstall model

The same installed `GhostFTP-Setup.exe` is registered as the normal uninstall command. The current release deliberately does not advertise `QuietUninstallString` because a true silent-uninstall contract has not yet been implemented and tested.

## Desktop shortcut and language

The desktop shortcut is optional. Setup exposes the same local language catalog used by the desktop product. English (`en`) is the primary/default/fallback language and the selected language is saved locally.

## Privacy during installation

Setup does not send install analytics, create a Ghost FTP account, upload machine inventory or register a tracking service. It uses local package resources and Windows registration APIs required for install/update/uninstall.

## Windows architecture assets

Official releases may provide Windows x64/ARM64 Setup, Windows x64/ARM64 Portable, canonical aliases `setup.exe` / `portable.exe`, SHA-256 information and signing metadata. Release verification confirms required assets and matching version metadata before publication.

## Linux installation

Ghost FTP Linux packages are self-contained application builds for x64/ARM64. The native renderer uses the system X11/XWayland environment and supported `libX11.so.6` ABI.

Typical use is to extract the versioned archive or place the executable in a user-controlled application directory and ensure it is executable. Installed-mode settings/profile data remains under the current user's local application-data path.

## Linux dependencies

The .NET runtime is included in self-contained packages. The native renderer still requires a supported X11 environment/libraries because it uses Xlib directly. Ghost FTP does not bundle a separate GTK/Qt/Electron framework.

## Upgrade from 0.1.3

Windows users may run 0.1.4 Setup over an existing 0.1.3 per-user installation. Setup performs product/version validation and transaction/rollback handling before committing new binaries.

0.1.4 changes the shared protocol/session/queue implementation but does not require a profile/settings migration. Local profiles and settings remain local and use the same persistence model.

Linux users can replace the previous application binary/archive with the matching 0.1.4 architecture package. Existing installed-mode settings/profile files remain compatible with the current persistence format.

## Protocol/stability changes relevant to upgrades

0.1.4 adds stricter FTP reply and passive-mode validation plus coordinated session/queue shutdown. These are runtime hardening changes and do not modify the installer data layout. Servers that return malformed FTP reply framing or invalid PASV/EPSV tuples will now be rejected more explicitly rather than parsed permissively.

## Troubleshooting

### Setup reports a downgrade refusal

Confirm that the package version is newer than or equal to the installed application/maintenance version. Ghost FTP intentionally blocks a lower-version replacement.

### Setup fails and returns to Ready

Review the displayed error and ensure the current user can write to the per-user install location. Setup attempts local rollback before returning control.

### Linux reports no X11/XWayland display

Ensure a working desktop display/session and `DISPLAY` environment are available and supported X11 runtime libraries are installed.

### Portable data appears beside the executable

That is expected portable behavior. Use the installed Setup build when settings/profile data should live under the user's application-data directory.

## Verification

Official public releases are generated by the release workflow after Windows/Linux build, audits, Core/queue/protocol hardening tests, renderer smoke tests, package verification and checksums. Prefer assets attached to the official GitHub Release over unverified third-party copies.
