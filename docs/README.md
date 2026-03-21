# FUEngine 0.0.1

Motor pixel art para Freddy's UnWired.

## Cómo abrir la app

### Desde la terminal (desarrollo)
```bash
cd FUEngine
dotnet run --project FUEngine
```

### Desde Visual Studio / Rider
Abre `FUEngine.sln`, marca el proyecto **FUEngine** como inicio y pulsa F5 (o Run).

### Ejecutable publicado
Desde la raíz del repositorio ejecuta `publicar.bat`. Cada vez se crea una carpeta nueva (`publish\Release_AAAAMMDD_HHMMSS`) con el .exe actualizado; no hace falta cerrar FUEngine. Si publicas a mano con `dotnet publish ... -o publish`, cierra FUEngine antes o los archivos pueden estar en uso. Luego ejecuta el .exe de la carpeta generada.

Si no puedes eliminar la carpeta `publish` (dice que está en uso): ejecuta `liberar_publish.bat` para cerrar FUEngine. Si sigue bloqueada, cierra el Explorador que tenga esa carpeta abierta o el Administrador de tareas (busca FUEngine, dotnet o msbuild).

**Importante:** el .exe es una “foto” del código en el momento en que ejecutas el comando. Si modificas el proyecto, tienes que volver a ejecutar `dotnet publish` para que el .exe tenga la última versión.
