param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\artifacts\signing'),
    [string]$Password,
    [int]$ValidYears = 2
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if (-not $IsWindows -and $PSVersionTable.PSEdition -eq 'Core') {
    throw 'Development Authenticode certificate generation must run on Windows.'
}

if ([string]::IsNullOrWhiteSpace($Password)) {
    $Password = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
    Write-Host 'Generated a random PFX password for this local development certificate:'
    Write-Host $Password
    Write-Host 'Store it securely. It is not written into the repository.'
}

$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$pfxPath = Join-Path $OutputDirectory 'GhostFTP-Development-CodeSigning.pfx'
$cerPath = Join-Path $OutputDirectory 'GhostFTP-Development-CodeSigning.cer'

$notAfter = (Get-Date).AddYears([Math]::Clamp($ValidYears, 1, 5))
$certificate = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject 'CN=Ghost FTP Development, O=BRENDIGO LTD' `
    -FriendlyName 'Ghost FTP Development Code Signing' `
    -CertStoreLocation 'Cert:\CurrentUser\My' `
    -KeyAlgorithm RSA `
    -KeyLength 3072 `
    -HashAlgorithm SHA256 `
    -KeyExportPolicy Exportable `
    -KeySpec Signature `
    -NotAfter $notAfter

try {
    $securePassword = ConvertTo-SecureString $Password -AsPlainText -Force
    Export-PfxCertificate -Cert $certificate -FilePath $pfxPath -Password $securePassword -CryptoAlgorithmOption AES256_SHA256 | Out-Null
    Export-Certificate -Cert $certificate -FilePath $cerPath -Type CERT | Out-Null

    Write-Host "Development PFX: $pfxPath"
    Write-Host "Public certificate: $cerPath"
    Write-Host "Thumbprint: $($certificate.Thumbprint)"
    Write-Host ''
    Write-Host 'IMPORTANT:'
    Write-Host '- This certificate is for local/test signing only.'
    Write-Host '- Windows will not automatically trust a self-signed development certificate.'
    Write-Host '- Do not commit the PFX or its password.'
    Write-Host '- A CA-issued code-signing certificate is required for normal publisher trust/SmartScreen reputation.'
}
finally {
    $path = "Cert:\CurrentUser\My\$($certificate.Thumbprint)"
    if (Test-Path $path) {
        Remove-Item $path -Force -ErrorAction SilentlyContinue
    }
}
