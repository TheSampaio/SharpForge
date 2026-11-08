[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$packageUrl = 'https://github.com/xinntao/Real-ESRGAN/releases/download/v0.2.5.0/realesrgan-ncnn-vulkan-20220424-windows.zip'
$converterPackageUrl = 'https://github.com/n00mkrad/cupscale/releases/download/1.39/Cupscale.1.39.0f1.zip'
$texconvUrl = 'https://github.com/microsoft/DirectXTex/releases/download/may2026/texconv.exe'
$texconvSha256 = 'dcfdec10244e02cf5037fba089c55fb7e1326b1c8181742d77d15fa5cb5eef06'
$converterArchivePrefix = 'Cupscale 1.39.0f1/CupscaleData/bin/pth2ncnn/'
$projectDirectory = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\SharpForge'))
$runtimeDirectory = Join-Path $projectDirectory 'Runtime\realesrgan'
$converterDirectory = Join-Path $runtimeDirectory 'tools\pth2ncnn'
$texconvDirectory = Join-Path $projectDirectory 'Runtime\texconv'
$temporaryDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("SharpForge-{0}" -f [Guid]::NewGuid())
$archivePath = Join-Path $temporaryDirectory 'realesrgan.zip'
$converterArchivePath = Join-Path $temporaryDirectory 'cupscale.zip'
$texconvDownloadPath = Join-Path $temporaryDirectory 'texconv.exe'
$extractionDirectory = Join-Path $temporaryDirectory 'extracted'

try {
    New-Item -ItemType Directory -Path $temporaryDirectory, $extractionDirectory, $runtimeDirectory -Force | Out-Null

    Write-Host 'Downloading the pinned Real-ESRGAN runtime...'
    Invoke-WebRequest -Uri $packageUrl -OutFile $archivePath

    Write-Host 'Extracting the runtime package...'
    Expand-Archive -LiteralPath $archivePath -DestinationPath $extractionDirectory -Force

    $executable = Get-ChildItem -LiteralPath $extractionDirectory -Filter 'realesrgan-ncnn-vulkan.exe' -File -Recurse |
        Select-Object -First 1

    if ($null -eq $executable) {
        throw 'The downloaded package does not contain realesrgan-ncnn-vulkan.exe.'
    }

    Copy-Item -Path (Join-Path $executable.DirectoryName '*') -Destination $runtimeDirectory -Recurse -Force
    Write-Host "Real-ESRGAN was installed at $runtimeDirectory"

    Write-Host 'Downloading the pinned PyTorch-to-NCNN model converter...'
    Invoke-WebRequest -Uri $converterPackageUrl -OutFile $converterArchivePath

    Write-Host 'Extracting the model converter...'
    New-Item -ItemType Directory -Path $converterDirectory -Force | Out-Null
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $converterArchive = [System.IO.Compression.ZipFile]::OpenRead($converterArchivePath)
    try {
        foreach ($entry in $converterArchive.Entries) {
            if (-not $entry.FullName.StartsWith($converterArchivePrefix, [System.StringComparison]::Ordinal)) {
                continue
            }

            $relativePath = $entry.FullName.Substring($converterArchivePrefix.Length)
            if ([string]::IsNullOrEmpty($relativePath)) {
                continue
            }

            $destinationPath = [System.IO.Path]::GetFullPath((Join-Path $converterDirectory $relativePath))
            $converterRoot = [System.IO.Path]::GetFullPath($converterDirectory) + [System.IO.Path]::DirectorySeparatorChar
            if (-not $destinationPath.StartsWith($converterRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
                throw "The converter archive contains an unsafe path: $relativePath"
            }

            if ($entry.FullName.EndsWith('/')) {
                New-Item -ItemType Directory -Path $destinationPath -Force | Out-Null
                continue
            }

            $destinationParent = Split-Path -Parent $destinationPath
            New-Item -ItemType Directory -Path $destinationParent -Force | Out-Null
            [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $destinationPath, $true)
        }
    }
    finally {
        $converterArchive.Dispose()
    }

    Write-Host "The model converter was installed at $converterDirectory"

    Write-Host 'Downloading the pinned DirectXTex BC7 texture compressor...'
    Invoke-WebRequest -Uri $texconvUrl -OutFile $texconvDownloadPath
    $texconvHash = (Get-FileHash -LiteralPath $texconvDownloadPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($texconvHash -ne $texconvSha256) {
        throw "The DirectXTex download failed SHA-256 validation. Actual digest: $texconvHash"
    }

    New-Item -ItemType Directory -Path $texconvDirectory -Force | Out-Null
    Copy-Item -LiteralPath $texconvDownloadPath -Destination (Join-Path $texconvDirectory 'texconv.exe') -Force
    Write-Host "DirectXTex texconv was installed at $texconvDirectory"
}
finally {
    if (Test-Path -LiteralPath $temporaryDirectory) {
        Remove-Item -LiteralPath $temporaryDirectory -Recurse -Force
    }
}
