@echo off
echo Liberando carpeta publish...
echo.

taskkill /IM FUEngine.exe /F 2>nul
if %ERRORLEVEL% equ 0 (
    echo FUEngine.exe cerrado. Ya puedes eliminar la carpeta publish.
) else (
    echo FUEngine no estaba en ejecucion.
    echo.
    echo Si aun no puedes borrar publish, puede estar abierta por:
    echo - Explorador de archivos: cierra la ventana que muestre esa carpeta.
    echo - Cursor / Visual Studio: cierra el proyecto o cualquier archivo dentro de publish.
    echo - Otro proceso: abre el Administrador de tareas (Ctrl+Shift+Esc),
    echo   busca FUEngine, dotnet o msbuild y finaliza la tarea.
)
echo.
pause
