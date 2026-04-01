# Crea un .zip con el contenido de $Source (sin incluir la carpeta raíz como segmento).
param(
    [Parameter(Mandatory = $true)][string]$Source,
    [Parameter(Mandatory = $true)][string]$DestZip
)
$ErrorActionPreference = "Stop"
if (-not (Test-Path $Source)) { throw "No existe la carpeta de origen: $Source" }
Add-Type -AssemblyName System.IO.Compression.FileSystem
if (Test-Path $DestZip) { Remove-Item $DestZip -Force }
[System.IO.Compression.ZipFile]::CreateFromDirectory($Source, $DestZip, [System.IO.Compression.CompressionLevel]::Optimal, $false)
