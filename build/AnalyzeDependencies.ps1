$ErrorActionPreference = 'Stop'
$src = 'C:\Users\Admin\WorkBuddy\2026-11-16-17-36-09\ZoneTray\deployment\src'
$raw = Get-Content (Join-Path $src 'PowerToys.FancyZonesEditor.deps.json') -Raw -Encoding UTF8

$allowed = @{}
# Extract file names declared as runtime/native/resource assets: "name.dll": { ... }
$matches = [regex]::Matches($raw, '"([^"]+\.(?:dll|exe))"\s*:\s*\{')
foreach ($m in $matches) {
    $name = Split-Path -Leaf $m.Groups[1].Value
    $allowed[$name] = $true
}

# Files required by the core engine / tray but not declared in editor deps.json
$extra = @(
    'PowerToys.FancyZones.exe',
    'PowerToys.FancyZonesEditor.exe',
    'PowerToys.FancyZonesEditor.runtimeconfig.json',
    'PowerToys.FancyZonesEditor.deps.json',
    'Resource.dll'
)
foreach ($e in $extra) { $allowed[$e] = $true }

Write-Output ("DEBUG allowed_count={0}" -f $allowed.Count)
Write-Output ("DEBUG allowed_sample={0}" -f (($allowed.Keys | Select-Object -First 8) -join ', '))
Write-Output ("DEBUG targets={0}" -f @($json.targets.PSObject.Properties).Count)

$all = Get-ChildItem $src -File
$keep = @($all | Where-Object { $allowed.ContainsKey($_.Name) })
$drop = @($all | Where-Object { -not $allowed.ContainsKey($_.Name) })

Write-Output ("TOTAL={0} KEEP={1} DROP={2}" -f $all.Count, $keep.Count, $drop.Count)
Write-Output ("KEEP_MB={0} DROP_MB={1}" -f [math]::Round((($keep | Measure-Object Length -Sum).Sum)/1MB, 2), [math]::Round((($drop | Measure-Object Length -Sum).Sum)/1MB, 2))
Write-Output '=== DROP list (name, MB) ==='
$drop | Sort-Object Length -Descending | ForEach-Object { Write-Output ("{0}`t{1}" -f $_.Name, [math]::Round($_.Length/1MB, 3)) }
