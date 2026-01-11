$ErrorActionPreference = "Stop"

$Root = (Resolve-Path (Join-Path $PSScriptRoot ".."))
$ManifestPath = Join-Path $Root "src/Kitsub.Tooling/Tools/ToolsManifest.json"
$OutputRoot = Join-Path $Root "src/Kitsub.Cli/tools/win-x64"
$TempRoot = Join-Path $Root "vendor/tools-temp"

if (-not (Test-Path $ManifestPath)) {
    throw "Tools manifest not found at $ManifestPath"
}

if (-not (Get-Command 7z -ErrorAction SilentlyContinue)) {
    throw "7z is required to extract .7z archives. Install 7-Zip and ensure '7z' is on PATH."
}

$manifest = Get-Content -Raw -Path $ManifestPath | ConvertFrom-Json
$rid = "win-x64"
$ridEntry = $manifest.rids.$rid
if (-not $ridEntry) {
    throw "Manifest does not contain RID '$rid'"
}

function Get-Sha256FromUrl {
    param(
        [string]$Url,
        [string]$Entry
    )

    $content = Invoke-WebRequest -Uri $Url -UseBasicParsing
    if ([string]::IsNullOrWhiteSpace($Entry)) {
        $match = [regex]::Match($content.Content, "[0-9a-fA-F]{64}")
        if (-not $match.Success) {
            throw "Unable to parse SHA256 from $Url"
        }
        return $match.Value.ToLowerInvariant()
    }

    foreach ($line in $content.Content -split "`r?`n") {
        if ($line -match [regex]::Escape($Entry)) {
            $match = [regex]::Match($line, "[0-9a-fA-F]{64}")
            if ($match.Success) {
                return $match.Value.ToLowerInvariant()
            }
        }
    }

    throw "SHA256 entry '$Entry' not found in $Url"
}

function Get-FileSha256 {
    param([string]$Path)
    return (Get-FileHash -Path $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Extract-Archive {
    param(
        [string]$Archive,
        [string]$Destination
    )

    if (Test-Path $Destination) {
        Remove-Item -Recurse -Force $Destination
    }

    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    & 7z x $Archive ("-o$Destination") | Out-Null
}

function Stage-Tool {
    param(
        [string]$ToolName,
        $ToolDefinition
    )

    $toolRoot = Join-Path $OutputRoot $ToolName
    $archiveName = [System.IO.Path]::GetFileName([System.Uri]$ToolDefinition.archiveUrl)
    $archivePath = Join-Path $TempRoot $archiveName
    $extractDir = Join-Path $TempRoot ($ToolName + "-extract")

    Write-Host "Downloading $ToolName from $($ToolDefinition.archiveUrl)"
    Invoke-WebRequest -Uri $ToolDefinition.archiveUrl -OutFile $archivePath

    $expectedHash = Get-Sha256FromUrl -Url $ToolDefinition.sha256Url -Entry $ToolDefinition.sha256Entry
    $actualHash = Get-FileSha256 -Path $archivePath
    if ($expectedHash -ne $actualHash) {
        throw "$ToolName SHA256 mismatch. Expected $expectedHash, got $actualHash"
    }

    Extract-Archive -Archive $archivePath -Destination $extractDir

    foreach ($entry in $ToolDefinition.extractMap.PSObject.Properties) {
        $fileName = $entry.Name
        $archivePathMatch = $entry.Value -replace "/", "\\"
        $source = Get-ChildItem -Path $extractDir -Recurse -Filter $fileName |
            Where-Object { $_.FullName -like "*${archivePathMatch}" } |
            Select-Object -First 1

        if (-not $source) {
            throw "Failed to locate $fileName in extracted $ToolName archive"
        }

        $destination = Join-Path $toolRoot ($entry.Value -replace "/", "\\")
        $destinationDir = Split-Path -Parent $destination
        New-Item -ItemType Directory -Force -Path $destinationDir | Out-Null
        Copy-Item -Path $source.FullName -Destination $destination -Force
    }
}

if (Test-Path $TempRoot) {
    Remove-Item -Recurse -Force $TempRoot
}
New-Item -ItemType Directory -Force -Path $TempRoot | Out-Null
New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null

Stage-Tool -ToolName "ffmpeg" -ToolDefinition $ridEntry.ffmpeg
Stage-Tool -ToolName "mkvtoolnix" -ToolDefinition $ridEntry.mkvtoolnix

Remove-Item -Recurse -Force $TempRoot
Write-Host "Tools staged under $OutputRoot"
