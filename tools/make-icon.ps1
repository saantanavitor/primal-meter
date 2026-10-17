# Gera assets/rhalgr.ico (16, 24, 32, 48, 64, 256 px, em PNG dentro do .ico).
# Uso: powershell -ExecutionPolicy Bypass -File tools/make-icon.ps1
Add-Type -AssemblyName System.Drawing
$out = Join-Path $PSScriptRoot "..\assets\rhalgr.ico"
$sizes = 16, 24, 32, 48, 64, 256
$pngs = @()
foreach ($s in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap $s, $s
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::Transparent)
    $r = [Math]::Max(2, $s * 0.22)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $w = $s - 1
    $path.AddArc(0, 0, $r, $r, 180, 90); $path.AddArc($w - $r, 0, $r, $r, 270, 90)
    $path.AddArc($w - $r, $w - $r, $r, $r, 0, 90); $path.AddArc(0, $w - $r, $r, $r, 90, 90); $path.CloseFigure()
    $g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 15, 19, 25))), $path)
    # três barras do medidor, a do meio em laranja
    $pad = $s * 0.2; $gap = $s * 0.06; $bw = ($s - 2 * $pad - 2 * $gap) / 3; $base = $s - $pad
    $hs = 0.42, 0.62, 0.30
    $cols = [System.Drawing.Color]::FromArgb(255, 91, 141, 239), [System.Drawing.Color]::FromArgb(255, 240, 138, 62), [System.Drawing.Color]::FromArgb(255, 79, 191, 122)
    for ($i = 0; $i -lt 3; $i++) {
        $h = ($s - 2 * $pad) * $hs[$i] / 0.62
        $x = $pad + $i * ($bw + $gap)
        $g.FillRectangle((New-Object System.Drawing.SolidBrush $cols[$i]), [float]$x, [float]($base - $h), [float]$bw, [float]$h)
    }
    $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngs += , $ms.ToArray()
    $bmp.Dispose()
}
$fs = [System.IO.File]::Create($out)
$bw = New-Object System.IO.BinaryWriter $fs
$bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]; $d = if ($s -ge 256) { 0 } else { $s }
    $bw.Write([byte]$d); $bw.Write([byte]$d); $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([UInt16]1); $bw.Write([UInt16]32)
    $bw.Write([UInt32]$pngs[$i].Length); $bw.Write([UInt32]$offset)
    $offset += $pngs[$i].Length
}
foreach ($p in $pngs) { $bw.Write($p) }
$bw.Close()
Write-Output "Ícone gerado em $out"
