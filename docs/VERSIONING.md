# Ghost FTP versioning policy

Ghost FTP uses a clean public pre-1.0 version line. The current public development version is **0.1.6 Beta**.

The public numbering reset that began at 0.1.0 did **not** remove, revert or discard earlier application, protocol, UI, Setup, security, privacy, localization, testing or release-pipeline work. Earlier engineering history remains preserved under `docs/HISTORICAL-CHANGELOG.md` and historical release documents for traceability.

## Authoritative version sources

Two root files define release identity:

- `VERSION` — numeric `MAJOR.MINOR.PATCH`, currently `0.1.6`;
- `RELEASE_CHANNEL` — `beta` or `stable`, currently `beta`.

For the current Beta build:

```text
Version:              0.1.6
AssemblyVersion:      0.1.6.0
FileVersion:          0.1.6.0
InformationalVersion: 0.1.6-beta
```

Windows application and Setup manifests use assembly identity `0.1.6.0`.

## Tag format

Beta tag: `v<MAJOR>.<MINOR>.<PATCH>-beta`  
Stable tag: `v<MAJOR>.<MINOR>.<PATCH>`

Current expected tag: `v0.1.6-beta`.

## Pre-1.0 policy

Every `0.x.y` build is Beta. No 0.x release is represented as stable. The first stable public target is **1.0.0**.

Patch increments may contain meaningful security/integrity hardening, protocol/parser work, transfer-management changes, performance improvements, deterministic regression expansion and UX refinement while the product remains pre-1.0.

## Release-note requirements

Every public version has detailed notes at `docs/releases/v<VERSION>.md`. The active public changelog summarizes each release and links to its detailed body.

Retained public release notes include `v0.1.0.md` through `v0.1.6.md`. The pre-reset cumulative engineering changelog remains in `docs/HISTORICAL-CHANGELOG.md`.

## Source and binary identity

A source commit containing a version string is not automatically an official Ghost FTP release. The exact version source must pass release CI and produce canonical Windows/Linux assets attached to the matching GitHub Release.

Windows binary identity must match ProductName **Ghost FTP**, CompanyName **BRENDIGO LTD** and the current file version. Setup validates this metadata during update and refuses version downgrade.

## Release trigger

The active `.github/release-trigger-<VERSION>` marker participates in publication from `main` after a version is ready. Obsolete active markers are removed while historical release notes and changelog entries remain.

For 0.1.6 the active marker is:

```text
.github/release-trigger-0.1.6
```

## Release branch policy

A feature/release branch may advance `VERSION`, manifests and documentation before merge so CI validates the exact intended next-release state. Such a branch is prepared release source, not a published release.

Normal sequence:

1. branch from verified `main`;
2. implement and document the next version;
3. run PR CI against the exact versioned source;
4. fix every failing build/audit/test/package gate;
5. inspect authentic UI captures when visual code changed;
6. merge only when the intended release source is green;
7. allow the main release workflow to publish canonical artifacts;
8. verify GitHub Release metadata and all required assets.

## 0.1.6 scope

The 0.1.6 line is centered on safe download resume integrity:

- bounded local partial-file identity sidecars;
- endpoint/security/path/`SIZE`/`MDTM` matching before REST resume;
- safe restart from zero for legacy, corrupt or stale partial state;
- post-transfer remote-revision validation;
- recursive directory downloads using the same per-file resume contract;
- isolated package-free `GhostFTP.ResumeSelfTest` coverage on Windows and Linux;
- preservation of 0.1.5 parser, memory, UI-efficiency and workstation improvements;
- preservation of 0.1.4 protocol/lifecycle hardening and 0.1.3 transfer queue semantics.

## Stable 1.0 requirements

The project must not set `RELEASE_CHANNEL=stable` for a version below 1.0.0. Stable 1.0 additionally requires the release policy's trusted Windows signing requirements and completion of the complete Windows/Linux build, test, audit, capture, packaging and public GitHub Release gates.

## Historical version references

Historical documents may contain older internal/public versions. They are preserved for traceability and must not be mass-rewritten merely to make every historical number equal to the active public version.
