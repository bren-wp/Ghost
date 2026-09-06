# Ghost FTP Release Policy

This document defines the minimum release discipline for official **Ghost FTP** Windows and Linux builds published by **BRENDIGO LTD**.

## Public versioning

Authoritative root files:

```text
VERSION=0.1.3
RELEASE_CHANNEL=beta
```

All 0.x versions are Beta. The first stable release target is 1.0.0.

For this source line the expected tag is:

```text
v0.1.3-beta
```

## GitHub Release requirement

An official public binary release is complete only when the exact verified source revision is represented by the expected tag and a **GitHub Release requirement** has been satisfied: the release exists, is not a draft, carries the expected Beta/stable classification and contains the required Windows/Linux artifacts.

A local build, CI artifact or unmerged branch is not an official release by itself.

## Required Windows assets

Canonical names:

- `setup.exe`
- `portable.exe`

Architecture-specific variants include x64/ARM64 Setup and Portable executables. The release process verifies expected PE/product/version identity and SHA-256 information.

The installed maintenance Setup is also the uninstall entry. Ghost FTP does not generate a separate uninstaller executable.

## Required Linux assets

Linux assets include:

- `GhostFTP-linux-x64`
- `GhostFTP-linux-arm64`
- x64/ARM64 archives;
- versioned archive forms;
- release build/checksum information.

The Linux packages are self-contained application builds but still rely on the supported system X11 ABI used by the native renderer.

## Required release source state

Before the release trigger may be merged to main:

- `VERSION` and `RELEASE_CHANNEL` must be correct;
- `Directory.Build.props` metadata must match;
- Windows manifests must match the four-part assembly version;
- the current `docs/releases/vVERSION.md` must exist;
- README, CHANGELOG, SECURITY, PRIVACY, architecture, UI, installation, localization, platform and legal documentation must reference the current line;
- the current `.github/release-trigger-VERSION` marker must exist;
- stale previous release-trigger markers must be removed from active source.

## Build gates

### Windows

Required successful stages include:

1. restore;
2. Windows solution build plus Linux compile target;
3. dependency/version/privacy/platform/signing audit;
4. final hardening audit;
5. Core self-test;
6. local Demo workflow self-test;
7. parallel transfer queue self-test;
8. WPF editable-input/localization smoke test;
9. authentic production UI capture;
10. Setup/Portable package build;
11. required asset identity/version verification;
12. artifact upload.

### Linux

Required successful stages include:

1. restore;
2. native Linux renderer and live-smoke harness build;
3. X11/XWayland runtime smoke test;
4. Core self-test;
5. local Demo workflow self-test;
6. parallel transfer queue self-test;
7. cross-platform source audit;
8. final hardening audit;
9. self-contained x64/ARM64 package build;
10. checksum/runtime verification;
11. artifact upload.

## 0.1.3 transfer regression requirement

The queue test must verify the new dispatch-pause contract:

- paused queue does not start a new transfer session;
- resume releases queued work;
- queue state change is observable;
- cancellation while paused terminates that job without blocking the worker;
- completed, cancelled and failed history can be cleared selectively.

This is required because the UI now exposes pause/resume on both Windows and Linux.

## Authentic UI rule

README product screenshots must come from the compiled Windows application capture path. Decorative/generated mockups must not replace the canonical application capture.

The canonical main capture remains 1914 × 907 logical pixels at the documented capture DPI.

## Security/privacy release invariants

A release must preserve:

- fail-closed FTP security mode validation;
- strict `AUTH TLS` behavior;
- normal TLS certificate/hostname validation;
- no silent FTPS-to-FTP downgrade;
- `PBSZ 0` / `PROT P` for FTPS;
- required `TYPE I` transfer mode;
- authenticated-control-host protection for passive data channels;
- bounded untrusted input/traversal/queue resources;
- no credential logging;
- no application telemetry/tracking SDK;
- no cloud profile/account requirement;
- zero third-party NuGet `PackageReference` dependencies in shipping projects.

## Platform release invariant

The active desktop product ships for Windows and Linux. Android/iOS/MacCatalyst application targets remain outside the repository shipping scope and are rejected by source audit.

## Setup release invariant

Setup must validate application and maintenance candidates, refuse to downgrade an existing newer binary and preserve rollback behavior. It must not advertise a silent uninstall command until a genuine tested silent-uninstall implementation exists.

## Release notes

Every version must include detailed notes with at least:

- Summary;
- New;
- Improved;
- Fixed;
- Security;
- Privacy;
- Stability/performance;
- UI/UX;
- Setup/packaging;
- Localization;
- Testing;
- internal engineering notes;
- known limitations/upgrade notes where relevant.

Older public releases remain documented; the preserved engineering history remains under `docs/HISTORICAL-CHANGELOG.md`.

## Signing

Where signing credentials are configured, official Windows release files may be Authenticode signed by the release workflow. Private signing material must never be committed to source. Unsigned development builds must not be represented as signed official artifacts.

## Failure policy

If any required build/audit/test/package/asset gate fails, do not merge the release trigger and do not label that source as fully published. Fix the source or release pipeline, re-run against the exact new revision and only publish after the full current-source gate is green.
