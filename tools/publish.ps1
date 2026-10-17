# Gera dist/RhalgrMeter.exe: um único arquivo, sem instalador e sem precisar do .NET instalado.
# O motor do ACT não vai dentro do .exe: ele é baixado na primeira execução, na máquina de quem usa.
# Uso: powershell -ExecutionPolicy Bypass -File tools/publish.ps1 [-Out pasta]
param([string]$Out = "")
$root = Split-Path $PSScriptRoot -Parent
if (-not $Out) { $Out = Join-Path $root "dist" }

# o motor só é necessário para compilar (referências); fica fora do .exe
& "$PSScriptRoot\fetch-engine.ps1"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet publish "$root\src\RhalgrMeter" -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true `
    -p:DebugType=none -o $Out -nologo -v q
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# nenhuma DLL do motor pode ter ido parar na saída
if (Get-ChildItem $Out -Filter "FFXIV_ACT_Plugin*" -ErrorAction SilentlyContinue) {
    Write-Error "O motor do ACT foi copiado para $Out; ele não pode ser redistribuído."
    exit 1
}

$exe = Get-Item (Join-Path $Out "RhalgrMeter.exe")
Write-Output ("Pronto: {0} ({1:N1} MB)" -f $exe.FullName, ($exe.Length / 1MB))
