# FUEngine

Micro-motor pixel art para Freddy's UnWired y juegos 2D.

## Licencia

El texto legal completo está en **[LICENSE.md](LICENSE.md)** (FUEngine License Agreement — Copyright © Red Redtid). **No** es MIT ni Apache: es código **source-available** con condiciones propias.

Resumen de puntos clave (no sustituye el archivo de licencia):

| Tema | Contenido |
|------|-----------|
| Uso personal y educativo | Puedes leer, modificar y compilar el código para aprendizaje y uso no comercial. |
| Uso comercial | Sujeto a **reparto de ingresos** sobre el neto del producto hecho con FUEngine: **2 %** para estudios/empresas y **5 %** para desarrolladores individuales; umbrales orientativos en la licencia (p. ej. 2 000 USD por trimestre por juego). Los detalles y excepciones están en [LICENSE.md](LICENSE.md). |
| Modificaciones | Puedes modificar el núcleo (Core, Runtime, Editor, Vulkan). Las versiones derivadas deben indicar que son obras derivadas, no incluir código malicioso y no eludir las condiciones comerciales. |
| Plugins y extensiones | Puedes crear plugins o assets; la licencia indica condiciones de integridad y posibles acuerdos comerciales con el titular. |
| Distribución | Los proyectos compilados deben cumplir la licencia si se distribuyen comercialmente; el código fuente o derivados deben incluir la licencia y el aviso de copyright. |
| Garantía | El software se ofrece **tal cual**, sin garantías. |
| Contacto | Dudas comerciales o de licencia: ver sección 9 de [LICENSE.md](LICENSE.md). |

Si clonas este repositorio desde GitHub, revisa siempre **LICENSE.md** antes de publicar un juego o producto comercial.

---

## Requisitos para compilar

- **Windows** (el editor usa WPF).
- **[.NET SDK 8](https://dotnet.microsoft.com/download)** (`dotnet --version` debe ser 8.x).

---

## Compilar y usar el motor (editor)

Desde la **raíz del repositorio** (donde está `FUEngine.sln`):

```powershell
dotnet restore FUEngine.sln
dotnet build FUEngine.sln -c Release
```

Ejecutar el editor sin generar carpeta `publish`:

```powershell
dotnet run --project FUEngine\FUEngine.csproj -c Release
```

Con **Visual Studio** o **JetBrains Rider**: abre `FUEngine.sln`, proyecto de inicio **FUEngine**, ejecutar (F5).

Tras compilar, el editor queda en la salida típica de .NET bajo `FUEngine\bin\Release\net8.0-windows\` (puedes lanzar el `.exe` desde ahí o usar `dotnet run` como arriba).

---

## Scripts en `tools\` (compilar, publicar, limpiar)

Todos los `.bat` asumen que los ejecutas desde Windows; internamente cambian a la **raíz del repo**.

| Script | Qué hace |
|--------|----------|
| **`tools\publicar.bat`** | Publica un **FUEngine.exe** listo para distribuir: `dotnet publish` en Release, **win-x64**, **self-contained**. Crea `publish\Release_AAAAMMDD_HHMMSS\` para no pisar builds si el editor sigue abierto. Al terminar, abre esa carpeta. Úsalo cuando quieras un ejecutable portable para usar el motor sin instalar el SDK en otra máquina. |
| **`tools\release_publish.bat`** | Intenta cerrar `FUEngine.exe` y da consejos si la carpeta `publish` sigue bloqueada (Explorador, VS, etc.). Úsalo antes de borrar o vaciar `publish` manualmente. |
| **`tools\limpiar.bat`** | Borra `bin`, `obj`, `publish` en los proyectos del motor y la carpeta `publish` en la raíz, y `.vs`. Reduce tamaño en disco; **después** debes volver a compilar (`dotnet build` o Visual Studio). No borra código fuente. |

**Flujo habitual:** desarrollo con `dotnet run` o F5 → cuando quieras un `.exe` autocontenido, **`tools\publicar.bat`** → si necesitas liberar archivos, **`tools\release_publish.bat`** → si el repo pesa mucho por artefactos, **`tools\limpiar.bat`** y recompilar.

---

## Estructura del código

- **FUEngine** – Aplicación editor (WPF)
- **FUEngine.Core** – Motor: mapa, objetos, scripts, animación, proyecto
- **FUEngine.Editor** – Serialización y DTO (JSON)
- **FUEngine.Runtime** – Tiempo de ejecución del juego (GameLoop, Lua, APIs)
- **FUEngine.Graphics.Vulkan** – Backend gráfico Vulkan
