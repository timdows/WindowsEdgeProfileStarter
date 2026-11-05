$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework 4.x is nodig om de app te bouwen.' }
$output = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$sources = Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter *.cs -Recurse | Sort-Object FullName | Select-Object -ExpandProperty FullName
& $compiler /nologo /target:winexe /optimize+ "/win32icon:$PSScriptRoot\assets\app.ico" "/out:$output\EdgeProfileStarter.exe" "/resource:$PSScriptRoot\assets\app.ico,EdgeProfileStarter.AppIcon" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll @sources
if ($LASTEXITCODE -ne 0) { throw 'Bouwen mislukt.' }
$config = Join-Path $output 'config.json'
if (-not (Test-Path -LiteralPath $config)) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'config.json') -Destination $config
}
Write-Host "Klaar: $output\EdgeProfileStarter.exe"
