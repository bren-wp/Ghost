# Ghost FTP Installation, Update and Uninstall Guide

This guide describes the Windows and Linux installation model for **Ghost FTP 0.1.2 Beta**, developed and published by **BRENDIGO LTD**.

## Release identity

```text
VERSION=0.1.2
RELEASE_CHANNEL=beta
```

All 0.x packages are Beta. Windows file/assembly version for this line is `0.1.2.0`; informational version is `0.1.2-beta`; expected public tag is `v0.1.2-beta`.

## Windows download choices

### Setup x64

`setup.exe` is the canonical Windows x64 installer/maintenance executable. It installs Ghost FTP for the current user and does not require a second dedicated uninstaller executable.

### Portable x64

`portable.exe` is the canonical x64 portable executable. It is self-contained and keeps portable application data under a local `Data` directory beside the executable.

### Windows ARM64

- `setup-arm64.exe`
- `portable-arm64.exe`

Architecture-specific descriptive aliases are also published in the GitHub Release.

## Per-user Setup model

Ghost FTP Setup runs as a per-user installer (`asInvoker`). It installs the application into the current user's application area and registers per-user uninstall metadata. Normal installation should not require administrative elevation.

The installed maintenance executable is `GhostFTP-Setup.exe`. Windows uninstall registration points back to that maintenance Setup in uninstall mode. Ghost FTP intentionally does not create a separate `uninstall.exe`.

## Installer flow

The Setup UI is a native WPF wizard using the Ghost FTP dark product theme and the same local language catalog as the application. Typical flow:

1. language selection;
2. welcome/product information;
3. license acceptance;
4. install options such as desktop shortcut;
5. ready confirmation;
6. staged install/update progress;
7. finish/launch.

Uninstall mode presents removal options including whether local Ghost FTP data should also be removed when that choice is available.

## Staging and candidate validation

Before replacing an active installation, Setup stages the incoming application and maintenance Setup binaries. Candidate checks verify expected Windows executable identity, Ghost FTP ProductName, BRENDIGO LTD CompanyName and the active release file version.

The installer **refuses to downgrade** an installed application or maintenance Setup to an older version. This applies to the staged application and Setup paths rather than only one executable.

## Transaction and rollback

Existing application and maintenance Setup binaries retain independent rollback copies until later installation stages complete.

If a later stage fails after one or both candidates have been committed, Setup attempts **rollback** of the prior binaries. During a first-time partial installation, newly committed binaries are removed where restoration is not possible because no previous file existed.

Transaction/staging leftovers are cleaned as part of install/uninstall maintenance paths.

This design reduces the chance that a failed update leaves a new app paired with an old maintenance Setup or vice versa.

## Uninstall

Use the normal Windows installed-apps control panel/settings entry or run the installed maintenance Setup with its uninstall mode. The same maintenance executable performs removal; there is no separate uninstaller binary to build, sign or maintain.

Removing application binaries and removing local profiles/settings are intentionally distinct decisions so an uninstall does not silently destroy saved server configuration unless the user requests local-data removal.

## Windows portable mode

Portable detection uses executable identity and/or `portable.flag`. Portable local data is stored under:

```text
<Data beside portable executable>\Data
```

This can include settings and saved-site data. Protect the portable folder/media accordingly, especially if saved passwords are enabled.

Portable mode does not install Start Menu/Apps registration and does not require uninstall; close Ghost FTP and remove the portable files when no longer needed.

## Saved passwords

Password persistence is opt-in.

Windows installed and portable profiles use current-user DPAPI protection for remembered passwords. Session-only Quick Connect passwords are not persisted.

## Language selection

Windows Setup consumes `GhostLocalization.SupportedLanguages`, the same 29-language local catalog used by the applications. English is the primary fallback. Setup does not contact an online translation service.

## Windows signatures and hashes

Official releases include `SHA256SUMS.txt` and `SIGNING.txt`. Hashes correspond to final executable bytes after the signing step.

Stable releases require a valid trusted Authenticode signature under release policy. Beta builds report signature state; a Beta may be unsigned if production signing credentials are intentionally unavailable.

## Linux installation

Linux release artifacts include self-contained executables and `.tar.gz` archives for x64 and ARM64.

Typical archive use:

```bash
tar -xzf GhostFTP-0.1.2-beta-linux-x64.tar.gz
chmod +x GhostFTP-linux-x64
./GhostFTP-linux-x64
```

The exact published archive name may include a versioned alias; use the matching 0.1.2 Beta GitHub Release asset.

Linux is self-contained for .NET runtime purposes but requires a working X11/XWayland environment and the system `libX11.so.6` ABI used by the native renderer.

There is no Windows-style Setup executable on Linux.

## Linux saved passwords

Linux remembered passwords use AES-256-GCM with locally generated per-user key material and best-effort private file permissions. Session-only Quick Connect entries remain non-persistent.

## Updates

Ghost FTP has no hidden automatic background update check. Users obtain a newer release from the official product/repository release channel and run the new Setup or replace a portable/package executable deliberately.

Setup itself enforces downgrade protection when installing over an existing Windows installation.

## Verification after installation

After starting Ghost FTP:

- verify the title/About version is 0.1.2 Beta;
- verify the expected language and theme;
- optionally use the built-in Demo profile to test Local/Remote navigation and transfer workflow without network access;
- for a real server, prefer explicit FTPS when supported;
- confirm the connection log reports TLS when using FTPS;
- confirm upload/download targets before destructive operations.

## Real-server verification

Developers can use the optional non-destructive live smoke harness described in `docs/LIVE-SMOKE-TEST.md`. Secrets are provided through environment variables/GitHub secrets and must never be committed.

## Troubleshooting

### Setup reports a downgrade

Confirm that the downloaded Setup is newer than or equal to the installed Ghost FTP version. Ghost FTP intentionally blocks downgrades in the normal maintenance path.

### Linux window does not start

Confirm an X11 or XWayland display is available and `libX11.so.6` is installed by the operating system.

### FTPS connection fails

Do not bypass certificate errors as a first response. Confirm host name, port, server certificate, explicit-vs-implicit mode and server FTPS configuration. Ghost FTP intentionally fails rather than silently downgrading an FTPS request to plaintext FTP.

### Portable data is not in the user profile

That is expected. Portable mode stores data beside the executable so it can remain self-contained.

## Release source

The authoritative detailed release body is `docs/releases/v0.1.2.md`. Canonical binaries are the assets attached to the verified `v0.1.2-beta` GitHub Release, not arbitrary executables copied from intermediate build folders.
