# Ghost FTP platform support

This document defines the shipping platform contract for the current **Ghost FTP 0.1.0 Beta** public line. Platform claims are based on source, build validation and actual release assets—not on naming alone.

## Release-channel context

Current public release state:

```text
VERSION=0.1.0
RELEASE_CHANNEL=beta
```

Every public `0.x.y` build remains Beta. The first stable product release is reserved for **1.0.0**. A platform must not be described as stable merely because a Beta package can be built for it.

## Shared FTP / FTPS engine

`src/GhostFTP.Core` targets plain `net10.0` and is shared by both desktop renderers.

Shared behavior includes:

- FTP;
- explicit FTPS (`AUTH TLS`);
- implicit FTPS;
- TLS 1.2 / TLS 1.3 through .NET;
- normal certificate-chain and hostname validation;
- EPSV preference with PASV fallback;
- UTF-8 negotiation;
- MLSD with LIST fallback;
- bounded parsing and traversal;
- create / rename / delete operations;
- recursive upload / download;
- resumable downloads where supported;
- transfer queue, retry and cancellation policy;
- path and command-injection guards;
- server-only keepalive and diagnostics.

The FTP implementation is not duplicated per operating system.

## Shared desktop reference

Windows, Windows portable mode, Windows Setup and Linux use the same Ghost FTP reference palette and information hierarchy. The canonical design tokens live in `src/GhostFTP.Design/GhostReferencePalette.cs`.

The normal workstation hierarchy is:

```text
permanent left rail
→ menu
→ global action toolbar
→ Connection Log + Quick Connect
→ Local + Remote file panes
→ Transfers
→ compact status/privacy state
```

The approved normal-desktop geometry uses a 292 px left rail, 38 px menu and 70 px toolbar. Windows and Linux consume the same reference colors rather than maintaining unrelated visual identities. Setup uses the same palette and control language while retaining its installer-specific workflow.

The installed Windows application and `portable.exe` are packaging modes of the same `GhostFTP.App` renderer. Portable mode may change local data paths, but it must not change the application UI, FTP behavior, privacy rules or security controls.

See `docs/UI-PARITY.md` for the complete visual contract and validation rules.

## Windows

**Status: supported desktop implementation; public packages remain Beta during the 0.x line.**

The Windows GUI is `src/GhostFTP.App`, built with C# / .NET 10 / WPF. The guided installer and maintenance application is `src/GhostFTP.Setup`.

Supported release architectures:

- Windows x64;
- Windows ARM64.

Windows intentionally uses platform facilities including WPF, DPAPI, DWM integration, shell integration and Installed Apps registration.

Official Windows release files include:

```text
portable.exe
setup.exe
portable-arm64.exe
setup-arm64.exe
GhostFTP-Portable-win-x64.exe
GhostFTP-Setup-win-x64.exe
GhostFTP-Portable-win-arm64.exe
GhostFTP-Setup-win-arm64.exe
SHA256SUMS.txt
SIGNING.txt
```

### Windows code signing

The release pipeline supports SHA-256 Authenticode signing using a private PFX supplied only through GitHub Actions secrets. Private signing material is never committed to the repository.

A self-signed development certificate is supported for local signing tests, but it does not create public Windows publisher trust. Stable publisher trust requires an appropriate CA-issued code-signing certificate whose legal publisher identity matches **BRENDIGO LTD**.

See `docs/CODE-SIGNING.md`.

## Linux

**Status: native desktop implementation present; current public Linux packages remain Beta during the 0.x line.**

The Linux desktop client is `src/GhostFTP.Linux`. It uses the same `GhostFTP.Core` engine and shared product/localization/reference-design definitions from `GhostFTP.Design`.

The Linux renderer is implemented directly against the standard X11 client ABI instead of introducing a third-party NuGet UI framework. This preserves the repository's zero-third-party-`PackageReference` shipping rule.

Supported release architectures:

- Linux x64 (`linux-x64`);
- Linux ARM64 (`linux-arm64`).

The release script is:

```bash
./build-linux-release.sh
```

It produces self-contained .NET 10 single-file binaries, user-local install/uninstall helpers, architecture-explicit tarballs and SHA-256 checksums.

Expected Linux release files include:

```text
GhostFTP-linux-x64
GhostFTP-linux-arm64
GhostFTP-linux-x64.tar.gz
GhostFTP-linux-arm64.tar.gz
GhostFTP-0.1.0-beta-linux-x64.tar.gz
GhostFTP-0.1.0-beta-linux-arm64.tar.gz
SHA256SUMS-linux.txt
BUILD-INFO.txt
```

### Linux desktop requirement

Ghost FTP's Linux renderer requires the standard X11 client library (`libX11.so.6`). Native X11 desktops can run it directly. Wayland desktops can run it through XWayland, which is widely available on mainstream desktop distributions.

This is an operating-system desktop library dependency, not a downloaded Ghost FTP framework, analytics SDK or online service.

### Linux credential protection

Linux saved-password support uses AES-256-GCM with a cryptographically random local key stored in the Ghost FTP application-data directory. The key file is restricted to the current user (`0600`) where the filesystem supports Unix permissions.

This prevents plaintext credential storage and detects ciphertext tampering. It is intentionally documented as local file-based protection rather than being misrepresented as equivalent to Windows DPAPI or a hardware-backed secret store.

### Linux workspace parity

The Linux implementation follows the same professional FTP workstation hierarchy and reference palette as the Windows client:

- permanent product/saved-site/privacy rail;
- top application menu / global actions;
- Connection Log and Quick Connect side by side at normal desktop width;
- Local / Remote file panes;
- create / rename / delete / refresh actions;
- upload and download queue;
- transfer progress and cancellation;
- settings and language selection;
- local-only persistence;
- no telemetry or tracking.

The renderer remains native X11/XWayland rather than WPF, so font rasterization, OS window chrome and native text metrics can differ by desktop environment. Those operating-system differences do not permit changing the product palette, feature placement, security model or core workstation hierarchy.

## Android and iOS

**Status: not supported and not shipping.**

Ghost FTP is currently a desktop product. Android and iOS projects are not part of the shipping source tree or release packaging.

## Parity policy

Features that must remain behaviorally aligned across Windows and Linux include:

- profile model and connection validation;
- FTP / FTPS protocol behavior;
- TLS validation policy;
- transfer queue state model;
- retry / cancellation semantics;
- destructive-operation safeguards;
- localization fallback policy;
- privacy guarantees;
- reference desktop hierarchy and palette;
- release/version documentation;
- no hidden product networking.

Platform-specific UI and OS integration code remains outside `GhostFTP.Core`.

## Privacy and networking

Platform expansion and UI parity work must never be used as a reason to add analytics, telemetry, crash uploading, advertising, automatic profile synchronization, embedded web UI or hidden background requests.

Ghost FTP application network traffic remains user-driven FTP/FTPS traffic, documented keepalive/diagnostics against the user-selected server, plus explicit user-opened website links. Code-signing and release packaging are build-time operations and do not create a runtime signing-service dependency.

## Stable 1.0.0 gate

A stable 1.0.0 platform claim requires the corresponding implementation to build, pass its platform-specific quality gates and publish the documented architecture-explicit assets. Stable Windows releases additionally require trusted Authenticode validation in the release pipeline.

Until those gates are satisfied, all 0.x desktop packages remain explicitly **Beta**.
