# Generates assets/app.ico — the WT Launcher application icon.
#
# Design (single source of truth):
#   - Glyph: lucide "terminal" (M4 17l6-6-6-6M12 19h8), stroke-based.
#   - Brand color: LauncherDesignTokens.PrimaryColor (#2563EB).
#   - Rounded-square background + white terminal glyph, multi-size 16..256.
#
# Output is committed; re-run only when the design changes.
# Requires Windows PowerShell (System.Drawing).

param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Add-Type -AssemblyName System.Drawing

$repoRoot = Split-Path -Parent $PSScriptRoot
# Single source of the icon: the App project's Assets folder. Used both as the
# exe ApplicationIcon and as the AvaloniaResource backing Window.Icon.
$outPath = Join-Path $repoRoot 'src\avalonia-launcher\src\WTLauncher.App\Assets\app.ico'

$brandColor = [System.Drawing.Color]::FromArgb(0xFF, 0x25, 0x63, 0xEB) # LauncherDesignTokens.PrimaryColor
$glyphColor = [System.Drawing.Color]::White
$sizes = 16, 24, 32, 48, 64, 128, 256

# lucide "terminal" viewBox 24x24, stroke width 2, round caps/joins
function New-TerminalGlyphPath {
    param([float]$Scale)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $pen = New-Object System.Drawing.Pen $glyphColor, (2 * $Scale)
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $pen.Width = 2 * $Scale
    # M4 17 l6-6 -6-6   (left chevron)
    $path.AddLine(4 * $Scale, 17 * $Scale, 10 * $Scale, 11 * $Scale)
    $path.AddLine(10 * $Scale, 11 * $Scale, 4 * $Scale, 5 * $Scale)
    # M12 19 h8         (prompt cursor)
    $path.AddLine(12 * $Scale, 19 * $Scale, 20 * $Scale, 19 * $Scale)
    $path.Widen($pen)
    return $path
}

function New-RoundedRectPath {
    param([float]$Size, [float]$Radius)
    $r = $Radius * $Size
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = 2 * $r
    $path.AddArc(0, 0, $d, $d, 180, 90)
    $path.AddArc($Size - $d, 0, $d, $d, 270, 90)
    $path.AddArc($Size - $d, $Size - $d, $d, $d, 0, 90)
    $path.AddArc(0, $Size - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-GlyphBitmap {
    param([int]$Size)
    $bmp = New-Object System.Drawing.Bitmap $Size, $Size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

    $bg = New-RoundedRectPath $Size 0.22
    $bgBrush = New-Object System.Drawing.SolidBrush $brandColor
    $g.FillPath($bgBrush, $bg)

    $glyph = New-TerminalGlyphPath ($Size / 24.0)
    $glyphBrush = New-Object System.Drawing.SolidBrush $glyphColor
    $g.FillPath($glyphBrush, $glyph)

    $bgBrush.Dispose(); $glyphBrush.Dispose(); $bg.Dispose(); $glyph.Dispose()
    $g.Dispose()
    return $bmp
}

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $outPath) | Out-Null

# ICO container: header + per-size directory entries + PNG image data.
$entries = foreach ($size in $sizes) {
    $bmp = New-GlyphBitmap $size
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    @{ Size = $size; Data = $ms.ToArray() }
}

$count = $entries.Count
$headerSize = 6 + 16 * $count
$offset = $headerSize
$ms = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter $ms
$bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]$count)
foreach ($entry in $entries) {
    $iconSize = if ($entry.Size -ge 256) { 0 } else { $entry.Size }
    $bw.Write([Byte]$iconSize); $bw.Write([Byte]$iconSize)
    $bw.Write([Byte]0); $bw.Write([Byte]0)          # color count, reserved
    $bw.Write([UInt16]1); $bw.Write([UInt16]32)     # planes, bit count
    $bw.Write([UInt32]$entry.Data.Length); $bw.Write([UInt32]$offset)
    $offset += $entry.Data.Length
}
foreach ($entry in $entries) { $bw.Write($entry.Data) }
$bw.Flush()
[System.IO.File]::WriteAllBytes($outPath, $ms.ToArray())
$bw.Dispose(); $ms.Dispose()

Write-Output "Wrote $outPath ($count sizes: $($sizes -join ', '))"
