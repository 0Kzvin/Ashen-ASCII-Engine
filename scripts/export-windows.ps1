param([string]$Godot,[string]$OutputDirectory)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$dest=if ($OutputDirectory) {[IO.Path]::GetFullPath($OutputDirectory)} else {Join-Path $root 'build/windows-v3'}
New-Item -ItemType Directory -Force $dest | Out-Null
& "$root/run.ps1" -Godot $Godot -BuildOnly
if ($LASTEXITCODE) {exit $LASTEXITCODE}
$log= & "$root/run.ps1" -Godot $Godot -SkipBuild -GameArgs @('--headless','--export-release','Windows',"$dest/ASHEN.exe") 2>&1
$code=$LASTEXITCODE
$log | ForEach-Object {Write-Host $_}
if ($code -or ($log -match '(^|\s)ERROR:')) {throw 'Falló la exportación Windows.'}
if (!(Test-Path "$dest/data_Ashen_windows_x86_64/Ashen.dll")) {throw 'Faltan los ensamblados .NET de la exportación.'}
