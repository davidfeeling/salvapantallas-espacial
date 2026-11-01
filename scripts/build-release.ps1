$ErrorActionPreference = "Stop"

$rootDir = Split-Path -Parent $PSScriptRoot
$distDir = Join-Path $rootDir "dist"
$payloadDir = Join-Path $rootDir "src\CosmosInstaller\Payload"

Write-Host "==> [1/4] Limpiando carpeta de distribución ($distDir)..." -ForegroundColor Cyan
if (Test-Path $distDir) { Remove-Item $distDir -Recurse -Force }
if (Test-Path $payloadDir) { Remove-Item $payloadDir -Recurse -Force }
New-Item -ItemType Directory -Path $distDir | Out-Null
New-Item -ItemType Directory -Path $payloadDir | Out-Null

Write-Host "==> [2/4] Publicando CosmosScreensaver (Single-File .scr)..." -ForegroundColor Cyan
$scrPublishDir = Join-Path $rootDir "src\CosmosScreensaver\bin\Publish"
dotnet publish "$rootDir\src\CosmosScreensaver\CosmosScreensaver.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $scrPublishDir

$scrExe = Join-Path $scrPublishDir "CosmosScreensaver.exe"
$distScr = Join-Path $distDir "CosmosScreensaver.scr"
$payloadScr = Join-Path $payloadDir "CosmosScreensaver.scr"

Copy-Item $scrExe $distScr -Force
Copy-Item $scrExe $payloadScr -Force

Write-Host "==> [3/4] Publicando Instalador CosmosInstaller.exe con Payload embebido..." -ForegroundColor Cyan
$installerPublishDir = Join-Path $rootDir "src\CosmosInstaller\bin\Publish"
dotnet publish "$rootDir\src\CosmosInstaller\CosmosInstaller.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -p:PublishSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $installerPublishDir

Copy-Item (Join-Path $installerPublishDir "CosmosInstaller.exe") (Join-Path $distDir "CosmosInstaller.exe") -Force

Write-Host "==> [4/4] ¡Empaquetado completado con éxito!" -ForegroundColor Green
Get-ChildItem $distDir | Select-Object Name, Length, LastWriteTime
