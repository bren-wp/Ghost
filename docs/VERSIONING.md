# Ghost FTP versioning policy

Ghost FTP uses a clean public pre-1.0 version line. The current public development version is **0.1.3 Beta**.

The public numbering reset that began at 0.1.0 did **not** remove, revert or discard earlier application, protocol, UI, Setup, security, privacy, localization, testing or release-pipeline work. Earlier internal 1.x engineering history remains preserved under `docs/HISTORICAL-CHANGELOG.md` and related historical documents for traceability; it does not define the current public version number.

## Authoritative version sources

Two root files define release identity:

- `VERSION` — numeric `MAJOR.MINOR.PATCH`, currently `0.1.3`;
- `RELEASE_CHANNEL` — `beta` or `stable`, currently `beta`.

For the current Beta build, synchronized .NET metadata is:

```text
Version:              0.1.3
AssemblyVersion:      0.1.3.0
FileVersion:          0.1.3.0
InformationalVersion: 0.1.3-beta
```

Windows application and Setup manifests use assembly identity `0.1.3.0`.

## Tag format

Beta tag:

```text
v<MAJOR>.<MINOR>.<PATCH>-beta
```

Stable tag:

```text
v<MAJOR>.<MINOR>.<PATCH>
```

Current expected tag: `v0.1.3-beta`.

## Pre-1.0 policy

Every `0.x.y` build is Beta. No 0.x release should be presented as fully stable. The first stable public target is **1.0.0**.

Patch increments may include substantial hardening, transfer-management work and UX cleanup while the product is pre-1.0. The patch number indicates continued evolution within the active Beta line rather than “trivial changes only.”

## Release-note requirements

Every public version must have detailed notes at:

```text
docs/releases/v<VERSION>.md
```

The active public changelog in `CHANGELOG.md` summarizes each public release and links to its detailed body.

Previous public release documentation must not be overwritten. Current retained public release notes include:

- `docs/releases/v0.1.0.md`;
- `docs/releases/v0.1.1.md`;
- `docs/releases/v0.1.2.md`;
- `docs/releases/v0.1.3.md`.

The pre-reset cumulative engineering changelog remains in `docs/HISTORICAL-CHANGELOG.md`.

## Source and binary identity

A source commit containing a version string is not automatically an official Ghost FTP release. The exact version source must pass release CI and produce canonical Windows/Linux assets attached to the matching GitHub Release.

Windows binary identity must match ProductName **Ghost FTP**, CompanyName **BRENDIGO LTD** and the current file version. Setup validates this metadata during update and refuses version downgrade.

## Release trigger

The active `.github/release-trigger-<VERSION>` marker participates in publication from `main` after a version is ready. Obsolete release-trigger markers are removed when the active source line advances; release notes and changelog history remain.

For 0.1.3 the active marker is:

```text
.github/release-trigger-0.1.3
```

## Release branch policy

A feature/release branch may advance `VERSION`, manifests and documentation before merge so CI can validate the exact intended next-release state. That branch is still **prepared release source**, not a published release.

The normal sequence is:

1. branch from verified `main`;
2. implement and document the next version;
3. run branch/PR CI against the exact versioned source;
4. fix all failing gates;
5. merge only when the intended release is approved;
6. allow the main release workflow to produce/publish canonical artifacts;
7. verify the GitHub Release and assets.

## 0.1.3 scope

The 0.1.3 line is centered on:

- transfer queue dispatch pause/resume;
- failed-transfer retry and selective queue-history cleanup;
- Windows/Linux transfer UI parity;
- additional queue regression tests;
- premium UI cleanup;
- Windows Setup polish;
- detailed documentation synchronization.

Running transfers deliberately continue through a queue pause. This semantic is part of the version's compatibility/documentation contract.

## Stable 1.0 requirements

The project must not set `RELEASE_CHANNEL=stable` for a version below 1.0.0. Stable 1.0 additionally requires the release policy's trusted Windows signing requirements and completion of the complete Windows/Linux build, test, audit, capture, packaging and public GitHub Release gates.

## Historical version references

Files under historical documentation may contain older internal engineering versions. They are preserved for traceability and must not be mass-rewritten merely to make every historical number equal to the active public version.
