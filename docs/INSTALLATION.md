# Ghost FTP Installation, Update and Uninstall Guide

This document describes the Windows and Linux installation model used by **Ghost FTP 0.1.7 Beta**. Ghost FTP is developed and published by **BRENDIGO LTD**.

## Release identity

```text
VERSION=0.1.7
RELEASE_CHANNEL=beta
```

Every public `0.x.y` package is Beta. Version 1.0.0 is reserved for the first stable release.

## Windows package choices

### Setup

Use `setup.exe` or the architecture-specific Setup asset for a normal per-user installation.

Setup:

- runs as the current user (`asInvoker`);
- installs under the current-user application location;
- can create a desktop shortcut when selected;
- writes Windows Apps & features/uninstall registration;
- stores the maintained `GhostFTP-Setup.exe` beside the installation for future maintenance;
- uses that same maintained Setup executable for uninstall;
- does **not** generate a separate `uninstall.exe`.

### Portable

Use `portable.exe` or an architecture-specific Portable asset when Windows installation registration is not wanted. Portable mode stores local Ghost FTP data under a `Data` directory beside the executable.

## Setup workflow

0.1.7 retains the premium Ghost FTP Setup workflow established in the prior Beta line:

1. Welcome / language selection;
2. license review;
3. install options;
4. Ready summary;
5. transactional progress;
6. Finish / launch.

Setup remains resizable, uses the canonical Ghost palette, explains the local-only/no-telemetry model and shows that one maintained Setup executable handles install/update/uninstall maintenance.

## Install location and local data

Ghost FTP uses a per-user install directory. No administrator-level machine-wide installation is claimed by the current Setup contract.

Installed application data is stored in the current user's local Ghost FTP data directory. It can contain settings, saved-site profiles and protected saved-password data when explicitly enabled. Uninstall lets the user choose whether local profiles/settings should also be removed.

## Windows saved passwords

Saved passwords are optional and use the current-user DPAPI boundary. A reinstall under another Windows account does not imply that another user can decrypt a previous user's saved password data.

## Update transaction

Setup does not blindly overwrite an active installation. It stages and validates application and maintenance Setup candidates first.

Candidate validation checks expected product/company/file-version identity. Setup refuses to downgrade a newer installation. Existing application and maintenance binaries retain rollback copies through the transaction; later-stage failure attempts local rollback before control is returned.

## Uninstall model

The installed `GhostFTP-Setup.exe` is the normal uninstall command. The current release still does not advertise `QuietUninstallString` because a genuine tested silent-uninstall contract has not been implemented.

## Desktop shortcut and language

The desktop shortcut is optional. Setup exposes the same local language catalog as the desktop client. English (`en`) is primary/default/fallback and language choice is stored locally.

## Privacy during installation

Setup sends no install analytics, creates no Ghost FTP account, uploads no machine inventory and installs no tracking or telemetry service. It uses only local package resources and Windows registration APIs needed for maintenance.

## Windows release assets

Official releases provide canonical and architecture-specific Windows artifacts such as:

- `setup.exe`
- `portable.exe`
- `setup-arm64.exe`
- `portable-arm64.exe`
- `GhostFTP-Setup-win-x64.exe`
- `GhostFTP-Portable-win-x64.exe`
- `GhostFTP-Setup-win-arm64.exe`
- `GhostFTP-Portable-win-arm64.exe`
- `SHA256SUMS.txt`
- `SIGNING.txt`

Release verification confirms required assets, product identity and matching version metadata before publication.

## Linux installation

Ghost FTP Linux packages are self-contained application builds for x64 and ARM64. The native renderer uses the system X11/XWayland environment and `libX11.so.6` ABI.

Typical installation is to extract the versioned archive or place the executable in a user-controlled application directory and ensure it is executable. Installed-mode settings/profile data stays under the current user's local application-data path.

The .NET runtime is included in self-contained Linux packages. The renderer still requires a supported X11 environment/libraries; Ghost FTP does not bundle GTK, Qt or Electron.

### Linux workstation geometry in 0.1.7

The Linux renderer now restores the normalized local `WindowWidth` and `WindowHeight` settings at launch and saves the final dimensions on shutdown. It also publishes a 980×680 minimum size through X11 WM normal hints so a compliant window manager cannot shrink the workstation below the supported rendering contract.

This state remains local. It is normal settings data and is not synchronized or reported to Ghost FTP.

## Upgrade from 0.1.6

Windows users can run 0.1.7 Setup over an existing 0.1.6 per-user installation. Setup validates product/version identity and preserves transactional rollback behavior.

No profile or settings migration is required. Existing local profiles, resume metadata and workstation settings remain compatible.

Linux users can replace the previous binary/archive with the matching 0.1.7 architecture package. Existing installed-mode profile/settings files remain compatible. Linux will begin honoring the previously stored window width/height values on launch.

## Download-resume state

The safe resume model introduced in 0.1.6 remains unchanged in 0.1.7 and uses bounded local sidecars:

```text
<destination>.ghostftp.part
<destination>.ghostftp.part.meta
```

These files are local temporary transfer state, not profile or account data. The metadata sidecar contains endpoint/security/path/remote revision information used to decide whether `REST` resume is safe; it does not contain the FTP password.

A staged download does not replace an existing destination until its remote revision has been validated after transfer. If the server object changes while bytes are in flight, the staged result is discarded and the previous destination is preserved.

If an old, corrupt or mismatched partial cannot be removed, Ghost FTP aborts instead of continuing with length-only resume. This is intentional fail-closed behavior and may surface a local filesystem permission error that must be corrected before retrying.

## Runtime changes relevant to upgrades

0.1.7 retains the bounded LIST/MLSD parser, pooled/cleared transfer buffers, reduced progress-renderer scheduling, coalesced post-transfer pane refresh and staged safe-resume integrity model.

The principal runtime/UI corrections are:

- Linux persisted window width/height are applied at startup and saved on shutdown;
- Linux publishes a supported minimum X11 window geometry;
- Linux Light theme is no longer replaced by the dark reference palette during redraw;
- newly created Linux saved sites are validated before persistence;
- Windows focus/selection states, splitters and Site Manager presentation are polished without replacing native WPF editors.

## Settings recovery

Settings writes continue to use atomic replacement plus a local backup. If the primary bounded JSON settings file is malformed, Ghost FTP attempts the bounded `.bak` copy and falls back to safe defaults only when required.

## Troubleshooting

### Setup reports a downgrade refusal

Confirm that the package version is newer than or equal to the installed application/maintenance version. Ghost FTP intentionally blocks lower-version replacement.

### Setup fails and returns to Ready

Review the displayed error and verify that the current user can write to the per-user install location. Setup attempts local rollback first.

### A stale `.ghostftp.part` cannot be removed

Check file/directory permissions, read-only attributes, locks and filesystem health. Ghost FTP intentionally refuses to resume an untrusted partial when cleanup cannot be proven successful.

### Linux reports no X11/XWayland display

Ensure a working desktop display/session, `DISPLAY` environment and supported X11 runtime libraries are available.

### Linux cannot be resized below 980×680

That is intentional in 0.1.7. The native renderer publishes this minimum to keep the sidebar, file panes and transfer workstation inside the supported geometry contract.

### Portable data appears beside the executable

That is expected Portable behavior. Use the installed Setup build when settings/profile data should live under the user's application-data directory.

## Verification

Official 0.1.7 binaries are published only after Windows/Linux release gates pass source/security audits, Core/Demo/Queue/protocol hardening tests, **safe download resume integrity tests on both platforms**, renderer smoke tests, authentic Windows capture, packaging, asset identity checks and SHA-256/runtime verification.

Prefer assets attached to the official GitHub Release over unverified third-party copies.
