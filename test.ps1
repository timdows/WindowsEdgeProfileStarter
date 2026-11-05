$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
$output = Join-Path $PSScriptRoot 'dist\SmokeTests.exe'
$sources = Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter *.cs -Recurse | Sort-Object FullName | Select-Object -ExpandProperty FullName
New-Item -ItemType Directory -Path (Join-Path $PSScriptRoot 'dist') -Force | Out-Null
& $compiler /nologo /target:exe /main:EdgeProfileStarter.SmokeTests "/out:$output" "/resource:$PSScriptRoot\assets\app.ico,EdgeProfileStarter.AppIcon" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll @sources (Join-Path $PSScriptRoot 'tests\SmokeTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Testbuild mislukt.' }
try {
    & $output
    if ($LASTEXITCODE -ne 0) { throw 'Tests mislukt.' }
} finally { Remove-Item -LiteralPath $output -ErrorAction SilentlyContinue }
