$ErrorActionPreference = "Stop"

$Root = (Resolve-Path (Join-Path $PSScriptRoot ".."))
$VendorDir = Join-Path $Root "vendor"
$ManifestPath = Join-Path $Root "src/Kitsub.Cli\ToolsManifest.json"

$FfmpegVersion = "7.1"
$MkvToolNixVersion = "87.0"
$ToolsetVersion = "ffmpeg-$FfmpegVersion-mkvtoolnix-$MkvToolNixVersion"

$FfmpegWinUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
$FfmpegLinuxX64Url = "https://johnvansickle.com/ffmpeg/releases/ffmpeg-release-amd64-static.tar.xz"
$FfmpegLinuxArm64Url = "https://johnvansickle.com/ffmpeg/releases/ffmpeg-release-arm64-static.tar.xz"

$MkvWinUrl = "https://mkvtoolnix.download/windows/releases/$MkvToolNixVersion/mkvtoolnix-64-bit-$MkvToolNixVersion.zip"
$MkvLinuxX64Url = "https://mkvtoolnix.download/linux/releases/$MkvToolNixVersion/mkvtoolnix-$MkvToolNixVersion-x86_64.AppImage"
$MkvLinuxArm64Url = "https://mkvtoolnix.download/linux/releases/$MkvToolNixVersion/mkvtoolnix-$MkvToolNixVersion-aarch64.AppImage"

New-Item -ItemType Directory -Force -Path $VendorDir | Out-Null

function Download-File {
    param([string]$Url, [string]$Destination)
    Write-Host "Downloading $Url"
    Invoke-WebRequest -Uri $Url -OutFile $Destination
}

function Extract-Zip {
    param([string]$Archive, [string]$Destination)
    Expand-Archive -Path $Archive -DestinationPath $Destination -Force
}

function Extract-TarXz {
    param([string]$Archive, [string]$Destination)
    tar -xf $Archive -C $Destination
}

function Extract-AppImage {
    param([string]$Archive, [string]$Destination)

    if ($IsWindows) {
        if (-not (Get-Command wsl -ErrorAction SilentlyContinue)) {
            throw "AppImage extraction requires WSL on Windows."
        }
        $wslPath = wsl wslpath -a $Archive
        wsl bash -lc "chmod +x $wslPath && $wslPath --appimage-extract" | Out-Null
        $squashRoot = Join-Path (Get-Location) "squashfs-root"
    }
    else {
        chmod +x $Archive
        & $Archive --appimage-extract | Out-Null
        $squashRoot = Join-Path (Get-Location) "squashfs-root"
    }

    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    Copy-Item (Join-Path $squashRoot "usr/bin/mkvmerge") (Join-Path $Destination "mkvmerge") -Force
    Copy-Item (Join-Path $squashRoot "usr/bin/mkvpropedit") (Join-Path $Destination "mkvpropedit") -Force
    Remove-Item -Recurse -Force $squashRoot
}

function Process-Rid {
    param(
        [string]$Rid,
        [string]$FfmpegUrl,
        [string]$MkvUrl
    )

    $ridDir = Join-Path $VendorDir $Rid
    New-Item -ItemType Directory -Force -Path $ridDir | Out-Null

    $ffmpegArchive = Join-Path $ridDir "ffmpeg.tar"
    Download-File -Url $FfmpegUrl -Destination $ffmpegArchive
    $ffmpegExtract = Join-Path $ridDir "ffmpeg.extract"
    New-Item -ItemType Directory -Force -Path $ffmpegExtract | Out-Null
    if ($Rid -eq "win-x64") {
        Extract-Zip -Archive $ffmpegArchive -Destination $ffmpegExtract
        $ffmpegExe = Get-ChildItem -Path $ffmpegExtract -Recurse -Filter ffmpeg.exe | Select-Object -First 1
        $ffprobeExe = Get-ChildItem -Path $ffmpegExtract -Recurse -Filter ffprobe.exe | Select-Object -First 1
        $dest = Join-Path $ridDir "ffmpeg"
        New-Item -ItemType Directory -Force -Path $dest | Out-Null
        Copy-Item $ffmpegExe.FullName (Join-Path $dest "ffmpeg.exe") -Force
        Copy-Item $ffprobeExe.FullName (Join-Path $dest "ffprobe.exe") -Force
    }
    else {
        Extract-TarXz -Archive $ffmpegArchive -Destination $ffmpegExtract
        $ffmpegExe = Get-ChildItem -Path $ffmpegExtract -Recurse -Filter ffmpeg | Select-Object -First 1
        $ffprobeExe = Get-ChildItem -Path $ffmpegExtract -Recurse -Filter ffprobe | Select-Object -First 1
        $dest = Join-Path $ridDir "ffmpeg"
        New-Item -ItemType Directory -Force -Path $dest | Out-Null
        Copy-Item $ffmpegExe.FullName (Join-Path $dest "ffmpeg") -Force
        Copy-Item $ffprobeExe.FullName (Join-Path $dest "ffprobe") -Force
    }
    Remove-Item -Recurse -Force $ffmpegExtract
    Remove-Item -Force $ffmpegArchive

    $mkvArchive = Join-Path $ridDir "mkvtoolnix.bin"
    Download-File -Url $MkvUrl -Destination $mkvArchive
    $mkvDest = Join-Path $ridDir "mkvtoolnix"
    if ($Rid -eq "win-x64") {
        $mkvExtract = Join-Path $ridDir "mkvtoolnix.extract"
        New-Item -ItemType Directory -Force -Path $mkvExtract | Out-Null
        Extract-Zip -Archive $mkvArchive -Destination $mkvExtract
        $mkvmergeExe = Get-ChildItem -Path $mkvExtract -Recurse -Filter mkvmerge.exe | Select-Object -First 1
        $mkvpropeditExe = Get-ChildItem -Path $mkvExtract -Recurse -Filter mkvpropedit.exe | Select-Object -First 1
        New-Item -ItemType Directory -Force -Path $mkvDest | Out-Null
        Copy-Item $mkvmergeExe.FullName (Join-Path $mkvDest "mkvmerge.exe") -Force
        Copy-Item $mkvpropeditExe.FullName (Join-Path $mkvDest "mkvpropedit.exe") -Force
        Remove-Item -Recurse -Force $mkvExtract
    }
    else {
        Extract-AppImage -Archive $mkvArchive -Destination $mkvDest
    }
    Remove-Item -Force $mkvArchive
}

Process-Rid -Rid "win-x64" -FfmpegUrl $FfmpegWinUrl -MkvUrl $MkvWinUrl
Process-Rid -Rid "linux-x64" -FfmpegUrl $FfmpegLinuxX64Url -MkvUrl $MkvLinuxX64Url
Process-Rid -Rid "linux-arm64" -FfmpegUrl $FfmpegLinuxArm64Url -MkvUrl $MkvLinuxArm64Url

python - <<PY
import hashlib
import json
from pathlib import Path

root = Path(r"$Root")
manifest_path = Path(r"$ManifestPath")
vendor = Path(r"$VendorDir")

def sha256(path: Path) -> str:
    hasher = hashlib.sha256()
    with path.open('rb') as fh:
        for chunk in iter(lambda: fh.read(1024 * 1024), b''):
            hasher.update(chunk)
    return hasher.hexdigest()

manifest = {
    "toolsetVersion": "$ToolsetVersion",
    "rids": {}
}

rids = ["win-x64", "linux-x64", "linux-arm64"]
for rid in rids:
    ffmpeg = vendor / rid / "ffmpeg"
    mkvtoolnix = vendor / rid / "mkvtoolnix"
    if rid.startswith("win"):
        ffmpeg_name = "ffmpeg.exe"
        ffprobe_name = "ffprobe.exe"
        mkvmerge_name = "mkvmerge.exe"
        mkvpropedit_name = "mkvpropedit.exe"
    else:
        ffmpeg_name = "ffmpeg"
        ffprobe_name = "ffprobe"
        mkvmerge_name = "mkvmerge"
        mkvpropedit_name = "mkvpropedit"

    manifest["rids"][rid] = {
        "ffmpeg": {
            "path": f"ffmpeg/{ffmpeg_name}",
            "sha256": sha256(ffmpeg / ffmpeg_name)
        },
        "ffprobe": {
            "path": f"ffmpeg/{ffprobe_name}",
            "sha256": sha256(ffmpeg / ffprobe_name)
        },
        "mkvmerge": {
            "path": f"mkvtoolnix/{mkvmerge_name}",
            "sha256": sha256(mkvtoolnix / mkvmerge_name)
        },
        "mkvpropedit": {
            "path": f"mkvtoolnix/{mkvpropedit_name}",
            "sha256": sha256(mkvtoolnix / mkvpropedit_name)
        }
    }

manifest_path.write_text(json.dumps(manifest, indent=2))
print(f"Wrote manifest to {manifest_path}")
PY

Write-Host "Tools staged under $VendorDir"
