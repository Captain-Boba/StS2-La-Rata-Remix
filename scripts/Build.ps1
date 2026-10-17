param([string]$GamePath = 'E:\SteamLibrary\steamapps\common\Slay the Spire 2')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$project = Join-Path $projectRoot 'src/LaRata/LaRata.csproj'
& dotnet build $project -c Release "-p:Sts2Path=$GamePath"
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
$package = Join-Path $projectRoot 'artifacts/package/LaRata'
New-Item -ItemType Directory -Force $package | Out-Null
$godot = Join-Path $projectRoot '.tools/Godot-4.5.1/Godot_v4.5.1-stable_win64_console.exe'
if (!(Test-Path -LiteralPath $godot)) { throw 'Install official Godot 4.5.1 into .tools/Godot-4.5.1 first.' }
& $godot --headless --path (Join-Path $projectRoot 'assets') --export-pack LaRata (Join-Path $package 'LaRata.pck')
if ($LASTEXITCODE -ne 0) { throw 'Resource export failed.' }
Copy-Item -LiteralPath (Join-Path $projectRoot 'src/LaRata/bin/Release/net9.0/LaRata.dll') -Destination $package
Copy-Item -LiteralPath (Join-Path $projectRoot 'src/LaRata/LaRata.json') -Destination $package
Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md'),(Join-Path $projectRoot 'TEST-REPORT.md'),(Join-Path $projectRoot 'LICENSE') -Destination $package
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs') -Destination $package -Recurse -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'La Rata Remix - Titelbild - V1.png') -Destination $package
$version = (Get-Content -LiteralPath (Join-Path $package 'LaRata.json') -Raw | ConvertFrom-Json).version
$archive = Join-Path $projectRoot "artifacts/LaRata-$version.zip"
Compress-Archive -LiteralPath $package -DestinationPath $archive -Force
Write-Output "Release archive: $archive"
Get-ChildItem -LiteralPath $package -File | Get-FileHash | Select-Object Path,Hash
