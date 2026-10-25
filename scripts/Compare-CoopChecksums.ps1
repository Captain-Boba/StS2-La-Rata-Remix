param(
    [Parameter(Mandatory)][string[]]$Logs,
    [ValidateRange(2, 4)][int]$ExpectedPlayers = 4,
    [string[]]$RequiredMarkers = @('encounter=TWO_TAILED_RATS_NORMAL players=4', 'Suppression resolved for 4 living players'),
    [int]$MinimumChecksums = 10,
    [string]$ExpectedModVersion = '0.2.0',
    [switch]$AllowEngineShutdownResourceLeaks,
    [switch]$RequireResetAudit
)
$ErrorActionPreference = 'Stop'
if ($Logs.Count -ne $ExpectedPlayers -or ($Logs | Select-Object -Unique).Count -ne $ExpectedPlayers) { throw "Supply $ExpectedPlayers distinct logs from the same run (host first)." }
$records = @()
$resetRecords = @()
foreach ($path in $Logs) {
    $text = Get-Content -LiteralPath $path -Raw
    if ($AllowEngineShutdownResourceLeaks) {
        # Preserve every gameplay error. Only classify known engine resource-leak
        # messages after its explicit shutdown marker separately from sync failures.
        $shutdown = $text.IndexOf('FMOD Sound System: System released', [StringComparison]::Ordinal)
        if ($shutdown -ge 0) {
            $head = $text.Substring(0, $shutdown)
            $tail = $text.Substring($shutdown)
            $known = '(?m)^ERROR: (?:\d+ RID allocations of type .+ were leaked at exit\.|\d+ shaders of type \w+ were never freed|\d+ resources still in use at exit \(run with --verbose for details\)\.)\r?$'
            $count = [regex]::Matches($tail, $known).Count
            if ($count) { Write-Output "NOTE: $count engine shutdown resource-leak diagnostics in $path (retained in original log)." }
            $text = $head + [regex]::Replace($tail, $known, '')
        }
    }
    if ($text -match 'State divergence|state diverged|Exception thrown|LaRata: unexpected|\[ERROR\]|(?m)^ERROR:|(?m)^SCRIPT ERROR:') { throw "Failure recorded in $path" }
    $versionPattern = '(?m)^\[INFO\] \[LaRata\] Loaded (?:La Rata Remix )?' + [regex]::Escape($ExpectedModVersion) + '(?:\.|:)(?:\s|$)'
    if ($text -notmatch $versionPattern) { throw "Incomplete scenario in ${path}: missing exact loaded version $ExpectedModVersion" }
    foreach ($required in $RequiredMarkers) {
        if (!$text.Contains($required)) { throw "Incomplete scenario in ${path}: missing $required" }
    }
    $map = @{}
    foreach ($match in [regex]::Matches($text, 'AUDIT checksum id=(\d+) value=(\d+) context=([^\r\n]+)')) {
        $id = $match.Groups[1].Value
        if ($map.ContainsKey($id)) { throw "Duplicate checksum ID $id in $path. Use logs from a single run, without mixing reloads." }
        # PlayCardAction.ToString embeds AbstractModel.ToString's process-local
        # RuntimeHelpers.GetHashCode. Preserve card ID, combat index and target;
        # normalize only that object identity, never the native checksum value.
        $context = [regex]::Replace($match.Groups[3].Value,
            '(?<=PlayCardAction card: )(CARD\.[A-Z0-9_-]+) \(\d+\)(?= index:)', '$1 (runtime-object)')
        $map[$id] = $match.Groups[2].Value + '|' + $context
    }
    if ($map.Count -lt $MinimumChecksums) { throw "Too few checksum observations in $path." }
    $records += ,$map
    if ($RequireResetAudit) {
        $resetMap = @{}
        foreach ($match in [regex]::Matches($text, 'AUDIT reset id=(\d+) (combat=[01] round=\d+ side=\w+ owners=\[(?:\d+:\d+(?:,\d+:\d+)*)?\])')) {
            $id = $match.Groups[1].Value
            if ($resetMap.ContainsKey($id)) { throw "Duplicate Reset audit ID $id in $path." }
            $resetMap[$id] = $match.Groups[2].Value
        }
        if ($resetMap.Count -ne $map.Count -or @($map.Keys | Where-Object { !$resetMap.ContainsKey($_) }).Count -ne 0) {
            throw "Reset ledger audit does not cover every native checksum in $path."
        }
        $resetRecords += ,$resetMap
    }
}
foreach ($peer in 1..($ExpectedPlayers - 1)) {
    if ($records[$peer].Count -ne $records[0].Count) { throw "Peer $peer has incomplete checksum coverage. Finish all actions and close cleanly before comparing." }
    foreach ($id in $records[0].Keys) {
        if ($records[$peer][$id] -cne $records[0][$id]) { throw "Peer $peer differs at native checksum ID $id." }
        if ($RequireResetAudit -and $resetRecords[$peer][$id] -cne $resetRecords[0][$id]) { throw "Peer $peer differs at private Reset ledger ID $id." }
    }
}
Write-Output "PASS: $($records[0].Count) identical native checksum IDs and contexts on all $ExpectedPlayers peers; all requested scenario markers observed. This covers this recorded scenario only."
if ($RequireResetAudit) { Write-Output "PASS: private Reset owner balances and native combat/round/side match at all $($records[0].Count) checksum boundaries on all $ExpectedPlayers actual peer logs." }
