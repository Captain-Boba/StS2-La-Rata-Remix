param(
    [ValidateSet('Solo','Host','Client1','Client2','Client3','LoadHost')][string]$Role = 'Solo',
    [string]$GamePath = 'E:\SteamLibrary\steamapps\common\Slay the Spire 2'
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$logDir = Join-Path $projectRoot 'artifacts/logs'
New-Item -ItemType Directory -Force $logDir | Out-Null
# The native fastmp host always has network ID 1. Its profile ID must match for save validation.
$ids = @{Solo=2000; Host=1; LoadHost=1; Client1=2101; Client2=2102; Client3=2103}
$modes = @{Host='host_standard'; LoadHost='load'; Client1='join'; Client2='join'; Client3='join'}
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$logPath = Join-Path $logDir "$stamp-$Role.log"
$arguments = "--force-steam off -clientId $($ids[$Role]) -larata-audit -log GameSync Debug --log-file `"$logPath`""
if ($modes.ContainsKey($Role)) { $arguments += " -fastmp $($modes[$Role])" }
# Local ENet is provided by the game. No Steam account, steam_appid.txt, cloud saves or custom transport needed.
$process = Start-Process -FilePath (Join-Path $GamePath 'SlayTheSpire2.exe') -WorkingDirectory $GamePath -WindowStyle Hidden -ArgumentList $arguments -PassThru
[pscustomobject]@{Role=$Role;ProcessId=$process.Id;Log=$logPath;LocalPlayerId=$ids[$Role]}
