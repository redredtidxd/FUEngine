# FUEngine

> **Licencia:** este repositorio **no** usa MIT, Apache ni otra licencia “open source” estándar. El código es **source-available** (visible) con condiciones estrictas. **Términos completos:** [LICENSE.md](LICENSE.md) (Copyright © Red Redtid).
>
> - Uso personal y aprendizaje; modificaciones del núcleo permitidas según la licencia.
> - **Productos comerciales** (p. ej. juegos de pago): **reparto de ingresos** hacia el autor del motor (detalle en [LICENSE.md](LICENSE.md)); en builds desde GitHub el pago de regalías es **manual**.
> - **Anuncios:** integrar redes de anuncios en forks/código descargado de GitHub **no** está permitido salvo autorización escrita; solo la **distribución oficial** (p. ej. instalador en sitio oficial) puede incluir publicidad autorizada.
> - **Plugins:** deben ser **gratuitos**; no se permite vender extensiones para el motor sin acuerdo con el titular.
> - La **versión pública** no está conectada a servidores de validación del autor y se ofrece **sin garantías**; la seguridad puede diferir de la build oficial.

Micro-motor pixel art para Freddy's UnWired y juegos 2D.

## Requisitos para compilar

- **Windows** (el editor usa WPF).
- **[.NET SDK 8](https://dotnet.microsoft.com/download)** instalado (`dotnet --version` debe mostrar 8.x).

## Cómo compilar y ejecutar el editor

Desde la **raíz del repositorio** (donde está `FUEngine.sln`):

```powershell
dotnet restore FUEngine.sln
dotnet build FUEngine.sln -c Release
```

Para arrancar el editor sin publicar:

```powershell
dotnet run --project FUEngine\FUEngine.csproj -c Release
```

Con **Visual Studio** o **Rider**: abre `FUEngine.sln`, establece el proyecto **FUEngine** como proyecto de inicio y ejecuta (F5 / Run).

## Publicar un ejecutable (.exe)

Desde la raíz del repo, ejecuta `tools\publicar.bat`. Genera una carpeta bajo `publish\Release_AAAAMMDD_HHMMSS` con `FUEngine.exe` (win-x64, self-contained). Puedes tener el editor abierto: cada publicación usa una carpeta nueva.

Si necesitas liberar la carpeta `publish` (archivos en uso): `tools\release_publish.bat`. Para borrar artefactos de compilación y aligerar el disco: `tools\limpiar.bat` (elimina `bin/`, `obj/`, `publish/`, `.vs/`); luego vuelve a compilar.

## Estructura del código

- **FUEngine** – Aplicación editor (WPF): ventanas, paneles, diálogos, servicios
- **FUEngine.Core** – Motor: mapa, objetos, scripts, animación, proyecto
- **FUEngine.Editor** – Serialización y DTO (JSON)
- **FUEngine.Runtime** – Tiempo de ejecución del juego (GameLoop, Lua, APIs)
- **FUEngine.Graphics.Vulkan** – Backend gráfico Vulkan
