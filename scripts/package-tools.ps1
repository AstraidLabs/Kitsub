$ErrorActionPreference = "Stop"

$Root = (Resolve-Path (Join-Path $PSScriptRoot ".."))
$VendorDir = Join-Path $Root "vendor"
$ToolsDir = Join-Path $Root "src/Kitsub.Cli\tools"
$ArchivesDir = Join-Path $Root "src/Kitsub.Cli\tools-archives"

$rids = @("win-x64", "linux-x64", "linux-arm64")

New-Item -ItemType Directory -Force -Path $ToolsDir | Out-Null
New-Item -ItemType Directory -Force -Path $ArchivesDir | Out-Null

foreach ($rid in $rids) {
    Write-Host "Packaging $rid"
    $ridToolsDir = Join-Path $ToolsDir $rid
    if (Test-Path $ridToolsDir) {
        Remove-Item -Recurse -Force $ridToolsDir
    }
    New-Item -ItemType Directory -Force -Path $ridToolsDir | Out-Null

    Copy-Item -Recurse -Force (Join-Path $VendorDir "$rid/ffmpeg") (Join-Path $ridToolsDir "ffmpeg")
    Copy-Item -Recurse -Force (Join-Path $VendorDir "$rid/mkvtoolnix") (Join-Path $ridToolsDir "mkvtoolnix")

    $archivePath = Join-Path $ArchivesDir "$rid.zip"
    if (Test-Path $archivePath) {
        Remove-Item -Force $archivePath
    }
    Compress-Archive -Path (Join-Path $ridToolsDir "*") -DestinationPath $archivePath
    Write-Host "Created archive $archivePath"
}

Write-Host "Tools staged under $ToolsDir"
