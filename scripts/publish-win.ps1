$ErrorActionPreference = "Stop"

param(
    [switch]$SelfContained
)

$Root = (Resolve-Path (Join-Path $PSScriptRoot ".."))
$Project = Join-Path $Root "src/Kitsub.Cli/Kitsub.csproj"

$arguments = @("publish", $Project, "-c", "Release", "-r", "win-x64")
if ($SelfContained) {
    $arguments += "--self-contained"
    $arguments += "true"
}

Write-Host "Running dotnet $($arguments -join ' ')"
& dotnet @arguments
