# Ghost FTP Release Policy

This document defines the minimum release discipline for official Ghost FTP Windows builds.

Ghost FTP is developed and published by **BRENDIGO LTD**. Product identity remains **Ghost FTP / GhostFTP**.

## Versioning

Ghost FTP uses semantic-style `MAJOR.MINOR.PATCH` versions.

- **MAJOR** — incompatible product/platform changes.
- **MINOR** — meaningful capabilities, architecture or substantial UX/protocol improvements.
- **PATCH** — targeted correctness, security, stability or regression fixes.

`VERSION`, `Directory.Build.props`, assembly/file/informational metadata and both Windows manifests must remain synchronized.

## Mandatory release notes

Every official version requires:

```text
docs/releases/vMAJOR.MINOR.PATCH.md
```

Release notes must describe meaningful work rather than generic commit summaries. Relevant sections should cover user-visible features, UI/UX, FTP/FTPS behavior, transfer/connection resilience, security, Setup, localization, privacy/dependency impact, platform scope, known limitations, validation and required assets.

`CHANGELOG.md` is cumulative history. The per-version file is the authoritative detailed GitHub Release body.

## Historical releases

Historical documentation may be expanded for clarity but must not silently replace old binaries, move old tags or imply an older binary contains later fixes.

## Required Windows release assets

Every release must contain non-empty:

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

Canonical names remain `setup.exe`, `portable.exe`, `setup-arm64.exe` and `portable-arm64.exe`; architecture-explicit copies remain available for clarity/automation.

## Required repository UI assets

Starting with 1.7.0, repository presentation must contain authentic captures generated from production WPF code:

```text
assets/readme/ghostftp-client.png
assets/readme/ghostftp-site-manager.png
```

These images must be created by the real application `--capture-ui` path. AI-generated mockups, hand-drawn replacements or unrelated third-party screenshots must not be committed under these canonical filenames.

The dedicated screenshot workflow rebuilds the client and refreshes those images from the actual MainWindow and Site Manager. Normal CI independently regenerates the same views into an artifact and verifies that capture works from the exact candidate commit.

## Required validation gates

No official release should be published until the exact source commit passes:

1. repository checkout;
2. .NET SDK setup;
3. restore;
4. warning-as-error Release build;
5. dependency/version/privacy/product/publisher/platform audit;
6. Core security and correctness tests;
7. bounded parallel queue/session-isolation tests;
8. Windows/WPF editable-input tests;
9. application localization coverage;
10. Setup localization and live language-switch tests;
11. authentic production MainWindow + Site Manager capture;
12. product/publisher identity checks;
13. Windows x64/ARM64 self-contained packaging;
14. required executable verification;
15. SHA-256 manifest generation;
16. verified artifact upload.

The official Release workflow repeats required validation instead of trusting an unrelated prior build.

## Professional-workspace gate

The production FTP workspace must retain clear operational structure:

- discoverable File/View/Sites/Transfers/Tools/Help navigation;
- Saved Sites / Site Manager access;
- labeled Quick Connect fields;
- Local and Remote file tables;
- visible Transfers queue;
- focus-safe destructive actions;
- resizable pane boundaries with usable minimums;
- a bounded local Connection Log that never records passwords or secret blobs.

UI restructuring must not reduce file-transfer correctness, security or keyboard safety merely for visual similarity to another client.

## Connection-resilience gate

Automatic connection maintenance remains inside the explicit FTP/FTPS trust boundary.

Current keepalive policy:

- standard FTP `NOOP` only against the selected server session;
- user-configurable and disableable;
- no Ghost FTP, BRENDIGO LTD, GitHub, analytics or unrelated endpoint;
- no silent reconnect with saved credentials after health-check failure;
- confirmed failures invalidate stale control-channel state.

`PRIVACY.md`, `SECURITY.md` and current release notes must describe intentional changes before release.

## Transfer-concurrency gate

The queue remains bounded and testable.

- Concurrent workers have an explicit upper bound.
- Real transfer jobs do not share the browser control session.
- Cancelling/failing one transfer does not terminate unrelated workers.
- Queue saturation becomes visible application state.
- Progress/speed/ETA remain local UI state and are not product analytics.

`GhostFTP.QueueSelfTest` is mandatory.

## Dependency policy

Shipping projects currently require zero third-party NuGet `PackageReference` entries. Adding one requires an explicit product-policy decision and documentation/security review.

GitHub Actions and Microsoft .NET build infrastructure are build-time services, not runtime product dependencies.

## Privacy policy for releases

A release must not introduce:

- application telemetry;
- user tracking;
- analytics SDKs;
- advertising SDKs;
- background crash-report upload;
- hidden product network requests;
- automatic background update checks;
- cloud profile synchronization.

FTP/FTPS traffic to a user-selected server, including documented optional keepalive, is protocol traffic rather than Ghost FTP telemetry. Any new automatic network destination requires privacy review and documentation before release.

Documentation capture uses built-in Demo mode and must not create an FTP network connection or external image-generation request.

## Product and publisher metadata

Release metadata distinguishes:

- Product: **Ghost FTP / GhostFTP**.
- Developer / publisher / licensor: **BRENDIGO LTD**.
- Company number: **16545639**.
- Product website: **https://ghostftp.com**.
- Publisher website: **https://brendigo.com**.

Legacy product identities are rejected by source audit.

## Platform policy

Production GUI target is Windows x64/ARM64 using WPF. `GhostFTP.Core` remains platform-neutral `net10.0` for future renderer work.

Android/iOS shipping targets remain outside scope. A Linux GUI must not be claimed until an actual renderer passes the parity/security/privacy/localization gates in `docs/PLATFORM-SUPPORT.md`.

## Installer requirements

Official Setup must:

- use the shared Ghost FTP visual system;
- display the embedded repository license;
- require explicit license acceptance;
- provide language selection;
- persist selected client language locally;
- validate embedded application payload;
- install per-user by default without UAC/admin rights;
- register Ghost FTP in Windows Installed Apps;
- use installed `GhostFTP-Setup.exe --uninstall` for uninstall;
- not generate a separate uninstaller executable;
- preserve local user data unless removal is explicitly selected.

## Release publication

The Release workflow derives version from `VERSION`, requires `docs/releases/v<version>.md` and uses that file as the GitHub Release body.

If a release already exists, verified assets may be refreshed and notes synchronized without silently retargeting the existing tag to unrelated history.

## Checksums

`SHA256SUMS.txt` is created from final executable assets and published with the release.

## Failed gate policy

A failed build, audit, self-test, UI capture, smoke test, packaging check or required-asset check means the release is not ready. Do not label a release stable while a required gate is red.
