# Gera assets/primal.ico: a chama com chifres do Ifrit, o primeiro Primal do jogo, nas cores do medidor.
# Tamanhos 16, 24, 32, 48, 64 e 256 px, em PNG dentro do .ico. Também salva assets/primal.png (256 px).
# Uso: powershell -ExecutionPolicy Bypass -File tools/make-icon.ps1
Add-Type -AssemblyName System.Drawing
$assets = Join-Path $PSScriptRoot "..\assets"
$sizes = 16, 24, 32, 48, 64, 256

function P([double]$x, [double]$y, [double]$s) { New-Object System.Drawing.PointF ([float]($x * $s)), ([float]($y * $s)) }

# caminho feito de curvas de Bézier, em coordenadas de 0 a 1
function Shape([double[][]]$curves, [double]$s) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    foreach ($c in $curves) {
        $path.AddBezier((P $c[0] $c[1] $s), (P $c[2] $c[3] $s), (P $c[4] $c[5] $s), (P $c[6] $c[7] $s))
    }
    $path.CloseFigure()
    return $path
}

function Draw([int]$s) {
    $bmp = New-Object System.Drawing.Bitmap $s, $s
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::Transparent)

    # fundo: quadrado arredondado escuro
    $r = [Math]::Max(3, $s * 0.22); $w = $s - 1
    $bg = New-Object System.Drawing.Drawing2D.GraphicsPath
    $bg.AddArc(0, 0, $r, $r, 180, 90); $bg.AddArc($w - $r, 0, $r, $r, 270, 90)
    $bg.AddArc($w - $r, $w - $r, $r, $r, 0, 90); $bg.AddArc(0, $w - $r, $r, $r, 90, 90); $bg.CloseFigure()
    $g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 15, 19, 25))), $bg)

    # chifres (atrás da chama), curvados para fora e para cima
    $horn = [System.Drawing.Color]::FromArgb(255, 232, 214, 186)
    $hornL = Shape @(
        @(0.36, 0.52, 0.22, 0.48, 0.12, 0.36, 0.10, 0.12),
        @(0.10, 0.12, 0.20, 0.28, 0.30, 0.34, 0.42, 0.38),
        @(0.42, 0.38, 0.40, 0.44, 0.38, 0.48, 0.36, 0.52)) $s
    $hornR = Shape @(
        @(0.64, 0.52, 0.78, 0.48, 0.88, 0.36, 0.90, 0.12),
        @(0.90, 0.12, 0.80, 0.28, 0.70, 0.34, 0.58, 0.38),
        @(0.58, 0.38, 0.60, 0.44, 0.62, 0.48, 0.64, 0.52)) $s
    $hornBrush = New-Object System.Drawing.SolidBrush $horn
    $g.FillPath($hornBrush, $hornL); $g.FillPath($hornBrush, $hornR)

    # chama externa (laranja do medidor, mais escura embaixo)
    $outer = Shape @(
        @(0.50, 0.90, 0.30, 0.90, 0.21, 0.74, 0.26, 0.58),
        @(0.26, 0.58, 0.30, 0.47, 0.37, 0.42, 0.38, 0.30),
        @(0.38, 0.30, 0.44, 0.38, 0.45, 0.44, 0.47, 0.48),
        @(0.47, 0.48, 0.48, 0.36, 0.51, 0.24, 0.57, 0.12),
        @(0.57, 0.12, 0.61, 0.30, 0.71, 0.40, 0.74, 0.55),
        @(0.74, 0.55, 0.79, 0.73, 0.69, 0.90, 0.50, 0.90)) $s
    $rect = New-Object System.Drawing.RectangleF 0, ([float]($s * 0.1)), $s, ([float]($s * 0.82))
    $fire = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.Color]::FromArgb(255, 255, 170, 80)), ([System.Drawing.Color]::FromArgb(255, 214, 72, 38)), 90.0
    $g.FillPath($fire, $outer)

    # miolo amarelo
    $inner = Shape @(
        @(0.50, 0.84, 0.39, 0.84, 0.35, 0.74, 0.38, 0.64),
        @(0.38, 0.64, 0.41, 0.56, 0.46, 0.54, 0.49, 0.44),
        @(0.49, 0.44, 0.55, 0.54, 0.64, 0.60, 0.63, 0.70),
        @(0.63, 0.70, 0.62, 0.80, 0.58, 0.84, 0.50, 0.84)) $s
    $g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 255, 222, 140))), $inner)

    $g.Dispose()
    return $bmp
}

$pngs = @()
foreach ($s in $sizes) {
    $bmp = Draw $s
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngs += , $ms.ToArray()
    if ($s -eq 256) { $bmp.Save((Join-Path $assets "primal.png"), [System.Drawing.Imaging.ImageFormat]::Png) }
    $bmp.Dispose()
}

$out = Join-Path $assets "primal.ico"
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
