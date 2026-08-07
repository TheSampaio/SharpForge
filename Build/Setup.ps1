[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$packageUrl = 'https://github.com/xinntao/Real-ESRGAN/releases/download/v0.2.5.0/realesrgan-ncnn-vulkan-20220424-windows.zip'
$projectDirectory = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\SharpForge'))
$runtimeDirectory = Join-Path $projectDirectory 'Runtime\realesrgan'
$temporaryDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("SharpForge-{0}" -f [Guid]::NewGuid())
$archivePath = Join-Path $temporaryDirectory 'realesrgan.zip'
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
}
finally {
    if (Test-Path -LiteralPath $temporaryDirectory) {
        Remove-Item -LiteralPath $temporaryDirectory -Recurse -Force
    }
}
