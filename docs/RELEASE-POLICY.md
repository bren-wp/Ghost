# Ghost FTP Release Policy

This document defines the minimum release discipline for official **Ghost FTP** Windows and Linux builds published by **BRENDIGO LTD**.

## Public versioning

Authoritative root metadata:

```text
VERSION=0.1.2
RELEASE_CHANNEL=beta
```

All Ghost FTP 0.x releases are Beta. The first stable target is 1.0.0.

For the current line:

- semantic source version: **0.1.2**;
- informational version: **0.1.2-beta**;
- file/assembly version: **0.1.2.0**;
- expected tag: **`v0.1.2-beta`**.

`Directory.Build.props`, Windows application manifest and Windows Setup manifest must be synchronized with `VERSION` before publication.

## Release notes

Every public version requires a non-empty detailed release note at:

```text
docs/releases/v<VERSION>.md
```

For 0.1.2 this is `docs/releases/v0.1.2.md`. The note must identify the exact version/channel and describe user-visible changes, security/privacy implications, platform scope, packaging and test gates.

`CHANGELOG.md` must preserve all previous public versions. Earlier internal/pre-reset engineering history remains under `docs/HISTORICAL-CHANGELOG.md`.

## Release trigger

The Release workflow may be started manually or by the current `.github/release-trigger-<VERSION>` marker changing on `main`. Obsolete release-trigger markers are removed after the public line advances.

The marker is not a substitute for CI. It only signals that the exact source is intended for publication; the Release workflow must still rebuild and re-audit everything.

## Source/dependency gate

Before packaging, source audit must verify at minimum:

- shipping projects contain no third-party NuGet `PackageReference` entries;
- known telemetry/tracking/crash-upload SDK references are absent;
- private signing files are not tracked;
- version/channel/product/publisher metadata is synchronized;
- Android/iOS/MacCatalyst targets and known mobile source directories are absent;
- Windows and Linux native renderers remain in the solution;
- 29-language local catalog with English fallback remains available;
- protocol security boundary and clean workstation structure remain present.

## Hardening gate

Final hardening audit verifies security/privacy/release contracts that are too important to rely only on compiler success, including FTPS negotiation, binary transfer mode, installer rollback/downgrade protection, local Demo regression coverage, live-smoke non-destructive guarantees, authentic screenshot use and synchronized documentation.

## Build/test gate

The exact release source must pass:

1. Windows solution restore/build;
2. Linux solution restore/build;
3. source/dependency/privacy/platform audit;
4. final hardening audit;
5. Core self-test;
6. complete local Demo workflow self-test on Windows and Linux;
7. parallel transfer queue self-test;
8. Windows WPF editable-input/localization smoke test;
9. Linux native renderer/runtime checks;
10. authentic Windows application capture.

A merge that has not passed the required gates is not sufficient evidence for a public release.

## Authentic UI capture gate

The release Windows renderer is launched through the application capture path. Required images:

- `ghostftp-client.png` at exactly 1914 × 907 for the canonical main-window capture;
- `ghostftp-site-manager.png`.

Required captures must be non-empty and of plausible production size. A mockup or decorative illustration cannot substitute for the compiled application capture.

## Windows release artifacts

Canonical Windows x64 artifacts:

- `setup.exe`;
- `portable.exe`.

ARM64 canonical artifacts:

- `setup-arm64.exe`;
- `portable-arm64.exe`.

Descriptive aliases include:

- `GhostFTP-Setup-win-x64.exe`;
- `GhostFTP-Portable-win-x64.exe`;
- `GhostFTP-Setup-win-arm64.exe`;
- `GhostFTP-Portable-win-arm64.exe`.

Release output also includes `SHA256SUMS.txt` and `SIGNING.txt`.

Every Windows executable must report ProductName **Ghost FTP**, CompanyName **BRENDIGO LTD**, and file version `0.1.2.0` for this release.

## Windows Setup contract

Setup is self-contained, per-user and also serves as the installed maintenance/uninstall executable. No separate `uninstall.exe` is required or desired.

The installer must validate staged application and maintenance candidates, reject downgrades, keep rollback copies until later install steps succeed, and restore/remove committed files appropriately on failure.

`QuietUninstallString` must not be advertised until a genuine tested silent uninstall path exists.

## Signing policy

Windows release binaries are passed through the repository signing step. Stable releases require a valid trusted Authenticode signature. Beta releases record signature state and may remain unsigned/untrusted-signed when production signing credentials are intentionally unavailable.

Signing private keys/certificates must be provided through GitHub secrets or local secure developer inputs and must never be committed.

## Linux release artifacts

Required Linux architecture executables include:

- `GhostFTP-linux-x64`;
- `GhostFTP-linux-arm64`.

Equivalent `.tar.gz` archives and versioned aliases are published by the Linux release workflow. Packages are self-contained for the .NET runtime and use the system `libX11.so.6` ABI.

## Hash verification

Final release bytes are hashed after signing/packaging. `SHA256SUMS.txt` must match the exact Windows executables attached to the public release. Linux packaging produces corresponding checksums for Linux deliverables.

Release verification compares hashes against final bytes, not pre-signing intermediates.

## GitHub Release requirement

A Ghost FTP release is not complete until the expected version tag exists and the canonical Windows/Linux artifacts are attached to the matching **GitHub Release**.

For 0.1.2 Beta that means the release associated with `v0.1.2-beta` must contain the verified downloadable artifacts produced from the exact audited source.

## Live real-server smoke test

The optional live smoke workflow is separate from deterministic release CI. Credentials come from secrets/environment variables, are redacted, and the harness remains non-destructive. It may connect, read PWD/listing and send keepalive, but it must not upload, create, rename or delete remote content.

See `docs/LIVE-SMOKE-TEST.md`.

## Failure policy

If build, audit, test, capture, packaging, signing-policy, version, hash or asset verification fails, publication stops. The release must be fixed at source and rebuilt; failed release artifacts are not manually promoted as canonical.

## Repository history

Prior release notes, changelog entries and historical engineering documentation remain versioned. Advancing a release may remove obsolete trigger markers and dead shipping code, but it must not erase earlier public release history.
