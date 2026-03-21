@echo off
setlocal
cd /d "%~dp0\.."
echo Generando FUEngine.exe (ultima version)...

REM Carpeta con fecha y hora para no bloquear si FUEngine esta abierto
for /f "usebackq" %%t in (`powershell -NoProfile -Command "Get-Date -Format 'yyyyMMdd_HHmmss'"`) do set STAMP=%%t
set OUTDIR=publish\Release_%STAMP%

echo Salida: %OUTDIR%
dotnet publish FUEngine\FUEngine.csproj -c Release -r win-x64 --self-contained -o "%OUTDIR%"
if %ERRORLEVEL% equ 0 (
    echo.
    echo Listo. Ejecutable en: %OUTDIR%\FUEngine.exe
    echo Puedes tener FUEngine abierto: cada publicacion genera una carpeta nueva.
    start "" "%OUTDIR%"
) else (
    echo Error al compilar. Cierra FUEngine si esta abierto e intentalo de nuevo.
)
pause
