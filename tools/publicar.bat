@echo off
setlocal
cd /d "%~dp0\.."
echo Generando FUEngine.exe (ultima version)...

REM Requisito: .NET SDK en PATH (muy habitual que falle en PCs que solo descargaron el ZIP sin instalar el SDK)
where dotnet >nul 2>&1
if errorlevel 1 goto :NoDotnet
dotnet --version >nul 2>&1
if errorlevel 1 goto :NoDotnet

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
exit /b 0

:NoDotnet
echo.
echo ------------------------------------------------------------------
echo   No se encuentra "dotnet" (SDK de .NET no instalado o no en PATH).
echo ------------------------------------------------------------------
echo.
echo   Para generar FUEngine.exe desde el codigo necesitas el .NET SDK 8,
echo   no solo el runtime ni otro programa.
echo.
echo   1) Descarga e instala el SDK 8 para Windows:
echo      https://dotnet.microsoft.com/download/dotnet/8.0
echo.
echo   2) En el instalador, deja marcada la opcion que anade dotnet al PATH.
echo.
echo   3) Cierra esta ventana, abre de nuevo CMD o PowerShell y ejecuta:
echo        dotnet --version
echo      Debe mostrar 8.x. Luego vuelve a ejecutar publicar.bat.
echo.
echo   Mas detalle: README.md del repositorio, seccion Requisitos.
echo ------------------------------------------------------------------
echo.
pause
exit /b 1
