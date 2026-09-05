# Ghost FTP platform support

This document defines the shipping platform contract for Ghost FTP. It exists to prevent documentation, release assets and source layout from claiming platform support that the repository cannot actually build and validate.

## Current production desktop target

### Windows

**Status: supported production desktop client.**

The shipping GUI is `src/GhostFTP.App`, built with C# / .NET 10 / WPF. The guided installer/maintenance application is `src/GhostFTP.Setup`, also WPF.

Supported release architectures:

- Windows x64
- Windows ARM64

The Windows application uses platform facilities intentionally, including WPF, DPAPI, DWM/Mica integration, Windows shell integration and Installed Apps registration.

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
2. provide the same major workspace model: Saved Servers, Quick Connect, Local/Remote panes, transfer queue, settings, dialogs and keyboard workflows;
3. preserve English as the primary language plus the same supported localization catalog;
4. provide secure local credential protection appropriate to Linux without writing plaintext passwords by default;
5. retain strict TLS certificate validation with no unsafe bypass;
6. contain no telemetry, advertising, tracking SDK or background cloud dependency;
7. have a reproducible build and Linux-specific smoke/integration tests;
8. publish architecture-explicit packages and SHA-256 checksums;
9. document any unavoidable platform-specific visual differences instead of claiming false pixel identity.

Because the current constraints prohibit external UI/runtime dependencies, the Linux renderer requires an explicit architectural decision before implementation. That decision must not silently weaken the zero-dependency policy.

## Android and iOS

**Status: not supported and not shipping.**

Ghost FTP is a desktop product. Android and iOS application projects are not part of the current shipping source tree and should not be added to release packaging, documentation or quality gates unless the product scope is deliberately changed in a future version.

The repository must not contain stale mobile build outputs, mobile release packages or documentation that implies an Android/iOS client currently exists.

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

## No external tracking or hidden networking

Platform expansion must never be used as a reason to add analytics, telemetry, crash uploading, advertising, automatic profile synchronization or hidden background requests.

Application network traffic remains user-driven FTP/FTPS traffic plus explicit user-opened product/publisher website links.
