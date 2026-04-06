#Requires -Version 5.1
# Una vez por clon: activa las reglas de omision del archivo "gitignore" (sin punto) y
# configura Git LFS para InstalarFUEngine.exe sin subir .gitignore ni .gitattributes al remoto.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$excludeFile = Join-Path $root "gitignore"
if (-not (Test-Path -LiteralPath $excludeFile)) {
    throw "No se encontro 'gitignore' en la raiz del repo (se versiona sin punto en el nombre)."
}

git config core.excludesfile (Resolve-Path -LiteralPath $excludeFile).Path
git lfs install
git lfs track "InstalarFUEngine.exe"
Write-Host "Listo: core.excludesfile apunta a gitignore; LFS rastrea InstalarFUEngine.exe ( .gitattributes local queda ignorado por Git )." -ForegroundColor Green
