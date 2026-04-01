# FUEngine — Referencia para IAs

Motor y editor 2D **tile-based** / **pixel art** (Freddy's UnWired). **Seis proyectos:** `FUEngine.Core`, `FUEngine.Editor`, `FUEngine` (WPF), `FUEngine.Runtime`, `FUEngine.Graphics.Vulkan`, **`FUEngine.Service`** (solo contratos, sin UI). **.NET 8.0**. Nombres y comentarios mezclan español e inglés.

**Índice:** [Cómo leer](#como-leer-doc) · [Proyectos](#proyectos-y-referencias) · [0–11](#0-núcleo-técnico-y-stack) · [Inventarios 12–16](#12-inventario-fuenginecore) · [17–18](#17-archivos-json-típicos) · [19–21](#19-paquetes-nuget-por-proyecto) · [§8.2 Componentes](#82-referencia-de-componentes-instancia--json--runtime) · [§22 Guía IA](#22-guía-ampliada-para-ias-patrones-depuración-y-temas-transversales) · [§23 Más temas editor](#23-más-temas-editor-ui-integridad-y-herramientas) · [§24 Referencia rápida final](#24-referencia-rápida-final-vulkan-audio-ide-y-extras) · [§25 Service](#25-inventario-fuengineservice-y-archivos-no-listados-en-12-16)

<a id="como-leer-doc"></a>

## Cómo leer este documento

Este archivo es **referencia técnica** para quien modifica el motor o automatiza con IA. **No sustituye** el manual orientado a usuarios, que vive **dentro del editor** (menú Ayuda).

| Si necesitas… | Empieza en… |
|---------------|----------------|
| Qué ensamblado hace qué y dependencias | [Proyectos](#proyectos-y-referencias), §0 |
| Mapa, capas, chunks, colisión tiles | §2, §8 |
| JSON en disco, qué pasa si falta un archivo | §3 ([tabla serializadores](#31-tabla-de-serializadores-detalle)), §17 |
| Herramientas del mapa, explorador, mini-IDE Lua | §4, §4.1 |
| Play Mode, runtime, orden de ticks | §9, §9.1 |
| Tablas Lua (`world`, `self`, …) y hooks | §11 |
| «¿Dónde está el código de X?» | §18 ([Buscar por tema](#18-buscar-por-tema)) |
| Lista de `.cs` por proyecto | §12–16, §25 |
| Trampas conocidas (stubs, límites) | §20 |
| Vulkan, NAudio, Spotlight, Hub | §24 |

**Convención:** las secciones **§12–§16** son inventarios de archivos; las **§0–§11** explican comportamiento. **§22–§24** mezclan guías y apuntes finos.

### Documentación en la app (usuarios)

- **Dónde:** menú **Ayuda** (manual rápido/completo, API Lua, logs, GitHub), botón **Ayuda / Guía del motor** en la pantalla de inicio. Host: [`DocumentationHostControl.xaml`](../FUEngine/Controls/DocumentationHostControl.xaml) — pestañas **Manual del motor** · **Lua** (`lua-kw-*`, `lua-guide-*`, `lua-reference-intro`) · **Ejemplos** (`script-ex-*`). **«Completa»** abre el manual largo en `crear-juego` (`FullManualStartTopicId`).
- **Fuentes en código:** [`EngineDocumentation.cs`](../FUEngine/Help/EngineDocumentation.cs) (manual general + `EngineDocumentation.Presentation.cs`), [`LuaReferenceDocumentation.cs`](../FUEngine/Help/LuaReferenceDocumentation.cs), [`ScriptExamplesDocumentation.cs`](../FUEngine/Help/ScriptExamplesDocumentation.cs); todo se une en `BuildTopics()`.
- **Spotlight** (Ctrl+P / Ctrl+Espacio) indexa manual, ejemplos y símbolos Lua/API sin mezclar categorías; detalle en §24.10.
- **Regla:** cualquier cambio visible en menús, inspectores o API Lua debe reflejarse en esos archivos **y** aquí cuando sea comportamiento técnico.

### Manual integrado — detalles de implementación (WPF)

Útil al tocar la ayuda embebida o AvalonEdit:

- **Navegación:** `DocumentationHostControl.Open` elige pestaña 0/1/2; evita `SelectionChanged` duplicados (`_lastDocTabIndex`); Lua y Ejemplos usan `PinIntroPreserveOrder` (intro primero, sin orden alfabético forzado en el resto).
- **Filtros y lista:** `DocumentationView.ApplyFilter` desuscribe `SelectionChanged` durante `Items.Clear`/`Add`; scroll del detalle en `ApplicationIdle`. Overlays capturan clics en el borde interior (`e.Handled = true`).
- **Modelo de tema:** `DocumentationTopic` con `Subtitle`, `EnMotor`, snippets con `LuaExampleCode`, `ExampleSearchTags`, `ExampleDifficulty` (🟢🟡🔴). Manual: `ManualPresentation` + `ApplyManualPresentation`. `Topics` con inicialización diferida (`LazyInitializer` + `BuildTopics()`) para no romper el orden de partials estáticos.
- **AvalonEdit / `LuaFUE`:** `LuaHighlightingLoader` registra `FUEngine.Resources.Lua.xshd`. Con `SyntaxHighlighting` activo, **no** fijar `Foreground` en el `TextEditor` ni heredar del `Window` — usar `ClearValue` en `TextEditor` y `TextArea` (ver §24.3). Ejemplos: bloque código con fondo `#0d1117`, botones **Crear script** / **Copiar**.
- **Scripts:** plantilla nueva `.lua` en [`DefaultLuaScriptTemplate.cs`](../FUEngine/Services/DefaultLuaScriptTemplate.cs). Tras `LuaScriptRuntime.ReloadScript` se invalida la caché de `require` (`LuaRequireSupport`). Búsqueda en panel y Spotlight usa tags y `PorQueImporta` para ejemplos.

### Rutas globales del editor (AppData)

**Datos del editor (no del juego exportado):** `%LocalAppData%/FUEngine/` — `Config/user_preferences.json` (preferencias del editor: tema, idioma, rutas; migración automática desde `settings.json`), `Storage/project_history.json` (Hub recientes; migración desde `recent.json`), `logs/session_*.log` y `logs/crash_*.txt` (autopsia en fallos no capturados), `ProjectThumbs/*.png` (miniatura del mapa tras Guardar todo), carpetas reservadas `GlobalTemplates/` (seeds globales entre proyectos), `Cache/LuaMetadata/` (caché Lua/IDE futura), `Extensions/`, `Vulkan/`. Clase central: `FUEngineAppPaths`.

**Convención del repo:** al cambiar el editor o APIs visibles para el usuario, actualizar **`docs/AI-ONBOARDING.md`** (versionado en Git), **`EngineDocumentation.Topics`** (guía rápida / manual) y, si aplica, **`LuaEditorCompletionCatalog`** (autocompletado del mini-IDE). Apuntes personales opcionales: [`instruccionescursor.md`](../instruccionescursor.md) (ignorado por Git).

**Ejecutar vs compilar el motor (humano, una sola fuente):** el **[README.md](../README.md)** en la raíz del repo explica que **no se debe confundir** abrir `FUEngine.exe` con generar el build. El **instalador** se genera con **`installer/build-installer.ps1`**: deja **`InstalarFUEngine.exe`** (single-file autocontenido) en la **raíz del repo** y el mismo binario en **`FUEngine.Installer/publish/`**; elimina el layout antiguo **`InstalarFUEngine/`** si existía. El motor va **embebido** en el ensamblado del instalador; **Release** obligatorio (`BundleMotor` desactivado en Debug). El payload del motor se publica primero en **`obj/engine_publish_stage/`** y el script ejecuta un **smoke test** mínimo antes de aceptar el instalador (`FUEngine.exe`, `Resources/Lua.xshd`, carpeta `Templates/`). El contrato de qué assets entran en el instalador vive en **`FUEngine/FUEngine.csproj`**: se usa una **cosecha amplia** de archivos no-fuente bajo el proyecto `FUEngine` para que casi cualquier archivo nuevo entre en output/publish sin tocar el instalador; se excluyen código, XAML, `bin/`, `obj/`, metadatos de build y `Lua.xshd`, que sigue además como recurso embebido para AvalonEdit. Las carpetas vacías no se publican por sí solas: deben contener al menos un archivo o crearse después. Al terminar, se abre el Explorador en la carpeta de instalación. En la UI se pueden marcar **dependencias opcionales** antes de copiar: **Visual C++ 2015-2022 x64** (NLua/Vulkan/NAudio), **DirectX End-User Runtime** (instalador web) y **comprobar .NET 8 Desktop** (abrir la página de descarga si falta; el editor publicado sigue siendo **autocontenido** y no lo exige). **Menú Inicio** (`Red Redtid\FUEngine`): motor, carpeta de logs (`%LocalAppData%\FUEngine\logs`), acceso a ayuda en el motor. **Asociación** `.FUE` y `.fueproj` → `FUEngine.exe`. Preferencias, logs y cachés del usuario siguen en **`%LocalAppData%/FUEngine`** (no en Archivos de programa); la raíz por defecto de proyectos también vive fuera de `Program Files` y es configurable desde preferencias del editor. **UI:** [`InstallForm.cs`](../FUEngine.Installer/InstallForm.cs), [`PrerequisiteOptions.cs`](../FUEngine.Installer/PrerequisiteOptions.cs), [`PrerequisitesInstaller.cs`](../FUEngine.Installer/PrerequisitesInstaller.cs), [`ShellFileAssociation.cs`](../FUEngine.Installer/ShellFileAssociation.cs).

---

## Proyectos y referencias

| Proyecto | Referencias | Rol |
|----------|-------------|-----|
| FUEngine.Core | — | Entidades y lógica de dominio (TileMap multicapa, GameObject, componentes, triggers, UI runtime, etc.) |
| FUEngine.Editor | Core | DTOs y Save/Load JSON (mapa con capas/chunks, objetos, proyecto, triggers, seeds, scripts, animaciones, UI, audio, biblioteca global) |
| FUEngine | Core, Editor, Runtime, Service | App WPF: ventanas, tabs, paneles, undo/redo, Play Mode, audio en editor (NAudio), export/build; registra `IEditorLog` y otros contratos vía `ServiceLocator` |
| FUEngine.Runtime | Core, Graphics.Vulkan | GameLoop, LuaScriptRuntime (NLua), Camera, SceneManager, WorldApi/InputApi/…, generación procedural de tiles (Lua), debug draw |
| FUEngine.Graphics.Vulkan | Core | `VulkanGraphicsDevice` — Silk.NET Vulkan + GLFW |
| FUEngine.Service | Core | Contratos de servicio (`IEditorLog`, `ServiceLocator`, interfaces de audio/build/…); sin implementaciones WPF |

**Grafos de dependencias (sin ciclos):** Core → nada. Editor → Core. Service → Core. Graphics.Vulkan → Core. **Runtime → Core + Graphics.Vulkan** (`GameLoop.Start()` crea `VulkanGraphicsDevice.Create()` si no hay dispositivo). La app WPF → Core + Editor + Runtime + Service (no referencia Vulkan directamente; usa el runtime).

---

## 0. Núcleo técnico y stack

- **Lenguaje:** C# (.NET 8). Editor: `net8.0-windows` (WPF). Core/Runtime/Vulkan: `net8.0`.
- **Gráficos:** **Vulkan** (Silk.NET: Vulkan, GLFW, extensiones KHR) para ventana de juego / headless. **WPF** (`Canvas`) para el mapa en el editor. Abstracción `IGraphicsDevice` en Core; implementación `VulkanGraphicsDevice`.
- **Pixel art vs AA:** para look retro, lo habitual es **muestreo nearest**, **escala entera** a pantalla y **sin FXAA/MSAA** por defecto (mezclan vecinos y «ensucian» los bordes). MSAA/FXAA/bilinear encajan mejor en arte HD o bordes suaves; ver tema de ayuda **«Render, pixel art y antialiasing»** en `EngineDocumentation.cs` (no hay selector global de AA en UI hasta que se implemente explícitamente).
- **Modelo de entidades:** **OOP con componentes** (estilo Unity), no ECS. `GameObject` + `Transform` + `Component` (`SpriteComponent`, `ColliderComponent`, `ScriptComponent`, `LightComponent`, `RigidbodyComponent`, `HealthComponent`, `ProximitySensorComponent`, `CameraTargetComponent`, `AudioSourceComponent`, `ParticleEmitterComponent`).
- **Mapa:** **Chunks** por capa (`Chunk.DefaultSize = 16` por defecto en la clase `Chunk`; el `TileMap` y `ProjectInfo.ChunkSize` pueden usar otros valores). **Varias capas** (`MapLayerDescriptor`, `LayerType`, blend, parallax, tileset por capa). Serialización en **`mapa.json`** vía `MapSerialization` (`MapDto` con `Layers` + `Chunks` con `LayerId`).
- **Tiles:** Modo clásico (pintura con `TipoTile`, imagen, overlay) y modo **catálogo** (`CatalogTileId`, rutas de atlas vía `TilesetPath` / tileset JSON). `world.getTile` / `world.setTile` en Lua usan nombre de capa y catálogo.
- **Física:** un solo bucle en Play: **`PhysicsWorld.StepPlayScene`** (tiles + AABB estático/dinámico + triggers). Consultas: **`ScenePhysicsQueries`** (`RaycastSolids`, `OverlapCircle`). **`world.raycast`** = query del host (misma geometría que colliders sólidos en Play). **`physics.raycast`** / **`physics.overlapCircle`** = **`PlayScenePhysicsApi`** sobre colliders (sin tilemap). **`CollisionBody`** no participa en ese paso (reservado).
- **Iluminación:** `LightSource`, `LightingManager`, `LightComponent`; el pipeline **Vulkan** es básico. El **visor WPF** usa **`SceneLighting.SampleBrightness`** en tiles y **`SampleRgbTint`** + multiplicación RGB en **sprites** (`SpriteBitmapTint`), leyendo **`LightComponent.ColorHex`**.
- **Scripting:** **Lua** (NLua). Tablas: `self`, `world`, `input`, `time`, `audio`, `physics`, `ui`, `game`, **`log`** (`LuaLogApi`: `info` / `warn` / `error` → consola del editor en Play con niveles), **`ads`** (`AdsApi`; en Play del editor usa `SimulatedAdsApi`), y opcionalmente **`Debug`** (`DebugDrawApi`). El sandbox expone **`debug.traceback`** (no el módulo `debug` completo). Scripts **de capa** del mapa usan la tabla **`layer`** (`LayerProxy`) y el hook **`onLayerUpdate(dt)`** (sin `self`). Constantes `Key.*`, `Mouse.*`.
- **Audio en Play/editor:** APIs Lua inyectables; en WPF suele usarse **NAudio** (`PlayNaudioAudioEngine`, `WpfPlayAudioApi`) con manifiesto `audio.json` (`AudioManifestSerialization`).
- **Lua y host .NET (sin romanticismos):** el motor **no** es C++/Lua: el host es **C# (.NET 8)**; render, física, audio y bucle pesado viven en ensamblados nativos administrados y Vulkan. Lua solo orquesta gameplay. **Rendimiento:** evita miles de llamadas Lua↔C# por frame; agrupa trabajo en APIs C# (`world`/`physics`/…). **Extensiones NuGet:** úsalas en C# y expón a Lua tablas finas; no cargues DLL arbitrarias desde scripts. **Plugins .NET:** carpeta del proyecto `Plugins/` + `plugins-manifest.json` (lista blanca; entradas string u objeto con `name`/`path` y `version` opcional para logs); `PluginLoader` carga `IPlugin` y, si implementan `ILuaEngineBinding`, reciben `IEngineContext` (world, `ProjectInfo`, APIs por nombre, servicios opcionales como `IEditorLog`) al llamar `RegisterEnginePlugins` **después** de cablear World/input/game/UI/ads en Play.

---

## 1. Arquitectura

Resumida en la tabla **Proyectos y referencias**: **seis** ensamblados y el grafo de dependencias del párrafo siguiente (Core base; WPF consume Editor + Runtime + Service).

- **Scripting y plugins:** NLua con entornos por instancia; errores formateados con ruta/línea. Plugins opcionales: `PluginLoader.cs`, `ILuaEngineBinding.cs`, `IEngineContext.cs`; en Play `RegisterEnginePlugins(project, hostServices)` va **después** de `SetWorldApi`/`SetAdsApi` para que el contexto exponga APIs reales.

---

## 2. Sistema de tiles, chunks y capas

- **TileMap (Core):** lista de `MapLayerDescriptor` (`Layers`) y, por cada capa, un diccionario `(cx, cy) → Chunk`. API principal: `TryGetTile(layerIndex, wx, wy)`, `SetTile`, `RemoveTile`, `IsCollisionAt` (respeta tipos de capa y flags), streaming/eviction según flags del proyecto y runtime.
- **Auto-tiling:** `AutoTiling` — máscara de vecinos N/S/E/W (bits 8/4/2/1) y variantes por `baseTileId`.
- **Raycast en grid:** `TileMapRaycast` (DDA sobre colisión del mapa).
- **Tileset en disco:** `Tileset`, `TilesetPersistence`, `TileCatalogHelper` — emparejan IDs de catálogo con recorte en atlas.
- **Alternativa “Scene + TilemapLayer”:** tipos `TilemapLayer`, `TilemapChunk` en Core para enfoques ligeros por IDs; el flujo principal del editor es **chunk + capas** serializadas en `MapDto`.

### 2.1 API de `TileMap` (referencia rápida)

- **Lectura/escritura:** `TryGetTile(layerIndex, wx, wy)`, `SetTile`, `RemoveTile`.
- **Runtime / Lua:** `SetTileFromRuntime`, `RemoveTileFromRuntime`, `MarkRuntimeTouched` — marcan chunks para no perder estado al hacer streaming o para volcar vacíos a caché.
- **Chunks:** `GetChunk(layerIndex, cx, cy)`, `GetOrCreateChunkAt`, `EnumerateChunkCoords()` (todas las capas), `EnumerateChunkCoords(layerIndex)`, `WorldTileToChunk`.
- **Evicción streaming:** `EvictEmptyChunksBeyond(centerCx, centerCy, radiusChunks, skipRuntimeTouched, spillRuntimeTouchedEmpty)` — vacía chunks vacíos lejanos; callback opcional para persistir vacíos tocados en runtime.
- **Capas:** `AddLayer`, `RemoveLayerAt`, `MoveLayer`, `ReplaceLayers` (carga desde DTO), `LayerHasAnyTiles`.
- **Otros:** `Clone()` (copia profunda), `IsCollisionAt(wx, wy)` — en capas `LayerType.Solid` cualquier tile cuenta como muro; en el resto se usa `TileData.Colision`.

### 2.2 `MapLayerDescriptor` y `LayerType`

- Cada capa tiene **`Id` único (GUID)** usado en JSON para enlazar `ChunkDto.LayerId` con la capa correcta aunque se reordenen las capas.
- **`LayerType`:** `Background` (sin colisión por ocupación), **`Solid`** (cualquier celda con tile = colisión), `Objects`, `Foreground` (orden visual / encima del jugador según flags como `RenderAbovePlayer`).
- Campos extra: opacidad, blend, parallax, offsets, `CollisionLayer` / `CollisionMask` (preparación para física por máscaras), `BackgroundTexturePath`, `TilesetAssetPath`, **`LayerScriptId`** (ruta `.lua` relativa al proyecto), **`LayerScriptEnabled`**, **`LayerScriptProperties`** (lista `ScriptPropertyEntry` inyectada en Lua como en objetos).

---

## 3. Serialización JSON

**Lectura rápida:** el código está en `FUEngine.Editor/Serialization/`. La tabla **§3.1** resume qué hace cada `Load` si **no existe** el archivo o si el JSON va mal (no memorizar: consultar al cambiar contratos).

**Opciones compartidas** (`SerializationDefaults.Options`): `WriteIndented = true`, `PropertyNamingPolicy = CamelCase`, `ReferenceHandler = IgnoreCycles`.

**Comportamientos:** mapa, objetos, triggers, seeds, scripts, animaciones, UI, manifiesto de audio, biblioteca global: **`Load` suele devolver vacío** si no existe el archivo (según el serializador). **Proyecto** (`ProjectSerialization`): `Load` **exige** archivo. **Validación** en `ProjectSerialization.FromDto`: p.ej. `TileSize > 0`, `Fps` en rango, `ChunkSize > 0`.

**Render (proyecto.json):** `renderAntiAliasMode` (`none` | `fxaa` | `msaa`), `msaaSampleCount` (0, 2, 4 u 8), `textureFilterMode` (`nearest` | `bilinear`). Normalización en `ProjectRenderSettings`. UI: Configuración del proyecto → pestaña **Juego**. Valores orientativos hasta que el backend Vulkan los consuma por completo.

**Mapa (`MapDto`):** `ChunkSize`, `Layers` (`LayerDto`: nombre, tipo, sort, visibilidad, bloqueo, opacidad, blend, parallax, offsets, máscaras de colisión, `TilesetAssetPath`, `layerScriptId`, `layerScriptEnabled`, `layerScriptProperties`, etc.), `Chunks` con `LayerId`, `Cx`, `Cy`, lista de `TileDto` (incluye `CatalogTileId`, overlay en Base64, etc.).

### 3.1 Tabla de serializadores (detalle)

| Clase | Si no existe el archivo | Si el JSON es inválido / error |
|--------|-------------------------|--------------------------------|
| `MapSerialization.Load` | `new TileMap(Chunk.DefaultSize)` | `InvalidOperationException` (incluye línea/posición en `JsonException`) |
| `ObjectsSerialization.Load` | `new ObjectLayer()` | Excepción al leer o deserializar |
| `ProjectSerialization.Load` | **Exige archivo** | Excepción |
| `TriggerZoneSerialization.Load` | Lista vacía | Excepción al leer; JSON mal formado al deserializar |
| `SeedSerialization.Load` | Lista vacía | Errores de lectura → excepción; **`JsonDocument` raíz inválida** → lista vacía; acepta clave raíz **`seeds`** o legacy **`prefabs`** |
| `ScriptSerialization.Load` | `new ScriptRegistry()` | Excepción con detalle de línea en JSON |
| `AnimationSerialization.Load` | Lista vacía | Excepción con detalle de línea en JSON |
| `UICanvasSerialization.Load` | `new UICanvas()` | Deserialización fallida → `new UICanvas()` (sin lanzar) |
| `AudioManifestSerialization.LoadOrEmpty` | Diccionario vacío | Archivo ilegible o `JsonException` → **diccionario vacío** (no lanza) |
| `GlobalLibrarySerialization.LoadOrEmpty` | Manifiesto vacío | Cualquier error → manifiesto vacío |
| `ProyectoConfigSerialization.Load` | `null` | `catch` amplio → `null` |

**`LuaScriptVariableParser`:** heurística por líneas (regex) para detectar **asignaciones globales** al inicio del script, ignorando bloques `function`/`if`/`for`/`while`; sirve al editor para listar variables editables — **no** es un parser Lua completo.

---

## 4. Sistema de herramientas (editor)

- **`ITool`:** `OnMouseDown(canvasPos, ctrl, shift)`, `OnMouseMove`, `OnMouseUp`.
- **`IMapEditorToolContext`:** expone `TileMap`, `ObjectLayer`, `Selection`, `History`, `Project`, conversión canvas↔tile, `DrawMap`, capa activa, brocha, etc. (ver interfaz en código; evoluciona con el editor).
- **`ToolController`:** delega al `CurrentTool` o a callbacks fallback; controla estado de arrastre.
- **`PaintTool`:** única implementación actual de `ITool`; pintura continua, brocha, bucket con Shift, comandos undo por celda o por lote.

### 4.1 Panel de capas, explorador y mini-IDE Lua (referencia de código)

- **`FUEngine/Panels/MapHierarchyPanel.*`:** árbol **«Jerarquía de la escena»**: raíz **`Scene: {nombre escena}`** (`SceneRoot`); **`Map · {archivo}`** con **Layers**; **instancias de objetos**, **Triggers** y **Canvas UI** van **directamente bajo la escena** (sin carpetas «Objetos»/«UI»); solo **Groups** sigue siendo carpeta para grupos de píxeles. Tras refrescar, el árbol **expande** raíz y primer nivel para no ocultar lo creado. `SetMapStructure(…)`; `EditorWindow.RefreshMapHierarchy()`. Clic derecho en **fondo vacío** = menú de raíz. **Crear → Nuevo Mapa** guarda un `.map` vacío (diálogo) y refresca el explorador. **UI** en raíz puede crear **Canvas** si no hay ninguno y luego **Text/Button/Panel/TabControl** (`UIElementKind.TabControl`). **Arrastrar assets** a la raíz escena o a **Object layer** instancia. **seeds.json** al crear proyecto incluye el seed **demo_square** (`obj_default`): es solo datos de instancia; **no** tiene script Lua propio.
- **Plantilla «proyecto nuevo» (Blank):** el cuadrado que rebota en los bordes del área de juego está implementado **solo** en **`Scripts/main.lua`** (script de capa **Ground**, escena **Start**, `onStart` / **`onLayerUpdate`**). No se genera otro `.lua` para ese demo. La escena **End** no asigna script de capa. Sin la asignación en el mapa, `main.lua` no se ejecuta en Play (no basta con `scripts.json`).
- **`FUEngine/Panels/LayersPanel.*`:** lista de capas del `TileMap` con visibilidad (✓/✕) y bloqueo; renombrado **inline** en la fila (doble clic o menú contextual); eventos `LayerSelected` / `ActiveLayerChanged` enlazan inspector y barra de estado. Pie: **«Añadir capa»** (menú por `LayerType`) y **Quitar**; **Subir/Bajar** no están en la barra (reordenar por menú contextual si aplica). **«Añadir componente…»** para capas vive en **`LayerInspectorPanel`** (inspector al seleccionar una capa), al final de las propiedades, no en la lista lateral. Puedes **deseleccionar** la capa en la lista (clic en vacío); si el JSON del mapa no trae `name` en una capa, el editor usa fallback `Capa i` / nombres del proyecto.
- **`ProjectExplorerPanel`:** vista **Grid** con carpeta actual y migas (navegar entrando en carpetas con doble clic). **Clic en el fondo** del árbol, lista o grid deselecciona el ítem y limpia el inspector rápido. Menú contextual de archivo: **sin entrada «Fijar»** (solo favoritos u otras acciones). Sobre **PNG/JPEG/BMP**: **Reescalar imagen…** (`ImageResizeDialog` + `ImageNearestNeighborResize`, vecino más cercano); al guardar se emite **`ExplorerImageFileChanged`** → `EditorWindow` repinta el mapa e invalida la caché de texturas del tab **Juego** (`TextureAssetCache.InvalidateAbsolutePath` / `GameTabContent.NotifyProjectImageFileChanged`). El archivo de manifiesto canónico (`Project.FUE` o JSON legacy según `ProjectManifestPaths.GetCanonicalManifestPath`) se **resalta** en el árbol (fondo/borde dorado) y usa icono **⚙**. **Clic en el manifiesto:** Inspector = **`ProjectManifestPanel`** (resolución lógica, tile size, autoría, cámara, colores, guardar, exportar build, integridad, abrir carpeta, configuración avanzada). **Doble clic en el `.FUE` del manifiesto:** abre el mismo diálogo que **Proyecto → Editor avanzado del proyecto** (`ProjectConfigWindow`). **Proyecto → Ajustes del proyecto (.FUE)** solo selecciona el manifiesto en el árbol y muestra el Inspector. **Clic en un `.seed`:** actualiza `SelectionManager.SelectedExplorerItem` y el **Inspector** (`QuickPropertiesPanel`) con datos del prefab, miniatura (`SeedExplorerHelpers.TryResolveSpritePreviewPath`) y **Abrir script** si el seed tiene script en `scripts.json`. **Doble clic en `.seed`:** abre el `.lua` resuelto en la pestaña **Scripts** (`SeedExplorerHelpers.TryResolveScriptPath`); si no hay script, aviso. **`.lua`** y **`.json`** genéricos abren la pestaña **Scripts** (`EditorWindow.ProjectExplorer_OnRequestOpenInEditor`). **Arrastrar un `.seed`** al **canvas del mapa** coloca las instancias en la casilla del soltar (`InstantiateSeedFromFile` con desplazamiento desde ese tile). Arrastrar un **`.seed`** (u otros assets admitidos) sobre la **jerarquía** en carpeta/capa de objetos dispara `RequestInstantiateAsset`; los `.seed` se expanden con `SeedSerialization.Load` + instancias (`EditorWindow.InstantiateSeedFromFile`). **Arrastrar un objeto** desde la jerarquía (mismo formato que el inspector) sobre una **carpeta del explorador** (árbol, lista o grid) guarda un **`.seed`** en esa ruta vía `RequestExportObjectAsSeed` → `ExportObjectInstanceAsSeedToFolder` (nombre de archivo único si hace falta). **Discord:** manifiesto del proyecto seleccionado (y no en pestaña Scripts) → **«Configurando ajustes globales»**; con un `.seed` seleccionado → **«Modificando semilla: …»**. **Spotlight** (`SpotlightIndex.SearchProjectFiles`) indexa también **`*.seed`**; al confirmar un resultado `.seed` en el editor se coloca en el mapa (`OpenProjectFileFromSpotlight` → `InstantiateSeedFromFile`).
- **`ScriptsTabContent`:** la lista mezcla scripts del registro (`ScriptDefinition`), el archivo de registro **`scripts.json`** (resuelto con `ProjectIndexPaths`, típicamente `Data/scripts.json` en proyectos nuevos) y **`*.json`** bajo **`Scripts/`** no duplicados; la etiqueta muestra **nombre · archivo.ext**. `SetProjectDirectory` debe ejecutarse **antes** de poblar la lista para que la primera selección abra rutas correctas (`CreateScriptsTabContent`).
- **`ScriptEditorControl` + `LuaEditorCompletionCatalog`:** AvalonEdit con resaltado **Lua** / **JSON**: para `.lua` se usa la definición embebida **`FUEngine.Resources.Lua.xshd`** (fallback: `HighlightingManager` «Lua»). En **AvalonEdit 6.x** el XSD de `xshd` **no** admite el atributo **`escape`** en `<Span>`: si está presente, la carga falla y el log muestra *The 'escape' attribute is not allowed* (texto todo del mismo color). **Orden al abrir archivo:** asignar `SyntaxHighlighting` **antes** de poner el texto; fijar un `Foreground` explícito en el `TextEditor` (el `Foreground` de la ventana en `App.xaml` se hereda y, sin resaltado aplicado bien, el texto puede verse **todo del mismo tono**). Tras cargar, `Redraw()` y un repaso en `Loaded`/`Dispatcher` para asegurar colores de comentarios/cadenas/keywords. **Errores de sintaxis en `.lua`/`.script`:** con debounce (~380 ms) tras editar, **`LuaScriptSyntaxChecker`** (runtime) intenta `load(código, nombre, 't', {})` vía NLua; si falla, **`LuaSyntaxErrorLineRenderer`** (`IBackgroundRenderer` en la capa `Background`) marca la línea (fondo rojizo + barra/subrayado rojo). **`LuaErrorLineParser`** reutiliza el mismo patrón `:(\d+):` que el runtime para extraer la línea del mensaje. Autocompletado al escribir identificadores y tras **`.`**; **Ctrl+Espacio**; **`LuaEditorApiReflection`** rellena el mapa `tabla.` → miembros desde tipos con **`[LuaVisible]`** (p. ej. `WorldApi`, `AdsApi`, `SelfProxy`, `KeyConstants`); **`LuaCompletionIcons`** asigna iconos WPF al popup; **`MergeDynamic`** añade globales extra. Snippet **`dbggrid`** inserta un `onUpdate` con **`Debug.drawGrid`** (rejilla en casillas).
- **Inspector de objeto — variables de script en caliente:** con Play activo (`PlayModeRunner` o pestaña Juego), **`ObjectInspectorPanel`** usa `LiveVariablesProvider` / `LiveVariableWriter` (`TryGetLiveScriptVariables` / `TrySetLiveScriptVariable` en **`PlayModeRunner`**) para mostrar valores del entorno Lua y escribir al perder foco. **`LuaScriptVariableDiscovery.MotorGlobalNames`** enlaza la misma lista de globales que el autocompletado (texto de ayuda).
- **Export build — Ads:** **`ProjectInfo.AdsExportProvider`** (`"simulated"` por defecto o `"google_mobile_ads"` en `proyecto.json`) y **`ProjectBuildService`** escriben **`Data/ads_export.json`** junto al bundle. Clase de contrato nativo: **`GoogleMobileAdsApi`** (runtime; no usar en WPF). El host móvil debe enlazar el SDK real.
- **Ads en Play:** `Runtime` — `ScriptBindings.SetAds` / `PopulateEnvironment(..., ads)`; **`LuaScriptRuntime.SetAdsApi`**; **`PlayModeRunner`** inyecta **`SimulatedAdsApi`** (retrasos simulados, callbacks Lua vía `Dispatcher.BeginInvoke`, logs en consola del editor).

**Modos de herramienta en `EditorWindow` (`ToolMode` enum):** además del pincel, el editor usa **lógica en `EditorWindow.xaml.cs`** (`HandleToolMouseDown` / `Move` / `Up`) para: **Rectángulo**, **Línea**, **Relleno** (bucket directo), **Goma**, **Cuentagotas**, **Stamp**, **Seleccionar** (objetos/tiles), **Colocar** (objetos), **Zona** (copiar/pegar región), **Medir**, **PixelEdit** (edición por píxel en tile). Solo cuando el modo es **Pintar** se asigna `PaintTool` a `ToolController.CurrentTool`; en el resto `CurrentTool` es `null` y actúan los *fallbacks*.

---

## 5. Undo/Redo

`EditorHistory` — máximo **100** pasos; `Push` ejecuta y apila; `Undo` / `Redo`; evento `HistoryChanged`.

**Comandos implementados** (`Editor/EditorCommands.cs`): `PaintTileCommand`, `PaintTileBatchCommand`, `RemoveTileCommand`, `AddObjectCommand`, `RemoveObjectCommand`, `TransformTileSelectionCommand` (rotación 90°/180°, flip H/V sobre selección rectangular). Todos los comandos de tiles usan **`layerIndex`** coherente con el mapa multicapa.

**`TilePaintService.ComputeBucketFill`:** flood-fill en **una capa** (`layerIndex`, por defecto 0); solo celdas con el **mismo `TipoTile`** que la semilla; respeta `isInsideSelection`; **`maxFill` por defecto 2000** celdas para evitar bucles costosos.

---

## 6. Renderizado en el editor

**`MapRenderer` + `MapRenderContext`:** grid, tiles por capa y visibilidad, objetos, triggers, máscaras de depuración, selección, herramientas (rectángulos, medición), minimapa opcional, **gizmos de streaming** si están activados (`ShowStreamingGizmos`).

**Otros render WPF:** `GameViewportRenderer` (vista de juego / debug), `PlayTileBitmapCompositor` (composición de tiles en Play).

---

## 7. Selección y portapapeles

- **`SelectionManager`:** objetos (simple/múltiple), triggers, ítems del explorador, selección rectangular de tiles con drag/commit.
- **`ZoneClipboard` / `ZoneClipboardService`:** copiar/pegar zona de tiles + objetos; genera comandos para el historial.

---

## 8. Objetos y seeds

- **`ObjectLayer`:** definiciones (`ObjectDefinition`) e instancias (`ObjectInstance`); consultas por celda, colisión.
- **`ObjectInstance`:** transformación, `LayerOrder`, `ScriptIds`, `ScriptProperties`, overrides, `Visible`, **`Enabled`** (scripts en Play / `GameObject.RuntimeActive`), **`Pivot`** (reservado render), **`PointLightEnabled`** + radio/intensidad/color (`LightComponent` en Play si está activo), tags. **Componentes persistidos en JSON (inspector «Sprite, física y gameplay»):** tinte/flip/sort del sprite (`SpriteColorTintHex`, `SpriteFlipX/Y`, `SpriteSortOffset`), **AnimationPlayer** (`DefaultAnimationClipId`, `AnimationAutoPlay`, `AnimationSpeedMultiplier`; `self.playAnimation` aplica clips de `animaciones.json`), **Collider** (`ColliderShape` Box/Circle, tamaños, offset; sigue siendo AABB en física), **Rigidbody** (`PhysicsGravity` del proyecto), **CameraTarget** (prioridad sobre protagonista para la cámara nativa), **ProximitySensor** (distancia + etiqueta → `onTriggerEnter`/`Exit` al objetivo), **Health**, **AudioSource** (metadatos; reproducir con `audio.play`), **ParticleEmitter** (datos; visor ampliable).
- **`ObjectDefinition`:** además de sprite/colisión/scripts: `AnimatronicType`, `MovementPattern`, `Personality`, `CanDetectPlayer`, `EnableInGameDrawing`.
- **`SeedDefinition` / `SeedObjectEntry`:** datos en `seeds.json` (editor). En Play, **`TryExpandPrefab`** (desde `PlayModeRunner`) expande el seed por **Id** o **Nombre**; si no hay coincidencia, `Instantiate` crea un `GameObject` mínimo con ese nombre.
- **`TriggerZone`:** rectángulo en celdas (`X`,`Y`,`Width`,`Height` en tiles), `Contains(tx,ty)`, `ScriptIdOnEnter` / `OnExit` / `OnTick`, `TriggerType` (p. ej. OnEnter, OnExit, Temporal, Persistent), `Descripcion`, `LayerId`, `Tags`. Los **triggers de mapa** (`triggerZones.json`) son distintos de los **`ColliderComponent` IsTrigger** en objetos; Play puede combinar ambos flujos. En **Play** del editor, `PlayModeRunner` carga `triggerZones.json`, toma la posición en celdas del **protagonista nativo** (`floor` de `Transform`) y: por **entrada** en una zona ejecuta el script de `ScriptIdOnEnter` (host efímero: `onAwake` → `onStart` → `onZoneEnter` con el proxy del jugador como argumento); por **salida**, `ScriptIdOnExit` (`onZoneExit`); **ScriptIdOnTick** mantiene un host en escena con `onUpdate`/`onLateUpdate` mientras el jugador permanece dentro (con `ChunkEntitySleep`, esos hosts se fuerzan en el conjunto activo). Los IDs deben existir en `scripts.json` con ruta `.lua` válida.

### 8.1 Colisiones y triggers en Play (`PlayModeRunner`)

Al convertir `ObjectInstance` → `GameObject`, `TryAddColliderFromInstance` crea un **`ColliderComponent`** (AABB en unidades de casillas; **círculo** = mitades iguales). Tamaños: `ColliderBoxWidthTiles`/`Height` (0 = usar tipo), o `ColliderCircleRadiusTiles` si `ColliderShape` = Circle; `ColliderOffsetX/Y`.

- **`CollisionType`** de la instancia = `"Trigger"` (ignorando mayúsculas) → `IsTrigger = true`, `BlocksMovement = false` (no frena al protagonista nativo).
- Colisión sólida: definición/override con colisión activa y **no** trigger → `BlocksMovement = true`.
- **`IsStatic`:** `false` si la instancia tiene tag **`player`** o **`dynamic`** (case insensitive); los estáticos participan en resolución AABB entre objetos; dinámicos se excluyen de empujes mutuos en esa pasada.

**`RigidbodyComponent`:** si `RigidbodyEnabled`, integración de velocidad + gravedad (`ProjectInfo.PhysicsGravity` × `RigidbodyGravityScale`) y drag **antes** de `StepPlayScene`; se omite en el protagonista con **`UseNativeInput`** para no duplicar movimiento.

**`ProximitySensorComponent`:** tras el paso de física, distancia al primer objeto con la etiqueta objetivo → `NotifyScripts` con `onTriggerEnter` / `onTriggerExit` (mismo hook que triggers AABB).

**`PhysicsWorld.StepPlayScene`:** mismo comportamiento que el antiguo paso AABB (tiles, estático/dinámico, triggers); lo invoca **`PlayModeRunner`** tras integración rigidbody y Lua `onUpdate`.

**`GameObject` (Core):** `InstanceId` (coincide con `ObjectInstance.InstanceId` en Play), jerarquía con `Parent`/`Children`, eventos `ChildAdded`/`ChildRemoved`/`ParentChanged`, `Find` / `FindInHierarchy`, `GetComponent<T>()` y `GetComponent("NombreTipo")`, `AddComponent`/`RemoveComponent`, `SetParent`/`AddChild`/`RemoveFromParent`.

### 8.2 Referencia de componentes (instancia → JSON → runtime)

**Persistencia:** `FUEngine.Editor/DTO/ObjectsDto.cs` (`ObjectInstanceDto`) y `ObjectsSerialization` ↔ `ObjectLayer` / `ObjectInstance`. Nombres JSON en **camelCase** (p. ej. `spriteColorTintHex`, `rigidbodyEnabled`). Instancias antiguas sin campos nuevos cargan con **defaults** en `FromDto` (p. ej. tinte `#ffffff`, `animationSpeedMultiplier` ≥ 1).

**Inspector (WPF):** `ObjectInspectorPanel` — expander **«Sprite, física y gameplay (Play)»** y botón **«Añadir componente…»** (`AddComponentPickerWindow`), que activa flags y enfoca controles sin duplicar el modelo: todo sigue siendo **campos de `ObjectInstance`**, no una lista genérica de componentes serializada aparte.

**Orden lógico en Play (por frame, simplificado):** `BeginTick` → spawns / `InvokeOnStarts` (una vez) → scripts de capa → entrada nativa opcional → **`InvokeOnUpdates`** → **integración `RigidbodyComponent`** (gravedad + drag + posición) → **`PhysicsWorld.StepPlayScene`** (tiles, AABB, triggers por overlap) → **`UpdateProximitySensors`** (distancia; mismos hooks `onTriggerEnter`/`Exit` que triggers AABB) → `InvokeOnLateUpdates` → cámara (`FindCameraFollowTarget` si hay `CameraTargetComponent`) → avance de frames de sprite → `FlushDestroyQueue`.

**Tabla rápida (datos → clase Core en Play):**

| Grupo | Campos `ObjectInstance` (idea) | Componente / efecto |
|--------|----------------------------------|----------------------|
| Identidad / render | `layerOrder` → `GameObject.RenderOrder` | Orden Z global del objeto |
| SpriteRenderer | `spriteColorTintHex`, `spriteFlipX`/`Y`, `spriteSortOffset` | `SpriteComponent`: multiplicador RGB sobre textura (tras muestreo de luz en visor WPF), flip añadido a escala, `SortOffset` para desempate con mismo `RenderOrder` |
| AnimationPlayer | `defaultAnimationClipId`, `animationAutoPlay`, `animationSpeedMultiplier` | Clips definidos en `animaciones.json` (`AnimationDefinition`); `NativeAutoAnimationApplier`; Lua: `self.playAnimation("Idle")`, `self.stopAnimation()` |
| Collider | `colliderShape` Box/Circle, tamaños, `colliderOffsetX/Y`; base `colisionOverride`, `collisionType` | `ColliderComponent`: **solo AABB**; Circle = mitades iguales. `IsTrigger` si `collisionType` = Trigger. Tags `player`/`dynamic` → dinámico |
| Rigidbody | `rigidbodyEnabled`, masa, `rigidbodyGravityScale`, `rigidbodyDrag`, `rigidbodyFreezeRotation` | `RigidbodyComponent`; gravedad = `ProjectInfo.PhysicsGravity`. **No** se integra en el GO del protagonista si `UseNativeInput` (evita doble control) |
| Luz | `pointLightEnabled`, radio, intensidad, `pointLightColorHex` | `LightComponent`; visor WPF: `SceneLighting.SampleRgbTint` |
| CameraTarget | `cameraTargetEnabled` | `CameraTargetComponent`; primera instancia con el flag «gana» para la cámara nativa |
| ProximitySensor | `proximitySensorEnabled`, rango en casillas, `proximityTargetTag` | Primer `GameObject` con esa **tag** en `Tags`; distancia euclídea centro–centro |
| Health | `healthEnabled`, `healthMax`, `healthCurrent`, `healthInvulnerable` | `HealthComponent`; uso típico vía `getComponent("HealthComponent")` + `invoke` si expones métodos Lua |
| AudioSource | `audioSourceEnabled`, `audioClipId`, volumen, pitch, loop, spatial blend | `AudioSourceComponent`; reproducción real con tabla **`audio`** y IDs del manifiesto |
| ParticleEmitter | textura, tasa, vida, gravedad | `ParticleEmitterComponent`; datos guardados; **render de partículas en visor** puede ampliarse |

**Consultas físicas:** `ScenePhysicsQueries.OverlapCircle` prueba distancia del centro del círculo al **AABB** de cada collider (no círculo contra círculo). Raycast sólido usa la misma geometría AABB.

**Clases Core:** `FUEngine.Core/Objects/*.cs` — `SpriteComponent`, `ColliderComponent` + `ColliderShapeKind`, `RigidbodyComponent`, `HealthComponent`, `ProximitySensorComponent`, `CameraTargetComponent`, `AudioSourceComponent`, `ParticleEmitterComponent`, `LightComponent`, `ScriptComponent`.

**Lua — nombres de tipo para `getComponent`:** coinciden con el nombre de clase C# (`"SpriteComponent"`, `"HealthComponent"`, `"ColliderComponent"`, …). `ComponentProxy.invoke` depende de que el componente exponga métodos al runtime.

**Lista camelCase en `ObjectInstanceDto` (grep / merge):** además de `pointLightEnabled`, `pointLightRadius`, `pointLightIntensity`, `pointLightColorHex`: `spriteColorTintHex`, `spriteFlipX`, `spriteFlipY`, `spriteSortOffset`, `defaultAnimationClipId`, `animationAutoPlay`, `animationSpeedMultiplier`, `particleEmitterEnabled`, `particleTexturePath`, `particleEmissionRate`, `particleLifeTime`, `particleGravityScale`, `colliderShape`, `colliderBoxWidthTiles`, `colliderBoxHeightTiles`, `colliderCircleRadiusTiles`, `colliderOffsetX`, `colliderOffsetY`, `rigidbodyEnabled`, `rigidbodyMass`, `rigidbodyGravityScale`, `rigidbodyDrag`, `rigidbodyFreezeRotation`, `cameraTargetEnabled`, `audioSourceEnabled`, `audioClipId`, `audioVolume`, `audioPitch`, `audioLoop`, `audioSpatialBlend`, `proximitySensorEnabled`, `proximityDetectionRangeTiles`, `proximityTargetTag`, `healthEnabled`, `healthMax`, `healthCurrent`, `healthInvulnerable`.

**Diferencias editor vs Play:** el mapa en editor usa WPF; Play del tab Juego usa el mismo `GameViewportRenderer` para sprites con luces y tintes. Vulkan u otra ruta puede no aplicar aún todos los efectos del visor: trata el visor embebido como referencia de comportamiento documentado.

---

## 9. Runtime (FUEngine.Runtime) y Play Mode

En el **editor**, el flujo que importa para simular el juego es **`PlayModeRunner`** (proyecto `FUEngine`). `GameLoop` / `SceneManager` existen en **Runtime** pero el tab Juego y hot reload se apoyan en el runner. Abajo: piezas del runtime y cómo las usa Play.

**`GameLoop`:** `Renderer` (Core) con `IGraphicsDevice` opcional; `InputManager`; `LuaScriptRuntime` opcional; `Tick` → `BeginFrame`, scripts, `EndFrame`.

**`Camera`:** posición + `Zoom`.

**`SceneManager`:** `LoadScene(Scene)` o `LoadScene(string)` — con `string` asigna un `Scene` de Core con solo id/nombre (sin cargar mapa/objetos). El flujo real del juego en el editor es **`PlayModeRunner`**, no este `SceneManager`.

**`SceneDefinition` (proyecto)** vs **`Scene` (Core):** el editor multiescena usa `SceneDefinition` + rutas a mapa/objetos/UI. `Scene` en Core es otro modelo (`TilemapLayers`, listas de objetos/luces/triggers) poco acoplado al editor actual.

**`LuaScriptRuntime`:** entornos por instancia, `CreateInstance`, **`CreateLayerScriptInstance`** (entorno con `layer`, sin `self`; eventos `onLayerUpdate`, etc.), `Tick` (y sobrecarga con `activeGameObjects` para `ChunkEntitySleep`), `BeginTick` / `InvokeOnUpdates` / `InvokeOnLateUpdates` / `InvokeLayerOnStarts` / `InvokeLayerScripts` / `EndTick`, `NotifyScripts`, `GetScriptInstancesFor`, `GetActiveScriptCount`, `GetLuaMemoryKb`, `ReloadScript`, breakpoints (`SetBreakpoints`, `IsBreakpointHit`, …), eventos y APIs inyectadas; **`SetLuaLogSink`** / tabla **`log`**; **`RegisterEnginePlugins(ProjectInfo?, hostServices?)`** construye **`PlayLuaEngineContext`** (`IEngineContext`) para plugins. `ScriptPropertyEntry` → `Set` en el entorno; tipos `int`/`float`/`bool`/string y **`object`** (InstanceId → `SelfProxy` vía `IWorldContext.GetObjectByInstanceId`). El Inspector puede declarar propiedades con `-- @prop nombre: tipo = valor` en el `.lua` (ver `LuaScriptVariableParser.ParseMergedForInspector`).

**`PlayModeRunner` (proyecto FUEngine):** construye `GameObject`s desde `ObjectLayer`, copia o carga `TileMap`, registra `LuaScriptRuntime`, **`PluginLoader.LoadFromDirectory(Plugins)`** al crear el runtime, luego `WorldApi`, `NativeProtagonistController`, triggers (enter/exit), UI (`UIRuntimeBackend`), **`SimulatedAdsApi`** (tabla `ads` en Lua), y **después** **`RegisterEnginePlugins`** con `_project` y servicios del host (`IEditorLog` si está en `ServiceLocator`). Streaming de chunks según proyecto, `ScriptHotReloadWatcher`, audio **`PlayNaudioAudioEngine`** / `WpfPlayAudioApi`. Tras enlazar scripts de objetos, **`BindLayerScripts`** carga un `.lua` por capa si `LayerScriptId` está definido y `LayerScriptEnabled`; cada tick: `InvokeOnStarts` → `InvokeLayerOnStarts` → `InvokeLayerScripts` → input/Lua de objetos → física. **`Stop`** llama **`PluginLoader.UnloadAll`** antes de disponer el runtime.

**Mapa infinito y streaming (Play):** con `Infinite == true` y `ChunkStreaming` / `ChunkUnloadFar`, el tilemap en Play mantiene solo chunks cerca de la cámara (radio `ChunkLoadRadius` + margen `ChunkStreamEvictMargin`); los chunks vacíos lejanos pueden descargarse (`EvictEmptyChunksBeyond`). Con **`ChunkEntitySleep`**, `InvokeOnUpdates` / `InvokeOnLateUpdates` de Lua solo reciben objetos cuyo chunk está dentro del radio de la cámara (Chebyshev); **`onStart` / capas** siguen globales en el flujo del tick. Chunks tocados en runtime (`setTile` desde Lua, etc.) pueden persistirse al descargar según `ChunkStreamSpillRuntimeEmpty`.

**API pública relevante de `PlayModeRunner`:** `Start(useMainScene)`, `Stop()`, `Pause()` / `Resume()`, `GetPlayTileMap()`, `GetRuntime()` → `LuaScriptRuntime?`, `GetSceneObjects()` / `GetSceneObjectNames()`, `TryGetCameraCenterOverride`, propiedades `IsRunning`, `IsPaused`, `FrameCount`, `GameTimeSeconds`, `LastDeltaTimeSeconds`, `CurrentFps`, `OnScriptSaved(relativePath)` (hot reload), `SetBreakpoints`, `IsPausedForBreakpoint`, `ResumeFromBreakpoint()`, `PlayAudioInGame(id)`, `DestroyObject(gameObject)`.

**Protagonista:** `ProjectInfo.ProtagonistInstanceId` o fallback nombre `"Player"`. Opciones nativas: input, follow de cámara, flip de sprite, animación automática desde `animaciones.json`, velocidad en tiles/s.

**Generación / utilidades runtime:** `LuaTileGenerator`, `TileGeneratorCanvas`, `TileGeneratorPropertyDefinition`, `Mathematics/PerlinNoise` — hooks para herramientas de generación procedural expuestas al flujo de Lua/editor.

**Raycast:** `RaycastHitInfo`, `TileRaycastHitInfo`, `CombinedRaycastHitInfo` — resultados para `world.raycast`, `world.raycastTiles`, `world.raycastCombined`.

**Debug:** `DebugDrawApi` — en Lua: `Debug.drawLine(...)`, `Debug.drawCircle(...)` (RGBA 0–255). Al final del tick el motor llama `FinalizeFrame()`; el visor lee `GetLastFrameSnapshot()` / `LuaScriptRuntime.GetDebugDrawSnapshot()`. También existe `LuaScriptRuntime.SharedDebugDraw` para que el **motor** añada primitivas (p. ej. contornos).

**Contexto de mundo:** `IWorldContext` — en Play, **`WorldContextFromList`** con **`DeferDestroy = true`**: **`world.destroy` / `self.destroy`** marcan **`GameObject.PendingDestroy`**; el flush real al final del tick quita listas y ejecuta **`onDestroy`**. Las consultas **`GetAllObjects` / por tag / por nombre** omiten objetos con **`PendingDestroy`**. **`TryExpandPrefab`** expande **`seeds.json`**; el binding de scripts de seeds va por cola **`_pendingSpawnBinds`** (inicio de cada tick, antes de **`onStart`**).

### 9.1 Orden de arranque en `PlayModeRunner.Start`

1. Elegir **`ObjectLayer`**: si `useMainScene` y existe archivo principal → `ObjectsSerialization.Load`; si falla → capa del editor.
2. **`PlayNaudioAudioEngine`**: carga `audio.json`, aplica volúmenes del proyecto.
3. **`LuaScriptRuntime`** + **`WorldContextFromList`** (`DeferDestroy = true`); cargar **`seeds.json`** y **`TryExpandPrefab`**; poblar **`Objects`** con los `GameObject` de la escena.
4. **`WorldApi`**: contexto, `SetRaycastImpl` → `RaycastScene`, mapa Play (`Clone` del snapshot del editor o `LoadPlayTileMapFromDisk`), `ConfigurePlayTilemap` con `DefaultTilesetPath`.
5. **`WpfPlayInputApi`**, **`GameApi`** (opcional **`RuntimeRandomSeed`**), **`PlayScenePhysicsApi`** (colliders en escena), **`UIRuntimeBackend`** + `UiApi`.
6. Por cada `ScriptComponent`: **`CreateInstance`** (chunk Lua, `ScriptProperties`, **`onAwake`** si existe). **`onStart`** no se llama aquí: el **primer tick** ejecuta **`InvokeOnStarts`** antes de **`onUpdate`**.
7. Música de arranque si `StartupMusicPath`.
8. Cámara inicial en el protagonista; **`DispatcherTimer`** según `Project.Fps`; **`ScriptHotReloadWatcher`** (`.lua` con debounce ~250 ms).

**Tick por frame (resumen):** **`BeginTick`** (delta, `time`, `frame`) → **`FlushPendingSpawnBinds`** (seeds: `CreateInstance` + **`onAwake`**) → **`InvokeOnStarts`** (una vez por script) → movimiento nativo opcional → **`onUpdate` / `onLateUpdate`** (sin **`PendingDestroy`**, sin **`RuntimeActive`** false; filtro `ChunkEntitySleep` si aplica) → **física AABB + triggers** (ignora **`PendingDestroy`**) → **cámara** → **sprites** → **`FlushDestroyQueue`** (**`onDestroy`**, `RemoveInstance`, quitar de escena) → **`EndTick`** / `FinalizeFrame` → streaming de chunks si aplica.

---


## 10. ProjectInfo (campos principales)

`ProjectInfo` concentra metadatos del juego y rutas. Incluye entre otros:

- **Identidad:** `Id`, `Nombre`, `Descripcion`, `Author`, `Copyright`, `Version`, `IconPath`, `PaletteId`, `TemplateType`.
- **Grid / mapa:** `TileSize`, `MapWidth`, `MapHeight`, `MapBoundsOriginWorldTileX/Y` (esquina mínima del rectángulo de juego en casillas mundo; suele coincidir con la unión de chunks al expandir), `Infinite`, `ChunkSize`, `InitialChunksW/H`, `TileHeight`, `AutoTiling`, `Fps`, `AnimationSpeedMultiplier`.
- **Streaming de chunks:** `ChunkLoadRadius`, `ChunkStreamEvictMargin`, `ChunkStreamSpillRuntimeEmpty`, `ChunkUnloadFar`, `ChunkSaveByChunk`, `ChunkEntitySleep`, `ChunkStreaming`, `ShowChunkBounds`.
- **Render juego:** `GameResolutionWidth/Height`, `CameraSizeWidth/Height`, `PixelPerfect`, `RenderAntiAliasMode`, `MsaaSampleCount`, `TextureFilterMode`, `InitialZoom`, `DefaultFirstSceneBackgroundColor`, `HUDColor`, `HUDStyle`, `GameFontFamily`, `GameFontSize`, exportación de formatos, `AssetsRootFolder`, `ProjectGridSnapPx`.
- **Cámara / jugador:** `CameraLimits`, `CameraEffects`, `ProtagonistInstanceId`, `UseNativeInput`, `UseNativeCameraFollow`, `NativeCameraSmoothing`, `NativeMoveSpeedTilesPerSecond`, `AutoFlipSprite`, `UseNativeAutoAnimation`.
- **Audio:** `StartupMusicPath`, `StartupSoundPath`, `AudioManifestPath`, `MasterVolume`, `MusicVolume`, `SfxVolume`.
- **Física / gameplay:** `PhysicsEnabled`, `PhysicsGravity`, `DefaultCollisionEnabled`, `DefaultAnimationFps`, `FearMeterEnabled`, `DangerMeterEnabled`, `LightShadowDefault`, **`RuntimeRandomSeed`** (opcional; fija el RNG de **`GameApi`** en Lua al iniciar Play).
- **Scripts:** `ScriptingLanguage`, `BootstrapScriptId`, `DebugMode`, `ScriptNodes`.
- **Autoguardado:** `AutoSaveIntervalSeconds`, `AutoSaveEnabled`, `AutoSaveIntervalMinutes`, `AutoSaveMaxBackupsPerType`, `AutoSaveFolder`, `AutoSaveOnClose`, `AutoSaveOnlyWhenDirty`.
- **Capas:** `LayerNames` (índice = `LayerId` lógico en datos de tile).
- **Rutas:** `ProjectDirectory`, `MapPathRelative`, `MainMapPath`, `MainObjectsPath`, `Scenes` (`SceneDefinition`), `DefaultTilesetPath`, propiedades calculadas `MapPath`, `ObjectsPath`, `MainSceneMapPath`, `MainSceneObjectsPath`. Índices JSON (`scripts.json`, `seeds.json`, `animaciones.json`, `triggerZones.json`, `audio.json` vía `AudioManifestPath`): **`ProjectIndexPaths.Resolve`** en `FUEngine.Core` — en proyectos **nuevos** viven bajo **`Data/`**; en proyectos **legacy** siguen en la raíz si el archivo existe allí (compatibilidad).
- **Plugins:** `ProjectEnabledPlugins`.
- **Motor:** `EngineVersion` en proyecto cargado.

### 10.1 `GameViewportMath`

Utilidades estáticas para alinear resolución lógica del juego con el visor:

- **`GetEffectiveResolutionPixels`:** si `GameResolutionWidth/Height` > 0, píxeles lógicos **alineados a múltiplos de `TileSize`**; si alguno es 0 (“Auto”), con tamaño de viewport del editor (scroll / zoom) se usa ese rectángulo en casillas; sin viewport, ~**12×10 casillas** mínimo.
- **`GetViewportSizeInWorldTiles`:** ancho/alto visible en **casillas mundo** (float), coherente con la resolución efectiva.
- **`GetCameraViewportRectInEditorCanvasPixels`:** rectángulo del **marco azul** en px del lienzo del mapa: tamaño = **resolución interna** (`GetEffectiveResolutionPixels`), posición según centro de **cámara** `EditorViewportCenterWorldX/Y` (casillas). El zoom del editor es `LayoutTransform` sobre el canvas.
- **`ClampViewportCenterToFiniteMap`:** si el mapa no es infinito (`Infinite == false`), ajusta `EditorViewportCenterWorldX/Y` para que el rectángulo de vista quede dentro del rectángulo de juego `[OriginX, OriginX+MapWidth] × [OriginY, OriginY+MapHeight]` en casillas (opcionalmente con viewport del editor para resolución Auto). Se usa al cargar, al cerrar **Proyecto → Configuración** con OK y al expandir el mapa.

**Editor (UX):** orden de pestañas centrales: **Mapa**, **Juego**, **Consola** (la primera es la predeterminada al abrir). Sin `.editorlayout`, se selecciona **Mapa**. `KeepEmbeddedPlayRunningWithMapTab` en `ProjectInfo` (checkbox **Visual → Play activo también en pestaña Mapa**) evita pausar el sandbox al editar en Mapa. El marco **área visible** es la **vista de cámara/render** (resolución lógica); `EditorViewportCenterWorldX/Y` es su centro en casillas. **Alt+arrastrar** o **clic central y arrastrar** sobre el marco mueve la cámara y se guarda en `proyecto.json`. **Centro mapa** / menú homónimo: centro geométrico del mapa finito o `(0,0)` en infinitos. **Mundo 0,0**: alinea el marco para que la esquina superior izquierda del área de juego coincida con la casilla mundo (0,0). **Ctrl+rueda** zoom del lienzo; **rueda** sin Ctrl desplaza el scroll. En Play embebido, la cámara sigue el marco del editor salvo `UseNativeCameraFollow`, pausa o UI modal. Letterbox en tab Juego con **`DefaultFirstSceneBackgroundColor`**; lienzo del mapa con **`EditorMapCanvasBackgroundColor`** (**Proyecto → Avanzado**).

**Mapa finito (editor):** si `Infinite == false`, el área jugable es `[OriginX, OriginX+MapWidth) × [OriginY, OriginY+MapHeight)` en casillas mundo (`Origin*` = `MapBoundsOriginWorldTile*`, por defecto 0). En el margen (fuera del rectángulo pero dentro del lienzo) el editor **no** muestra coordenadas de casilla ni aplica herramientas de tiles; la barra de estado indica «Fuera del área del mapa». El clic en **+ chunk** se resuelve **antes** (`TryHandleFiniteMapExpandClick`). En la **frontera** del conjunto de chunks (o del rectángulo inicial solo si el .map no tiene ningún chunk) aparecen celdas «**+ chunk**» de **ChunkSize×ChunkSize** casillas; **un clic** crea un chunk vacío y **sincroniza** origen y tamaño con la **unión** de todos los chunks (mapas con huecos o forma irregular). En **HUD** del editor, **(0,0)** es el **centro aproximado** del rectángulo de juego; el marco azul usa coordenadas absolutas. **Proyectos nuevos**: origen 0,0, `MapWidth`/`Height` = `InitialChunks × ChunkSize` y **`NewProjectStructure` materializa todos los chunks vacíos** del rectángulo (por defecto **4×4 = 16 chunks** en el .map); visor inicial centrado. Sin `editor-state.json`, se ajusta el scroll al centro (tras clamp).

**Mapa infinito (editor):** si `Infinite == true`, el lienzo es una **ventana deslizante** (viewport del scroll + marco de cámara + padding en chunks según `ChunkLoadRadius`), con tope (~384 casillas por eje) para no generar millones de píxeles ni colgar WPF. **No** se une el bounding box de todo el mapa. Los tiles/chunks solo se **dibujan** si intersectan esa ventana; al pintar se crean chunks bajo demanda. Si el unión viewport+cámara supera el tope, la ventana fija se **centra en el viewport** (donde estás mirando) y solo se **desplaza** lo mínimo para incluir el rectángulo de la cámara (marco azul). La extensión del lienzo en mapas infinitos **no se reduce** al mover el scroll (solo crece según necesidad), para que el **thumb** de las barras no cambie de tamaño constantemente; el dibujado de tiles/objetos se **acota** a la región visible. El área de scroll del mapa fuerza **izquierda‑derecha** LTR. `ScrollChanged` solo encola `DrawMap` cuando cambia el offset del scroll o el tamaño del viewport, no cuando solo crece la **Extent** (evita bucles de redibujado y la vista «corriendo» sola).

---

## 11. Scripting Lua: APIs y eventos

### Tablas globales por script

| Tabla | Uso |
|-------|-----|
| `self` | `SelfProxy` — transform, `getComponent`, eventos |
| `world` | `WorldApi` — entidades, tiles, **`findNearestByTag`**, **`instantiate(..., variant?)`**, raycast del host (objetos + mismo criterio sólido que Play) |
| `input` | `InputApi` — teclado/ratón (implementado en host, p.ej. `WpfPlayInputApi`) |
| `time` | `TimeApi` — `delta`, `time`/`seconds`, `frame`, `scale` |
| `audio` | `AudioApi` — play, música, volúmenes |
| `physics` | `PlayScenePhysicsApi` — **`physics.raycast`** / **`physics.overlapCircle`** solo **colliders** (incl. triggers en overlap); **no** incluye tilemap |
| `ui` | `UiApi` — canvas runtime (`UIRuntimeBackend`) |
| `game` | `GameApi` — `loadScene`, `quit`, **`setRandomSeed`**, **`randomInt`**, **`randomDouble`** (validación de tipos en el host) |
| `log` | `LuaLogApi` — `info` / `warn` / `error` (en Play del editor van a la consola con nivel) |
| `Debug` | `DebugDrawApi` — depuración dibujada (si se inyecta) |

**`WorldApi` (métodos destacados):** `SetWorldContext`, `ConfigurePlayTilemap`, `ConfigurePlayViewport`, **`getPlayViewportLeft`**, **`getPlayViewportTop`**, **`getPlayViewportWidthTiles`**, **`getPlayViewportHeightTiles`** (rectángulo de vista lógica: mismo criterio que el marco azul; en Play el motor pasa el tamaño en px del canvas del tab Juego para que resolución Auto y rebotes Lua coincidan con el render), `getTile` / `setTile`, `findObject` / `getObjectByName`, `findByTag`, `getObjects`, `findByPath`, `instantiate` / `spawn`, `destroy`, `setPosition`, `getPlayer`, `raycast`, `raycastTiles`, `raycastCombined`.

### `SelfProxy` (tabla `self` en Lua)

Propiedades: `id`, `name`, **`tag`** (primera etiqueta), **`tags`** (array), `x`, `y`, `rotation`, `scale`, `visible`, **`active`** (refleja **`GameObject.RuntimeActive`** y **`!PendingDestroy`**), `renderOrder`, `spriteFrame`. Métodos: **`hasTag(name)`**, **`destroy`** (delegado en **`world.destroy`** en Play → destrucción diferida), `move`, `rotate`, `playAnimation` / `stopAnimation`, `setSpriteTexture`, `addSpriteFrame` / `clearSpriteFrames`, `setSpriteAnimationFps`, `setSpriteSortOffset`, `setSpriteDisplaySize`, `getComponent`, `addComponent` / `removeComponent`, jerarquía `find`, `findInHierarchy`, `setParent`, `getParent`, `getChildren`, `instantiate` (delegado en mundo; scripts del seed **siguiente tick** salvo anidación en **`onAwake`**).

### `ScriptInstance` (C#, no es tabla Lua directa)

`Get` / `Set` de variables en el entorno del script; `HasFunction`, `Invoke`, `TryInvoke` (eventos como `onUpdate` sin lanzar excepción al fallo). El Inspector/Debug tab pueden leer variables vía `Get`. Tras hot reload conviene `Dispose` y recrear instancias.

### `InputApi` en Play (WPF)

**`WpfPlayInputApi`:** `isKeyDown` para **W, A, S, D, LEFT, RIGHT, UP, DOWN, SPACE, E, Q, F, ENTER, SHIFT, CTRL** (cadenas en mayúsculas). **`mouseX` / `mouseY`** y **`isMouseDown(0)`** / **`"LEFT"`** según el viewport (tab Juego / `PlayerWindow`). En el tab Juego, **Espacio** sigue reservado para pausar/reanudar en **`GameTabContent_KeyDown`** (no entra en el snapshot vía `PreviewKeyDown`).

### `IGraphicsDevice` vs Vulkan

La interfaz **`IGraphicsDevice`** solo expone `IsValid`, `Width`, `Height`, `SetClearColor`, `BeginFrame`, `EndFrame`, `Clear`, `IDisposable`. El **handle de ventana** está en la clase concreta **`VulkanGraphicsDevice.WindowHandle`** (solo válido si se creó con `CreateWithWindow`).

### Resultados de raycast (Lua)

- **`RaycastHitInfo`:** `hit` (`SelfProxy`), `distance`, `x`, `y` (punto de impacto en casillas).
- **`TileRaycastHitInfo`:** `tileX`, `tileY`, `distance`, `x`, `y` (punto de impacto en casillas).
- **`CombinedRaycastHitInfo`:** `kind` (`"tile"` \| `"object"`), `hit` (proxy si objeto), `tileX`/`tileY` (o -1 si no aplica), `distance`, `x`, `y`.

### `ComponentProxy`

Proxy delgado para exponer un **`Component`** concreto a Lua vía `getComponent` (según tipo); ver `ComponentProxy.cs` junto a `SelfProxy`.

### Eventos conocidos (`KnownEvents`)

Incluye: **`onAwake`** (al crear instancia, tras propiedades), **`onStart`** (primer frame, antes del primer **`onUpdate`**), `onInteract`, `onCollision`, `onFear`, `onSpawn`, **`onDestroy`** (al hacer flush de destrucción), `onRepair`, `onHack`, `onUpdate`, `onLateUpdate`, `onTriggerEnter`, `onTriggerExit`, `onTrigger`, `onDayStart`, `onNightStart`, `onPlayerMove`, `onZoneEnter`, `onZoneExit`, `onChildAdded`, `onChildRemoved`, `onParentChanged`.  
`IsReservedScriptVariableName` evita que estos nombres se editen como variables personalizadas.  
No todos los hooks opcionales se invocan automáticamente en cada versión del runtime (p. ej. `onCollision` / `onInteract` pueden depender de que el host los dispare); la ayuda in-app (tema «Eventos y hooks Lua») lo advierte. Comprueba el código de `GameLoop` / `PlayModeRunner` / física si el comportamiento no coincide con lo esperado.

---

## 12. Inventario FUEngine.Core

Carpeta raíz del proyecto: `FUEngine.Core/`. Solo archivos fuente `.cs` (sin `obj`/`bin`).

| Archivo | Rol |
|---------|-----|
| **Animation/** `AnimationDefinition.cs` | Definición de clip (frames, fps). |
| | `AnimationController.cs` | Control de reproducción. |
| **Assets/** `AssetCache.cs` | Caché genérica por ruta/id. |
| | `AssetDatabase.cs` | Base de datos de assets. |
| | `ResourceLoader.cs` | Carga de recursos. |
| | `ScriptAsset.cs`, `SoundAsset.cs`, `TextureAsset.cs` | Tipos de asset. |
| **Audio/** `AudioManager.cs`, `SoundSource.cs` | Audio de alto nivel. |
| **Engine/** `DebugOverlay.cs` | Configuración de overlay de depuración. |
| | `EngineVersion.cs` | `Current = "0.0.1"`. |
| | `GameTiming.cs` | Delta/tiempo total/target FPS. |
| | `ProjectEngineCompatibilityChecker.cs` | Compatibilidad proyecto/motor. |
| | `SplashScreenConfig.cs` | Datos de splash. |
| **Graphics/** `IGraphicsDevice.cs` | Interfaz del backend gráfico. |
| **Input/** `InputManager.cs` | Entrada abstracta. |
| **Lighting/** `LightSource.cs`, `LightingManager.cs` | Luces (stubs/preparación). |
| **Map/** `AutoTiling.cs` | Autotiling por vecinos/máscara. |
| | `Chunk.cs` | Chunk `DefaultSize = 16`. |
| | `LayerBlendMode.cs`, `LayerType.cs` | Enumeraciones de capas. |
| | `MapLayerDescriptor.cs` | Descriptor de capa del mapa. |
| | `PixelLayer.cs` | Capa de píxeles auxiliar. |
| | `Tile.cs`, `TileAnimation.cs`, `TileMaterial.cs` | Tipos de tile avanzados. |
| | `TileCatalogHelper.cs` | Colocación desde catálogo/tileset. |
| | `TileData.cs` | Celda: colisión, catálogo, overlay, tags. |
| | `TileMap.cs` | Mapa multicapa por chunks. |
| | `TileMapRaycast.cs` | Raycast DDA en celdas. |
| | `TilemapChunk.cs`, `TilemapLayer.cs` | Sistema tilemap alternativo. |
| | `TilePixelOverlay.cs` | RGBA por tile. |
| | `Tileset.cs` | Definición de tileset. |
| | `TilesetPersistence.cs` | Load/save tileset JSON. |
| | `TileType.cs` | Enum de tipo lógico. |
| | `LayerOrder.cs` | Constantes de orden visual recomendado (Background…Foreground) para tilemap alternativo. |
| **Objects/** `ColliderComponent.cs` | Collider en componente. |
| | `Component.cs` | Base de componentes. |
| | `GameObject.cs` | Entidad con hijos y componentes. |
| | `LightComponent.cs` | Luz en entidad. |
| | `ObjectDefinition.cs`, `ObjectInstance.cs` | Blueprint e instancia. |
| | `ObjectLayer.cs` | Capa de objetos del nivel. |
| | `ScriptComponent.cs` | Referencia a script Lua. |
| | `SpriteComponent.cs`, `SpriteFrameRegion.cs` | Sprite y regiones. |
| | `Transform.cs` | Transform 2D. |
| **Physics/** `CollisionBody.cs` (opcional), `PhysicsWorld.cs` (`StepPlayScene`), `ScenePhysicsQueries.cs` | Física 2D Play. |
| **Plugins/** `PluginInterface.cs`, `ILuaEngineBinding.cs`, `IEngineContext.cs`, `PluginLoader.cs` | Plugins .NET (`IPlugin`); Lua con `RegisterLuaHost(lua, IEngineContext)` + `plugins-manifest.json`. |
| **Project/** `FiniteMapExpand.cs` | Expansión de mapa finito (+chunks en frontera; coordina con el editor). |
| | `GameViewportMath.cs` | Matemáticas vista/resolución. |
| **Project/** `ProjectRenderSettings.cs` | Normalización de `renderAntiAliasMode`, `textureFilterMode`, muestras MSAA. |
| | `ProjectIndexPaths.cs` | Resolución de rutas de índices JSON (`Data/` vs raíz legacy). |
| | `ProjectInfo.cs` | Configuración completa del proyecto. |
| | `ProjectSchema.cs` | `CurrentFormatVersion` y esquema de `Project.FUE`. |
| | `Scene.cs`, `SceneDefinition.cs`, `SceneObject.cs` | Escenas y definiciones. |
| **Rendering/** `Renderer.cs` | Wrapper sobre `IGraphicsDevice`. |
| | `SpriteRenderer.cs`, `TileRenderer.cs` | Render de sprites/tiles (según uso). |
| **Scripts/** `KnownEvents.cs` | Eventos reservados. |
| | `ScriptDefinition.cs`, `ScriptRegistry.cs` | Registro de scripts. |
| | `ScriptInstancePropertySet.cs`, `ScriptPropertyEntry.cs` | Propiedades por instancia. |
| **Seeds/** `SeedDefinition.cs`, `SeedInstance.cs` | Seeds y colocación. |
| **Triggers/** `TriggerZone.cs` | Zonas rectangulares. |
| **UI/** `UIAnchors.cs`, `UICanvas.cs`, `UIElement.cs`, `UIElementKind.cs`, `UIPrefabPolicy.cs`, `UIRect.cs`, `UIRoot.cs` | Modelo de UI en datos. |

---

## 13. Inventario FUEngine.Editor

| Archivo | Rol |
|---------|-----|
| **DTO/** `AnimationsDto.cs`, `AudioManifestDto.cs`, `GlobalLibraryDto.cs`, `MapDto.cs`, `ObjectsDto.cs`, `ProyectoConfigDto.cs`, `ProjectDto.cs`, `ScriptsDto.cs`, `SeedDto.cs`, `TriggerZoneDto.cs` | Contratos JSON. |
| **Scripting/** `LuaScriptVariableParser.cs` | Análisis de variables Lua para el editor. |
| **Serialization/** `AnimationSerialization.cs`, `AudioManifestSerialization.cs`, `GlobalLibrarySerialization.cs`, `MapSerialization.cs`, `ObjectsSerialization.cs`, `ProyectoConfigSerialization.cs`, `ProjectSerialization.cs`, `ScriptSerialization.cs`, `SeedSerialization.cs`, `SerializationDefaults.cs`, `TriggerZoneSerialization.cs`, `UICanvasSerialization.cs` | Save/Load por dominio. |
| **Services/** `NewProjectStructure.cs` | Plantilla de carpetas y proyecto (`Project.FUE`, escenas `.map`/`.objects`, etc.). |
| | `SceneDescriptorSync.cs` | Sincroniza metadatos `.scene` al guardar el proyecto. |
| `ProjectFormatMigration.cs` | Migración additiva de `projectFormatVersion` (`Project.FUE`); usado con `ProjectFormatOpenHelper` en la app. |

---

## 14. Inventario FUEngine.Runtime

| Archivo | Rol |
|---------|-----|
| `Camera.cs` | Cámara 2D. |
| `CombinedRaycastHitInfo.cs` | Resultado `raycastCombined`. |
| `ComponentProxy.cs` | Proxy de componente hacia Lua. |
| `DebugDrawApi.cs` | API de debug draw + enum `DebugDrawKind`. |
| `GameLoop.cs` | Bucle principal. |
| `IWorldContext.cs` | Interfaz de consulta/instanciación en el mundo. |
| `LuaEnvironment.cs` | Estado NLua y entornos (sandbox; `require` lo inyecta `LuaRequireSupport`). |
| `LuaErrorLineParser.cs` | Extrae número de línea de mensajes Lua/NLua (`:(\d+):`); usado por `LuaScriptRuntime` y por el editor al mostrar errores. |
| `LuaErrorFormatter.cs` | Formato uniforme de mensajes `path:line` para `ScriptError`. |
| `LuaLogApi.cs` | Tabla global `log` (`info`/`warn`/`error`). |
| `LuaTypeCheck.cs` | Conversiones seguras en el borde Lua→C# (`ToInt32`, `ToDouble`). |
| `LuaRequireSupport.cs` | Inyección de `require` / `package` solo para módulos bajo `Scripts/`; caché invalidada en `ReloadScript`. |
| `LuaScriptRuntime.cs` | Runtime principal de scripts (`SetLuaLogSink`, `RegisterEnginePlugins`, getters de API). |
| `PlayLuaEngineContext.cs` | Implementación de `IEngineContext` en Play. |
| `LuaScriptSyntaxChecker.cs` | `TryValidate`: compila chunk con `load(..., 't', {})` sin ejecutar (misma idea que carga de script); usado por el mini-IDE para subrayar errores de sintaxis. |
| `LuaTileGenerator.cs` | Generación de tiles vía Lua. |
| `LayerProxy.cs` | Tabla `layer` en scripts de capa. |
| `Mathematics/PerlinNoise.cs` | Ruido Perlin. |
| `PlayScenePhysicsApi.cs` | Tabla Lua `physics` (raycast / overlapCircle sobre colliders). |
| `RaycastHitInfo.cs` | Golpe raycast objetos. |
| `SceneManager.cs` | Escenas. |
| `ScriptBindings.cs` | `WorldApi`, `InputApi`, `TimeApi`, `AudioApi`, `PhysicsApi`, `UiApi`, `GameApi`, `LuaLogApi`, `AdsApi`, `DebugDrawApi` opcional, constantes `Key`/`Mouse`. |
| `AdsApi.cs` | Tabla Lua `ads` (callbacks opcionales; base stub). |
| `GoogleMobileAdsApi.cs` | Contrato nativo para export / host móvil (no WPF). |
| `LuaVisibleAttribute.cs` | Marca tipos API para reflexión del mini-IDE (`LuaEditorApiReflection`). |
| `ScriptInstance.cs` | Instancia de script. |
| `ScriptLoader.cs` | Carga de archivos `.lua`. |
| `SelfProxy.cs` | Proxy `self`. |
| `TileGeneratorCanvas.cs` | Canvas de generación (API de generador). |
| `TileGeneratorPropertyDefinition.cs` | Definición de propiedades del generador. |
| `TileRaycastHitInfo.cs` | Golpe raycast tiles. |
| `UIRuntimeBackend.cs` | Backend de UI en tiempo de ejecución. |

---

## 15. Inventario FUEngine.Graphics.Vulkan

| Archivo | Rol |
|---------|-----|
| `VulkanGraphicsDevice.cs` | `Create()` headless, `CreateWithWindow(w,h,title)`, swapchain, comandos, present. |

---

## 16. Inventario FUEngine (app WPF)

Código del editor **WPF** (`FUEngine.csproj` en la carpeta `FUEngine/` del repo). Listado de fuentes **excluyendo** `obj` y `bin`. No lista cada ventana con descripción larga: van agrupadas (Dialogs, Tabs, Panels…).

### App y ensamblado
- `App/App.xaml`, `App.xaml.cs` — arranque WPF, recursos globales.
- `AssemblyInfo.cs` — metadatos.

### Controles
- `CollisionShapesCanvasControl` — edición visual de formas de colisión.
- `DrawingCanvasControl` — lienzo de dibujo (paint pipeline).
- `DocumentationHostControl` / `DocumentationView` — ayuda integrada (tres pestañas: Manual, Lua, Ejemplos; `ToolTip` en pestañas; `PinIntroPreserveOrder` en Lua/Ejemplos).
- `GlobalScriptsHubPanel` — Hub: scripts `.lua` en `SharedAssets/Scripts` con mini-IDE.
- `LuaHighlightingLoader` — registra `LuaFUE` + `Lua.xshd` en `HighlightingManager`.
- `LuaSyntaxErrorLineRenderer` — marca línea con error de sintaxis Lua (ver §24.3).
- `NewProjectWizardPanel` — asistente de nuevo proyecto (overlay del Hub o del editor).
- `ScriptEditorControl` — AvalonEdit embebido; `LuaScriptSyntaxChecker` (debounce ~380 ms) para errores de sintaxis en `.lua`/`.script`.
- `CreateScriptFromExampleEventArgs` — evento al crear `.lua` desde un ejemplo de la ayuda.

### Converters
- `BoolToPinLabelConverter`, `NullToVisibilityConverter`, `PathToFileNameConverter`, `StringToBrushConverter`.

### Dialogs
- `AboutWindow`, `BuildExportWindow`, `CleanOrphansDialog`, `ConfirmDeleteProjectDialog`, `CreateFromTemplateDialog`, `ExportPartialWindow`, `GlobalLibraryBrowserWindow`, `ImportSceneAssetScanDialog`, `PixelEditWindow`, `ProjectConfigWindow`, `ScriptEditorWindow`, `SettingsPage`/`SettingsWindow`, `ShortcutsHelpContent`, `ShortcutsWindow`, `SimulateWindow`, `SnapshotPickerWindow`, `TemplatePickerWindow`, `TilesetEditorWindow`, `UnusedAssetsDialog`.
- `EditorShortcutBindings`, `EditorShortcutPresets`, `EditorShortcutRegistry` — sistema de atajos.

### Editor (lógica)
- `EditorCommands.cs` — historial y comandos (ver sección 5).
- `EditorLog.cs` — log interno.
- `MapSnapshot.cs` — modelo de snapshot.
- `ZoneClipboard.cs` — modelo de portapapeles de zona.

### Input (Play)
- `PlayKeyboardSnapshot.cs` — snapshot de teclas para Play.
- `WpfPlayInputApi.cs` — implementación de `InputApi` desde WPF.

### Models
- `ProjectExplorerItem.cs`, `RecentProjectInfo.cs`.

### Panels (inspectores y dock)
- `AnimationInspectorPanel`, `DefaultInspectorPanel`, `LayerInspectorPanel`, `LayersPanel`, `LogPanel`, `MapHierarchyPanel`, `MapPropertiesInspectorPanel`, `MultiObjectInspectorPanel`, `ObjectInspectorPanel`, `PlayHierarchyPanel`, `ProjectExplorerPanel`, `ProjectManifestPanel`, `QuickPropertiesPanel`, `TileInspectorPanel`, `TriggerZoneInspectorPanel`, `UIElementInspectorPanel`.

### Rendering
- `GameViewportRenderer.cs`, `MapRenderContext.cs`, `MapRenderer.cs`, `PlayTileBitmapCompositor.cs`.

### Spotlight (búsqueda global)
- `Spotlight/SpotlightControl`, `SpotlightIndex`, `SpotlightItem`, `LuaSpotlightDescriptions`, `LuaLanguageKeywords`, `LuaSpotlightBuiltins` — índice y UI (ver §24.10).

### Mini-IDE / reflexión Lua (raíz del ensamblado)
- `LuaEditorCompletionCatalog.cs`, `LuaEditorApiReflection.cs`, `LuaCompletionIcons.cs`, `LuaScriptVariableDiscovery.cs` — autocompletado, iconos y lista de globales para inspector.

### Services (lógica de aplicación)
- `CrashReportWriter.cs` — `crash_*.txt` en AppData ante fallos no capturados (junto a logs de sesión).
- `EditorLogServiceAdapter.cs` — adapta `IEditorLog` al `ServiceLocator`.
- `ImageNearestNeighborResize.cs` — reescala PNG/JPEG/BMP (vecino más cercano); usado con `ImageResizeDialog`.
- `NewProjectCreation.cs` — creación en disco desde `NewProjectWizardPanel` (Hub y menú del editor, sin ventana nueva).
- `DiscordRichPresenceService.cs` — Rich Presence de Discord (NuGet **DiscordRichPresence**): `DiscordRpcClient` con **autoEvents: true** (constructor de 5 parámetros; evita depender de `Invoke()` manual); Hub, pestañas del editor, Play embebido y ventana Play; botón **«Ver en GitHub»** → `https://github.com/redredtidxd/FUEngine` vía `SetPresence` + **`UpdateButtons`** (refuerzo IPC); `OnError` → `EditorLog` si Discord rechaza el payload; asset **`logo_principal`** debe coincidir con el portal; si el botón no se ve en el cliente, revisar portal (Rich Presence / URLs) y perfil de usuario (actividad expandida).
- `AudioAssetRegistry.cs`, `AudioSystem.cs`, `AutoSaveService.cs`, `CanvasControllerLuaTemplate.cs`, `CreativeSuiteMetadata.cs`, `DefaultLuaScriptTemplate.cs`, `EditorAudioBackend.cs`, `ExplorerMetadataService.cs`, `FUEngineAppPaths.cs`, `GlobalAssetLibraryService.cs`, `IAudioBackend.cs`, `IAudioHandle.cs`, `MapSnapshotService.cs`, `NativeAutoAnimationApplier.cs`, `NativeProtagonistController.cs`, `PlayerLaunchArgs.cs`, `PlayModeRunner.cs`, `PlayNaudioAudioEngine.cs`, `ProjectBuildService.cs`, `ProjectExportHelper.cs`, `ProjectFormatOpenHelper.cs`, `ProjectIntegrityChecker.cs`, `ProjectManifestPaths.cs`, `ProjectThumbnailService.cs`, `SceneAssetReferenceCollector.cs`, `ScriptHotReloadWatcher.cs`, `ScriptRegistryProjectWriter.cs`, `SelectionManager.cs`, `SimulatedAdsApi.cs`, `StartupService.cs`, `TemplateProvider.cs`, `TextureAssetCache.cs`, `TileDataFile.cs`, `TilePaintService.cs`, `UnusedAssetScanner.cs`, `WpfPlayAudioApi.cs`, `ZoneClipboardService.cs`.

### Settings
- `EngineFontPresets.cs`, `EngineSettings.cs`, `EngineTypography.cs`.

### Tabs (contenido del editor central)
- `AnimationsTabContent`, `AudioTabContent`, `CollisionsEditorTabContent`, `ConsoleTabContent`, `DebugTabContent`, `ExplorerTabContent`, `GameTabContent`, `ObjectsTabContent`, `PaintCreatorTabContent`, `PaintEditorTabContent`, `PlaceholderTabContent`, `ScriptableTileTabContent`, `ScriptsTabContent`, `TileCreatorTabContent`, `TileEditorTabContent`, `TilesTabContent`, `UITabContent`.

### Tools
- `IMapEditorToolContext.cs`, `ITool.cs`, `PaintTool.cs`, `PlaceholderGenerator.cs`, `ToolController.cs`.

### Windows
- `DocumentationWindow`, `EditorWindow` (+ `EditorWindow.Menus.cs`, `EditorWindow.Inspector.cs`, `EditorWindow.Discord.cs`, `EditorWindow.DocumentationEmbedded.cs`, `EditorWindow.Spotlight.cs`), `GamePlayWindow`, `PlayerWindow`, `SplashScreenWindow`, `StartupWindow`.

### Ayuda (contenido)
- `FUEngine/Help/EngineDocumentation.cs` (+ `EngineDocumentation.Presentation.cs`) — `BuildTopics()`, manual general + `ApplyManualPresentation`.
- `LuaReferenceDocumentation.cs` — pestaña Lua (`kw`/`Guide`).
- `ScriptExamplesDocumentation.cs` — pestaña Ejemplos.

### Otros
- `TileImageLoader.cs` — carga de bitmaps para tiles.

---

## 17. Archivos JSON típicos

| Archivo (relativo al proyecto) | Contenido |
|-------------------------------|-----------|
| `proyecto.json` / **`Project.FUE`** / nombre configurado | `ProjectDto` / `ProjectInfo` (incl. `projectFormatVersion`, `engineVersion`) |
| `proyecto.config` | `ProyectoConfigDto` (opcional; ver `ProyectoConfigSerialization`) |
| `mapa.json` (o ruta en escena) | `MapDto` |
| `objetos.json` | `ObjectsDto` |
| `triggerZones.json` | Lista de triggers |
| `seeds.json` | Seeds |
| `scripts.json` | Registro de scripts |
| `animaciones.json` | Animaciones |
| UI (ruta según proyecto) | `UICanvasSerialization` |
| `audio.json` | Manifiesto de audio |
| `Library/library.json` (assets globales) | `GlobalLibraryManifestDto` |
| Caché Play streaming | bajo `.fue_play_chunk_cache/` (cuando aplica) |

**Proyectos nuevos (`NewProjectStructure`):** además de rutas legacy (`mapa.json` en algunos flujos), la plantilla moderna usa **`Project.FUE`** como archivo de proyecto, **`Settings.json`**, carpeta **`Data/`** con índices (`scripts.json`, `seeds.json`, `animaciones.json`, `audio.json` referenciado por `AudioManifestPath = Data/audio.json`), extensiones **`.map`** / **`.objects`** por escena (p. ej. `Maps/Start/map.map`, `Objects/Start/objects.objects`), carpetas `Assets` (incl. `Sprites`, `Audio`, etc.), **`Scenes/`** con un archivo **`.scene`** por escena (JSON de metadatos; se regenera al guardar `Project.FUE` vía `SceneDescriptorSync`), `Scripts`, `Seeds`, `Autoguardados/Mapa|Objetos|Escenas`, escenas por defecto **Start** y **End** con `SceneDefinition` (`DefaultTabKinds`, `UIFolderRelative` por escena). El **Explorador** puede ocultar **`Data/`** (`EngineSettings.HideDataFolderInExplorer`, predeterminado **true**). El mapa inicial de escena es **finito en datos** (**tamaño en casillas** = `InitialChunksW/H × ChunkSize`; **`DefaultMapChunksPerSide` = 4** ⇒ cuadrícula **4×4 = 16 chunks** vacíos escritos en el `.map`); el asistente **`NewProjectWizardPanel`** fija **`Infinite = false`** en `ProjectInfo` al crear. `EditorViewportCenterWorld*` en el **centro** del rectángulo (`MapWidth/2`, `MapHeight/2`). Opcional **`Generar jerarquía estándar`**: crea en la raíz las carpetas del orden en **Configuración del motor → Explorador** (`GetResolvedNewProjectStandardRootFolders`) y **`EnsureStandardRootFolders`** también fusiona **`extraNewProjectRootFolders`** + tema (`newProjectExplorerThemeId`, `GetResolvedExtraNewProjectRootFolders`). Constantes útiles: `DefaultMapChunksPerSide`, `DefaultStandardRootFolderNames`, `MapFileExtension`, `ObjectsFileExtension`, `DefaultScenes`.

Plantilla reciente: **`audio.json`** mínimo en raíz, **`Assets/Textures`**, tileset por defecto (`Assets/Tilesets` + `default.tileset.json` y `DefaultTilesetPath` en proyecto); **`scripts.json`** / **`animaciones.json`** pueden generarse al primer uso (`EnsureProjectFolders` / escritores). **`ExplorerMetadataService`** evita crear **`.fuengine-explorer.json`** mientras el estado sea el vacío por defecto. La escena **Start** puede fijar **`DefaultTabKinds`** (p. ej. **Mapa** por defecto en código); en **`EditorWindow.xaml`** el orden es **Mapa**, **Juego**, **Consola**. **`.editorstate`** puede incluir **`selectedTabKind`**; los índices antiguos sin kind se mapean a ese orden.

---

## 18. Buscar por tema

Índice **por tema funcional** → clases o carpetas típicas. Las rutas son relativas al repo salvo `Core/` = `FUEngine.Core`. Si no aparece un tema, usa búsqueda en el IDE o §12–§16.

| Tema | Dónde mirar |
|------|-------------|
| Mapa multicapa, chunks | `Core/Map/TileMap.cs`, `MapLayerDescriptor`, `Editor/DTO/MapDto.cs` |
| Catálogo / tileset | `TileData`, `TileCatalogHelper`, `TilesetPersistence`, `WorldApi.setTile` |
| Serialización | `FUEngine.Editor/Serialization/*.cs` |
| Undo/redo | `FUEngine/Editor/EditorCommands.cs` |
| Dibujado mapa WPF | `MapRenderer.cs`, `MapRenderContext.cs` |
| Play Mode | `PlayModeRunner.cs`, `GameTabContent.xaml.cs`, `NativeProtagonistController.cs` |
| Lua | `LuaScriptRuntime.cs`, `ScriptBindings.cs`, `SelfProxy.cs` |
| Vulkan | `VulkanGraphicsDevice.cs`, `IGraphicsDevice.cs` |
| Audio proyecto | `AudioManifestSerialization`, `WpfPlayAudioApi`, `PlayNaudioAudioEngine` |
| Biblioteca global | `GlobalAssetLibraryService`, `GlobalLibrarySerialization` |
| Export / build | `ProjectBuildService`, `BuildExportWindow` |
| Atajos | `EditorShortcutRegistry`, `ShortcutsWindow` |
| Herramientas del mapa (no ITool) | `EditorWindow.xaml.cs` — `ToolMode`, `HandleToolMouseDown` |
| TileMap streaming / runtime | `TileMap.EvictEmptyChunksBeyond`, `SetTileFromRuntime`, `MarkRuntimeTouched` |
| Breakpoints Lua | `LuaScriptRuntime.SetBreakpoints`, `PlayModeRunner.SetBreakpoints` |
| Proyecto / plantilla | `NewProjectStructure`, `TemplateProvider`, `StartupService` |
| Config UI proyecto | `ProjectConfigWindow`, `ProyectoConfigSerialization` |
| Integridad / huérfanos | `ProjectIntegrityChecker`, `CleanOrphansDialog`, `UnusedAssetScanner` |
| Metadata explorador | `ExplorerMetadataService`, `CreativeSuiteMetadata` |
| Argumentos player | `PlayerLaunchArgs` |
| Resolución visor / casillas | `GameViewportMath` |
| Hot reload Lua | `ScriptHotReloadWatcher` (debounce, `FileSystemWatcher`) |
| Discord Rich Presence | `DiscordRichPresenceService`, `EditorWindow.SyncDiscordRichPresence` / `BeginModalDiscordPresence`, `StartupWindow.RefreshDiscordStartupPresence`; Hub por pestaña (cabecera XAML), modales (config motor/proyecto, export, ayuda, Spotlight…) |
| Autoguardado | `AutoSaveService` (`.tmp` → definitivo, subcarpetas Mapa/Objetos) |
| UI runtime Play | `UIRuntimeBackend` (`Show`/`Hide`/`SetFocus`, `pushState`/`popState` máx. 16 niveles, `bind`, `CallbackError`) |
| Parser variables script | `LuaScriptVariableParser` |
| Errores sintaxis en editor (línea roja) | `LuaScriptSyntaxChecker`, `LuaSyntaxErrorLineRenderer` (`IBackgroundRenderer`), `ScriptEditorControl` |
| Parser línea en mensaje Lua | `LuaErrorLineParser` (runtime; mensajes `archivo:linea:`) |
| `require` bajo Scripts/ | `LuaRequireSupport`, caché en `LuaScriptRuntime` |
| Contratos sin WPF (`FUEngine.Service`) | Ver §25 — `IEditorLog`, `IBuildService`, `IScriptHotReloadWatcher`, etc. |

---

## 19. Paquetes NuGet por proyecto

| Proyecto | Paquetes |
|----------|----------|
| **FUEngine** | **AvalonEdit** (editor de scripts / resaltado; `Lua.xshd` embebido), **NAudio** (audio en editor y Play), **DiscordRichPresence** (RPC con Discord; imagen `logo_principal` en el Developer Portal). **WPF** + **Windows Forms** habilitados en el `.csproj`. |
| **FUEngine.Runtime** | **NLua** |
| **FUEngine.Graphics.Vulkan** | **Silk.NET.Vulkan**, **Silk.NET.Glfw**, **Silk.NET.Vulkan.Extensions.KHR** (`AllowUnsafeBlocks`) |
| **FUEngine.Core** | Sin paquetes NuGet en el proyecto típico (solo BCL). |
| **FUEngine.Editor** | Sin paquetes NuGet en el proyecto típico. |

---

## 20. Limitaciones, stubs y comportamientos a tener en cuenta

- **`PhysicsWorld`:** **`StepPlayScene`** es el único paso AABB en Play (tiles + objetos + triggers). **`physics.*`** y **`world.raycast`** comparten la misma geometría de colliders sólidos, pero **`world.raycast`** es la API de “escena/host”; **`physics.overlapCircle`** no tiene equivalente en `world` todavía.
- **Ciclo de vida scripts:** **`onAwake`** → **`onStart`** (1×) → **`onUpdate`** / **`onLateUpdate`**; **`onDestroy`** al cerrar el frame tras **`self.destroy()`** / **`world.destroy`**. Hot reload llama **`InvokeOnStartFor`** tras recrear la instancia. **Pooling** sigue siendo manual (no hay pool en motor).
- **Tags:** `GameObject.Tags` en Play; Lua **`world.findNearestByTag`**, **`self.tags`**, **`self.hasTag`**.
- **`world.getPlayer`:** busca el primer objeto llamado `"Player"`; el protagonista configurado por proyecto se usa en el controlador nativo, no necesariamente en esta API Lua.
- **Iluminación:** **Vulkan** mínimo. Visor WPF: tiles con **`SampleBrightness`**; sprites con **`SampleRgbTint`** + **`SpriteBitmapTint`** (color por **`LightComponent.ColorHex`**).
- **`Key` / `Mouse` en Lua:** ampliados (ver §11); coordenadas de ratón en **píxeles del canvas** del visor, no en casillas mundo.
- **Herramientas:** solo **Pintar** está refactorizada como `ITool`; el resto depende de **`EditorWindow`** (`HandleTool*` vía `ToolController` fallback). Para nuevas herramientas: **`ITool` + `IMapEditorToolContext`** o extender esos handlers.
- **`BootstrapScriptId`:** guardado en proyecto/UI; **`PlayModeRunner` no lo ejecuta** al iniciar Play.
- **`seeds.json`:** variante **`name_variant`**; objetos en escena al instante, **scripts encolados** al inicio del siguiente tick (o el mismo si **`onAwake`** anida más spawns — cola drenada en bucle).
- **`SceneManager.LoadScene(string)`:** no carga mapa ni objetos; asigna un **`Scene`** mínimo (id/nombre) — usar `PlayModeRunner` / flujo del editor para simulación real.
- **`UICanvasSerialization.Load`:** deserialización fallida → `new UICanvas()` sin excepción.
- **`PluginLoader`:** `LoadFromDirectory` lee `Plugins/plugins-manifest.json` (`assemblies`: cadenas u objetos con `name`/`path` y `version` opcional); carga ensamblados con `IPlugin`; `UnloadAll` en teardown de Play. **`ILuaEngineBinding`:** `RegisterLuaHost(lua, IEngineContext)`. **`IEngineContext`:** world, `ProjectInfo`, `GetRuntimeApi`, `GetService`. **`ProjectEngineCompatibilityChecker`:** compara `EngineVersion` del proyecto con `EngineVersion.Current`.
- **`EditorLog`:** `LogLevel`, `MaxEntries`, `Entries`, `EntryAdded`, `ToastRequested`; Lua `print` → `PrintOutput` (suele ir al log).

---

## 21. `EngineSettings` / `settings.json`

Configuración **global del editor** (no del juego), típicamente en `settings.json` junto al ejecutable o perfil de usuario según implementación de carga:

- **UI:** `Theme`, `UiScalePercent`, `Language`, `ShowTipsOnStartup`, `WelcomeOverlayDismissedOnce`, fuentes del editor.
- **Proyecto por defecto:** `DefaultProjectFps`, etc.
- **Actualizaciones:** `AutoUpdateCheckEnabled`, `AutoUpdateChannel`.
- **Arranque:** `StartupBehavior` (Hub, LastProject, NewProject).
- **Rendimiento:** `HardwareAccelerationEnabled`.
- **Atajos:** `ShortcutBindings` (mapa acción → texto), `ShortcutPreset` (Default, Unity, Photoshop, Custom).
- **Autoguardado del motor vs proyecto:** flags para usar intervalo global del motor.
- **Explorador (carpetas al crear proyectos):** `newProjectRootFolderOrderPresetId` (`default` | `custom`), `customNewProjectRootFolderOrder`, `extraNewProjectRootFolders`, `newProjectExplorerThemeId` (`none` | `ui` | `jam` | `content`), `defaultNewProjectDebugMode`; `GetResolvedNewProjectStandardRootFolders()`, `GetResolvedExtraNewProjectRootFolders()`. `MergeStandardAndExtraRootFolders` + `EnsureStandardRootFolders` en `NewProjectStructure.Create`. `autoLogsEnabled` por defecto **true** en `EngineSettings`.
- (Ver `EngineSettings.cs` para el resto de propiedades JSON: rutas recientes, preferencias de ventana, etc.)

---

## 22. Guía ampliada para IAs: patrones, depuración y temas transversales

Orden sugerido: **22.1** (checklist al tocar datos) → **22.2** (depuración) → **22.3** (hooks Lua) → resto según el bug. Para localizar código, **§18** suele ser más rápido que releer todo §9–§11.

### 22.1 Checklist al modificar el motor o datos

1. **¿Toca serialización?** Actualizar DTO (`FUEngine.Editor/DTO`), `ObjectsSerialization` / `MapSerialization` / equivalente, y **defaults** para JSON antiguo.
2. **¿Toca Play?** Revisar `PlayModeRunner`, `PhysicsWorld`, `LuaScriptRuntime`, proxies (`SelfProxy`, `WorldApi`).
3. **¿Toca UI del editor?** XAML + code-behind; comprobar `\_updating` en inspectores para no disparar guardados en bucle.
4. **¿API Lua nueva?** Clase con `[LuaVisible]`, inyección en `ScriptBindings`, y **ayuda** (`EngineDocumentation.cs` + este archivo).
5. **¿Evento nuevo?** Añadir a `KnownEvents.All` si debe reservarse en variables de script.

### 22.2 Depuración: Play, Lua y consola

- **Consola del editor:** `EditorLog` — niveles Info / Warning / Error / Lua; errores de script suelen incluir ruta y línea si el mensaje lo permite.
- **Print desde Lua:** va a `LuaScriptRuntime.PrintOutput` → consola (nivel Lua).
- **Breakpoints:** `LuaScriptRuntime.SetBreakpoints` — pausa Play; `ResumeFromBreakpoint` en `PlayModeRunner`.
- **Debug draw:** `Debug.drawLine` / `drawCircle` en unidades mundo; snapshot al final del frame para el visor.
- **Hot reload:** `ScriptHotReloadWatcher` — guardar `.lua` en disco recrea instancias del script afectado; revisar consola si falla el chunk.

### 22.3 Patrones de scripting (orden mental)

| Fase | Hook | Uso típico |
|------|------|------------|
| Carga | `onAwake` | Referencias, lectura de `ScriptProperties`, sin asumir que otros objetos ya hicieron `onStart`. |
| Primer frame | `onStart` | Una vez: registrar estado, buscar vecinos con `world.findByTag`. |
| Cada frame | `onUpdate(dt)` | IA, movimiento por código, lectura de input. |
| Tras física | `onLateUpdate` | Cámara custom, correcciones tras colisiones. |
| Solapamiento | `onTriggerEnter` / `Exit` | Colliders trigger o ProximitySensor; el argumento es proxy del otro cuerpo. |
| Fin de vida | `onDestroy` | Limpieza antes de borrar el `GameObject`. |

**`self.active`:** `RuntimeActive`; `false` pausa `onUpdate` sin destruir. **`PendingDestroy`:** el objeto sale de consultas y se destruye al final del tick.

### 22.4 Escenas, rutas y datos

- **`SceneDefinition`:** lista en `ProjectInfo.Scenes` — mapa, objetos, UI por escena; `game.loadScene` en Lua debe alinearse con el flujo del host (editor usa rutas del proyecto).
- **Escena principal:** `MainMapPath`, `MainObjectsPath` — Play con «escena principal» carga desde disco esas rutas si existen.
- **Copiar proyectos:** mantener rutas relativas; `ProjectDirectory` resuelve assets.

### 22.5 Rendimiento y streaming

- **`ChunkEntitySleep`:** con streaming activo, solo los `GameObject` en chunks dentro del radio reciben `onUpdate` (lista filtrada en `InvokeOnUpdates`).
- **Evicción de chunks:** tiles modificados en runtime pueden volcarse a caché (`.fue_play_chunk_cache`) según flags.
- **Física:** coste ~ O(n²) pequeño en colliders por pasadas fijas; muchos objetos dinámicos en un mismo chunk pueden acumular trabajo.

### 22.6 Tabla `ads` (Lua en Play del editor)

- **`SimulatedAdsApi`:** sin SDK real; `loadInterstitial`, `showInterstitial`, rewarded, banner — útil para flujo y callbacks sin red.
- **Build:** `ads_export.json` en export según proveedor del proyecto (ver `AdsExportProvider` / documentación de exportación).

### 22.7 Seeds vs objetos en mapa

- **Objeto en mapa:** instancia en `objetos.json` con posición fija en el editor.
- **Seed:** definición reutilizable; `world.instantiate("Nombre", x, y)` o `self:instantiate` crea instancias en runtime (posible variante `nombre_variante`).
- **Spawns anidados:** cola `_pendingSpawnBinds` — scripts del objeto nuevo al **siguiente** inicio de tick (salvo bucles en `onAwake`).

### 22.8 Glosario rápido

| Término | Significado en FUEngine |
|---------|-------------------------|
| Casilla / tile mundo | Coordenada entera `(tx, ty)` en el grid del mapa. |
| Chunk | Bloque de celdas agrupadas para I/O y streaming (`ChunkSize` en proyecto). |
| Catálogo | Tile identificado por ID en atlas/tileset JSON. |
| Definición de objeto | `ObjectDefinition` — tipo/plantilla (sprite, tamaño, script por defecto). |
| Instancia | `ObjectInstance` — posición en escena, overrides, componentes en JSON. |
| Proxy | Objeto C# envuelto para Lua (`SelfProxy`, resultados de raycast). |

### 22.9 Archivos y responsabilidades (recordatorio)

| Archivo / área | Responsabilidad |
|----------------|-----------------|
| `proyecto.json` | `ProjectInfo`, rutas, FPS, chunk, física, audio, escenas. |
| `mapa.json` | Capas, chunks, tiles (GUID de capa). |
| `objetos.json` | Definiciones + instancias + campos de componentes. |
| `scripts.json` | Registro de scripts (id → ruta .lua). |
| `animaciones.json` | Clips para `AnimationPlayer` / `self.playAnimation`. |
| `audio.json` | IDs → archivos de sonido. |
| `seeds.json` | Prefabs lógicos. |
| `triggerZones.json` | Zonas rectangulares del mapa (distinto de triggers de objeto). |

---

## 23. Más temas: editor, UI, integridad y herramientas

### 23.1 Etiquetas (`Tags`)

- **`GameObject.Tags`** / **`ObjectInstance.Tags`:** lista de cadenas; comparación **sin distinguir mayúsculas** en Lua (`hasTag`, `findByTag`, `findNearestByTag`).
- **ProximitySensor** usa la etiqueta objetivo para elegir el primer objeto coincidente en la escena.
- Convención habitual: `player`, `dynamic`, `enemy`; el protagonista nativo puede identificarse por **`ProtagonistInstanceId`** o nombre `Player`.

### 23.2 UI runtime (canvas y Lua)

- **`UiApi`:** `show` / `hide` / `setFocus` sobre un canvas; **pila de estados** (`pushState` / `popState`, límite práctico de profundidad).
- **`bind(canvasId, elementId, evento, callback)`:** eventos como click, hover, pressed, released; errores en callbacks pueden registrarse (`CallbackError` en el backend).
- Solo el canvas con **foco** recibe input; diseña menús como estados apilados o uno visible a la vez.
- Los canvas y elementos se serializan en JSON de UI del proyecto/escena.

### 23.3 Biblioteca global de assets

- **`GlobalAssetLibrary` / servicios de importación:** copiar recursos al proyecto desde una biblioteca compartida (menú Assets).
- Rutas finales suelen ser relativas al directorio del proyecto; al mover carpetas, actualizar referencias o reimportar.

### 23.4 Integridad del proyecto y huérfanos

- **`ProjectIntegrityChecker`**, **`CleanOrphansDialog`**, **`UnusedAssetScanner`:** detectan referencias rotas, assets no usados o inconsistencias (desde menú Proyecto según versión).
- Antes de publicar: ejecutar limpieza o revisar informes para no empaquetar basura ni rutas inválidas.

### 23.5 Atajos de teclado

- **`EditorShortcutRegistry`**, **`ShortcutsWindow`**, **`EngineSettings.ShortcutBindings`:** mapas acción → tecla; presets (Default, Unity, Photoshop, Custom).
- Atajos globales del editor (guardar, deshacer, herramientas) son distintos de **Key.*** en Lua durante Play.

### 23.6 Creative Suite y editores auxiliares

- **TileCreator / TileEditor, PaintCreator / PaintEditor, CollisionsEditor, ScriptableTile:** flujos para crear o retocar gráficos y máscaras de colisión enlazados al flujo de tiles.
- No sustituyen un DAW externo para audio ni Blender para 3D; el motor es **2D tile-based**.

### 23.7 Copiar / pegar y portapapeles en el mapa

- **`ZoneClipboard` / `ZoneClipboardService`:** copiar zonas de tiles (y objetos según implementación) con integración al historial **undo/redo**.
- Límites de tamaño en rellenos y operaciones masivas protegen contra congelar la UI.

### 23.8 `ComponentProxy` e `invoke` desde Lua

- **`getComponent("NombreTipo")`** devuelve un proxy con **`typeName`** y **`invoke("Metodo", ...)`** si el componente C# expone métodos usables.
- **`HealthComponent`** u otros sin métodos Lua explícitos pueden requerir ampliar el bridge en el motor.
- **`ScriptComponent`:** el proxy puede enlazar con la instancia NLua para llamar funciones definidas en el mismo archivo.

### 23.9 `game`: RNG y escenas

- **`game.setRandomSeed`**, **`randomInt`**, **`randomDouble`:** RNG del proyecto; **`RuntimeRandomSeed`** en `ProjectInfo` fija semilla al iniciar Play.
- **`game.loadScene`**, **`game.quit`:** dependen de la implementación host del `GameApi` en el editor.

### 23.10 Tipos de capa (`LayerType`)

- **Background:** sin colisión por «hay tile» (según reglas del mapa).
- **Solid:** cualquier tile en la celda cuenta como muro para `IsCollisionAt` y raycast de tiles.
- **Objects / Foreground:** orden visual, decoración; flags como **RenderAbovePlayer** afectan dibujado respecto al jugador.

### 23.11 Multiselección y inspectores

- Varios objetos seleccionados abren **MultiObjectInspector** (edición limitada o masiva según implementación).
- Un solo objeto: **ObjectInspector** completo con componentes y scripts.

### 23.12 Versión del motor vs proyecto

- **`ProjectSchema.CurrentFormatVersion`** (Core): entero de esquema del JSON `Project.FUE` (`projectFormatVersion`). Proyectos nuevos lo guardan; JSON antiguo sin campo se interpreta como **0**.
- Al abrir en el editor, **`ProjectFormatOpenHelper.TryPromptAndLoad`**: si el formato es menor que el actual, diálogo **Sí** (migración aditiva + guardar), **No** (abrir sin tocar disco; se vuelve a preguntar), **Cancelar**. Lógica en **`ProjectFormatMigration`** (Editor). El reproductor mínimo aplica migración solo en memoria.
- **`EngineVersion.Current`** y campo **`engineVersion`** en el proyecto: **`ProjectEngineCompatibilityChecker`** puede advertir si difiere del ejecutable.

### 23.13 Consola: niveles de log

- **`EditorLog`:** Info, Warning, Error, Lua; **`ToastRequested`** para notificaciones ligeras; **`MaxEntries`** acota memoria.
- Filtrar por pestaña o nivel según UI de consola.

### 23.14 Variables de script: `-- @prop` y parser

- **`LuaScriptVariableParser`:** anotaciones **`-- @prop nombre: tipo = valor`** tienen prioridad sobre heurística de asignaciones globales.
- Tipos soportados para inspector incluyen **object** (InstanceId → proxy en Play).
- Variables con nombres de **`KnownEvents`** no se muestran como editables personalizadas.

---

## 24. Referencia rápida final: Vulkan, audio, IDE y extras

### 24.1 Ventana de juego y Vulkan

- **`FUEngine.Graphics.Vulkan`:** `VulkanGraphicsDevice` (Silk.NET + GLFW). **`IGraphicsDevice`** en Core expone pocos métodos (`BeginFrame`, `Clear`, `EndFrame`, etc.).
- **Handle de ventana:** útil para integraciones; no mezclar con el canvas WPF del editor.
- El **tab Juego embebido** usa jerarquía de runtime + visor WPF + `GameViewportRenderer` (sin panel inspector duplicado; el inspector de objetos es el de la pestaña Mapa). `GameTabContent.SyncHierarchyWithRuntime` mantiene la lista alineada con `PlayModeRunner.GetSceneObjects` mientras corre Play (incluye instancias creadas por Lua). La ventana Vulkan es otro pipeline.

### 24.2 NAudio en editor y Play

- **`PlayNaudioAudioEngine`**, **`WpfPlayAudioApi`:** reproducción en Play del editor según manifiesto `audio.json`.
- Buses: maestro, música, SFX (`ProjectInfo`); normalizar volúmenes 0–1 antes de asumir perceptivo lineal.

### 24.3 Mini-IDE de scripts (AvalonEdit)

- **`AvalonEdit`** + **`Lua.xshd`:** resaltado de sintaxis en la pestaña Scripts.
- **`LuaScriptSyntaxChecker`** (`FUEngine.Runtime`): tras debounce (~380 ms) en `TextChanged`, compila el buffer con `load(..., 't', {})` (sin ejecutar el chunk). **`LuaSyntaxErrorLineRenderer`** (`IBackgroundRenderer`, capa `Background`): fondo rojizo + franja roja bajo la línea; **`LuaErrorLineParser`** alinea línea con mensajes del runtime (`:(\d+):`). Si Lua no informa línea, se usa heurística (p. ej. última línea del documento).
- **`LuaEditorCompletionCatalog`** / reflexión `[LuaVisible]` alimentan sugerencias (Ctrl+Espacio); al cambiar APIs Lua, revisar catálogo y ayuda.

### 24.4 Plugins del proyecto

- **`ProjectEnabledPlugins`:** flags de proyecto (si aplica). **`PluginLoader`:** carga real desde `{Proyecto}/Plugins/plugins-manifest.json` (solo DLLs listadas; versión en manifiesto es informativa: no resuelve dependencias entre plugins). Implementa `IPlugin` (constructor sin parámetros); opcionalmente `ILuaEngineBinding` con `RegisterLuaHost(lua, context)` para exponer tablas a Lua. Sin manifiesto no se carga ningún ensamblado.

### 24.5 Jerarquía de `GameObject` en Lua

- **`setParent`**, **`getParent`**, **`getChildren`**, **`find`**, **`findInHierarchy`:** orden de render hijos y eventos `onChildAdded` / `onParentChanged` si el motor los dispara.
- Transformaciones en coordenadas de mundo según implementación del host.

### 24.6 `BootstrapScriptId`

- **Guardado en proyecto** pero **`PlayModeRunner` no lo ejecuta automáticamente** al iniciar Play (comportamiento documentado en §20); usar `game.loadScene` o flujo manual si se necesita.

### 24.7 Medidores Fear / Danger (flags de proyecto)

- **`FearMeterEnabled`**, **`DangerMeterEnabled`**, **`LightShadowDefault`:** flags en `ProjectInfo` para juegos que usen esos sistemas; la lógica fina puede estar en scripts o módulos concretos.

### 24.8 `TileData` y colisión por celda

- **`TileMap.IsCollisionAt`** respeta tipo de capa y datos del tile (sólido, catálogo, etc.); ver `TileData` / flags en Core.
- **Auto-tiling** y variantes de catálogo no sustituyen capas **Solid** explícitas para muros.

### 24.9 Deshacer / rehacer

- Historial **acotado** (órdenes de decenas de pasos); operaciones grandes de mapa pueden fusionar en un solo comando según implementación.

### 24.10 Pantalla de inicio y proyectos

- **FUEngine Spotlight (`SpotlightControl` + overlay en `StartupWindow` / `EditorWindow`, `SpotlightIndex`):** atajo **Ctrl+P** o **Ctrl+Espacio** en Hub y en el editor (misma ventana, sin ventana modal aparte). El panel muestra **totales del índice** (manual, Lua desglosado en API por reflexión + hooks `KnownEvents` + biblioteca estándar 5.x en `LuaSpotlightBuiltins`, y recuento de proyectos recientes del Hub) y **coincidencias por categoría** en la búsqueda actual. Los resultados van **agrupados** (repo, manual, Lua, Hub, archivos del proyecto, escena). **`LuaSpotlightDescriptions`** incluye **`GlobalTableGuides`** (resumen por tabla: «¿qué es world/self/input…?»), textos «**para qué sirve**» para **todos los hooks** de `KnownEvents` y entradas **`MemberHints`** para **muchos** miembros `tabla.miembro`; el resto de símbolos que salen solo por reflexión usan **`DefaultMemberDetail`** (texto genérico + enlace al manual). No cubre: globales extra de `MergeDynamic`, métodos de componentes C# arbitrarios vía `invoke`, ni campos internos de tablas de retorno (p. ej. estructura de un hit de raycast) salvo que se documenten aparte. Al confirmar una entrada **Lua / API** (Enter o doble clic), se abre **`DocumentationWindow`** en el tema del manual más acorde (`ResolveLuaApiManualTopicId` en `SpotlightControl`: hooks → `eventos-hooks-lua`, reflexión/tablas → `scripting-lua`, palabras clave → `editor-mini-ide-lua`, `layer.*` → `scripts-capa-layer`, `ads.*` → `ads-simulado`, etc.). Fuentes: `EngineDocumentation.Topics`, `LuaEditorApiReflection`, `LuaLanguageKeywords` (las **23** palabras reservadas del manual Lua 5.5 §3.1, orden manual, incluye `global`; NLua puede usar otra versión) y `LuaSpotlightBuiltins` (biblioteca estándar; el runtime de juego solo expone un subconjunto, ver `LuaEnvironment`), archivos `.lua`/`.map`/`.seed` (`SearchProjectFiles`), instancias en `ObjectLayer`, proyectos recientes en Hub; «novedades» / «onboarding» abren `docs/AI-ONBOARDING.md`; «changelog» / «historial» pueden abrir `docs/CHANGELOG.md` si existe. En el **editor**, confirmar un `.seed` en Spotlight coloca instancias en el mapa (`OpenProjectFileFromSpotlight` → `InstantiateSeedFromFile`).
- **Hub / bienvenida (`StartupWindow`):** pestaña Hub con botón **«Buscar en el motor…»** (Spotlight sin Ctrl+P), pestaña **Scripts globales** (`GlobalScriptsHubPanel`: lista de `.lua` bajo SharedAssets/Scripts + `ScriptEditorControl` integrado), acceso rápido a pestaña **Assets** (biblioteca); proyectos fijados/recientes, miniatura de mapa, **«N escenas · M objetos»**. Tras recargar la lista (p. ej. eliminar un proyecto), el scroll del `ScrollViewer` que envuelve cada lista se resetea a la parte superior para no dejar el viewport en un offset vacío. Las miniaturas (`GeneratePreviewAsync`): el mapa y el buffer de píxeles pueden calcularse en `Task.Run`, pero el **`WriteableBitmap`** debe crearse y asignarse a `RecentProjectInfo.Preview` en el **Dispatcher de la ventana** (es `DispatcherObject`); si no, `XamlParseException` / binding en `ListBox` virtualizado.
- **Estado del motor:** último autoguardado entre proyectos recientes (`Autoguardados/Mapa/*_mapa.json`), resumen de biblioteca global alineado con la pestaña Assets (`StartupHubHelpers`), versión `EngineVersion.Current` y enlaces a `docs/CHANGELOG.md` / ayuda interna.
- **Acciones rápidas:** Lua en `SharedAssets/Scripts` (`ScriptEditorWindow`), `GlobalLibraryBrowserWindow` sin proyecto, `UnusedAssetsDialog` vía último proyecto o carpeta (menú contextual).
- **Pie:** líneas `[Error]`/`[Critical]` en el log de sesión del día (`EditorLog.SessionLogFilePath`), RAM aproximada del proceso, y botones **Carpeta** / **Limpiar** (abrir `EditorLog.LogsDirectory`, vaciar el `.log` de hoy con `EditorLog.TryClearSessionLogFile()`). `App.xaml.cs` agrupa repeticiones inmediatas del mismo **CRASH UI** (excepción no capturada en el dispatcher) y difiere el registro con `ApplicationIdle` para no re-disparar fallos de layout al escribir en la consola.
- **Plantillas:** `TemplateProvider`, `NewProjectStructure`.
- **`StartupBehavior`** en `EngineSettings`: abrir último proyecto, hub o proyecto nuevo.
- **Configuración del motor → Explorador** (antes «Jerarquía»): orden estándar vs personalizado, **carpetas extra** y **tema** (`newProjectExplorerThemeId`); no reemplazan la estructura base Mapa/Assets del `NewProjectStructure.Create`, sino que se añaden vía `EnsureStandardRootFolders` cuando «Generar jerarquía estándar» está activo en el asistente.

### 24.11 Idioma y tema del editor

- **`EngineSettings`:** `Language`, `Theme`, `UiScalePercent` — afectan al IDE, no al juego exportado salvo que el build copie esos textos.

### 24.12 Correspondencia con el manual in-app (`EngineDocumentation.cs`)

- **§24** resume stack y archivos; el **Manual completo** en la app reparte el mismo contenido en temas por **`DocumentationTopic.Id`**, por ejemplo: `vulkan-ventana-juego`, `naudio-audio-proyecto`, `avalonedit-scripts-ide`, `plugins-y-extensiones`, `jerarquia-gameobject-lua`, `bootstrap-script`, `tiledata-collision-flags`, `deshacer-rehacer-editor`, `pantalla-inicio-hub`, `asistente-nuevo-proyecto-jerarquia` (Explorador + asistente), `idioma-tema-editor`, `medidores-gameplay-flags`, `lua-completion-catalogo`.
- **Añadidos recientes:** `particulas-render-estado` (datos vs render de emisores), `fisica-raycast-dos-mundos` (`world.raycast` vs `physics.*`).
- Al cambiar comportamiento visible, actualizar **tanto** este archivo como los temas listados en `EngineDocumentation.Topics`.

---

## 25. Inventario FUEngine.Service y archivos no listados en 12-16

Complementa los inventarios §12–§16: aquí el ensamblado de **solo interfaces** y un recordatorio de qué se añadió en revisiones recientes. Para orientación general, ver [Cómo leer este documento](#como-leer-doc).

El sexto ensamblado **`FUEngine.Service`** solo define **contratos** (sin UI). Correspondencia **interfaz → uso típico**:

| Interfaz / tipo | Rol |
|-----------------|-----|
| `IEditorLog` | Log del editor (consola, toasts, sesión en disco). |
| `ServiceLocator` | Resolución estática de servicios registrados al arranque (`App.xaml.cs`). |
| `IAppPaths` | Rutas `%LocalAppData%/FUEngine/` y derivadas. |
| `IAudioBackend`, `IAudioSystem`, `IAudioAssetRegistry` | Audio en editor / manifiesto. |
| `ITextureAssetCache` | Caché de texturas WPF. |
| `IAssetScanner` | Escaneo de assets (p. ej. huérfanos). |
| `IBuildService` | Contrato de build/export (implementación en `ProjectBuildService`). |
| `IAutosaveService` | Autoguardado entre dominios. |
| `IStartupService` | Arranque Hub / último proyecto (`StartupService`). |
| `IProjectIntegrityChecker` | Integridad de referencias (`ProjectIntegrityChecker`). |
| `ISelectionService` | Selección en editor (opcional según registro). |
| `IDiscordPresenceService` | Rich Presence (`DiscordRichPresenceService`). |
| `IScriptHotReloadWatcher` | Observador de `.lua` (`ScriptHotReloadWatcher`). |
| `IScriptRegistryWriter` | Escritura de `scripts.json` (`ScriptRegistryProjectWriter`). |

**Qué ya se incorporó en §12–§16 con esta revisión:** `FiniteMapExpand`, `ProjectIndexPaths`, `ProjectSchema`, `LayerOrder` (Core); `SceneDescriptorSync`, `ProjectFormatMigration` (Editor); partials **`EditorWindow.DocumentationEmbedded`** / **`Spotlight`**; carpeta **Spotlight/**; **LuaHighlightingLoader**, **GlobalScriptsHubPanel**, **LuaEditorCompletionCatalog** / **LuaEditorApiReflection** / **LuaCompletionIcons** / **LuaScriptVariableDiscovery**, **CrashReportWriter**, **EditorLogServiceAdapter**, **ImageNearestNeighborResize**; servicios **FUEngineAppPaths**, **ProjectFormatOpenHelper**, **ProjectManifestPaths**, **ProjectThumbnailService**.

**Siguen siendo «ruido» deliberado en inventarios largos:** cada `*Window`/`*Dialog` individual (ya nombrados por categoría en §16), tests si se añaden, y recursos XAML sin lógica.

---

## Convenciones

- **Partials:** `EditorWindow` dividido en `.xaml.cs`, `.Menus.cs`, `.Inspector.cs`, `.Discord.cs`, `.DocumentationEmbedded.cs`, `.Spotlight.cs`.
- **Coordenadas:** `(tx, ty)` celda mundo; `(cx, cy)` chunk; `(lx, ly)` local al chunk; **capa** por índice en `TileMap.Layers`.
- **Idioma:** comentarios y UI en español en muchos sitios; identificadores API a veces en inglés (`onStart`, `world.raycast`).

---

*Actualizar inventarios (12–16), §25, §3.1, §8–11, §17–18, §20–24 (incl. §24.12 y temas `particulas-render-estado` / `fisica-raycast-dos-mundos`) y ayuda en `EngineDocumentation.cs` cuando cambie el código.*
