[CmdletBinding()]
param([switch]$ExportTemplates)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$local=Join-Path $root '.local'
$version='4.7.2-stable'
$editorName="Godot_v$($version)_mono_win64"
$editorDir=Join-Path $local $editorName
New-Item -ItemType Directory -Force $local | Out-Null
function Get-VerifiedArchive($name,$path,$digest) {
    if (!(Test-Path $path) -or (Get-FileHash -LiteralPath $path).Hash -ne $digest) {
        $url="https://github.com/godotengine/godot/releases/download/$version/$name"
        curl.exe --fail --location --retry 2 $url -o "$path.partial"
        if ($LASTEXITCODE -or (Get-FileHash -LiteralPath "$path.partial").Hash -ne $digest) {throw "Falló la verificación de $name"}
        Move-Item -LiteralPath "$path.partial" -Destination $path -Force
    }
}
if (!(Test-Path "$editorDir/$($editorName)_console.exe")) {
    Get-VerifiedArchive "$editorName.zip" "$local/editor.zip" 'A2A48473A7414C5F19FAB690518CAEBB738C09EF9601F6BD2388676A7F53B3C0'
    Expand-Archive -LiteralPath "$local/editor.zip" -DestinationPath $local -Force
}
New-Item -ItemType File -Force "$editorDir/_sc_" | Out-Null
if ($ExportTemplates) {
    $dest=Join-Path $editorDir 'editor_data/export_templates/4.7.2.stable.mono'
    New-Item -ItemType Directory -Force $dest | Out-Null
    Get-VerifiedArchive "Godot_v$($version)_mono_export_templates.tpz" "$local/templates.zip" '92F8681E349EF1F90891B792DA95E3B2B0BD1ED610B78018C58FEB2D87E15A9D'
    tar -xf "$local/templates.zip" -C $dest --strip-components 1 templates/windows_release_x86_64.exe templates/windows_release_x86_64_console.exe templates/windows_debug_x86_64.exe
    if ($LASTEXITCODE) {throw 'Error al extraer plantillas.'}
}
& "$editorDir/$($editorName)_console.exe" --version
