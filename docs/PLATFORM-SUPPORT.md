# Ghost FTP platform support

This document defines the shipping platform contract for **Ghost FTP 0.1.2 Beta**. Platform claims are based on source, CI/runtime tests and published release artifacts rather than marketing text.

## Current public line

```text
VERSION=0.1.2
RELEASE_CHANNEL=beta
```

All 0.x builds remain Beta. The first stable target is **1.0.0**.

## Supported desktop platforms

### Windows

Ghost FTP ships a native WPF desktop renderer targeting modern supported Windows environments through `net10.0-windows10.0.19041.0`.

Release architectures:

- Windows x64;
- Windows ARM64.

Release forms:

- self-contained portable executable;
- self-contained per-user Setup/maintenance executable.

Windows application behavior includes native minimize, maximize, restore and resizing. The 0.1.2 workstation additionally exposes draggable sidebar, connection-area, Local/Remote and Transfers splitters.

Saved passwords are opt-in and protected with Windows DPAPI for the current user.

### Linux

Ghost FTP ships a native .NET 10 X11/XWayland renderer, not a Wine package and not a web wrapper.

Release architectures:

- Linux x64;
- Linux ARM64.

Release forms:

- self-contained executable;
- `.tar.gz` archive.

The application uses the system `libX11.so.6` ABI for native desktop window integration. XWayland environments are supported through the X11 compatibility path used by the renderer.

Linux saved passwords are opt-in and protected with AES-256-GCM using local per-user key material.

## Shared engine

Windows and Linux both reference the same platform-neutral `GhostFTP.Core` project for:

- FTP/FTPS connection lifecycle;
- TLS negotiation;
- FTP command/reply handling;
- directory listing/navigation;
- upload/download;
- recursive directory operations;
- transfer queue/cancellation/retry;
- input/path validation;
- local Demo session.

They also share `GhostFTP.Design` for product identity, reference palette and the 29-language local catalog.

This prevents platform-specific renderers from implementing incompatible protocol engines.

## Security parity

Both desktop platforms inherit the same core security rules:

- fail-closed security-mode selection;
- explicit FTPS requires successful `AUTH TLS`;
- normal TLS certificate-chain/hostname validation;
- encrypted sessions require `PBSZ 0` and `PROT P`;
- binary data transfer uses `TYPE I`;
- passive data connections are constrained to the authenticated control host;
- command/control input is bounded and CR/LF/NUL is rejected;
- recursive traversal is bounded.

Plain FTP remains an explicit legacy mode and is not advertised as secure.

## Privacy parity

Both platforms operate without application telemetry, analytics, advertising SDKs, tracking SDKs, automatic crash upload or cloud profile synchronization.

Connection/profile data is local. Session-only Quick Connect entries are not persisted. No Ghost FTP account is required.

## Localization parity

Windows, Linux and Windows Setup use the same local `GhostLocalization.SupportedLanguages` catalog. English is the primary fallback and the current release exposes 29 selectable languages.

No online translation service is used.

## Windows Setup

Setup is Windows-specific and is not copied to Linux. The installed maintenance `GhostFTP-Setup.exe` handles update and uninstall. Ghost FTP does not ship a separate `uninstall.exe`.

Setup validates staged application/maintenance candidates, rejects downgrades and maintains rollback copies until later install steps succeed.

## Portable mode

Windows portable builds store local data beside the executable under `Data`. Installed builds use per-user application data.

Linux packages use the normal Linux per-user application data path unless launched in a portable identity/configuration supported by the shared path model.

## Unsupported application platforms

The active Ghost FTP desktop line intentionally does **not** ship:

- Android application;
- iOS application;
- MacCatalyst application;
- macOS native application;
- **Web/browser client**.

Source audit rejects known Android/iOS/mobile source directories and mobile target frameworks if they are reintroduced.

A web/browser client is not considered equivalent to the native product because browser sandboxes cannot expose the same unrestricted local-file workflow and desktop credential-store semantics.

## Runtime prerequisites

Windows packages are self-contained for .NET runtime purposes. Linux release executables are also self-contained but require a working X11/XWayland environment and the system X11 ABI used by the renderer.

No third-party NuGet package dependency is required by shipping projects.

## CI and release evidence

Platform support is gated by GitHub Actions:

- solution builds on Windows and Linux;
- source/dependency/platform audit;
- Core self-test;
- complete local Demo workflow on both platforms;
- transfer queue self-test;
- WPF UI smoke test;
- Linux renderer/runtime checks;
- authentic Windows UI capture;
- Windows x64/ARM64 packaging;
- Linux x64/ARM64 packaging.

The optional real-server smoke harness is documented in `docs/LIVE-SMOKE-TEST.md`. It is secret-backed and non-destructive.

## Release artifacts

The expected 0.1.2 Beta GitHub Release tag is `v0.1.2-beta`.

Windows canonical artifacts include `setup.exe`, `portable.exe`, ARM64 variants, architecture aliases, SHA-256 hashes and signing report. Linux canonical artifacts include x64/ARM64 executables and archives plus release hashes.

A platform is considered publicly supported by a release only when its expected artifacts were produced from the exact release source and attached successfully to the matching GitHub Release.
