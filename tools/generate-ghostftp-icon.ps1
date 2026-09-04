param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

function New-RoundedPath {
    param(
        [System.Drawing.RectangleF]$Rect,
        [float]$Radius
    )

    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $diameter = $Radius * 2
    $arc = [System.Drawing.RectangleF]::new($Rect.X, $Rect.Y, $diameter, $diameter)
    $path.AddArc($arc, 180, 90)
    $arc.X = $Rect.Right - $diameter
    $path.AddArc($arc, 270, 90)
    $arc.Y = $Rect.Bottom - $diameter
    $path.AddArc($arc, 0, 90)
    $arc.X = $Rect.Left
    $path.AddArc($arc, 90, 90)
    $path.CloseFigure()
    return $path
}

$fullPath = [System.IO.Path]::GetFullPath($OutputPath)
$directory = [System.IO.Path]::GetDirectoryName($fullPath)
[System.IO.Directory]::CreateDirectory($directory) | Out-Null

$size = 256
$bitmap = [System.Drawing.Bitmap]::new($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$graphics.Clear([System.Drawing.Color]::Transparent)

$backgroundPath = New-RoundedPath ([System.Drawing.RectangleF]::new(5, 5, 246, 246)) 48
$backgroundBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
    [System.Drawing.PointF]::new(18, 18),
    [System.Drawing.PointF]::new(238, 238),
    [System.Drawing.Color]::FromArgb(255, 8, 26, 67),
    [System.Drawing.Color]::FromArgb(255, 74, 42, 169))
$graphics.FillPath($backgroundBrush, $backgroundPath)
$rimPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(230, 69, 203, 255), 5)
$graphics.DrawPath($rimPen, $backgroundPath)

$folderBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
    [System.Drawing.PointF]::new(38, 58),
    [System.Drawing.PointF]::new(220, 215),
    [System.Drawing.Color]::FromArgb(255, 49, 220, 255),
    [System.Drawing.Color]::FromArgb(255, 139, 74, 255))

$tab = [System.Drawing.Drawing2D.GraphicsPath]::new()
$tab.AddPolygon([System.Drawing.PointF[]]@(
    [System.Drawing.PointF]::new(46, 64),
    [System.Drawing.PointF]::new(108, 64),
    [System.Drawing.PointF]::new(132, 86),
    [System.Drawing.PointF]::new(210, 86),
    [System.Drawing.PointF]::new(210, 116),
    [System.Drawing.PointF]::new(46, 116)
))
$graphics.FillPath($folderBrush, $tab)

$folderPath = New-RoundedPath ([System.Drawing.RectangleF]::new(36, 92, 184, 122)) 25
$graphics.FillPath($folderBrush, $folderPath)
$folderPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(100, 255, 255, 255), 2)
$graphics.DrawPath($folderPen, $folderPath)

$font = [System.Drawing.Font]::new('Segoe UI', 86, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$textBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
$graphics.DrawString('G', $font, $textBrush, [System.Drawing.PointF]::new(47, 102))

$cyanBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 70, 226, 255))
$violetBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 184, 104, 255))
$graphics.FillPolygon($cyanBrush, [System.Drawing.PointF[]]@(
    [System.Drawing.PointF]::new(181, 108),
    [System.Drawing.PointF]::new(159, 134),
    [System.Drawing.PointF]::new(172, 134),
    [System.Drawing.PointF]::new(172, 160),
    [System.Drawing.PointF]::new(190, 160),
    [System.Drawing.PointF]::new(190, 134),
    [System.Drawing.PointF]::new(203, 134)
))
$graphics.FillPolygon($violetBrush, [System.Drawing.PointF[]]@(
    [System.Drawing.PointF]::new(181, 202),
    [System.Drawing.PointF]::new(159, 176),
    [System.Drawing.PointF]::new(172, 176),
    [System.Drawing.PointF]::new(172, 150),
    [System.Drawing.PointF]::new(190, 150),
    [System.Drawing.PointF]::new(190, 176),
    [System.Drawing.PointF]::new(203, 176)
))

$pngStream = [System.IO.MemoryStream]::new()
$bitmap.Save($pngStream, [System.Drawing.Imaging.ImageFormat]::Png)
$pngBytes = $pngStream.ToArray()

$fileStream = [System.IO.File]::Open($fullPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
$writer = [System.IO.BinaryWriter]::new($fileStream)
try {
    $writer.Write([UInt16]0) # reserved
    $writer.Write([UInt16]1) # icon
    $writer.Write([UInt16]1) # one image
    $writer.Write([Byte]0)   # width 256
    $writer.Write([Byte]0)   # height 256
    $writer.Write([Byte]0)   # palette
    $writer.Write([Byte]0)   # reserved
    $writer.Write([UInt16]1) # planes
    $writer.Write([UInt16]32)
    $writer.Write([UInt32]$pngBytes.Length)
    $writer.Write([UInt32]22)
    $writer.Write($pngBytes)
}
finally {
    $writer.Dispose()
    $fileStream.Dispose()
    $pngStream.Dispose()
    $textBrush.Dispose()
    $font.Dispose()
    $cyanBrush.Dispose()
    $violetBrush.Dispose()
    $folderPen.Dispose()
    $folderBrush.Dispose()
    $rimPen.Dispose()
    $backgroundBrush.Dispose()
    $folderPath.Dispose()
    $tab.Dispose()
    $backgroundPath.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
}

Write-Host "Generated Ghost FTP application icon: $fullPath"
