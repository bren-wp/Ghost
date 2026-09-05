# Ghost FTP code signing

Ghost FTP supports Windows Authenticode signing without committing a private key to the repository.

## Security rule

**Never commit a PFX, PEM private key, password, token or signing secret to Git.**

The repository ignores common private-key formats and the official signing script consumes the certificate only from environment/GitHub Actions secrets. The PFX is written to a temporary directory for the duration of signing, imported into the current-user certificate store, used for signing, removed from the store and deleted from disk before the job finishes.

## Official GitHub Actions secrets

Configure these repository secrets before a publisher-signed release:

- `GHOSTFTP_SIGNING_PFX_BASE64` — Base64 representation of the complete code-signing PFX.
- `GHOSTFTP_SIGNING_PFX_PASSWORD` — password protecting that PFX.

Optional:

- `GHOSTFTP_TIMESTAMP_URL` — HTTPS RFC3161/Authenticode timestamp service URL supplied by the certificate provider.

The release workflow passes those values only to `tools/Sign-WindowsRelease.ps1`. They are not embedded in Ghost FTP, Setup, logs, documentation screenshots or release metadata.

## Creating the Base64 secret

On Windows PowerShell:

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes('C:\secure\GhostFTP-CodeSigning.pfx')) | Set-Clipboard
```

Paste that value into the `GHOSTFTP_SIGNING_PFX_BASE64` GitHub Actions secret. Store the PFX password separately as `GHOSTFTP_SIGNING_PFX_PASSWORD`.

## Signing behavior

`tools/Sign-WindowsRelease.ps1`:

1. validates that release `.exe` files exist;
2. decodes the PFX into a temporary directory;
3. imports the certificate without making the private key exportable;
4. requires a valid code-signing EKU (`1.3.6.1.5.5.7.3.3`);
5. signs every Windows release executable with SHA-256;
6. optionally timestamps the signature;
7. verifies signer identity on every executable;
8. requires Windows trust validation for stable releases;
9. regenerates `SHA256SUMS.txt` **after** signing so published hashes describe the final bytes;
10. removes the imported certificate and temporary PFX material.

## Beta versus stable

A `0.x.y` Beta build can still be produced when the publisher certificate secret has not been configured, because blocking all development artifacts would make normal CI impossible. The release workflow records that state rather than pretending the executable is publisher-signed.

The first stable `1.0.0` release is different: a trusted Authenticode signature is a release gate. Stable publication must fail if the signing secret is absent or the resulting signature does not validate as trusted on the Windows release runner.

## Local development certificate

`tools/New-DevelopmentSigningCertificate.ps1` can create a local self-signed RSA-3072 SHA-256 code-signing certificate for testing the signing mechanics.

Example:

```powershell
./tools/New-DevelopmentSigningCertificate.ps1
```

The generated PFX is placed under `artifacts/signing/`, which is ignored by Git. It must remain local/private.

A self-signed development certificate **does not solve Windows SmartScreen or Unknown Publisher for normal users**. Windows does not automatically trust that certificate. It is useful only to test Authenticode mechanics or in an environment where the public development certificate has been explicitly trusted.

## SmartScreen and publisher reputation

Authenticode signing and SmartScreen reputation are related but not identical.

For normal end-user publisher trust, use a code-signing certificate whose chain Windows trusts. The certificate's legal publisher identity should match **BRENDIGO LTD**. A self-signed private key cannot by itself establish public trust.

SmartScreen can still warn for a new/low-reputation application even when the file is correctly signed. Reputation normally improves as the same trusted publisher identity signs legitimate releases over time. An EV code-signing option may provide different reputation behavior depending on current Microsoft policy and certificate-provider requirements.

Ghost FTP documentation therefore must never claim that merely possessing any private key removes SmartScreen warnings.

## No runtime dependency

Code signing is a build/release operation. It does not add telemetry, analytics, a runtime signing service, cloud account requirement or an application dependency. End-user Ghost FTP does not contact the signing certificate provider.
