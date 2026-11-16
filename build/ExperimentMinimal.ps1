$ErrorActionPreference = 'Stop'

$root = 'C:\Users\Admin\WorkBuddy\2026-11-16-17-36-09\ZoneTray'
$src = Join-Path $root 'deployment\src'
$exp = Join-Path $root 'exp-minimal'
$expSrc = Join-Path $exp 'src'

# Clean and create isolated experiment dir
if (Test-Path $exp) { Remove-Item $exp -Recurse -Force }
New-Item -ItemType Directory -Force -Path $expSrc | Out-Null

Copy-Item (Join-Path $root 'deployment\ZoneTray.exe') (Join-Path $exp 'ZoneTray.exe') -Force

$core = @(
    'PowerToys.FancyZones.exe',
    'Resource.dll',
    'PowerToys.Interop.dll',
    'PowerToys.GPOWrapper.dll'
)
foreach ($f in $core) {
    $p = Join-Path $src $f
    if (Test-Path $p) { Copy-Item $p (Join-Path $expSrc $f) -Force }
}

Write-Output ("EXPERIMENT_FILES={0}" -f @(Get-ChildItem $expSrc -File).Count)
Get-ChildItem $expSrc -File | ForEach-Object { Write-Output ("  {0} {1} MB" -f $_.Name, [math]::Round($_.Length/1MB,3)) }

# Launch and observe
$p = Start-Process -FilePath (Join-Path $exp 'ZoneTray.exe') -PassThru
Start-Sleep -Seconds 6

$fz = Get-Process -Name 'PowerToys.FancyZones' -ErrorAction SilentlyContinue
if ($fz) {
    foreach ($f in $fz) { Write-Output ("FZ_ALIVE PID={0} MainWindowHandle={1}" -f $f.Id, $f.MainWindowHandle) }
} else {
    Write-Output 'FZ_ALIVE=NO'
}

$name = 'FancyZones-ToggleEditorEvent-1e174338-06a3-472b-874d-073b21c62f14'
try {
    $ev = [System.Threading.EventWaitHandle]::OpenExisting("Local\$name")
    Write-Output 'EDITOR_EVENT_REGISTERED=YES'
    $ev.Dispose()
} catch [System.Threading.WaitHandleCannotBeOpenedException] {
    Write-Output 'EDITOR_EVENT_REGISTERED=NO'
}

# Cleanup
Stop-Process -Name 'ZoneTray','PowerToys.FancyZones' -Force -ErrorAction SilentlyContinue
Write-Output 'CLEANED'
