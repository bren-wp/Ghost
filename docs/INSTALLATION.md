# Ghost FTP Installation, Update and Uninstall Guide

This document describes the Windows and Linux installation model used by the current **Ghost FTP 0.1.1 Beta** public line. Ghost FTP is developed and published by **BRENDIGO LTD** (Company number 16545639), registered office 71–75 Shelton Street, Covent Garden, London, WC2H 9JQ, United Kingdom.

## Release identity

The active release identity is defined by the root `VERSION` and `RELEASE_CHANNEL` files:

```text
VERSION=0.1.1
RELEASE_CHANNEL=beta
```

Every public `0.x.y` package is Beta. The first release that may be presented as fully stable is **1.0.0**. Canonical package filenames remain predictable while executable/file metadata identifies the exact release.

## Verify downloads before running them

Official GitHub Releases contain SHA-256 manifests. Verify the downloaded file against the manifest published in the same release before execution, especially when the file was mirrored or transferred through another system.

Windows uses `SHA256SUMS.txt`; Linux uses `SHA256SUMS-linux.txt`.

The current 0.1.1 Beta Windows executables are not Authenticode-signed when the release signing secrets are not configured. In that case Windows can display an Unknown Publisher or SmartScreen warning. This is not hidden by Setup. A trusted publisher experience requires a valid CA-issued code-signing certificate for **BRENDIGO LTD** and a release that passes the repository signing gate. Never disable Windows security checks merely to suppress such a warning.

## Windows

### Supported environment

Ghost FTP targets Windows 10 version 2004 / build 19041 or newer. Windows 11 is recommended for the complete modern visual treatment. Official packages are self-contained and do not require a separate .NET installation.

Windows packages are produced for **x64** and **ARM64**.

### Which Windows file should I use?

For most x64 PCs use:

```text
setup.exe
```

For a no-install x64 copy use:

```text
portable.exe
```

For Windows on ARM use:

```text
setup-arm64.exe
portable-arm64.exe
```

Architecture-explicit aliases are also published:

```text
GhostFTP-Setup-win-x64.exe
GhostFTP-Portable-win-x64.exe
GhostFTP-Setup-win-arm64.exe
GhostFTP-Portable-win-arm64.exe
```

All executables from one release must carry the same active numeric file version. For Ghost FTP 0.1.1 Beta that version is `0.1.1.0`.

### Windows Setup flow

The standard Setup wizard uses this sequence:

1. **Language** — select the Setup language and initial Ghost FTP language.
2. **License Agreement** — review the embedded BRENDIGO LTD Ghost FTP license.
3. **Accept license** — installation remains unavailable until the license is explicitly accepted.
4. **Install options** — review the per-user location and optional Desktop shortcut.
5. **Ready** — review product, publisher, language, version/channel and options.
6. **Install / Update** — Setup validates and installs the architecture-matching Ghost FTP payload.
7. **Finish** — launch Ghost FTP or close Setup.

The repository `LICENSE` text embedded in Setup is the governing license text.

### Windows install location

The default installation is per-user and normally needs no administrator privileges:

```text
%LOCALAPPDATA%\Programs\GhostFTP\
```

The directory contains:

```text
GhostFTP.exe
GhostFTP-Setup.exe
```

There is intentionally no separate `uninstall.exe`. The installed maintenance Setup handles update and uninstall.

### Windows user data

Installed-mode data is stored under:

```text
%LOCALAPPDATA%\GhostFTP\
```

This includes local settings and saved server profiles. A password is persisted only when **Remember password** is enabled. Windows protects persisted passwords with CurrentUser DPAPI. Ghost FTP does not upload saved profiles or credentials.

### Windows update safety

Running a newer matching-architecture Setup over an existing installation performs an in-place per-user update. Setup stages and validates both the application payload and the maintenance Setup copy **before** changing the active installation. Validation checks minimum payload size, Windows executable identity, ProductName **Ghost FTP**, CompanyName **BRENDIGO LTD** and the exact candidate file version.

Setup compares installed and candidate file versions and refuses to install an older Ghost FTP binary over a newer one. Both the existing application executable and installed maintenance Setup copy keep rollback material until all later install stages have completed. If a later stage fails, Setup attempts to restore both previous binaries rather than intentionally leaving a mixed-version installation. On a first installation, a failure after a binary commit removes the incomplete binary during rollback.

Close Ghost FTP before updating so Windows can replace the active file. A locked application or Setup copy causes a visible failure rather than a false successful update. Stale transaction files are also cleaned during uninstall.

Setup preserves local settings and saved profiles. Invalid or oversized settings are treated as untrusted input and can be quarantined before Setup writes the selected language.

### Windows Installed Apps integration

Setup registers Ghost FTP under the current user's Windows uninstall registry location with the Ghost FTP display name, BRENDIGO LTD publisher, current numeric display version, install location, icon, website/help link and uninstall command.

The uninstall command is:

```text
"%LOCALAPPDATA%\Programs\GhostFTP\GhostFTP-Setup.exe" --uninstall
```

Ghost FTP does not publish a `QuietUninstallString` while uninstall remains interactive. This avoids misleading endpoint-management software into assuming silent removal exists.

### Windows uninstall

Uninstall uses the installed Setup and allows the user to choose whether local settings and saved profiles should also be removed. Normal removal deletes the client, Start Menu shortcut, optional Desktop shortcut and Installed Apps registration. Local data remains unless removal is explicitly selected.

A running Setup cannot reliably delete its own executable immediately. Ghost FTP therefore registers Windows delete-on-reboot as an eventual fallback and also starts a bounded hidden local cleanup helper that retries after Setup exits. The helper uses `127.0.0.1` only as a local delay mechanism; it does not contact an external service.

No Windows service, scheduled task, analytics component or background updater is installed.

### Windows portable edition

`portable.exe` does not register Installed Apps and does not run the Setup wizard. Portable mode keeps Ghost FTP data in a `Data` directory next to the executable where portable storage is available. To remove the portable edition, delete the executable and optionally its local `Data` directory.

## Linux

### Supported environment

Ghost FTP ships a native Linux desktop renderer implemented directly against the X11 client ABI while sharing the same platform-neutral FTP/FTPS core and Ghost FTP design/localization contract used by Windows.

The Linux UI runs on X11 and can run on Wayland desktops through XWayland. The standard desktop library `libX11.so.6` must be available. Official self-contained packages do not require a separate .NET installation.

Linux packages are produced for **x64** and **ARM64**.

### Which Linux file should I use?

For a typical Intel/AMD 64-bit Linux desktop use either:

```text
GhostFTP-linux-x64
GhostFTP-linux-x64.tar.gz
```

For ARM64 Linux use:

```text
GhostFTP-linux-arm64
GhostFTP-linux-arm64.tar.gz
```

Version/channel-explicit tarballs are also published:

```text
GhostFTP-0.1.1-beta-linux-x64.tar.gz
GhostFTP-0.1.1-beta-linux-arm64.tar.gz
```

Do not try to run Windows `.exe` packages on Linux as the supported Linux distribution method. Use the Linux artifacts from the same GitHub Release.

### Run the standalone Linux binary

After verifying its SHA-256 checksum, make the architecture-matching binary executable if needed and start it from your desktop/session:

```bash
chmod +x GhostFTP-linux-x64
./GhostFTP-linux-x64
```

Use `GhostFTP-linux-arm64` instead on ARM64.

### Install from the Linux tarball

The architecture-specific tarball contains the Ghost FTP executable plus user-local `install.sh` and `uninstall.sh` helpers. Extract the matching archive and run:

```bash
tar -xzf GhostFTP-linux-x64.tar.gz
cd GhostFTP-linux-x64
./install.sh
```

The helper performs a user-local installation; Ghost FTP does not require a cloud account, package-manager repository, background daemon or telemetry service.

If script execution permission was not preserved by the download/extraction path, inspect the script and then restore its executable bit before running it:

```bash
chmod +x install.sh uninstall.sh
```

### Linux user data and credentials

Linux settings and profiles remain local to the current user. Saved passwords are opt-in. Linux uses AES-256-GCM local credential protection backed by a cryptographically random per-user key file. Ghost FTP attempts to restrict that key file to user-only filesystem permissions (`0600`) where Unix permissions are available.

This is local file-based protection and is not described as Windows DPAPI or as a hardware-backed secret store.

### Linux uninstall

Use the installed/unpacked uninstall helper for the matching package:

```bash
./uninstall.sh
```

Normal uninstall preserves local Ghost FTP settings/profiles. Use the documented explicit purge option only when you also want local Ghost FTP data removed:

```bash
./uninstall.sh --purge
```

Review the helper from the exact release before execution if the package was obtained from a mirror.

## Demo mode

The built-in **GhostFTP Demo** profile is local-only on Windows and Linux. It provides deterministic sample directories, files and transfer operations without opening an FTP socket. Demo mode is used by automated regression tests and authentic Windows documentation capture.

The 0.1.1 regression harness exercises connect, diagnostics, PWD/CWD, listing, keepalive, download, upload/download byte round-trip, rename, directory creation/removal, recursive directory round-trip, conflict protection, root-delete protection, disconnect reset and rejection of post-disconnect operations on both Windows and Linux CI.

Demo mode is not evidence that an arbitrary external FTP server is reachable. Real-server interoperability is tested separately with the credential-safe, non-destructive workflow documented in `docs/LIVE-SMOKE-TEST.md`.

## Language selection

English is the primary/default language and guaranteed fallback. Ghost FTP validates 29 local language choices. Language data is compiled with the application; selecting a language does not contact an online translation provider or download a language pack.

## Privacy and network behavior

Installation and uninstall are local operations. Ghost FTP contains no application telemetry, advertising SDK, analytics SDK, automatic crash uploader, cloud profile synchronization or automatic background update checker.

Normal runtime network activity is limited to FTP/FTPS servers selected by the user, documented keepalive/diagnostics on an already selected server session, and website links explicitly opened by the user. Opening `ghostftp.com` or `brendigo.com` is user-initiated.

## Troubleshooting

### Windows Setup says Ghost FTP is running or locked

Close Ghost FTP and wait for the application process to exit, then run Setup again. Setup deliberately refuses to claim a successful executable replacement while Windows still holds the installed binary open.

### Windows Setup refuses to downgrade

This is intentional. A package with an older file version cannot overwrite a newer installed Ghost FTP application or maintenance Setup copy. Use the same or a newer official release package.

### Windows reports Unknown Publisher / SmartScreen

Check `SIGNING.txt` in the same GitHub Release. If that release is unsigned, the warning is expected. Verify the SHA-256 manifest and obtain Ghost FTP only from a trusted release source. Do not disable SmartScreen or certificate validation as a workaround. A future trusted-signed release must identify BRENDIGO LTD and pass the repository signing gate.

### Linux fails with `libX11.so.6` missing

Install the standard X11 client runtime supplied by your Linux distribution, then start Ghost FTP again. On a Wayland-only session, XWayland must be available for the current renderer.

### Linux binary is not executable

After verifying the checksum, restore the executable permission with `chmod +x` for the correct architecture-specific binary or install/uninstall helper.

### Settings are malformed or oversized

Ghost FTP treats persisted configuration as untrusted input, applies size/bounds validation and uses recovery/quarantine behavior rather than assuming local JSON is safe.

### Package says Beta

That is expected for every public Ghost FTP version below 1.0.0. Beta status is removed only after the explicit stable 1.0.0 gate passes for the exact source commit.

See `README.md`, `SECURITY.md`, `PRIVACY.md`, `docs/PLATFORM-SUPPORT.md`, `docs/CODE-SIGNING.md`, `docs/LIVE-SMOKE-TEST.md` and `docs/RELEASE-POLICY.md` for the current product contracts.
