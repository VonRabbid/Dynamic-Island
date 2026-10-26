# Dynamic Island - Script de Publicación para Producción
$ErrorActionPreference = "Stop"

$userPath = [Environment]::GetEnvironmentVariable("Path", "User")
$machinePath = [Environment]::GetEnvironmentVariable("Path", "Machine")
$dotnetRoot = [Environment]::GetEnvironmentVariable("DOTNET_ROOT", "User")

$env:Path = "$userPath;$machinePath"
if (![string]::IsNullOrEmpty($dotnetRoot)) {
    $env:DOTNET_ROOT = $dotnetRoot
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Dynamic Island - Compilando y Publicando Perfil Release   " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:PublishReadyToRun=true -o ./publish

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nPublicación exitosa! Ejecutable listo en carpeta ./publish" -ForegroundColor Green
    if (Test-Path "./publish/DynamicIsland.exe") {
        $size = (Get-Item "./publish/DynamicIsland.exe").Length / 1MB
        Write-Host ("Tamaño del binario optimizado: {0:N2} MB" -f $size) -ForegroundColor Yellow
    }
} else {
    Write-Host "`nError durante la publicación de la aplicación." -ForegroundColor Red
    exit $LASTEXITCODE
}
