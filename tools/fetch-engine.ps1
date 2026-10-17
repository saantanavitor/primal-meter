# Baixa e corrige o motor do ACT (FFXIV_ACT_Plugin) em external_dependencies/, necessário para compilar.
# O motor não é redistribuído: em execução, cada pessoa baixa o seu em %LOCALAPPDATA%\RhalgrMeter\engine.
# Uso: powershell -ExecutionPolicy Bypass -File tools/fetch-engine.ps1
$root = Split-Path $PSScriptRoot -Parent
dotnet run --project "$root\src\EngineSetup.Cli" -v q -- "$root\external_dependencies"
exit $LASTEXITCODE
