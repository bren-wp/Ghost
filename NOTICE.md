# Ghost FTP — Legal Notice

**Ghost FTP / GhostFTP** is a software product developed and published by **BRENDIGO LTD**.

BRENDIGO LTD  
Company number: **16545639**  
Registered office: **71–75 Shelton Street, Covent Garden, London, WC2H 9JQ, United Kingdom**  
Developer / publisher website: **https://brendigo.com**

Copyright © 2026 BRENDIGO LTD. All rights reserved.

Product website: https://ghostftp.com  
Source repository: https://github.com/bren-wp/Ghost

The source code in this repository is source-available/proprietary and is governed by the repository [`LICENSE`](LICENSE). Publication of source code does not grant general permission to redistribute, rebrand, sublicense or commercialize the source or modified builds.

Official unmodified Ghost FTP binaries are distributed through official Ghost FTP release channels under the terms stated in the LICENSE.

## Current public release status

The source tree is prepared for **Ghost FTP 0.1.5 Beta**.

Public 0.x versions remain Beta software. Version 1.0.0 is reserved for the first stable release.

Ghost FTP 0.1.5 Beta targets Windows and Linux desktop systems only. Android, iOS, MacCatalyst/macOS application targets and a Web/browser application are not part of the shipping repository scope.

## Product privacy statement

Ghost FTP is designed without application telemetry, advertising SDKs, usage analytics, hidden crash upload, fingerprinting, cloud profile synchronization or a required Ghost FTP account.

Saved profiles/settings remain local. Saved-password protection is opt-in and platform-local. User-selected FTP/FTPS servers are third-party endpoints and are not operated by BRENDIGO LTD unless explicitly identified otherwise.

0.1.5's pooled transfer buffers and expanded layout/settings state remain process/device-local. Transfer buffers are cleared before pool reuse because they may contain user file data.

## Product security statement

Ghost FTP supports FTP, Explicit FTPS and Implicit FTPS. SFTP/SSH is not represented as an FTP mode. FTPS certificate/hostname validation is not silently bypassed, and the product does not intentionally downgrade a failed FTPS connection to plain FTP.

0.1.5 strengthens server-controlled LIST/MLSD parser bounds, retains strict FTP reply/EPSV/PASV validation and coordinated session/transfer-queue shutdown, and expands deterministic parser/passive-mode/settings regression coverage. These changes do not introduce an external service dependency.

## Release artifacts

Official release artifacts may include Windows Setup/Portable x64/ARM64 packages and Linux x64/ARM64 packages. Canonical Windows names include `setup.exe` and `portable.exe`; the Windows Setup executable is also used for installed maintenance/uninstall rather than generating a separate uninstaller executable.

Release artifacts are produced through repository CI/release workflows with version, test, protocol/parser hardening, audit, package and checksum/runtime gates.

## Documentation precedence

For operational details see:

- [`LICENSE`](LICENSE)
- [`SECURITY.md`](SECURITY.md)
- [`PRIVACY.md`](PRIVACY.md)
- [`docs/releases/v0.1.5.md`](docs/releases/v0.1.5.md)
- [`docs/RELEASE-POLICY.md`](docs/RELEASE-POLICY.md)

Nothing in README or marketing-style product copy overrides the repository LICENSE.
