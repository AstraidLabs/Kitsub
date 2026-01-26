[CmdletBinding()]
param(
    [string]$Repo = "AstraidLabs/Kitsub",
    [string]$InstallDir = "$(Join-Path $env:LOCALAPPDATA 'Kitsub\\bin')",
    [switch]$Force,
    [switch]$NoPath,
    [switch]$IncludePrerelease
)

$ErrorActionPreference = 'Stop'

function Write-Status {
    param([string]$Message)
    Write-Host "[kitsub] $Message"
}

function Test-Interactive {
    if (-not $Host.UI -or -not $Host.UI.RawUI) {
        return $false
    }

    if ([Console]::IsInputRedirected) {
        return $false
    }

    return $true
}

try {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 -bor [Net.ServicePointManager]::SecurityProtocol
} catch {
    Write-Status "Unable to set TLS 1.2 explicitly; continuing."
}

if (-not $Repo.Contains('/')) {
    throw "Repo must be in the form 'owner/name'."
}

$apiBase = "https://api.github.com/repos/$Repo/releases"
$headers = @{ 'User-Agent' = 'kitsub-installer' }

Write-Status "Fetching release metadata for $Repo..."
if ($IncludePrerelease) {
    $releases = Invoke-RestMethod -Uri $apiBase -Headers $headers
    $release = $releases | Where-Object { -not $_.draft } | Select-Object -First 1
    if (-not $release) {
        throw "No releases found for $Repo."
    }
} else {
    $release = Invoke-RestMethod -Uri "$apiBase/latest" -Headers $headers
}

$tag = $release.tag_name
if (-not $tag) {
    throw "Release tag not found."
}

$zipName = "Kitsub-$tag-win-x64.zip"
$shaName = "$zipName.sha256"

$zipAsset = $release.assets | Where-Object { $_.name -eq $zipName } | Select-Object -First 1
$shaAsset = $release.assets | Where-Object { $_.name -eq $shaName } | Select-Object -First 1

if (-not $zipAsset -or -not $shaAsset) {
    throw "Release assets not found. Expected $zipName and $shaName."
}

$interactive = Test-Interactive
if (Test-Path $InstallDir) {
    if (-not $Force) {
        if ($interactive) {
            $response = Read-Host "Install directory exists at '$InstallDir'. Overwrite? (y/N)"
            if ($response -notmatch '^(y|yes)$') {
                Write-Status "Aborting install."
                exit 1
            }
        } else {
            throw "Install directory exists. Re-run with -Force to overwrite."
        }
    }

    Write-Status "Removing existing install at $InstallDir..."
    Remove-Item -Path $InstallDir -Recurse -Force
}

$tempDir = Join-Path ([IO.Path]::GetTempPath()) ("kitsub-" + [guid]::NewGuid().ToString('n'))
New-Item -ItemType Directory -Path $tempDir | Out-Null

$zipPath = Join-Path $tempDir $zipName
$shaPath = Join-Path $tempDir $shaName

Write-Status "Downloading $zipName..."
Invoke-WebRequest -Uri $zipAsset.browser_download_url -Headers $headers -OutFile $zipPath

Write-Status "Downloading $shaName..."
Invoke-WebRequest -Uri $shaAsset.browser_download_url -Headers $headers -OutFile $shaPath

Write-Status "Verifying SHA256 checksum..."
$shaLine = Get-Content -Path $shaPath -TotalCount 1
if ($shaLine -notmatch '^([a-fA-F0-9]{64})\s+(.+)$') {
    throw "Unexpected checksum format in $shaName."
}

$expectedHash = $Matches[1].ToLowerInvariant()
$expectedFile = $Matches[2]
if ($expectedFile -ne $zipName) {
    throw "Checksum filename mismatch. Expected $zipName, got $expectedFile."
}

$actualHash = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actualHash -ne $expectedHash) {
    throw "Checksum verification failed."
}

Write-Status "Extracting to $InstallDir..."
New-Item -ItemType Directory -Path $InstallDir | Out-Null
Expand-Archive -Path $zipPath -DestinationPath $InstallDir -Force

$exePath = Join-Path $InstallDir 'kitsub.exe'
if (-not (Test-Path $exePath)) {
    $exePath = Get-ChildItem -Path $InstallDir -Filter 'kitsub.exe' -Recurse | Select-Object -First 1 | ForEach-Object { $_.FullName }
}

if (-not $exePath -or -not (Test-Path $exePath)) {
    throw "kitsub.exe not found after extraction."
}

$cmdPath = Join-Path $InstallDir 'kitsub.cmd'
$cmdContent = "@echo off`r`n\"%~dp0kitsub.exe\" %*`r`n"
Set-Content -Path $cmdPath -Value $cmdContent -Encoding ASCII

if (-not $NoPath) {
    $currentUserPath = [Environment]::GetEnvironmentVariable('Path', 'User')
    if (-not $currentUserPath) {
        $currentUserPath = ''
    }

    $pathParts = $currentUserPath -split ';' | Where-Object { $_ -ne '' }
    $alreadyInPath = $pathParts | Where-Object { $_.TrimEnd('\\') -ieq $InstallDir.TrimEnd('\\') }

    if (-not $alreadyInPath) {
        $newUserPath = ($pathParts + $InstallDir) -join ';'
        [Environment]::SetEnvironmentVariable('Path', $newUserPath, 'User')
        Write-Status "Added $InstallDir to user PATH."
    } else {
        Write-Status "Install directory already in user PATH."
    }

    if ($env:Path -notmatch [regex]::Escape($InstallDir)) {
        $env:Path = "$InstallDir;$env:Path"
    }
}

Write-Status "Installation complete. Run 'kitsub --help' to verify."

Remove-Item -Path $tempDir -Recurse -Force
