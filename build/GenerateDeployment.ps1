[CmdletBinding()]
param(
    [string]$PowerToysInstallPath,
    [string]$PowerToysVersion = '0.99.1'
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$currentDir = $PSScriptRoot
$projectRoot = Split-Path -Parent $currentDir
$dependenciesFile = Join-Path $currentDir 'FancyZonesDependencies.txt'
$trayProjectDir = Join-Path $projectRoot 'src\ZoneTray'
$deploymentDir = Join-Path $projectRoot 'deployment'
$deploymentSrcDir = Join-Path $deploymentDir 'src'
$publishDir = Join-Path $env:TEMP 'ZoneTray-Publish'
$docsDir = Join-Path $currentDir 'docs'

$dotnet = Get-Command 'dotnet.exe' -ErrorAction SilentlyContinue
if (-not $dotnet) { throw "dotnet.exe not found on PATH. Install .NET SDK 9 first." }
Write-Host "dotnet.exe : $($dotnet.Source)"

function Test-PowerToysInstall {
    param([string]$Path)
    if (-not $Path -or -not (Test-Path -Path $Path -PathType Container)) { return $false }
    $hit = Get-ChildItem -Path $Path -Recurse -Filter 'PowerToys.FancyZones.exe' -ErrorAction SilentlyContinue | Select-Object -First 1
    return [bool]$hit
}

if (-not $PowerToysInstallPath) {
    $candidates = @(
        (Join-Path $env:LOCALAPPDATA 'PowerToys'),
        (Join-Path ${env:ProgramFiles} 'PowerToys')
    )
    foreach ($c in $candidates) {
        if (Test-PowerToysInstall -Path $c) { $PowerToysInstallPath = $c; break }
    }
}
$coreDeps = @('PowerToys.FancyZones.exe', 'PowerToys.Interop.dll', 'PowerToys.GPOWrapper.dll')

if (-not $PowerToysInstallPath -or -not (Test-PowerToysInstall -Path $PowerToysInstallPath)) {
    # Fallback: reuse already-extracted core binaries from an existing deployment.
    $reusable = @($coreDeps | Where-Object { Test-Path -LiteralPath (Join-Path $deploymentSrcDir $_) })
    if ($reusable.Count -eq $coreDeps.Count) {
        Write-Host "PowerToys install not found; reusing extracted binaries from $deploymentSrcDir"
        $PowerToysInstallPath = $null
    } else {
        throw "PowerToys install not found and no complete extracted binaries in deployment\src. Install PowerToys $PowerToysVersion or pass -PowerToysInstallPath."
    }
}

if ($PowerToysInstallPath) {
    Write-Host "PowerToys install: $PowerToysInstallPath"
    $fzExe = Get-ChildItem -Path $PowerToysInstallPath -Recurse -Filter 'PowerToys.FancyZones.exe' | Select-Object -First 1
    Write-Host "FancyZones version: $($fzExe.VersionInfo.ProductVersion) (pinned: $PowerToysVersion)"

    $installedFiles = Get-ChildItem -Path $PowerToysInstallPath -Recurse -File -ErrorAction SilentlyContinue
    $fileIndex = @{}
    foreach ($file in $installedFiles) {
        $key = $file.Name.ToLowerInvariant()
        if (-not $fileIndex.ContainsKey($key)) {
            $fileIndex[$key] = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
        }
        $fileIndex[$key].Add($file)
    }

    $stageDir = Join-Path $env:TEMP 'ZoneTray-CoreStage'
    if (Test-Path $stageDir) { Remove-Item -Recurse -Force $stageDir }
    New-Item -ItemType Directory -Force -Path $stageDir | Out-Null
    $missing = [System.Collections.Generic.List[string]]::new()
    foreach ($dep in $coreDeps) {
        $candidates = $fileIndex[$dep.ToLowerInvariant()]
        if (-not $candidates -or $candidates.Count -eq 0) { $missing.Add($dep); continue }
        $source = $candidates | Sort-Object { $_.FullName.Length } | Select-Object -First 1
        Copy-Item -LiteralPath $source.FullName -Destination (Join-Path $stageDir $dep) -Force
    }
    if ($missing.Count -gt 0) {
        throw ("Missing core dependencies:`n  " + ($missing -join "`n  "))
    }
    $copySource = $stageDir
} else {
    # Reuse path: keep binaries in place, nothing to stage or re-copy.
    Write-Host "Copied $($coreDeps.Count)/$($coreDeps.Count) core dependencies (reused, editor runtime omitted)"
    $copySource = $null
}

if ($copySource) {
    if (Test-Path $deploymentSrcDir) { Remove-Item -Recurse -Force $deploymentSrcDir }
    New-Item -ItemType Directory -Force -Path $deploymentSrcDir | Out-Null
    foreach ($dep in $coreDeps) {
        Copy-Item -LiteralPath (Join-Path $copySource $dep) -Destination (Join-Path $deploymentSrcDir $dep) -Force
    }
    Write-Host "Copied $($coreDeps.Count)/$($coreDeps.Count) core dependencies (editor runtime omitted)"
    Remove-Item -Recurse -Force $copySource -ErrorAction SilentlyContinue
}

Write-Host "Publishing ZoneTray..."
if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }
& $dotnet.Source publish $trayProjectDir -c Release -o $publishDir --nologo | Out-Host
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }
$trayExe = Join-Path $publishDir 'ZoneTray.exe'
if (-not (Test-Path $trayExe)) { throw "ZoneTray.exe was not produced." }
Copy-Item -LiteralPath $trayExe -Destination (Join-Path $deploymentDir 'ZoneTray.exe') -Force
Write-Host "Tray helper -> $deploymentDir\ZoneTray.exe"
Remove-Item -Recurse -Force $publishDir -ErrorAction SilentlyContinue

$addAutostartPath = Join-Path $deploymentDir 'Add-ToAutostart.ps1'
@'
$scriptDir = $PSScriptRoot
$exePath = Join-Path $scriptDir 'ZoneTray.exe'
if (-not (Test-Path $exePath -PathType Leaf)) { Write-Error "ZoneTray.exe not found"; exit 1 }
$startupDir = [Environment]::GetFolderPath('Startup')
$shortcutPath = Join-Path $startupDir 'ZoneTray.lnk'
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $exePath
$shortcut.WorkingDirectory = $scriptDir
$shortcut.Description = 'ZoneTray'
$shortcut.Save()
Write-Host "Autostart shortcut created: $shortcutPath"
'@ | Out-File -FilePath $addAutostartPath -Encoding utf8

foreach ($doc in @('readme.md', 'THIRD_PARTY_NOTICES.txt')) {
    $src = Join-Path $docsDir $doc
    if (Test-Path $src) {
        Copy-Item -LiteralPath $src -Destination (Join-Path $deploymentDir $doc) -Force
    }
}

$rootLicense = Join-Path $projectRoot 'LICENSE'
if (Test-Path $rootLicense) {
    Copy-Item -LiteralPath $rootLicense -Destination (Join-Path $deploymentDir 'LICENSE') -Force
    Write-Host "Project LICENSE -> $deploymentDir\LICENSE"
}

$zipPath = Join-Path $projectRoot 'ZoneTray.zip'
if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
Compress-Archive -Path (Join-Path $deploymentDir '*') -DestinationPath $zipPath -CompressionLevel Optimal
$sizeMB = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)
Write-Host "Deployment ready: $zipPath ($sizeMB MB)" -ForegroundColor Green
