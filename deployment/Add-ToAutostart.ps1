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
