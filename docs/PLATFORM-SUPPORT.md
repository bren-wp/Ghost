# Ghost FTP platform support

This document defines the shipping platform contract for **Ghost FTP 0.1.6 Beta**. Platform claims are based on source, CI runtime tests and release artifacts rather than repository description text.

## Current public line

```text
VERSION=0.1.6
RELEASE_CHANNEL=beta
```

All 0.x builds remain Beta. The first stable target is **1.0.0**.

## Shared engine

Windows and Linux share `GhostFTP.Core`, including FTP/FTPS protocol handling, LIST/MLSD parser, safe download-resume integrity logic, transfer queue, profile/settings models, input/path guards and the Demo FTP session. Both also share `GhostFTP.Design` identity/palette/localization semantics.

0.1.6 adds the same host/port/security/path/`SIZE`/`MDTM` resume identity rules on both shipping platforms. The dedicated `GhostFTP.ResumeSelfTest` runs on both Windows and Linux.

## Windows

**Supported shipping platform.**

Renderer: native WPF (`GhostFTP.App`). Official release builds include x64 and ARM64 variants. The application is per-monitor DPI aware, long-path aware and uses per-user local settings/profile storage. Saved passwords are opt-in and protected by the current-user DPAPI boundary.

Windows packaging provides canonical `setup.exe` and `portable.exe` names plus architecture-specific variants. The workstation persists bounded window/splitter state.

## Linux

**Supported shipping platform.**

Renderer: native C# X11/XWayland application (`GhostFTP.Linux`) using the system `libX11.so.6` ABI. Official release builds include self-contained x64 and ARM64 binaries/archives. Linux saved passwords are opt-in and protected with AES-256-GCM plus local user-private key material.

Linux uses the same Core transfer queue, parser, transfer buffers and download-resume integrity path as Windows.

## Android

Not a shipping target. No Android application package, TFM or source directory belongs to this desktop release line.

## iOS

Not a shipping target. No iOS application package, TFM or source directory belongs to this desktop release line.

## MacCatalyst/macOS application

Not a shipping target in the current release contract.

## Web/browser client

A Web/browser client is not part of the Ghost FTP desktop repository shipping scope. The project does not use a browser shell to claim Windows/Linux desktop parity.

## Mobile-scope audit

Repository source audit rejects known Android/iOS/MacCatalyst target-framework patterns and known mobile source directories. This keeps the project intentionally focused on Windows and Linux.

## Protocol support across platforms

Both shipping platforms support FTP, Explicit FTPS and Implicit FTPS. SFTP/SSH is not implemented and must not be presented as an FTP security mode.

Shared Core provides bounded preliminary greeting handling, strict reply framing, required `TYPE I`, strict EPSV/PASV parsing and authenticated-control-host passive data routing.

## Listing parser parity

Both platforms consume the same bounded LIST/MLSD parser. Per-line/fact limits, non-backtracking regexes, incremental line enumeration and safe Unix symlink-name handling are Core behavior rather than renderer-specific features.

## Transfer and resume parity

Both platforms use the same bounded `TransferQueueService`, including dispatch pause/resume, bounded transient retry, isolated cancellation, progress/speed state, selective history cleanup and coordinated shutdown.

Both platforms also use the same pooled 128 KiB transfer buffers, which are cleared before reuse.

For a resumable download, both platforms require a matching bounded local sidecar plus current server `SIZE` and `MDTM` identity before REST is used. Legacy, corrupt or stale partials restart from zero. If a verifiable remote revision changes while the transfer is running, the completed local result is discarded.

## Localization

Both platforms consume the same **29-language** local catalog. English (`en`) is primary/default/fallback. No online translation service is required.

## Privacy parity

Both platforms are designed without application telemetry, analytics, advertising SDKs, hidden crash upload, cloud profile sync or account requirement. Quick Connect is session-only unless explicitly saved. Resume metadata remains local and contains no credential material.

## CI verification

Windows CI verifies solution/test build, source/hardening audits, Core/Demo/Queue/protocol-parser-settings tests, the isolated safe-resume suite, WPF input/localization smoke, authentic UI capture and Setup/Portable packaging/asset verification.

Linux CI verifies native renderer build, X11/XWayland runtime smoke, Core/Demo/Queue/protocol-parser-settings tests, the isolated safe-resume suite, source/hardening audits and x64/ARM64 self-contained packaging/checksums.

## Real-server testing

A non-destructive live-server harness is documented at [`docs/LIVE-SMOKE-TEST.md`](LIVE-SMOKE-TEST.md). It uses explicit credentials supplied through protected CI secrets and performs connect/PWD/LIST/NOOP/disconnect without write operations.

## Release assets

Official release assets are attached to the versioned GitHub Release only after the release workflow validates the exact source version and expected platform packages.
