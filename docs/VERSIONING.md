# Ghost FTP versioning policy

Ghost FTP now uses a clean public pre-1.0 version line. The current public development version is **0.1.0 Beta**.

This numbering reset does **not** remove, revert or discard any previously implemented application, protocol, UI, setup, security, privacy, localization, testing or release-pipeline work. The earlier 1.x development documents remain in the repository as historical engineering snapshots. They are preserved for traceability, but they no longer define the public release number.

## Current version sources

Two root files define the release identity:

- `VERSION` contains the numeric `MAJOR.MINOR.PATCH` value, currently `0.1.0`.
- `RELEASE_CHANNEL` contains `beta` or `stable`, currently `beta`.

For a Beta build, .NET metadata uses:

```text
Version:              0.1.0
AssemblyVersion:      0.1.0.0
FileVersion:          0.1.0.0
InformationalVersion: 0.1.0-beta
```

The Windows application and Setup manifests use the matching four-part numeric assembly version.

## Pre-1.0 development line

Until Ghost FTP is considered complete and stable enough for its first production release, public development versions remain below `1.0.0` and use the **Beta** channel.

Normal progression can therefore use versions such as:

```text
0.1.0 Beta
0.2.0 Beta
0.3.0 Beta
...
0.9.0 Beta
0.9.1 Beta
0.9.2 Beta
```

Minor versions are appropriate for meaningful new capabilities or substantial UX/protocol work. Patch versions are appropriate for focused fixes, hardening and release-candidate cleanup within the same milestone.

There is no requirement to consume every possible number. The next version is chosen according to the scope of the completed work.

## First stable release

The first release that is presented as fully complete and stable must be **Ghost FTP 1.0.0**.

At that point:

- `VERSION` becomes `1.0.0`;
- `RELEASE_CHANNEL` becomes `stable`;
- assembly and file versions become `1.0.0.0`;
- informational version becomes `1.0.0` without the Beta suffix;
- the canonical `portable.exe` and `setup.exe` packages represent the **1.0.0 stable** product;
- GitHub Release is published as a normal stable release rather than a prerelease.

Until that stable gate is reached, `portable.exe` and `setup.exe` are Beta packages for the current `0.x.y` version even though their canonical filenames stay unchanged.

## Canonical package filenames

The download filenames are deliberately stable and do not encode the version number:

```text
portable.exe
setup.exe
portable-arm64.exe
setup-arm64.exe
GhostFTP-Portable-win-x64.exe
GhostFTP-Setup-win-x64.exe
GhostFTP-Portable-win-arm64.exe
GhostFTP-Setup-win-arm64.exe
SHA256SUMS.txt
```

Their internal version metadata comes from the current release version. Keeping stable filenames allows the website and automation to point to predictable download URLs while the internal product version advances.

## Git tags and GitHub Releases

Beta releases use a tag that makes the prerelease state explicit:

```text
v0.1.0-beta
```

Stable releases use the normal version tag:

```text
v1.0.0
```

The release workflow must mark Beta releases as GitHub prereleases. A stable `1.0.0` release is allowed only after the complete release validation suite passes for the exact source commit.

## Historical 1.x development records

The repository already contains detailed `v1.1.0` through `v1.7.0` development-era notes and changelog entries created before this numbering reset. Those files are intentionally retained.

They should be interpreted as **historical internal development milestones**, not as the current public version sequence. The features and fixes described there remain part of the codebase unless a later change explicitly supersedes them.

No historical file should be deleted merely to make the numbering look clean. New public release documentation starts from `docs/releases/v0.1.0.md` and continues forward from the new Beta line.

## Release gates

A version number alone does not make a build stable. Every Beta and stable package must continue to pass the repository's build, source audit, security/correctness tests, transfer queue tests, WPF UI smoke tests, authentic UI capture checks and packaging checks required by `docs/RELEASE-POLICY.md`.

The **stable** label is reserved for a build that satisfies the 1.0.0 readiness criteria and is intended for production use without the Beta qualification.
