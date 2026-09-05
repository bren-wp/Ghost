param(
    [Parameter(Mandatory = $true)]
    [string]$ReleaseDirectory,

    [string]$PfxBase64 = $env:GHOSTFTP_SIGNING_PFX_BASE64,
    [string]$PfxPassword = $env:GHOSTFTP_SIGNING_PFX_PASSWORD,
    [string]$TimestampServer = $env:GHOSTFTP_TIMESTAMP_URL,
    [switch]$RequireTrustedSignature
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$ReleaseDirectory = (Resolve-Path $ReleaseDirectory).Path
$executables = Get-ChildItem $ReleaseDirectory -Filter '*.exe' -File | Sort-Object Name
if ($executables.Count -eq 0) {
    throw "No Windows executables were found in $ReleaseDirectory."
}

if ([string]::IsNullOrWhiteSpace($PfxBase64)) {
    if ($RequireTrustedSignature) {
        throw 'GHOSTFTP_SIGNING_PFX_BASE64 is required for this release.'
    }
    Write-Host 'No signing certificate secret is configured. Windows binaries remain unsigned.'
    exit 0
}

if ([string]::IsNullOrWhiteSpace($PfxPassword)) {
    throw 'GHOSTFTP_SIGNING_PFX_PASSWORD is required when a signing certificate is configured.'
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('ghostftp-sign-' + [Guid]::NewGuid().ToString('N'))
$pfxPath = Join-Path $tempRoot 'codesign.pfx'
$importedThumbprints = [System.Collections.Generic.List[string]]::new()

try {
    New-Item -ItemType Directory -Force $tempRoot | Out-Null
    [System.IO.File]::WriteAllBytes($pfxPath, [Convert]::FromBase64String($PfxBase64))

    $securePassword = ConvertTo-SecureString $PfxPassword -AsPlainText -Force
    $imported = Import-PfxCertificate -FilePath $pfxPath -CertStoreLocation 'Cert:\CurrentUser\My' -Password $securePassword -Exportable:$false
    if ($null -eq $imported) {
        throw 'The Ghost FTP code-signing certificate could not be imported.'
    }

    foreach ($certificate in @($imported)) {
        if ($certificate.Thumbprint) {
            $importedThumbprints.Add($certificate.Thumbprint)
        }
    }

    $signingCertificate = @($imported) |
        Where-Object {
            $_.HasPrivateKey -and
            $_.NotAfter -gt (Get-Date).AddDays(1) -and
            ($_.EnhancedKeyUsageList.ObjectId.Value -contains '1.3.6.1.5.5.7.3.3')
        } |
        Sort-Object NotAfter -Descending |
        Select-Object -First 1

    if ($null -eq $signingCertificate) {
        throw 'The configured PFX does not contain a usable, non-expired code-signing certificate with a private key.'
    }

    Write-Host "Signing Ghost FTP with certificate: $($signingCertificate.Subject)"
    Write-Host "Certificate thumbprint: $($signingCertificate.Thumbprint)"
    Write-Host "Certificate expires: $($signingCertificate.NotAfter.ToUniversalTime().ToString('u'))"

    foreach ($file in $executables) {
        $arguments = @{
            FilePath = $file.FullName
            Certificate = $signingCertificate
            HashAlgorithm = 'SHA256'
        }
        if (-not [string]::IsNullOrWhiteSpace($TimestampServer)) {
            $arguments.TimestampServer = $TimestampServer
        }

        $signature = Set-AuthenticodeSignature @arguments
        if ($null -eq $signature.SignerCertificate) {
            throw "Authenticode signing did not produce a signer certificate for $($file.Name)."
        }
        if ($signature.SignerCertificate.Thumbprint -ne $signingCertificate.Thumbprint) {
            throw "Unexpected signing certificate was used for $($file.Name)."
        }

        Write-Host "Signed $($file.Name) [$($signature.Status)]"
    }

    foreach ($file in $executables) {
        $signature = Get-AuthenticodeSignature $file.FullName
        if ($null -eq $signature.SignerCertificate) {
            throw "Missing Authenticode signature: $($file.Name)"
        }
        if ($signature.SignerCertificate.Thumbprint -ne $signingCertificate.Thumbprint) {
            throw "Signer mismatch after verification: $($file.Name)"
        }
        if ($RequireTrustedSignature -and $signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
            throw "Signature validation failed for $($file.Name): $($signature.Status) $($signature.StatusMessage)"
        }
    }

    Write-Host "Verified Authenticode signatures for $($executables.Count) Ghost FTP executable(s)."
}
finally {
    foreach ($thumbprint in $importedThumbprints) {
        $certPath = "Cert:\CurrentUser\My\$thumbprint"
        if (Test-Path $certPath) {
            Remove-Item $certPath -Force -ErrorAction SilentlyContinue
        }
    }

    if (Test-Path $pfxPath) {
        try {
            $bytes = [System.IO.File]::ReadAllBytes($pfxPath)
            [System.Security.Cryptography.CryptographicOperations]::ZeroMemory($bytes)
        }
        catch {
        }
    }
    Remove-Item $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
