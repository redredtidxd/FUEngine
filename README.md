# FUEngine

> **Licencia:** este repositorio **no** usa MIT, Apache ni otra licencia “open source” estándar. El código es **source-available** (visible) con condiciones estrictas. **Términos completos:** [LICENSE.md](LICENSE.md) (Copyright © Red Redtid).
>
> - Uso personal y aprendizaje; modificaciones del núcleo permitidas según la licencia.
> - **Productos comerciales** (p. ej. juegos de pago): **reparto de ingresos** hacia el autor del motor (detalle en [LICENSE.md](LICENSE.md)); en builds desde GitHub el pago de regalías es **manual**.
> - **Anuncios:** integrar redes de anuncios en forks/código descargado de GitHub **no** está permitido salvo autorización escrita; solo la **distribución oficial** (p. ej. instalador en sitio oficial) puede incluir publicidad autorizada.
> - **Plugins:** deben ser **gratuitos**; no se permite vender extensiones para el motor sin acuerdo con el titular.
> - La **versión pública** no está conectada a servidores de validación del autor y se ofrece **sin garantías**; la seguridad puede diferir de la build oficial.

Micro-motor pixel art para Freddy's UnWired y juegos 2D.

- **Documentación**: [docs/README.md](docs/README.md)
- **Estado del proyecto**: [docs/STATUS.md](docs/STATUS.md)
- **Licencia y monetización**: [LICENSE.md](LICENSE.md)
- **Publicar ejecutable**: ejecuta `tools\publicar.bat`
- **Liberar carpeta publish**: `tools\release_publish.bat`
- **Reducir tamaño del proyecto** (quitar miles de archivos de compilación): ejecuta `tools\limpiar.bat` (borra `bin/`, `obj/`, `publish/`, `.vs/`). Luego compila de nuevo.

## Estructura

- **FUEngine** – Aplicación editor (WPF): ventanas, paneles, diálogos, servicios
- **FUEngine.Core** – Motor: mapa, objetos, scripts, animación, proyecto
- **FUEngine.Editor** – Serialización y DTO (JSON)
- **FUEngine.Runtime** – Tiempo de ejecución del juego (GameLoop, Lua, APIs)
- **FUEngine.Graphics.Vulkan** – Backend gráfico Vulkan
