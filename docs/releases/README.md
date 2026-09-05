# Ghost FTP release-note index

The active public Ghost FTP version line begins at **0.1.0 Beta**.

## Current public line

- [`v0.1.0.md`](v0.1.0.md) — current Ghost FTP 0.1.0 Beta release notes.
- Future public Beta notes continue through the `0.x.y` line.
- The first fully stable public release is reserved for **1.0.0**.

See [`../VERSIONING.md`](../VERSIONING.md) for the authoritative version/channel contract.

## Preserved internal development history

The files below were created during the earlier internal 1.x development numbering and are intentionally preserved:

- `v1.1.0.md`
- `v1.2.0.md`
- `v1.3.0.md`
- `v1.3.1.md`
- `v1.4.0.md`
- `v1.4.1.md`
- `v1.4.2.md`
- `v1.5.0.md`
- `v1.6.0.md`
- `v1.7.0.md`

These files remain historical snapshots of engineering work that is still present in the codebase unless a later change explicitly supersedes it. They are **not** the active public version sequence after the numbering reset.

They must not be deleted merely to make the new public numbering appear cleaner. Keeping them preserves traceability for the FTP/FTPS engine, Setup, security/privacy hardening, localization, transfer reliability, professional workstation UI, Site Manager, Connection Log and authentic WPF screenshot work completed before the public 0.1.0 Beta line was established.

## Release naming

Public Beta releases use tags such as:

```text
v0.1.0-beta
v0.2.0-beta
```

The first stable release uses:

```text
v1.0.0
```

Canonical download filenames remain `portable.exe`, `setup.exe` and their ARM64/architecture-explicit variants. Their internal file version follows the active numeric release version.
