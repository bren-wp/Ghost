# GhostFTP Security

## Secure defaults

GhostFTP prioritizes FTPS with valid server certificates. Explicit FTPS is the default for new server profiles.

- TLS 1.2 and TLS 1.3 only for FTPS.
- Standard Windows/.NET certificate-chain and hostname validation.
- Certificate revocation uses the Windows offline cache so FTPS validation does not create hidden CRL/OCSP web requests.
- No "accept any certificate" setting.
- Passive data connections only.
- PASV host values are not trusted; data channels connect to the authenticated control host to reduce FTP bounce/NAT abuse.
- FTP command arguments reject CR, LF and NUL characters to block command injection.
- Control replies have line and size limits.
- Connection, command and transfer timeouts are enforced.
- Downloads use temporary `.ghostftp.part` files and are promoted only after a successful transfer.
- Uploads use a unique temporary remote name and are renamed only after successful completion, reducing partial-file replacement risk.
- Deleting the FTP root directory is blocked.
- Remote filenames are sanitized before writing to Windows paths and are prevented from escaping the selected local directory.
- Local recursive operations protect against NTFS reparse-point/junction expansion.
- Saved passwords are optional and protected using Windows DPAPI.

## Quick Connect and saved profiles

The Quick Connect form is authoritative. A selected saved profile is reused only while its host, port, username and security mode still match the visible connection fields. Editing those fields therefore cannot silently connect using a different Demo/saved-profile mode.

Plain FTP always requires an explicit warning confirmation before a connection is opened.

## Local file visibility

The **Show hidden and system items** preference only controls which local entries are shown in the file pane. It does not change filesystem permissions or bypass Windows access control. Inaccessible items remain subject to normal Windows permissions.

## UI and installer boundary

`GhostFTP.Design` contains presentation resources and Windows 11 DWM integration only. It does not own credentials, FTP sockets or server data. The installer uses the same design project but has no access to saved FTP credentials beyond normal filesystem operations performed during install/uninstall.

## Plain FTP warning

Plain FTP provides no transport encryption. Use it only when required by a trusted server or isolated network. FTPS should be preferred whenever possible.

## Reporting security issues

Please use the project's GitHub issue tracker for non-sensitive issues. Do not post passwords, private server addresses, private keys or other credentials in public issues.
