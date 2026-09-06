# Ghost FTP Release Policy

This document defines the minimum release discipline for official **Ghost FTP** Windows and Linux builds published by **BRENDIGO LTD**.

## Public versioning

Authoritative root files:

```text
VERSION=0.1.6
RELEASE_CHANNEL=beta
```

All 0.x versions are Beta. The first stable release target is 1.0.0.

Expected tag for this source line:

```text
v0.1.6-beta
```

## GitHub Release requirement

An official public binary release is complete only when the exact verified source revision is represented by the expected tag and the GitHub Release exists, is not a draft, carries the correct Beta/stable classification and contains all required Windows/Linux artifacts.

A local build, CI artifact or unmerged branch is not an official release by itself.

## Required Windows assets

Canonical and architecture-specific assets include `setup.exe`, `portable.exe`, ARM64 aliases, `GhostFTP-Setup-win-x64.exe`, `GhostFTP-Portable-win-x64.exe`, ARM64 variants, `SHA256SUMS.txt` and `SIGNING.txt`.

The process verifies PE/product/version identity and SHA-256 information. The installed maintenance Setup is also the uninstall entry; no separate uninstaller executable is generated.

## Required Linux assets

Linux assets include `GhostFTP-linux-x64`, `GhostFTP-linux-arm64`, x64/ARM64 archives, versioned archive forms, `SHA256SUMS-linux.txt` and `BUILD-INFO.txt`.

## Required release source state

Before the release trigger may be merged to `main`:

- `VERSION` / `RELEASE_CHANNEL` are correct;
- `Directory.Build.props` metadata matches;
- Windows application/Setup manifests match the four-part version;
- `docs/releases/v0.1.6.md` exists;
- README, CHANGELOG, SECURITY, PRIVACY, NOTICE, architecture, UI, installation, localization, platform, versioning and release-policy documentation reference the current line;
- `.github/release-trigger-0.1.6` exists;
- obsolete active release-trigger markers are removed;
- the dedicated safe-resume test project is present and wired into Windows/Linux CI and release workflows;
- authentic UI capture has been inspected when visual layout changed.

## Build gates

### Windows

Required successful stages:

1. restore;
2. Windows solution build plus Linux compile target and isolated resume-test build;
3. dependency/version/privacy/platform/signing audit;
4. final hardening audit;
5. Core self-test;
6. local Demo workflow self-test;
7. parallel transfer queue self-test;
8. protocol/parser/settings/shutdown hardening self-test;
9. safe download resume-integrity self-test;
10. WPF editable-input/localization smoke test;
11. authentic production UI capture;
12. Setup/Portable package build;
13. executable identity/version verification;
14. artifact upload.

### Linux

Required successful stages:

1. restore;
2. native Linux renderer plus shared/isolated test targets;
3. X11/XWayland runtime smoke test;
4. Core self-test;
5. local Demo workflow self-test;
6. parallel transfer queue self-test;
7. protocol/parser/settings/shutdown hardening self-test;
8. safe download resume-integrity self-test;
9. cross-platform source audit;
10. final hardening audit;
11. self-contained x64/ARM64 package build;
12. checksum/runtime verification;
13. artifact upload.

## Transfer regression requirement

Queue regression must verify paused dispatch does not create a new transfer session, resume releases work, queue state is observable, cancellation while paused terminates the job, completed/cancelled/failed history can be cleared selectively, concurrent shutdown is idempotent and post-shutdown enqueue cannot dispatch. Progress delivery remains bounded and terminal state immediate.

## Protocol/parser hardening regression requirement

The package-free `GhostFTP.HardeningSelfTest` must run on Windows and Linux and verify concurrent session/queue disposal, malformed reply framing, bounded preliminary greetings, strict EPSV/PASV behavior, real loopback LIST data flow, pathological LIST/MLSD bounds, safe Unix symlink-name handling and settings backup/value normalization.

## Safe resume integrity regression requirement

The package-free `GhostFTP.ResumeSelfTest` must run independently on Windows and Linux and verify at least:

- a matching partial uses exactly the validated REST offset and produces byte-for-byte correct output;
- changed remote identity causes a restart from byte zero and does not mix stale bytes;
- a same-size remote revision change during transfer is detected by post-transfer metadata validation;
- a rejected changing remote object never replaces an existing local destination;
- staged bytes are not committed until final revision validation succeeds;
- failure to remove an untrusted stale partial aborts before either REST or RETR;
- the test uses process-local loopback only and no external FTP service.

This **resume integrity** gate must execute again in the publication workflow; a green PR-only run is not sufficient for an official release.

## Authentic UI rule

README product screenshots come from the compiled Windows application capture path. Generated/decorative mockups may not replace the canonical application capture. The canonical main capture remains **1914 × 907** logical pixels.

## Security/privacy release invariants

A release preserves fail-closed FTP security selection, strict `AUTH TLS`, TLS certificate/hostname validation, no FTPS downgrade, `PBSZ 0` / `PROT P`, required `TYPE I`, strict/bounded reply and EPSV/PASV parsing, authenticated-control-host passive data routing, bounded LIST/MLSD/traversal/queue resources, deterministic shutdown, cleared pooled transfer buffers, no credential logging, no telemetry/tracking SDK, no cloud account/profile requirement and zero third-party NuGet `PackageReference` dependencies.

0.1.6 additionally requires fail-closed partial-download identity validation. Resume sidecars must be bounded and credential-free; untrusted legacy/stale partials must not be appended blindly; untrusted cleanup failure must abort before data transfer; and post-transfer revision mismatch must discard staged bytes while preserving any previous destination.

## Platform release invariant

The active desktop product ships for Windows and Linux. Android/iOS/MacCatalyst/macOS application targets and a Web/browser application remain outside the repository shipping scope.

## Setup release invariant

Setup validates application and maintenance candidates, refuses to downgrade an existing newer binary and preserves rollback behavior. It must not advertise a silent uninstall command until a genuine tested silent-uninstall implementation exists.

## Release notes and history

Every version includes detailed release notes covering behavior, fixes, security/privacy, stability/performance, UI/UX, Setup/packaging, localization, testing, engineering details and upgrade limitations. Older public releases remain under `docs/releases/`; pre-reset history remains under `docs/HISTORICAL-CHANGELOG.md`.

## Signing

Where signing credentials are configured, official Windows release files may be Authenticode signed by the release workflow. Private signing material must never be committed to source. Unsigned development builds must not be represented as signed official artifacts.

## Failure policy

If any required build/audit/test/package/asset gate fails, do not merge/publish the release source. Fix the source or pipeline, run the full gate against the exact new revision and publish only after the current-source Windows/Linux release contract is green.
