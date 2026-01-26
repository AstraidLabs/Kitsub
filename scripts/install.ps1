[CmdletBinding()]
param(
    [string]$Repo = "AstraidLabs/Kitsub",
    [string]$InstallDir = "$(Join-Path $env:LOCALAPPDATA 'Kitsub\bin')",
    [string]$Rid = "win-x64",
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
    if (-not $Host.UI -or -not $Host.UI.RawUI) { return $false }
    if ([Console]::IsInputRedirected) { return $false }
    return $true
}

function Get-RelativePath {
    param(
        [Parameter(Mandatory=$true)][string]$BasePath,
        [Parameter(Mandatory=$true)][string]$TargetPath
    )
    # PS 7+ (.NET) umí GetRelativePath; pro PS 5.1 fallback:
    try {
        return [IO.Path]::GetRelativePath($BasePath, $TargetPath)
    } catch {
        $base = (Resolve-Path $BasePath).Path.TrimEnd('\') + '\'
        $target = (Resolve-Path $TargetPath).Path
        if ($target.StartsWith($base, [StringComparison]::OrdinalIgnoreCase)) {
            return $target.Substring($base.Length)
        }
        return $TargetPath
    }
}

try {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 -bor [Net.ServicePointManager]::SecurityProtocol
} catch {
    Write-Status "Unable to set TLS 1.2 explicitly; continuing."
}

if (-not $Repo.Contains('/')) {
    throw "Repo must be in the form 'owner/name'."
}

$headers = @{
    'User-Agent' = 'kitsub-installer'
    'Accept'     = 'application/vnd.github+json'
}

$apiBase = "https://api.github.com/repos/$Repo/releases"

$interactive = Test-Interactive
$tempDir = $null

try {
    Write-Status "Fetching release metadata for $Repo..."

    if ($IncludePrerelease) {
        $releases = Invoke-RestMethod -Uri $apiBase -Headers $headers

        $release = $releases |
            Where-Object { -not $_.draft } |
            Sort-Object { [datetime]$_.published_at } -Descending |
            Select-Object -First 1

        if (-not $release) { throw "No releases found for $Repo." }
    } else {
        # Stabilní a jednoduché: /latest vrací latest non-prerelease non-draft
        $release = Invoke-RestMethod -Uri "$apiBase/latest" -Headers $headers
    }

    $tag = $release.tag_name
    if (-not $tag) { throw "Release tag not found." }

    $zipName = "Kitsub-$tag-$Rid.zip"
    $shaName = "$zipName.sha256"

    $zipAsset = $release.assets | Where-Object { $_.name -eq $zipName } | Select-Object -First 1
    $shaAsset = $release.assets | Where-Object { $_.name -eq $shaName } | Select-Object -First 1

    if (-not $zipAsset -or -not $shaAsset) {
        $available = ($release.assets | ForEach-Object { $_.name }) -join ", "
        throw "Release assets not found. Expected '$zipName' and '$shaName'. Available: $available"
    }

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

    # Zrychlení IWR v PS 5.1 (volitelné):
    $oldProgress = $ProgressPreference
    $ProgressPreference = 'SilentlyContinue'

    try {
        Write-Status "Downloading $zipName..."
        Invoke-WebRequest -Uri $zipAsset.browser_download_url -Headers $headers -OutFile $zipPath

        Write-Status "Downloading $shaName..."
        Invoke-WebRequest -Uri $shaAsset.browser_download_url -Headers $headers -OutFile $shaPath
    } finally {
        $ProgressPreference = $oldProgress
    }

    Write-Status "Verifying SHA256 checksum..."
    $shaLine = (Get-Content -Path $shaPath -TotalCount 1).Trim()

    # Podpora: "HASH  file", "HASH *file", nebo jen "HASH"
    $expectedHash = $null
    $expectedFile = $zipName

    if ($shaLine -match '^([a-fA-F0-9]{64})\s+\*?(.+)$') {
        $expectedHash = $Matches[1].ToLowerInvariant()
        $expectedFile = $Matches[2].Trim()
        if ($expectedFile -ne $zipName) {
            throw "Checksum filename mismatch. Expected '$zipName', got '$expectedFile'."
        }
    } elseif ($shaLine -match '^([a-fA-F0-9]{64})$') {
        $expectedHash = $Matches[1].ToLowerInvariant()
    } else {
        throw "Unexpected checksum format in $shaName."
    }

    $actualHash = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualHash -ne $expectedHash) {
        throw "Checksum verification failed."
    }

    Write-Status "Extracting to $InstallDir..."
    New-Item -ItemType Directory -Path $InstallDir | Out-Null
    Expand-Archive -Path $zipPath -DestinationPath $InstallDir -Force

    # Najdi kitsub.exe kdekoliv v rozbaleném obsahu
    $exeItem = Get-ChildItem -Path $InstallDir -Filter 'kitsub.exe' -Recurse | Select-Object -First 1
    if (-not $exeItem) {
        throw "kitsub.exe not found after extraction."
    }

    $exePath = $exeItem.FullName
    $relExe  = (Get-RelativePath -BasePath $InstallDir -TargetPath $exePath).Replace('/', '\')
    if ($relExe.StartsWith('.\')) { $relExe = $relExe.Substring(2) }

    # Wrapper vždy míří na skutečné umístění exe (i v podsložce)
    $cmdPath = Join-Path $InstallDir 'kitsub.cmd'
    $cmdContent = "@echo off`r`n`"%~dp0$relExe`" %*`r`n"
    Set-Content -Path $cmdPath -Value $cmdContent -Encoding ASCII

    if (-not $NoPath) {
        $currentUserPath = [Environment]::GetEnvironmentVariable('Path', 'User')
        if (-not $currentUserPath) { $currentUserPath = '' }

        $pathParts = $currentUserPath -split ';' | Where-Object { $_ -ne '' }
        $alreadyInPath = $pathParts | Where-Object { $_.TrimEnd('\') -ieq $InstallDir.TrimEnd('\') }

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
}
finally {
    if ($tempDir -and (Test-Path $tempDir)) {
        Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
