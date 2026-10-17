# Gera dist/RhalgrMeter.exe: um único arquivo, sem instalador e sem precisar do .NET instalado.
# Uso: powershell -ExecutionPolicy Bypass -File tools/publish.ps1
$root = Split-Path $PSScriptRoot -Parent
dotnet publish "$root\src\RhalgrMeter" -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true `
    -p:DebugType=none -o "$root\dist" -nologo -v q
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$exe = Get-Item "$root\dist\RhalgrMeter.exe"
Write-Output ("Pronto: {0} ({1:N1} MB)" -f $exe.FullName, ($exe.Length / 1MB))
