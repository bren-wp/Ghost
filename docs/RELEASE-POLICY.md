# Ghost FTP Release Policy

This document defines the minimum release discipline for official Ghost FTP Windows builds.

Ghost FTP is developed and published by **BRENDIGO LTD**. Product identity remains **Ghost FTP / GhostFTP**.

## Versioning

Ghost FTP uses semantic-style `MAJOR.MINOR.PATCH` version numbers.

- **MAJOR** — reserved for incompatible product/platform changes.
- **MINOR** — meaningful new capabilities, architecture changes or substantial UX/protocol improvements.
- **PATCH** — targeted correctness, security, stability or regression fixes.

The repository `VERSION`, `Directory.Build.props`, assembly/file/informational version metadata and Windows manifests must remain synchronized.

## Mandatory release notes

Every official version must have a dedicated file:

```text
docs/releases/vMAJOR.MINOR.PATCH.md
```

Release notes must describe meaningful changes rather than use generic generated commit summaries. Relevant sections should include, as applicable:

- user-visible features;
- UI/UX changes;
- FTP/FTPS behavior;
- security and stability fixes;
- installer/update/uninstall changes;
- localization changes;
- privacy/dependency impact;
- known limitations;
- validation performed;
- required download assets.

`CHANGELOG.md` is the cumulative product history. The per-version file is the authoritative detailed body for that version's GitHub Release.

## Historical releases

Previously published release descriptions may be expanded to match the repository's maintained version history. Enriching historical documentation must not silently replace old release binaries, change old tags or imply that an old binary contains fixes introduced later.

Historical notes should clearly describe what belonged to that version.

## Required Windows release assets

Every release must contain all of the following non-empty files:

```text
setup.exe
portable.exe
setup-arm64.exe
portable-arm64.exe
GhostFTP-Setup-win-x64.exe
GhostFTP-Portable-win-x64.exe
GhostFTP-Setup-win-arm64.exe
GhostFTP-Portable-win-arm64.exe
SHA256SUMS.txt
```

Canonical names:

- `setup.exe` — standard Windows x64 installer.
- `portable.exe` — standard Windows x64 portable build.
- `setup-arm64.exe` — ARM64 installer.
- `portable-arm64.exe` — ARM64 portable build.

Architecture-explicit copies are kept for clarity and automation.

## Required validation gates

No official release should be published until the exact source commit has passed:

1. repository checkout;
2. .NET SDK setup;
3. restore;
4. Release build with warnings treated as errors;
5. dependency/version/privacy/product/publisher audit;
6. Core security and correctness self-tests;
7. Windows/WPF editable-input smoke tests;
8. application localization coverage tests;
9. Setup localization coverage tests;
10. x64 and ARM64 self-contained packaging;
11. required executable verification;
12. release artifact upload.

The official Release workflow repeats these gates instead of trusting a previous unrelated build.

## Dependency policy

Shipping source must not add third-party runtime `PackageReference` dependencies without an explicit product-policy decision and corresponding documentation change.

The current release policy requires zero NuGet `PackageReference` entries in shipping projects.

Build-time GitHub/Microsoft infrastructure is not an application runtime dependency.

## Privacy policy for releases

A release must not introduce:

- application telemetry;
- user tracking;
- analytics SDKs;
- advertising SDKs;
- background crash-report upload;
- hidden product network requests;
- background update checks;
- cloud profile synchronization.

Any intentional future change to network behavior would require an explicit privacy review and documentation before release.

## Product and publisher metadata

Release metadata must keep the distinction clear:

- Product: **Ghost FTP / GhostFTP**.
- Developer / publisher / licensor: **BRENDIGO LTD**.
- Company number: **16545639**.
- Website: **https://ghostftp.com**.

Legacy product identities are rejected by source audit.

## Installer requirements

An official Setup release must:

- use the shared Ghost FTP visual system;
- display the embedded repository license;
- require explicit license acceptance for installation/update;
- provide language selection;
- persist selected client language locally;
- validate its embedded Ghost FTP application payload;
- install per-user by default without UAC/admin rights;
- register Ghost FTP in Windows Installed Apps;
- use the same installed `GhostFTP-Setup.exe --uninstall` for uninstall;
- not generate a separate uninstaller executable;
- preserve local user data unless removal is explicitly selected during uninstall.

## Release publication

The Release workflow derives the version from `VERSION`, requires `docs/releases/v<version>.md`, and publishes that file as the GitHub Release body.

If a release already exists for the tag, verified assets may be refreshed and the release body may be synchronized with the maintained version-specific notes. Existing tags must not be silently retargeted to unrelated source history.

## Checksums

`SHA256SUMS.txt` must be created from the final packaged executable assets and published with the release. Users and downstream automation can use it to verify downloaded binaries.

## Failed gate policy

A failed build, audit, self-test, smoke test, packaging check or required-asset check means the release is not ready. Do not label a release stable while a required gate is red.
