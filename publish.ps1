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

    # Automatización de firma digital de código para evitar falsos positivos
    Write-Host "`nVerificando certificado de firma de código..." -ForegroundColor Cyan
    $cert = Get-ChildItem -Path Cert:\CurrentUser\My -CodeSigningCert -ErrorAction SilentlyContinue | Where-Object { $_.Subject -like "*CN=DynamicIsland*" } | Select-Object -First 1

    if ($null -eq $cert) {
        Write-Host "Generando certificado autofirmado de código (CN=DynamicIsland)..." -ForegroundColor Cyan
        $cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject "CN=DynamicIsland" -CertStoreLocation "Cert:\CurrentUser\My"
    }

    if (Test-Path "./publish/DynamicIsland.exe") {
        Write-Host "Firmando digitalmente ./publish/DynamicIsland.exe..." -ForegroundColor Cyan
        Set-AuthenticodeSignature -FilePath "./publish/DynamicIsland.exe" -Certificate $cert | Out-Null
        Write-Host "Firma digital aplicada correctamente a DynamicIsland.exe." -ForegroundColor Green
    }

    # Intentar compilar instalador con Inno Setup si ISCC está instalado
    $isccPaths = @(
        "iscc",
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    )
    $isccExe = $null
    foreach ($path in $isccPaths) {
        if ((Get-Command $path -ErrorAction SilentlyContinue) -or (Test-Path $path -ErrorAction SilentlyContinue)) {
            $isccExe = $path
            break
        }
    }

    if ($isccExe) {
        Write-Host "`nCompilando instalador Inno Setup con $isccExe..." -ForegroundColor Cyan
        & $isccExe "./installer/setup.iss"
    }

    # Firmar el instalador si fue generado o ya existe
    $installerPath = "./publish_installer/DynamicIsland-Setup-v1.0.0.exe"
    if (Test-Path $installerPath) {
        Write-Host "Firmando digitalmente $installerPath..." -ForegroundColor Cyan
        Set-AuthenticodeSignature -FilePath $installerPath -Certificate $cert | Out-Null
        Write-Host "Firma digital aplicada correctamente al instalador." -ForegroundColor Green
    }
} else {
    Write-Host "`nError durante la publicación de la aplicación." -ForegroundColor Red
    exit $LASTEXITCODE
}
