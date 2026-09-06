# Ghost FTP Release Policy

This document defines the minimum release discipline for official **Ghost FTP** Windows and Linux builds published by **BRENDIGO LTD**.

## Public versioning

Authoritative root files:

```text
VERSION=0.1.5
RELEASE_CHANNEL=beta
```

All 0.x versions are Beta. The first stable release target is 1.0.0.

Expected tag for this source line:

```text
v0.1.5-beta
```

## GitHub Release requirement

An official public binary release is complete only when the exact verified source revision is represented by the expected tag and the **GitHub Release requirement** is satisfied: the release exists, is not a draft, carries the correct Beta/stable classification and contains the required Windows/Linux artifacts.

A local build, CI artifact or unmerged branch is not an official release by itself.

## Required Windows assets

Canonical and architecture-specific assets include:

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

The process verifies PE/product/version identity and final SHA-256 information. The installed maintenance Setup is also the uninstall entry; no separate uninstaller executable is generated.

## Required Linux assets

Linux assets include:

- `GhostFTP-linux-x64`
- `GhostFTP-linux-arm64`
- x64/ARM64 archives;
- versioned archive forms;
- `SHA256SUMS-linux.txt`;
- `BUILD-INFO.txt`.

The Linux packages are self-contained application builds but use the supported system X11 ABI required by the native renderer.

## Required release source state

Before the release trigger may be merged to `main`:

- `VERSION` and `RELEASE_CHANNEL` are correct;
- `Directory.Build.props` metadata matches;
- Windows application/Setup manifests match the four-part version;
- `docs/releases/v0.1.5.md` exists;
- README, CHANGELOG, SECURITY, PRIVACY, NOTICE, architecture, UI, installation, localization, platform, versioning and release-policy documentation reference the current line;
- `.github/release-trigger-0.1.5` exists;
- obsolete active release-trigger markers are removed;
- authentic UI capture has been inspected when visual layout changed.

## Build gates

### Windows

Required successful stages:

1. restore;
2. Windows solution build plus Linux compile target;
3. dependency/version/privacy/platform/signing audit;
4. final hardening audit;
5. Core self-test;
6. local Demo workflow self-test;
7. parallel transfer queue self-test;
8. protocol/parser/settings/shutdown hardening self-test;
9. WPF editable-input/localization smoke test;
10. authentic production UI capture;
11. Setup/Portable package build;
12. required executable identity/version verification;
13. artifact upload.

### Linux

Required successful stages:

1. restore;
2. native Linux renderer plus shared test targets;
3. X11/XWayland runtime smoke test;
4. Core self-test;
5. local Demo workflow self-test;
6. parallel transfer queue self-test;
7. protocol/parser/settings/shutdown hardening self-test;
8. cross-platform source audit;
9. final hardening audit;
10. self-contained x64/ARM64 package build;
11. checksum/runtime verification;
12. artifact upload.

## Transfer regression requirement

Queue regression must verify paused dispatch does not create a new transfer session, resume releases work, queue state is observable, cancellation while paused terminates the job, completed/cancelled/failed history can be cleared selectively, concurrent shutdown is idempotent and post-shutdown enqueue cannot dispatch.

0.1.5 additionally requires progress delivery to remain bounded without delaying terminal state and no redundant terminal queue notification path.

## Protocol/parser hardening regression requirement

The package-free `GhostFTP.HardeningSelfTest` must run on Windows and Linux and verify at least:

- concurrent `FtpSession.DisposeAsync()` behavior;
- concurrent `TransferQueueService.DisposeAsync()` behavior;
- malformed FTP reply-framing rejection;
- bounded preliminary greeting interoperability (`120 -> 220`);
- valid custom-delimiter EPSV;
- strict PASV fallback and malformed PASV rejection;
- a real loopback LIST data connection;
- bounded/pathological LIST/MLSD parser behavior;
- safe Unix symlink-name handling;
- settings backup recovery and persisted-value bounds.

The test uses loopback only and does not require an external FTP server.

## Authentic UI rule

README product screenshots come from the compiled Windows application capture path. Generated/decorative mockups may not replace the canonical application capture. The canonical main capture remains **1914 × 907** logical pixels.

A visual PR must not be described as fully verified until the exact-head capture is inspected for overlap, clipping, state labels and core action discoverability.

## Security/privacy release invariants

A release preserves:

- fail-closed FTP security-mode validation;
- strict `AUTH TLS` behavior;
- TLS certificate/hostname validation;
- no silent FTPS-to-FTP downgrade;
- `PBSZ 0` / `PROT P` for FTPS;
- required `TYPE I` transfer mode;
- strict/bounded FTP reply framing;
- strict EPSV/PASV parsing;
- authenticated-control-host protection for passive data channels;
- bounded LIST/MLSD payload/line/parser resources;
- bounded traversal/queue resources;
- deterministic session/queue shutdown;
- cleared pooled transfer buffers before reuse;
- no credential logging;
- no application telemetry/tracking SDK;
- no cloud profile/account requirement;
- zero third-party NuGet `PackageReference` dependencies in shipping/regression projects.

## Platform release invariant

The active desktop product ships for Windows and Linux. Android/iOS/MacCatalyst/macOS application targets and a Web/browser application remain outside the repository shipping scope.

## Setup release invariant

Setup validates application and maintenance candidates, refuses to downgrade an existing newer binary and preserves rollback behavior. It must not advertise a silent uninstall command until a genuine tested silent-uninstall implementation exists.

## Release notes and history

Every version includes detailed release notes describing new behavior, fixes, security/privacy, stability/performance, UI/UX, Setup/packaging, localization, testing, engineering details and relevant upgrade limitations.

Older public releases remain under `docs/releases/`; preserved pre-reset engineering history remains under `docs/HISTORICAL-CHANGELOG.md`.

## Signing

Where signing credentials are configured, official Windows release files may be Authenticode signed by the release workflow. Private signing material must never be committed to source. Unsigned development builds must not be represented as signed official artifacts.

## Failure policy

If any required build/audit/test/package/asset gate fails, do not merge the release trigger and do not label that source as fully published. Fix the source or pipeline, re-run against the exact new revision and publish only after the full current-source gate is green.
