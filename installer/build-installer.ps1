#Requires -Version 5.1
# Single-file a FUEngine.Installer/publish/, copia InstalarFUEngine.exe a la raiz del repo.
# Requisitos: .NET SDK 8.

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$propsPath = Join-Path $root "Directory.Build.props"
if (-not (Test-Path $propsPath)) {
    throw "No se encontro Directory.Build.props en la raiz del repo."
}
$propsText = Get-Content $propsPath -Raw
if ($propsText -notmatch '<Version>([^<]+)</Version>') {
    throw "No se pudo leer <Version> en Directory.Build.props."
}
$version = $Matches[1].Trim()

$publishDir = Join-Path $root "FUEngine.Installer\publish"
$engineStage = Join-Path $root "obj\engine_publish_stage"
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

Write-Host "dotnet publish -> $publishDir (v $version)..."
dotnet publish "FUEngine.Installer\FUEngine.Installer.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained `
    -o $publishDir `
    -p:DebugType=none `
    -p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$requiredFiles = @(
    (Join-Path $engineStage "FUEngine.exe"),
    (Join-Path $engineStage "Resources\Lua.xshd")
)

foreach ($required in $requiredFiles) {
    if (-not (Test-Path $required)) {
        Write-Error "Smoke test del instalador: falta el archivo obligatorio '$required' en el publish del motor."
        exit 1
    }
}

$requiredDirectories = @(
    (Join-Path $engineStage "Templates")
)

foreach ($requiredDir in $requiredDirectories) {
    if (-not (Test-Path $requiredDir -PathType Container)) {
        Write-Error "Smoke test del instalador: falta la carpeta obligatoria '$requiredDir' en el publish del motor."
        exit 1
    }
}

Write-Host "Smoke test OK: el publish del motor contiene ejecutable, Resources y Templates."

$built = Join-Path $publishDir "InstalarFUEngine.exe"
if (-not (Test-Path $built)) {
    Write-Error "No se genero InstalarFUEngine.exe en $publishDir"
    exit 1
}

$rootExe = Join-Path $root "InstalarFUEngine.exe"
Copy-Item -LiteralPath $built -Destination $rootExe -Force

$legacyFolder = Join-Path $root "InstalarFUEngine"
if (Test-Path $legacyFolder) {
    Remove-Item $legacyFolder -Recurse -Force
}

Get-ChildItem -LiteralPath $publishDir -Force | ForEach-Object {
    if ($_.Name -ine "InstalarFUEngine.exe") {
        Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "Listo: $rootExe"
Write-Host "El instalador no se versiona en Git (tamaño). Para distribuirlo: GitHub Releases u otro canal." -ForegroundColor DarkGray
