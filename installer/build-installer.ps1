#Requires -Version 5.1
# Publica en una carpeta temporal (no persistente) y deja SOLO InstalarFUEngine.exe en la raíz del repo.
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

$engineStage = Join-Path $root "obj\engine_publish_stage"
$publishDir = Join-Path $root "obj\installer_publish_stage"
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

# Limpieza de publish antiguo (no debe persistir dentro del repo).
$legacyInstallerPublish = Join-Path $root "FUEngine.Installer\publish"
if (Test-Path $legacyInstallerPublish) { Remove-Item $legacyInstallerPublish -Recurse -Force -ErrorAction SilentlyContinue }

Write-Host "dotnet publish -> $publishDir (v $version)..."
$dotnetPublishArgs = @(
    "publish"
    "FUEngine.Installer\FUEngine.Installer.csproj"
    "-c", "Release"
    "-r", "win-x64"
    "--self-contained"
    "-o", $publishDir
    "-p:DebugType=none"
    "-p:DebugSymbols=false"
    "-p:SkipEngineStageCleanup=true"
)
& dotnet @dotnetPublishArgs
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

# Limpieza: no dejar publish duplicado dentro del repo (solo interesa el .exe final en la raíz).
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force -ErrorAction SilentlyContinue
}

# Misma idea que en BundleMotor (csproj): el motor ya va embebido en fue_motor.pack / InstalarFUEngine.exe.
if (Test-Path $engineStage) {
    Remove-Item $engineStage -Recurse -Force -ErrorAction SilentlyContinue
}

# Por si dotnet/otros scripts re-crearan la carpeta antigua, elimínala al final también.
if (Test-Path $legacyInstallerPublish) {
    Remove-Item $legacyInstallerPublish -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "Listo: $rootExe"
Write-Host "Para versionar el instalador con LFS (sin subir .gitignore/.gitattributes): installer\setup-git-local.ps1 si no lo hiciste; luego git add InstalarFUEngine.exe; git commit; git push" -ForegroundColor DarkGray
