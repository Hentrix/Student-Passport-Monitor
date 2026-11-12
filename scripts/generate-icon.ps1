param(
    [string]$Output = (Join-Path $PSScriptRoot '..\assets\student-passport-monitor.ico')
)

# Формируем многомасштабную иконку без внешних графических библиотек.
Add-Type -AssemblyName System.Drawing

function New-RoundRect([single]$x, [single]$y, [single]$w, [single]$h, [single]$r) {
    $p = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $d = $r * 2
    $p.AddArc($x, $y, $d, $d, 180, 90)
    $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $p.CloseFigure()
    return $p
}

function Brush([string]$color) {
    return [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml($color))
}

$images = [System.Collections.Generic.List[byte[]]]::new()
$sizes = @(16, 24, 32, 48, 64, 128, 256)
foreach ($size in $sizes) {
    $bitmap = [System.Drawing.Bitmap]::new($size, $size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.ScaleTransform($size / 256.0, $size / 256.0)

        $background = New-RoundRect 5 5 246 246 54
        $b = Brush '#211c42'
        $graphics.FillPath($b, $background)
        $b.Dispose()
        $pen = [System.Drawing.Pen]::new([System.Drawing.ColorTranslator]::FromHtml('#7865bd'), 5)
        $graphics.DrawPath($pen, $background)
        $pen.Dispose()
        $background.Dispose()

        $passport = New-RoundRect 39 27 131 177 14
        $b = Brush '#7d3459'
        $graphics.FillPath($b, $passport)
        $b.Dispose()
        $passport.Dispose()

        $pen = [System.Drawing.Pen]::new([System.Drawing.ColorTranslator]::FromHtml('#eac37c'), 6)
        $graphics.DrawEllipse($pen, 83, 68, 44, 44)
        $graphics.DrawLine($pen, 105, 75, 105, 104)
        $graphics.DrawLine($pen, 89, 89, 121, 89)
        $graphics.DrawLine($pen, 65, 147, 143, 147)
        $graphics.DrawLine($pen, 75, 163, 133, 163)
        $pen.Dispose()

        $card = New-RoundRect 146 73 84 91 12
        $b = Brush '#eae7ff'
        $graphics.FillPath($b, $card)
        $b.Dispose()
        $card.Dispose()
        $b = Brush '#665e99'
        $graphics.FillEllipse($b, 157, 94, 28, 28)
        $graphics.FillEllipse($b, 151, 125, 40, 20)
        $graphics.FillRectangle($b, 196, 104, 22, 5)
        $graphics.FillRectangle($b, 196, 118, 22, 5)
        $graphics.FillRectangle($b, 196, 132, 17, 5)
        $b.Dispose()

        $check = New-RoundRect 143 158 86 73 16
        $b = Brush '#191a38'
        $graphics.FillPath($b, $check)
        $b.Dispose()
        $pen = [System.Drawing.Pen]::new([System.Drawing.ColorTranslator]::FromHtml('#b49cff'), 6)
        $graphics.DrawPath($pen, $check)
        $pen.Dispose()
        $check.Dispose()
        $pen = [System.Drawing.Pen]::new([System.Drawing.ColorTranslator]::FromHtml('#d3bdff'), 10)
        $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $graphics.DrawLines($pen, [System.Drawing.Point[]]@(
            [System.Drawing.Point]::new(160, 193),
            [System.Drawing.Point]::new(176, 207),
            [System.Drawing.Point]::new(207, 176)
        ))
        $pen.Dispose()

        $stream = [System.IO.MemoryStream]::new()
        try {
            $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
            $images.Add($stream.ToArray())
        }
        finally { $stream.Dispose() }
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

# ICO хранит несколько PNG-изображений и таблицу смещений.
$fullPath = [System.IO.Path]::GetFullPath($Output)
$folder = [System.IO.Path]::GetDirectoryName($fullPath)
[System.IO.Directory]::CreateDirectory($folder) | Out-Null
$stream = [System.IO.File]::Create($fullPath)
$writer = [System.IO.BinaryWriter]::new($stream)
try {
    $writer.Write([uint16]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]$sizes.Count)
    $offset = 6 + 16 * $sizes.Count
    for ($i = 0; $i -lt $sizes.Count; $i++) {
        $s = $sizes[$i]
        $writer.Write([byte]($s % 256))
        $writer.Write([byte]($s % 256))
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]32)
        $writer.Write([uint32]$images[$i].Length)
        $writer.Write([uint32]$offset)
        $offset += $images[$i].Length
    }
    foreach ($bytes in $images) { $writer.Write($bytes) }
}
finally { $writer.Dispose() }
Write-Host "Иконка создана: $fullPath"
