# Ghost FTP versioning policy

Ghost FTP uses a clean public pre-1.0 version line. The current public development version is **0.1.2 Beta**.

The public numbering reset that began at 0.1.0 did **not** remove, revert or discard earlier application, protocol, UI, Setup, security, privacy, localization, testing or release-pipeline work. Earlier internal 1.x engineering history remains preserved under `docs/HISTORICAL-CHANGELOG.md` and related historical documents for traceability; it does not define the current public version number.

## Authoritative version sources

Two root files define release identity:

- `VERSION` — numeric `MAJOR.MINOR.PATCH`, currently `0.1.2`;
- `RELEASE_CHANNEL` — `beta` or `stable`, currently `beta`.

For the current Beta build, synchronized .NET metadata is:

```text
Version:              0.1.2
AssemblyVersion:      0.1.2.0
FileVersion:          0.1.2.0
InformationalVersion: 0.1.2-beta
```

Windows application and Setup manifests use assembly identity `0.1.2.0`.

## Tag format

Beta tag:

```text
v<MAJOR>.<MINOR>.<PATCH>-beta
```

Stable tag:

```text
v<MAJOR>.<MINOR>.<PATCH>
```

Current expected tag: `v0.1.2-beta`.

## Pre-1.0 policy

Every `0.x.y` build is Beta. No 0.x release should be presented as fully stable. The first stable public target is **1.0.0**.

Patch increments may include substantial hardening and UX cleanup while the product is still pre-1.0. The patch number does not imply that only trivial line changes are allowed; it indicates continued compatibility within the active Beta line.

## Release-note requirements

Every public version must have detailed notes at:

```text
docs/releases/v<VERSION>.md
```

The active public changelog in `CHANGELOG.md` summarizes each public release and links users to the detailed per-version body.

Previous release documentation must not be overwritten when a new version is published. Current retained public release notes include:

- `docs/releases/v0.1.0.md`;
- `docs/releases/v0.1.1.md`;
- `docs/releases/v0.1.2.md`.

The pre-reset cumulative engineering changelog remains in `docs/HISTORICAL-CHANGELOG.md`.

## Source and binary identity

A build is not considered an official Ghost FTP release merely because a source commit contains a version string. The same source version must pass release CI and produce canonical Windows/Linux assets attached to the matching GitHub Release.

Windows binary identity must match ProductName **Ghost FTP**, CompanyName **BRENDIGO LTD**, and the current file version. Setup validates this metadata during update and refuses version downgrade.

## Release trigger

The current `.github/release-trigger-<VERSION>` marker is used to trigger publication from `main` after the version is ready. Obsolete release-trigger markers are removed when the public line advances; release notes/changelog history remain.

## Stable 1.0 requirements

The project should not change `RELEASE_CHANNEL` to `stable` for a version below 1.0.0. Stable 1.0 additionally requires the release policy's trusted Windows signing requirement and completion of the full Windows/Linux build, test, audit, capture, packaging and public GitHub Release gates.

## Development branches

Feature/release branches may advance version metadata before merge so CI can validate the exact intended release state. The public release exists only after verified source reaches `main` and the Release workflow publishes the expected artifacts.
