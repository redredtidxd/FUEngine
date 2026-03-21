@echo off
setlocal
cd /d "%~dp0\.."
echo Limpiando carpetas de compilacion (bin, obj, publish, .vs)...
for %%D in (FUEngine FUEngine.Core FUEngine.Editor FUEngine.Runtime) do (
    if exist "%%D\bin" rd /s /q "%%D\bin" && echo   Eliminado: %%D\bin
    if exist "%%D\obj" rd /s /q "%%D\obj" && echo   Eliminado: %%D\obj
    if exist "%%D\publish" rd /s /q "%%D\publish" && echo   Eliminado: %%D\publish
)
if exist ".vs" rd /s /q ".vs" && echo   Eliminado: .vs
if exist "publish" rd /s /q "publish" && echo   Eliminado: publish
echo.
echo Listo. Vuelve a compilar con: dotnet build -c Release
pause
