[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [ValidateSet('Menu', 'Status', 'Local', 'Workshop')]
    [string]$Mode = 'Menu',
    [string]$GamePath = 'E:\SteamLibrary\steamapps\common\Slay the Spire 2',
    [string]$WorkshopPath = '',
    [string]$PackagePath = ''
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$gameRoot = [IO.Path]::GetFullPath($GamePath).TrimEnd('\', '/')
if (!(Test-Path -LiteralPath (Join-Path $gameRoot 'release_info.json'))) {
    throw "Game directory not found: $gameRoot"
}
if (!$WorkshopPath) {
    $steamApps = Split-Path (Split-Path $gameRoot -Parent) -Parent
    $WorkshopPath = Join-Path $steamApps 'workshop/content/2868840/3822192203/LaRata'
}
if (!$PackagePath) { $PackagePath = Join-Path $projectRoot 'artifacts/package/LaRata' }
$active = Join-Path $gameRoot 'mods/LaRata'
# Outside mods: the native loader must not discover the parked copy.
$parked = Join-Path $gameRoot '.larata-dev/LaRata'
$runtimeFiles = @('LaRata.dll', 'LaRata.json', 'LaRata.pck')

function Get-ModVersion([string]$Path) {
    foreach ($file in $runtimeFiles) {
        $item = Get-Item -LiteralPath (Join-Path $Path $file) -ErrorAction SilentlyContinue
        if (!$item -or $item.PSIsContainer -or $item.Length -eq 0) {
            throw "Missing or empty $file in $Path. No switch performed."
        }
    }
    $manifest = Get-Content -LiteralPath (Join-Path $Path 'LaRata.json') -Raw | ConvertFrom-Json
    if ($manifest.id -ne 'LaRata' -or !$manifest.version) { throw "Invalid LaRata manifest: $Path" }
    return [string]$manifest.version
}

function Show-Status {
    Write-Host ''
    if (Test-Path -LiteralPath $active) {
        Write-Host "Installation source: LOCAL TEST ($(Get-ModVersion $active))"
    } elseif (Test-Path -LiteralPath $WorkshopPath) {
        Write-Host "Installation source: WORKSHOP ($(Get-ModVersion $WorkshopPath))"
    } else {
        Write-Host 'Installation source: NONE - subscribe to La Rata in Steam.'
    }
    if (Test-Path -LiteralPath $parked) { Write-Host "Parked local version: $(Get-ModVersion $parked)" }
    Write-Host 'Enable La Rata and BaseLib in the game mod menu. Remove --nomods if set.'
    Write-Host 'For co-op, all players must use matching versions. Switch between runs.'
}

if ($Mode -eq 'Menu') {
    Show-Status
    Write-Host ''
    Write-Host '1 - Use local test version'
    Write-Host '2 - Use Steam Workshop version'
    Write-Host '0 - Exit'
    switch (Read-Host 'Choose') {
        '1' { $Mode = 'Local' }
        '2' { $Mode = 'Workshop' }
        default { return }
    }
}
if ($Mode -eq 'Status') { Show-Status; return }
if (Get-Process SlayTheSpire2 -ErrorAction SilentlyContinue) {
    throw 'Close Slay the Spire 2 before switching. No files were changed.'
}

# Reject junctions/symlinks in mutation paths so moves stay within this game folder.
foreach ($path in @($gameRoot, (Join-Path $gameRoot 'mods'), (Join-Path $gameRoot '.larata-dev'), $active, $parked)) {
    $item = Get-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue
    if ($item -and ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw "Refusing to move through a symbolic link or junction: $path"
    }
    $resolved = [IO.Path]::GetFullPath($path)
    if ($resolved -ne $gameRoot -and !$resolved.StartsWith($gameRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Path outside game directory: $resolved"
    }
}

if ($Mode -eq 'Workshop') {
    $null = Get-ModVersion $WorkshopPath
    if (!(Test-Path -LiteralPath $active)) { Show-Status; return }
    $null = Get-ModVersion $active
    if (Test-Path -LiteralPath $parked) {
        throw "Both active and parked local copies exist. Nothing overwritten. Resolve these copies first: $active ; $parked"
    }
    if ($PSCmdlet.ShouldProcess($active, "Park local mod at $parked and use Workshop")) {
        New-Item -ItemType Directory -Path (Split-Path $parked -Parent) -Force | Out-Null
        Move-Item -LiteralPath $active -Destination $parked
    }
} else {
    if (Test-Path -LiteralPath $active) { Show-Status; return }
    $source = $PackagePath
    if (Test-Path -LiteralPath $parked) { $source = $parked }
    $null = Get-ModVersion $source
    $release = Get-Content -LiteralPath (Join-Path $gameRoot 'release_info.json') -Raw | ConvertFrom-Json
    $manifest = Get-Content -LiteralPath (Join-Path $source 'LaRata.json') -Raw | ConvertFrom-Json
    if ($manifest.min_game_version -and $release.version.TrimStart('v') -ne $manifest.min_game_version.TrimStart('v')) {
        throw "Local build requires game $($manifest.min_game_version); installed: $($release.version)."
    }
    if ($PSCmdlet.ShouldProcess($active, "Activate local test mod from $source")) {
        New-Item -ItemType Directory -Path (Split-Path $active -Parent) -Force | Out-Null
        if ($source -eq $parked) {
            Move-Item -LiteralPath $parked -Destination $active
        } else {
            # Copy into a unique staging folder outside mods before exposing the installation.
            $stage = Join-Path $gameRoot ('.larata-dev/staging-' + [guid]::NewGuid().ToString('N'))
            New-Item -ItemType Directory -Path $stage -Force | Out-Null
            foreach ($file in $runtimeFiles) {
                Copy-Item -LiteralPath (Join-Path $source $file) -Destination (Join-Path $stage $file)
                if ((Get-FileHash -LiteralPath (Join-Path $source $file)).Hash -ne (Get-FileHash -LiteralPath (Join-Path $stage $file)).Hash) {
                    throw "Copy verification failed for $file. Staged files retained at $stage."
                }
            }
            Move-Item -LiteralPath $stage -Destination $active
        }
    }
}
Show-Status
