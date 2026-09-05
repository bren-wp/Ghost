# Ghost FTP platform support

This document defines the shipping platform contract for **Ghost FTP 0.1.1 Beta**. Platform claims are based on source, CI runtime tests and release artifacts rather than repository description text.

## Current public line

```text
VERSION=0.1.1
RELEASE_CHANNEL=beta
```

All 0.x builds remain Beta. The first stable target is **1.0.0**.

## Shared engine

`src/GhostFTP.Core` targets platform-neutral `net10.0` and is used by both desktop renderers. Shared behavior includes FTP/FTPS negotiation, TLS/data-channel policy, parsing, path safety, transfer queue, retry/cancellation policy, server-only keepalive, persistence primitives and the local-only Demo session used by deterministic regression tests.

## Windows

### Renderer

```text
src/GhostFTP.App
```

- .NET 10 WPF desktop application;
- x64 and ARM64 self-contained publishing;
- installed and portable packaging modes;
- CurrentUser DPAPI for opt-in saved passwords;
- Windows shell/DWM integrations where available;
- deterministic authentic 1914×907 repository screenshot capture.

### Setup

```text
src/GhostFTP.Setup
```

Windows Setup is a separate maintenance workflow, not a second FTP client UI. It embeds the application payload, validates Ghost FTP/BRENDIGO LTD/file-version identity, installs per user, writes normal Installed Apps metadata and supports update/uninstall.

0.1.1 stages both the application and maintenance Setup binaries before changing an existing installation, rejects older candidate file versions and retains rollback copies for both binaries until later install stages succeed.

Expected Windows release assets include `setup.exe`, `portable.exe`, ARM64 aliases, architecture-specific canonical names, `SHA256SUMS.txt` and `SIGNING.txt`.

## Linux

### Renderer

```text
src/GhostFTP.Linux
```

- real native desktop renderer sharing Core + Design;
- no WPF compatibility shim or browser UI;
- direct system X11 client ABI integration;
- works on X11 and compatible Wayland desktops through XWayland;
- requires the standard system library **`libX11.so.6`**;
- AES-256-GCM local saved-secret protection with a per-user key file;
- same plain-FTP confirmation, server-only keepalive and stale-session policy as Windows;
- x64 and ARM64 self-contained packages.

Supported release RIDs:

```text
linux-x64
linux-arm64
```

`libX11.so.6` is an operating-system desktop library. It is not a Ghost FTP cloud service, analytics dependency or NuGet package.

### Linux runtime validation

CI does more than cross-compile:

1. restores and builds the Linux renderer on Ubuntu;
2. starts the real X11/XWayland renderer under Xvfb;
3. runs shared Core tests on Linux;
4. runs the complete local-only Demo workflow regression test on Linux;
5. runs transfer-queue tests on Linux;
6. runs the source/privacy/platform/security audits;
7. builds self-contained `linux-x64` and `linux-arm64` packages;
8. verifies SHA-256 manifests;
9. starts the final packaged x64 binary under Xvfb.

## Visual parity

Windows and Linux use the same `GhostReferencePalette`, product identity, localization catalog and workstation information hierarchy.

The two renderers use native OS graphics stacks, so exact OS font rasterization and window chrome can differ. The parity requirement is the same premium Ghost FTP structure, colors, controls, safety semantics and shared FTP behavior—not a false claim that WPF and X11 generate byte-identical pixels.

See `docs/UI-PARITY.md`.

## Unsupported shipping targets

The current desktop line does **not** ship:

- Android;
- iOS;
- MacCatalyst/macOS native app;
- Web/browser client.

The repository's source/build/release contract is Windows + Linux desktop. A stale GitHub repository metadata description does not override the actual source or release matrix.

## Dependency policy

Shipping projects contain zero third-party NuGet `PackageReference` entries.

- Windows renderer: Microsoft .NET/WPF + OS APIs.
- Linux renderer: Microsoft .NET + system `libX11.so.6` ABI.
- Shared protocol engine: .NET networking/cryptography primitives.

No embedded browser, web UI runtime, analytics SDK or downloaded theme framework is required to run Ghost FTP.

## Real-server verification

A credential-safe optional live test exists in `tests/GhostFTP.LiveSmoke`. It reads credentials only from environment variables/repository secrets and runs a non-destructive connect/PWD/LIST/NOOP/disconnect sequence.

Live credentials are intentionally not part of normal CI or repository fixtures. See `docs/LIVE-SMOKE-TEST.md`.

## Release gate

A platform package is publishable only after its build, runtime smoke tests, complete Demo regression workflow and checksum validation succeed for the exact release source. Windows stable publication additionally requires trusted Authenticode validation.
