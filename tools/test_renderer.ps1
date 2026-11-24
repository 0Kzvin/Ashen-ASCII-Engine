[CmdletBinding()]
param([string]$Godot = $env:GODOT4)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$work = Join-Path $root '.local/render-poc'
$captures = Join-Path $work 'captures'
if (!$Godot) {
    $portable = Get-ChildItem (Join-Path $root '.local') -Recurse -Filter '*mono*console.exe' -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($portable) { $Godot = $portable.FullName }
}
if (!$Godot) { throw 'Indica Godot 4.7.2 .NET con -Godot o GODOT4.' }

foreach ($folder in 'scripts/rendering', 'shaders', 'assets/fonts', 'captures') {
    New-Item -ItemType Directory -Path (Join-Path $work $folder) -Force | Out-Null
}
New-Item -ItemType File -Path (Join-Path $captures '.gdignore') -Force | Out-Null
foreach ($file in 'scripts/rendering/AsciiRenderer.cs', 'shaders/ascii_composite.gdshader', 'assets/fonts/glyph_atlas.png', 'Ashen.csproj') {
    Copy-Item -LiteralPath (Join-Path $root "game/$file") -Destination (Join-Path $work $file)
}
foreach ($file in 'project.godot', 'Probe.cs', 'Probe.tscn') {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot "render_poc/$file") -Destination (Join-Path $work $file)
}

dotnet build (Join-Path $work 'Ashen.csproj') --nologo -v:q
if ($LASTEXITCODE) { exit $LASTEXITCODE }
& $Godot --headless --editor --path $work --import
if ($LASTEXITCODE) { exit $LASTEXITCODE }
$log = Join-Path $captures 'run.log'
& $Godot --path $work --rendering-method forward_plus --resolution 1920x1080 --fullscreen --disable-vsync --log-file $log
if ($LASTEXITCODE) { exit $LASTEXITCODE }
if (Select-String -LiteralPath $log -Pattern 'SHADER ERROR|SCRIPT ERROR|^ERROR:') {
    throw "Godot registró un error. Consulta $log"
}
$hashes = foreach ($capture in 'raw', 'mono', 'colour', 'cinematic', 'diagnostic', 'runes') {
    (Get-FileHash -LiteralPath (Join-Path $captures "$capture.png")).Hash
}
if (($hashes | Select-Object -Unique).Count -ne 6) { throw 'Hay capturas idénticas en modos diferentes.' }
Write-Host "Capturas, registro y muestras en $captures"
