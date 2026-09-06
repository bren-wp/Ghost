# Ghost FTP platform support

This document defines the shipping platform contract for **Ghost FTP 0.1.4 Beta**. Platform claims are based on source, CI runtime tests and release artifacts rather than repository description text.

## Current public line

```text
VERSION=0.1.4
RELEASE_CHANNEL=beta
```

All 0.x builds remain Beta. The first stable target is **1.0.0**.

## Shared engine

Windows and Linux share `GhostFTP.Core`, the FTP/FTPS protocol implementation, transfer queue, profiles/settings models, input/path guards, Demo FTP session and `GhostFTP.Design` identity/palette/localization semantics. 0.1.4 also runs the same deterministic protocol/shutdown hardening suite on both operating systems.

## Windows

**Supported shipping platform.**

Renderer: native WPF (`GhostFTP.App`) targeting modern Windows desktop systems.

Official release builds include x64 and ARM64 variants. The Windows application is per-monitor DPI aware, long-path aware and uses per-user local settings/profile storage. Saved passwords are opt-in and use the current-user DPAPI boundary.

Windows packaging provides canonical `setup.exe` and `portable.exe` names plus architecture-specific variants.

## Linux

**Supported shipping platform.**

Renderer: native C# X11/XWayland application (`GhostFTP.Linux`) using the system `libX11.so.6` ABI.

Official release builds include self-contained x64 and ARM64 binaries/archives. Linux saved passwords are opt-in and protected with AES-256-GCM plus local user-private key material.

The Linux transfer UI includes selectable transfer rows, queue dispatch pause/resume, retry-failed, cancellation and cleanup controls using the same Core queue service as Windows.

## Android

Not a shipping target. No Android application package, TFM or source directory belongs to this desktop release line.

## iOS

Not a shipping target. No iOS application package, TFM or source directory belongs to this desktop release line.

## MacCatalyst/macOS application

Not a shipping target in the current release contract.

## Web/browser client

A **Web/browser client** is not part of the Ghost FTP desktop repository shipping scope. The project does not use a browser shell to claim Windows/Linux desktop parity.

## Mobile-scope audit

Repository source audit rejects known Android/iOS/MacCatalyst target-framework patterns and known mobile source directories. This keeps the project intentionally focused on Windows and Linux.

## Protocol support across platforms

Both shipping platforms support FTP, Explicit FTPS and Implicit FTPS. SFTP/SSH is not currently implemented and must not be presented as an FTP security mode.

0.1.4 adds shared bounded `1xx -> 2xx` greeting handling, stricter reply framing and strict EPSV/PASV port parsing in Core, so the same protocol hardening applies to Windows and Linux.

## Localization

Both platforms consume the same 29-language local catalog. English (`en`) is primary/default/fallback. No online translation service is required.

## Privacy parity

Both platforms are designed without application telemetry, analytics, advertising SDKs, hidden crash upload, cloud profile sync or account requirement. Quick Connect is session-only unless explicitly saved.

## Transfer parity

Both platforms use the same bounded `TransferQueueService`, including dispatch pause/resume, bounded transient retry, isolated cancellation, progress/speed state, selective finished-history cleanup and coordinated shutdown. A queue pause does not interrupt already-running FTP byte streams.

## CI verification

Windows CI verifies solution build, source/hardening audits, Core/Demo/Queue/protocol-hardening self-tests, WPF input/localization smoke, authentic UI capture, and Setup/Portable packaging/asset verification.

Linux CI verifies native renderer build, X11/XWayland runtime smoke, Core/Demo/Queue/protocol-hardening self-tests, source/hardening audits, and x64/ARM64 self-contained packaging/checksums.

## Real-server testing

A non-destructive live-server harness is documented at [`docs/LIVE-SMOKE-TEST.md`](LIVE-SMOKE-TEST.md). It uses explicit credentials supplied through protected CI secrets and performs connect/PWD/LIST/NOOP/disconnect without write operations.

## Release assets

Official release assets are attached to the versioned GitHub Release only after the release workflow validates the current source version and expected platform packages.
