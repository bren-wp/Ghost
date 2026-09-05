# Ghost FTP platform support

This document defines the shipping platform contract for the current **Ghost FTP 0.1.0 Beta** public line. It exists to prevent documentation, release assets and source layout from claiming platform support that the repository cannot actually build and validate.

The public version-number reset does not remove platform work already completed during the preserved internal-development history. Platform claims remain based on code and validation, not on the version number alone.

## Release-channel context

Current public release state:

```text
VERSION=0.1.0
RELEASE_CHANNEL=beta
```

Every public `0.x.y` build remains Beta. The first fully stable product release is reserved for **1.0.0**. A platform must not be described as stable merely because a Beta package can be built for it.

The `portable.exe` and `setup.exe` family reaches stable 1.0.0 status only when the Windows production target passes the complete stable release gate documented in `docs/RELEASE-POLICY.md` and `docs/VERSIONING.md`.

## Current production desktop target

### Windows

**Status: supported production desktop implementation; current public packages are Beta until 1.0.0.**

The shipping GUI is `src/GhostFTP.App`, built with C# / .NET 10 / WPF. The guided installer/maintenance application is `src/GhostFTP.Setup`, also WPF.

Supported release architectures:

- Windows x64
- Windows ARM64

The Windows application uses platform facilities intentionally, including WPF, DPAPI, DWM/Mica integration, Windows shell integration and Installed Apps registration.

For the current 0.1.0 Beta line, Windows executable file versions use `0.1.0.0`. When Ghost FTP reaches the first stable release, the canonical x64/ARM64 client and Setup packages must use matching `1.0.0.0` metadata.

## Shared FTP/FTPS engine

`src/GhostFTP.Core` targets plain `net10.0` and contains the protocol/session/queue model independently of WPF.

Core responsibilities include:

- FTP, explicit FTPS and implicit FTPS;
- TLS 1.2/1.3 through .NET;
- EPSV with PASV fallback;
- MLSD with LIST fallback;
- bounded parser/reply/traversal handling;
- upload/download directory traversal;
- download resume and integrity checks;
- rollback-safe remote replacement;
- transfer queue and retry policy;
- input/path safety guards.

This separation is deliberate so future desktop renderers do not need to fork the protocol engine.

## Linux

**Status: core-capable, GUI distribution not yet claimed.**

WPF does not run as a native Linux desktop UI. The current repository also enforces a zero-third-party-`PackageReference` shipping policy. Consequently, Ghost FTP does not claim that the existing WPF application is a Linux application and does not relabel a Windows build as Linux-compatible.

A production Linux desktop release must meet all of these requirements before it is advertised:

1. reuse the same `GhostFTP.Core` protocol engine rather than introducing a second FTP implementation;
2. provide the same major workspace model: Saved Sites, Site Manager, Quick Connect, Connection Log where appropriate, Local/Remote panes, transfer queue, settings, dialogs and keyboard workflows;
3. preserve English as the primary language plus the same supported localization catalog;
4. provide secure local credential protection appropriate to Linux without writing plaintext passwords by default;
5. retain strict TLS certificate validation with no unsafe bypass;
6. contain no telemetry, advertising, tracking SDK or background cloud dependency;
7. have a reproducible build and Linux-specific smoke/integration tests;
8. publish architecture-explicit packages and SHA-256 checksums;
9. document unavoidable platform-specific visual differences instead of claiming false pixel identity;
10. participate in the same Beta/stable version contract rather than inventing an independent stable version number.

Because the current constraints prohibit external UI/runtime dependencies, the Linux renderer requires an explicit architectural decision before implementation. That decision must not silently weaken the zero-dependency policy.

A Linux GUI is **not** required merely to reach Windows stable 1.0.0 unless product scope is explicitly changed before that milestone. Stable 1.0.0 claims only the platforms actually implemented, tested and documented at that time.

## Android and iOS

**Status: not supported and not shipping.**

Ghost FTP is currently a desktop product. Android and iOS application projects are not part of the shipping source tree and should not be added to release packaging, documentation or quality gates unless product scope is deliberately changed in a future version.

The repository must not contain stale mobile build outputs, mobile release packages or documentation that implies an Android/iOS client currently exists.

The move from 0.x Beta to 1.0.0 stable does not automatically create Android/iOS support.

## Parity policy

“Parity” means equivalent user capability and safety guarantees, not pretending that operating systems expose identical native APIs.

Features that must stay behaviorally aligned across desktop renderers include:

- profile model and connection validation;
- FTP/FTPS protocol behavior;
- transfer queue state model;
- retry/cancellation semantics;
- destructive-operation confirmation;
- localization keys and fallback policy;
- privacy guarantees;
- release/version documentation.

Platform-specific code should remain outside `GhostFTP.Core` whenever practical.

## Release artifacts and platform labels

Official package names must correspond to real build targets. Current Windows artifacts include:

```text
portable.exe
setup.exe
portable-arm64.exe
setup-arm64.exe
GhostFTP-Portable-win-x64.exe
GhostFTP-Setup-win-x64.exe
GhostFTP-Portable-win-arm64.exe
GhostFTP-Setup-win-arm64.exe
```

Do not publish Linux, Android or iOS filenames until an actual target exists and passes its own required validation. Do not reuse a Windows executable with a different filename to imply platform compatibility.

## No external tracking or hidden networking

Platform expansion must never be used as a reason to add analytics, telemetry, crash uploading, advertising, automatic profile synchronization or hidden background requests.

Application network traffic remains user-driven FTP/FTPS traffic, documented keepalive/diagnostics against the user-selected server where applicable, plus explicit user-opened product/publisher website links.

## Historical note

Earlier internal 1.x development documents may mention platform decisions made before the public version reset. Those records are preserved for traceability. The active public sequence begins at 0.1.0 Beta, but platform support continues to be determined by the current source tree and validation gates.
