# Ghost FTP Installation, Update and Uninstall Guide

This document describes the Windows and Linux installation model used by the current **Ghost FTP 0.1.3 Beta** source line. Ghost FTP is developed and published by **BRENDIGO LTD**.

## Release identity

```text
VERSION=0.1.3
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

Use `portable.exe` (or architecture-specific portable asset) when an installed Windows registration is not desired.

Portable mode is detected by the portable executable name/marker and stores local Ghost FTP data under a `Data` directory beside the executable.

## Premium Setup workflow

0.1.3 Setup uses the same canonical Ghost FTP dark palette as the Windows client and includes:

1. Welcome / language selection;
2. license review;
3. install options;
4. Ready summary;
5. transactional progress;
6. Finish / launch.

A compact progress badge indicates the active wizard step. Setup also explains the local-only/no-telemetry model and that one maintained Setup executable handles installation, update and uninstall.

## Install location

Ghost FTP uses a per-user install directory. No administrator-level machine-wide installation is claimed by the current Setup contract.

## Local data

Installed application data is stored in the current user's local Ghost FTP data directory. It can contain:

- settings;
- saved site profiles;
- protected saved-password data when explicitly enabled.

Uninstall allows the user to choose whether local profiles/settings should also be removed.

## Windows saved passwords

Saved passwords are optional. Windows protects them with the current-user DPAPI boundary. A reinstall under another Windows account does not imply that another user can decrypt the previous user's saved password data.

## Update transaction

Setup does not blindly overwrite the active application.

The installer stages and validates:

- the application candidate;
- the maintenance Setup candidate.

Candidate validation checks expected executable/product/company/file-version identity. Setup **refuses to downgrade** an existing newer installation/maintenance binary.

Before replacing an existing executable, Setup keeps an independent local backup. If a later stage fails, the installer attempts **rollback** of both application and maintenance binaries. During a first-time failed installation, newly committed partial binaries are removed where applicable.

## Uninstall model

The same installed `GhostFTP-Setup.exe` is registered as the normal uninstall command. This satisfies the requirement that installation and removal are handled by the maintained Setup program instead of generating a second uninstaller program.

The current release deliberately does not advertise `QuietUninstallString` because a true silent-uninstall contract has not yet been implemented and tested.

## Desktop shortcut

The desktop shortcut is optional. Reinstall/update may preserve or recreate it according to the selected Setup option.

## Language

Setup exposes the same local language catalog used by the desktop product. English (`en`) is the primary/default/fallback language. The selected language is saved locally for Ghost FTP.

## Privacy during installation

Setup does not send install analytics, create a Ghost FTP account, upload machine inventory or register a tracking service. The installer uses local package resources and Windows registration APIs required to install/update/uninstall the program.

## Windows architecture assets

Official releases may provide:

- Windows x64 Setup;
- Windows ARM64 Setup;
- Windows x64 Portable;
- Windows ARM64 Portable;
- canonical aliases `setup.exe` and `portable.exe`.

Release pipeline verification confirms the required assets and matching version metadata before publication.

## Linux installation

Ghost FTP Linux packages are self-contained application builds for x64/ARM64. The native renderer uses the system X11/XWayland environment and the supported system `libX11.so.6` ABI.

Typical use is to extract the versioned archive or place the executable in a user-controlled application directory and ensure it is executable.

The application stores normal installed-mode settings/profile data in the current user's local application-data path.

## Linux dependencies

The .NET runtime is included in self-contained packages. The native renderer still requires the supported host X11 libraries/environment because it uses Xlib directly.

No separate GTK/Qt/Electron framework is bundled by Ghost FTP.

## Upgrade from 0.1.2

Windows users may run 0.1.3 Setup over an existing 0.1.2 per-user installation. Setup performs version validation and transaction/rollback handling before committing the new binaries.

Local profiles/settings remain local and are not cloud-migrated.

Linux users can replace the previous application binary/archive with the matching 0.1.3 architecture package. Existing installed-mode local settings/profile files use the same shared persistence model.

## Troubleshooting

### Setup reports a downgrade refusal

Confirm that the package version is newer than or equal to the installed application/maintenance version. Ghost FTP intentionally blocks a lower-version replacement.

### Setup fails and returns to Ready

Review the displayed error and ensure the current user can write to the per-user install location. Setup attempts local rollback before returning control.

### Linux reports no X11/XWayland display

Ensure a working desktop display/session and `DISPLAY` environment are available and the supported X11 runtime libraries are installed.

### Portable data appears beside the executable

That is expected portable behavior. Use the normal installed Setup build when profile/settings data should live under the current user's application-data directory.

## Verification

Official public releases are generated by the release workflow after Windows/Linux build, audits, tests, package verification and checksums. Prefer assets attached to the official GitHub Release over unverified third-party copies.
