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
| Plugins y extensiones | Puedes crear plugins y assets; la licencia indica condiciones de integridad y posibles acuerdos comerciales con el titular. |
| Distribución | Los proyectos compilados deben cumplir la licencia si se distribuyen comercialmente; el código fuente o derivados deben incluir la licencia y el aviso de copyright. |
| Garantía | El software se ofrece **tal cual**, sin garantías. |
| Contacto | Dudas comerciales o de licencia: ver sección 9 de [LICENSE.md](LICENSE.md). |

Si clonas este repositorio desde GitHub, revisa siempre **LICENSE.md** antes de publicar un juego o producto comercial.

---

## Usar el editor (sin compilar ni instalar .NET)

Quien **solo prueba** el motor hace doble clic en **`InstalarFUEngine.exe`** en la **raíz del repositorio** (un solo ejecutable autocontenido). Genera o actualiza ese archivo con **`dotnet` SDK** y `installer\build-installer.ps1`; el staging del publish queda en **`FUEngine.Installer\publish\`** (también un solo `.exe`). Cada build empaqueta el motor dentro del instalador; al instalar o actualizar se reemplaza la carpeta de destino por completo. **No** hace falta .NET en el PC del usuario final ni carpeta `Payload` aparte.

1. Ejecuta el instalador (elige carpeta; por defecto Archivos de programa). Antes de copiar el motor puedes marcar dependencias opcionales (Visual C++ x64, DirectX web, comprobar .NET 8 Desktop). Al terminar se abre el Explorador en esa carpeta. Actualiza una instalación anterior en la misma ruta (mismo producto en el Panel de control). También puedes usar un **zip** con **`FUEngine.exe`** y DLLs y ejecutarlo sin instalador.
2. Abre **`FUEngine.exe`**. El runtime va **incluido** en el build autocontenido para Windows x64.

**Resumen:** usar el motor = ejecutar **`FUEngine.exe`** (o el instalador de un solo archivo). Eso no compila el código fuente del motor.

---

## Requisitos (solo si vas a compilar el motor desde el código)

- **Windows** (el editor usa WPF).
- **[.NET SDK 8 para Windows](https://dotnet.microsoft.com/download/dotnet/8.0)** — el paquete llamado **SDK**, no solo el *runtime* de escritorio. Tras instalar, en una **nueva** ventana de terminal debe funcionar `dotnet --version` (debería mostrar **8.x**).

Si al compilar aparece *«dotnet no se reconoce…»*, el SDK no está instalado o la terminal se abrió **antes** de instalarlo: instala el SDK, cierra todas las ventanas de CMD/PowerShell y vuelve a intentar (o reinicia el PC si el PATH no se actualiza).

---

## Compilar el motor y crear el paquete de instalación (mantenedores)

La versión del producto vive en **[Directory.Build.props](Directory.Build.props)** (`<Version>`).

| Objetivo | Qué hacer |
|----------|-----------|
| **Instalador para distribuir** (usuarios sin SDK) | `powershell -ExecutionPolicy Bypass -File installer\build-installer.ps1`. Publica **`InstalarFUEngine.exe`** (single-file) en **`FUEngine.Installer\publish\`** y lo copia a la **raíz del repo**. Instala el motor completo a partir de **`dotnet publish`** del editor: el contrato de assets vive en **`FUEngine\FUEngine.csproj`** y usa una **cosecha amplia** de archivos no-fuente del proyecto. En la práctica, casi cualquier archivo nuevo del árbol `FUEngine\` entra en output/publish sin tocar el instalador; se excluyen código, XAML, `bin/`, `obj/`, metadatos de build y el `Lua.xshd` especial, que sigue como recurso embebido + archivo copiado. Las carpetas vacías no se publican solas: deben contener al menos un archivo o crearse en runtime/instalación. El script hace **smoke test** sobre el payload del motor antes de dar el instalador por bueno (`FUEngine.exe`, `Resources\Lua.xshd`, `Templates\`). Opciones en pantalla: **VC++ 2015-2022 x64**, **DirectX End-User Runtime** (web) y **comprobar .NET 8 Desktop** (abrir descarga si falta). Menú **Inicio → Red Redtid → FUEngine**, acceso directo al escritorio y asociación **`.FUE`** / **`.fueproj`**. El motor publicado **incluye .NET**; el Desktop Runtime no es obligatorio para el editor autocontenido. |
| **Solo carpeta con `.exe` autocontenido** (sin instalador) | `dotnet publish FUEngine\FUEngine.csproj -c Release -r win-x64 --self-contained -o <carpeta>` |

**Nota:** Para el `.exe` que **distribuyes**, usa **`installer\build-installer.ps1`** (incluye FUEngine dentro del instalador). Un build Debug del proyecto Installer desde el IDE no sirve como paquete final. Si añades carpetas o assets nuevos del motor, normalmente **no** tocas el instalador: el `publish` ya cosecha casi cualquier archivo no-fuente del proyecto `FUEngine`.

Quien **construye** el paquete solo necesita **SDK 8**. El código del instalador está en el repo (`FUEngine.Installer\` + `installer\zip_payload.ps1`). **`InstalarFUEngine.exe`** en la raíz y **`FUEngine.Installer\publish\`** están en `.gitignore`. Para **Releases**, distribuye **`InstalarFUEngine.exe`**.

**Datos del usuario:** el instalador reemplaza el programa, no tus proyectos. Preferencias, logs y cachés viven en **`%LocalAppData%\FUEngine`**. La carpeta por defecto de proyectos depende de la configuración del editor; si no se ha personalizado, el motor usa una raíz dentro del perfil del usuario (no `Program Files`).

---

## Desarrollo: ejecutar sin publicar autocontenido

Si estás **modificando el código** del motor y quieres probar cambios rápido:

- Desde la **raíz del repositorio**:

```powershell
dotnet restore FUEngine.sln
dotnet run --project FUEngine\FUEngine.csproj -c Release
```

- Con **Visual Studio** o **JetBrains Rider**: abre `FUEngine.sln`, proyecto de inicio **FUEngine**, **F5**.

Tras un `dotnet build`, también puedes lanzar el `.exe` bajo `FUEngine\bin\Release\net8.0-windows\` (build normal, no autocontenido como el `publish` de release).

---

## Scripts locales (`tools\`)

La carpeta **`tools\`** está en **`.gitignore`**: no forma parte del repositorio. Puedes crear ahí tus propios `.bat` o scripts (por ejemplo un `publicar.bat` que llame a `dotnet publish` con una carpeta con fecha, o accesos rápidos personales). Cada colaborador mantiene la suya en local.

---

## Estructura del código

- **FUEngine** – Aplicación editor (WPF)
- **FUEngine.Core** – Motor: mapa, objetos, scripts, animación, proyecto
- **FUEngine.Service** – Contratos de servicio: interfaces y DTOs (audio, autoguardado, build, scripting…)
- **FUEngine.Editor** – Serialización y DTO (JSON)
- **FUEngine.Runtime** – Tiempo de ejecución del juego (GameLoop, Lua, APIs)
- **FUEngine.Graphics.Vulkan** – Backend gráfico Vulkan
- **FUEngine.Installer** (raíz del repo, junto a `FUEngine.sln`) – Código del instalador; `dotnet publish` single-file en `publish/`; el script copia **`InstalarFUEngine.exe`** a la raíz del repo (motor embebido en el ensamblado del instalador)
- **installer/** – `build-installer.ps1`, `zip_payload.ps1` (empaquetado interno del motor para el DLL del instalador)
