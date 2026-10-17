param([string]$GamePath = 'E:\SteamLibrary\steamapps\common\Slay the Spire 2')
$ErrorActionPreference = 'Stop'
$release = Get-Content -LiteralPath (Join-Path $GamePath 'release_info.json') -Raw | ConvertFrom-Json
if ($release.version -ne 'v0.107.1') { throw "Unsupported game: $($release.version). Expected v0.107.1." }
if (Get-Process SlayTheSpire2 -ErrorAction SilentlyContinue) { throw 'Close Slay the Spire 2 before installing.' }
$projectRoot = Split-Path $PSScriptRoot -Parent
$package = Join-Path $projectRoot 'artifacts/package/LaRata'
$destination = Join-Path $GamePath 'mods/LaRata'
foreach ($name in 'LaRata.dll','LaRata.json','LaRata.pck') {
    if (!(Test-Path -LiteralPath (Join-Path $package $name))) { throw 'Run scripts/Build.ps1 first.' }
}
New-Item -ItemType Directory -Force $destination | Out-Null
foreach ($name in 'LaRata.dll','LaRata.json','LaRata.pck') {
    Copy-Item -LiteralPath (Join-Path $package $name) -Destination (Join-Path $destination $name)
}
Write-Output "Installed LaRata DLL, manifest and resources into $destination. BaseLib 3.4.7 is required."
