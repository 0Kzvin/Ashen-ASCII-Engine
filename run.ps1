[CmdletBinding()]
param([string]$Godot, [switch]$BuildOnly, [switch]$SkipBuild, [string[]]$GameArgs=@())
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
if (!$Godot) { $Godot=$env:GODOT4 }
if (!$Godot -and (Test-Path "$root/.local")) {
    $candidate=Get-ChildItem "$root/.local" -Recurse -Filter '*mono*console.exe' | Select-Object -First 1
    if ($candidate) {$Godot=$candidate.FullName}
}
if (!$Godot) {
    foreach ($name in 'godot-mono','godot4','godot') {
        $cmd=Get-Command $name -ErrorAction SilentlyContinue
        if ($cmd) {$Godot=$cmd.Source;break}
    }
}
if (!$Godot) {throw 'Indica la ruta a Godot 4.7.2 .NET con -Godot o GODOT4.'}
if (!$SkipBuild) {
    dotnet build "$root/game/Ashen.csproj" --nologo -v:q
    if ($LASTEXITCODE) {exit $LASTEXITCODE}
    & $Godot --headless --editor --path "$root/game" --import
    if ($LASTEXITCODE) {exit $LASTEXITCODE}
}
if (!$BuildOnly) {
    & $Godot --path "$root/game" @GameArgs
    exit $LASTEXITCODE
}
