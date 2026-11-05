# Rebuild the original vector-drawn, multi-resolution Windows icon.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$assetPath = Join-Path $PSScriptRoot 'assets'
New-Item -ItemType Directory -Path $assetPath -Force | Out-Null
$images = @()
$sizes = @(16, 24, 32, 48, 64, 128, 256)
foreach ($size in $sizes) {
    $bitmap = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.ScaleTransform(($size / 256.0), ($size / 256.0))
    $shape = New-Object System.Drawing.Drawing2D.GraphicsPath
    $shape.AddArc(8, 8, 88, 88, 180, 90)
    $shape.AddArc(160, 8, 88, 88, 270, 90)
    $shape.AddArc(160, 160, 88, 88, 0, 90)
    $shape.AddArc(8, 160, 88, 88, 90, 90)
    $shape.CloseFigure()
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush ([System.Drawing.Rectangle]::new(8, 8, 240, 240)), ([System.Drawing.Color]::FromArgb(62, 163, 242)), ([System.Drawing.Color]::FromArgb(118, 91, 219)), 45
    $g.FillPath($brush, $shape)
    $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::White), 10
    $g.DrawEllipse($pen, 49, 49, 158, 158)
    $g.DrawEllipse($pen, 89, 49, 78, 158)
    $g.DrawLine($pen, 49, 128, 207, 128)
    $badge = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(45, 63, 132))
    $g.FillEllipse($badge, 155, 153, 85, 85)
    $g.FillEllipse([System.Drawing.Brushes]::White, 188, 168, 19, 19)
    $g.FillPie([System.Drawing.Brushes]::White, 174, 194, 47, 39, 180, 180)
    $stream = New-Object System.IO.MemoryStream
    $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $images += ,$stream.ToArray()
    if ($size -eq 256) { $bitmap.Save((Join-Path $assetPath 'app.png'), [System.Drawing.Imaging.ImageFormat]::Png) }
    $stream.Dispose(); $pen.Dispose(); $badge.Dispose(); $brush.Dispose(); $shape.Dispose(); $g.Dispose(); $bitmap.Dispose()
}
$file = [System.IO.File]::Create((Join-Path $assetPath 'app.ico'))
$writer = New-Object System.IO.BinaryWriter $file
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
    $offset = 6 + 16 * $sizes.Count
    for ($i = 0; $i -lt $sizes.Count; $i++) {
        $dimension = if ($sizes[$i] -eq 256) { 0 } else { $sizes[$i] }
        $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
        $writer.Write([byte]0); $writer.Write([byte]0)
        $writer.Write([uint16]1); $writer.Write([uint16]32)
        $writer.Write([uint32]$images[$i].Length); $writer.Write([uint32]$offset)
        $offset += $images[$i].Length
    }
    foreach ($data in $images) { $writer.Write([byte[]]$data) }
} finally { $writer.Dispose(); $file.Dispose() }
