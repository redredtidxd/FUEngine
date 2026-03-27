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

## Requisitos (solo si vas a compilar desde el código)

- **Windows** (el editor usa WPF).
- **[.NET SDK 8 para Windows](https://dotnet.microsoft.com/download/dotnet/8.0)** — el paquete llamado **SDK**, no solo el *runtime* de escritorio. Tras instalar, en una **nueva** ventana de terminal debe funcionar `dotnet --version` (debería mostrar **8.x**).

Si **`publicar.bat`** muestra *«dotnet no se reconoce…»*, el SDK no está instalado o la terminal se abrió **antes** de instalarlo: instala el SDK, cierra todas las ventanas de CMD/PowerShell y vuelve a ejecutar el script (o reinicia el PC si el PATH no se actualiza).

---

## Ejecutar el motor y compilar: no lo mezcles

| Qué quieres | Qué hacer |
|-------------|-----------|
| **Solo usar el editor** | Ejecuta **`FUEngine.exe`** (doble clic o desde la carpeta donde esté). Eso **no** compila nada: solo arranca el programa ya construido. |
| **Generar el ejecutable a partir del código fuente** (clonaste el repo y quieres el `.exe`) | 1) Entra en la carpeta **`tools`** de este repositorio.<br>2) Ejecuta **`publicar.bat`**.<br>3) Al terminar bien, el script **abre el Explorador de archivos** en la carpeta de salida (`publish\Release_AAAAMMDD_HHMMSS\`).<br>4) Ahí está **`FUEngine.exe`**: ejecútalo desde esa carpeta.<br><br>Cada publicación usa una carpeta nueva con fecha y hora para que puedas seguir con el editor abierto sin bloquear archivos. |

**Resumen:** compilar/publicar el motor en este repo = **`tools\publicar.bat`** → se abre la carpeta del build → **`FUEngine.exe`**. No es lo mismo que tener ya un `.exe` y solo abrirlo.

---

## Desarrollo (opcional): ejecutar sin publicar

Si estás **modificando el código** del motor y quieres probar cambios rápido sin pasar por `publicar.bat`:

- Desde la **raíz del repositorio**:

```powershell
dotnet restore FUEngine.sln
dotnet run --project FUEngine\FUEngine.csproj -c Release
```

- Con **Visual Studio** o **JetBrains Rider**: abre `FUEngine.sln`, proyecto de inicio **FUEngine**, **F5**.

Tras un `dotnet build`, también puedes lanzar el `.exe` bajo `FUEngine\bin\Release\net8.0-windows\` (build normal, no autocontenido como `publicar.bat`).

---

## Más scripts en `tools\`

Los `.bat` asumen **Windows**; por dentro pasan a la raíz del repo.

| Script | Qué hace |
|--------|----------|
| **`tools\publicar.bat`** | Publica **`FUEngine.exe`** listo para usar: `dotnet publish` Release, **win-x64**, **self-contained**. Crea `publish\Release_AAAAMMDD_HHMMSS\` y **abre esa carpeta** al terminar. |
| **`tools\release_publish.bat`** | Ayuda a cerrar `FUEngine.exe` y a desbloquear `publish` si hace falta antes de borrar carpetas a mano. |
| **`tools\limpiar.bat`** | Borra `bin`, `obj`, `publish` en los proyectos y la carpeta `publish` en la raíz, y `.vs`. Luego vuelve a compilar con **`publicar.bat`** o `dotnet build` / Visual Studio. |

---

## Estructura del código

- **FUEngine** – Aplicación editor (WPF)
- **FUEngine.Core** – Motor: mapa, objetos, scripts, animación, proyecto
- **FUEngine.Editor** – Serialización y DTO (JSON)
- **FUEngine.Runtime** – Tiempo de ejecución del juego (GameLoop, Lua, APIs)
- **FUEngine.Graphics.Vulkan** – Backend gráfico Vulkan
