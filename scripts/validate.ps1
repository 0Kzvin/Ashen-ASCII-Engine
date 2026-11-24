param([string]$Godot,[switch]$Graphics,[switch]$Benchmark,[switch]$Capture)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$dest=Join-Path $root 'captures-tmp'
New-Item -ItemType Directory -Force $dest | Out-Null
& "$root/run.ps1" -Godot $Godot -BuildOnly
if ($LASTEXITCODE) {exit $LASTEXITCODE}
function Invoke-Check([string[]]$arguments) {
    $log=& "$root/run.ps1" -Godot $Godot -SkipBuild -GameArgs $arguments 2>&1
    $code=$LASTEXITCODE
    $log | ForEach-Object {Write-Host $_}
    if ($code -or ($log -match '(^|\s)(ERROR|SCRIPT ERROR):')) {throw "Falló Godot ($code)"}
}
Invoke-Check @('--headless','--','--qa','--out',$dest)
if ($Graphics) {Invoke-Check @('--windowed','--resolution','1280x720','--','--qa','--out',$dest)}
if ($Capture) {
    Invoke-Check @('--fullscreen','--','--capture','--out',$dest)
    Invoke-Check @('--fullscreen','--','--lab-capture',$dest)
}
if ($Benchmark) {
    foreach ($mode in 5,6) {Invoke-Check @('--fullscreen','--disable-vsync','--','--benchmark','--mode',"$mode",'--out',$dest)}
    Invoke-Check @('--fullscreen','--disable-vsync','--','--benchmark','--mode','6','--screen-only','--out',$dest)
    Invoke-Check @('--fullscreen','--disable-vsync','--','--lab-benchmark',$dest)
}
