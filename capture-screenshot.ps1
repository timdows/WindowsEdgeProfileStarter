$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
$output = Join-Path $PSScriptRoot 'dist\CaptureScreenshot.exe'
$sources = Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter *.cs -Recurse | Sort-Object FullName | Select-Object -ExpandProperty FullName
New-Item -ItemType Directory -Path (Join-Path $PSScriptRoot 'dist'), (Join-Path $PSScriptRoot 'docs') -Force | Out-Null
& $compiler /nologo /target:exe /main:EdgeProfileStarter.CaptureScreenshot "/win32icon:$PSScriptRoot\assets\app.ico" "/out:$output" "/resource:$PSScriptRoot\assets\app.ico,EdgeProfileStarter.AppIcon" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll @sources (Join-Path $PSScriptRoot 'tools\CaptureScreenshot.cs')
if ($LASTEXITCODE -ne 0) { throw 'Screenshotbuild mislukt.' }
try {
    $config = Join-Path $PSScriptRoot 'dist\config.json'
    if (-not (Test-Path -LiteralPath $config)) { $config = Join-Path $PSScriptRoot 'config.json' }
    & $output $config (Join-Path $PSScriptRoot 'docs\screenshot.png')
    if ($LASTEXITCODE -ne 0) { throw 'Screenshot maken mislukt.' }
} finally { Remove-Item -LiteralPath $output -ErrorAction SilentlyContinue }
