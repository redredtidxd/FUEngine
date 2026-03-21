# FUEngine — Referencia para IAs

Motor y editor 2D **tile-based** / **pixel art** (Freddy's UnWired). **Cinco proyectos:** `FUEngine.Core`, `FUEngine.Editor`, `FUEngine` (WPF), `FUEngine.Runtime`, `FUEngine.Graphics.Vulkan`. **.NET 8.0**. Nombres y comentarios mezclan español e inglés.

**Índice:** [Proyectos](#proyectos-y-referencias) · [0–11](#0-núcleo-técnico-y-stack) · [Inventarios 12–16](#12-inventario-fuenginecore) · [17–18](#17-archivos-json-típicos) · [19–21](#19-paquetes-nuget-por-proyecto)

**Ayuda para usuarios (editor):** la documentación orientada a personas vive en la app: menú **Ayuda** o **Proyecto → Guía rápida / Manual completo** (con proyecto abierto), botón **Ayuda / Guía del motor** en la pantalla de inicio, implementada con [`FUEngine/Help/EngineDocumentation.cs`](../FUEngine/Help/EngineDocumentation.cs) y [`FUEngine/Windows/DocumentationWindow.xaml`](../FUEngine/Windows/DocumentationWindow.xaml). «Completa» abre en el tema `FullManualStartTopicId` (`crear-juego`). Al añadir una funcionalidad visible (menú, inspector, API Lua), actualizar esos textos; este archivo (`AI-ONBOARDING.md`) sigue siendo la referencia técnica detallada para desarrollo/IA.

---

## Proyectos y referencias

| Proyecto | Referencias | Rol |
|----------|-------------|-----|
| FUEngine.Core | — | Entidades y lógica de dominio (TileMap multicapa, GameObject, componentes, triggers, UI runtime, etc.) |
| FUEngine.Editor | Core | DTOs y Save/Load JSON (mapa con capas/chunks, objetos, proyecto, triggers, seeds, scripts, animaciones, UI, audio, biblioteca global) |
| FUEngine | Core, Editor, Runtime | App WPF: ventanas, tabs, paneles, undo/redo, Play Mode, audio en editor (NAudio), export/build |
| FUEngine.Runtime | Core, Graphics.Vulkan | GameLoop, LuaScriptRuntime (NLua), Camera, SceneManager, WorldApi/InputApi/…, generación procedural de tiles (Lua), debug draw |
| FUEngine.Graphics.Vulkan | Core | `VulkanGraphicsDevice` — Silk.NET Vulkan + GLFW |

**Grafos de dependencias (sin ciclos):** Core → nada. Editor → Core. Graphics.Vulkan → Core. **Runtime → Core + Graphics.Vulkan** (`GameLoop.Start()` crea `VulkanGraphicsDevice.Create()` si no hay dispositivo). La app WPF → Core + Editor + Runtime (no referencia Vulkan directamente; usa el runtime).

---

## 0. Núcleo técnico y stack

- **Lenguaje:** C# (.NET 8). Editor: `net8.0-windows` (WPF). Core/Runtime/Vulkan: `net8.0`.
- **Gráficos:** **Vulkan** (Silk.NET: Vulkan, GLFW, extensiones KHR) para ventana de juego / headless. **WPF** (`Canvas`) para el mapa en el editor. Abstracción `IGraphicsDevice` en Core; implementación `VulkanGraphicsDevice`.
- **Modelo de entidades:** **OOP con componentes** (estilo Unity), no ECS. `GameObject` + `Transform` + `Component` (`SpriteComponent`, `ColliderComponent`, `ScriptComponent`, `LightComponent`).
- **Mapa:** **Chunks** por capa (`Chunk.DefaultSize = 16` por defecto en la clase `Chunk`; el `TileMap` y `ProjectInfo.ChunkSize` pueden usar otros valores). **Varias capas** (`MapLayerDescriptor`, `LayerType`, blend, parallax, tileset por capa). Serialización en **`mapa.json`** vía `MapSerialization` (`MapDto` con `Layers` + `Chunks` con `LayerId`).
- **Tiles:** Modo clásico (pintura con `TipoTile`, imagen, overlay) y modo **catálogo** (`CatalogTileId`, rutas de atlas vía `TilesetPath` / tileset JSON). `world.getTile` / `world.setTile` en Lua usan nombre de capa y catálogo.
- **Física:** un solo bucle en Play: **`PhysicsWorld.StepPlayScene`** (tiles + AABB estático/dinámico + triggers). Consultas: **`ScenePhysicsQueries`** (`RaycastSolids`, `OverlapCircle`). **`world.raycast`** = query del host (misma geometría que colliders sólidos en Play). **`physics.raycast`** / **`physics.overlapCircle`** = **`PlayScenePhysicsApi`** sobre colliders (sin tilemap). **`CollisionBody`** no participa en ese paso (reservado).
- **Iluminación:** `LightSource`, `LightingManager`, `LightComponent`; el pipeline **Vulkan** es básico. El **visor WPF** usa **`SceneLighting.SampleBrightness`** en tiles y **`SampleRgbTint`** + multiplicación RGB en **sprites** (`SpriteBitmapTint`), leyendo **`LightComponent.ColorHex`**.
- **Scripting:** **Lua** (NLua). Tablas: `self`, `world`, `input`, `time`, `audio`, `physics`, `ui`, `game`, y opcionalmente **`Debug`** (`DebugDrawApi`). Constantes `Key.*`, `Mouse.*`.
- **Audio en Play/editor:** APIs Lua inyectables; en WPF suele usarse **NAudio** (`PlayNaudioAudioEngine`, `WpfPlayAudioApi`) con manifiesto `audio.json` (`AudioManifestSerialization`).

---

## 1. Arquitectura

Resumida en la tabla **Proyectos y referencias** (cinco ensamblados y dependencias).

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
- Campos extra: opacidad, blend, parallax, offsets, `CollisionLayer` / `CollisionMask` (preparación para física por máscaras), `BackgroundTexturePath`, `TilesetAssetPath`.

---

## 3. Serialización JSON

**Opciones compartidas** (`SerializationDefaults.Options`): `WriteIndented = true`, `PropertyNamingPolicy = CamelCase`, `ReferenceHandler = IgnoreCycles`.

**Comportamientos:** mapa, objetos, triggers, seeds, scripts, animaciones, UI, manifiesto de audio, biblioteca global: **`Load` suele devolver vacío** si no existe el archivo (según el serializador). **Proyecto** (`ProjectSerialization`): `Load` **exige** archivo. **Validación** en `ProjectSerialization.FromDto`: p.ej. `TileSize > 0`, `Fps` en rango, `ChunkSize > 0`.

**Mapa (`MapDto`):** `ChunkSize`, `Layers` (`LayerDto`: nombre, tipo, sort, visibilidad, bloqueo, opacidad, blend, parallax, offsets, máscaras de colisión, `TilesetAssetPath`, etc.), `Chunks` con `LayerId`, `Cx`, `Cy`, lista de `TileDto` (incluye `CatalogTileId`, overlay en Base64, etc.).

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
- **`ObjectInstance`:** transformación, `LayerOrder`, `ScriptIds`, `ScriptProperties`, overrides, visibilidad, tags.
- **`ObjectDefinition`:** además de sprite/colisión/scripts: `AnimatronicType`, `MovementPattern`, `Personality`, `CanDetectPlayer`, `EnableInGameDrawing`.
- **`SeedDefinition` / `SeedObjectEntry`:** datos en `seeds.json` (editor). En Play, **`TryExpandPrefab`** (desde `PlayModeRunner`) expande el seed por **Id** o **Nombre**; si no hay coincidencia, `Instantiate` crea un `GameObject` mínimo con ese nombre.
- **`TriggerZone`:** rectángulo en celdas (`X`,`Y`,`Width`,`Height` en tiles), `Contains(tx,ty)`, `ScriptIdOnEnter` / `OnExit` / `OnTick`, `TriggerType` (p. ej. OnEnter, OnExit, Temporal, Persistent), `Descripcion`, `LayerId`, `Tags`. Los **triggers de mapa** (JSON) son distintos de los **`ColliderComponent` IsTrigger** en objetos; Play puede combinar ambos flujos.

### 8.1 Colisiones y triggers en Play (`PlayModeRunner`)

Al convertir `ObjectInstance` → `GameObject`, `TryAddColliderFromInstance` crea un **`ColliderComponent`** (AABB en unidades de casillas, `TileHalfWidth/Height` desde `ObjectDefinition.Width/Height`):

- **`CollisionType`** de la instancia = `"Trigger"` (ignorando mayúsculas) → `IsTrigger = true`, `BlocksMovement = false` (no frena al protagonista nativo).
- Colisión sólida: definición/override con colisión activa y **no** trigger → `BlocksMovement = true`.
- **`IsStatic`:** `false` si la instancia tiene tag **`player`** o **`dynamic`** (case insensitive); los estáticos participan en resolución AABB entre objetos; dinámicos se excluyen de empujes mutuos en esa pasada.

**`PhysicsWorld.StepPlayScene`:** mismo comportamiento que el antiguo paso AABB (tiles, estático/dinámico, triggers); lo invoca **`PlayModeRunner`** tras Lua `onUpdate`.

**`GameObject` (Core):** jerarquía con `Parent`/`Children`, eventos `ChildAdded`/`ChildRemoved`/`ParentChanged`, `Find` / `FindInHierarchy`, `GetComponent<T>()` y `GetComponent("NombreTipo")`, `AddComponent`/`RemoveComponent`, `SetParent`/`AddChild`/`RemoveFromParent`.

---

## 9. Runtime (FUEngine.Runtime) y Play Mode

**`GameLoop`:** `Renderer` (Core) con `IGraphicsDevice` opcional; `InputManager`; `LuaScriptRuntime` opcional; `Tick` → `BeginFrame`, scripts, `EndFrame`.

**`Camera`:** posición + `Zoom`.

**`SceneManager`:** `LoadScene(Scene)` o `LoadScene(string)` — con `string` asigna un `Scene` de Core con solo id/nombre (sin cargar mapa/objetos). El flujo real del juego en el editor es **`PlayModeRunner`**, no este `SceneManager`.

**`SceneDefinition` (proyecto)** vs **`Scene` (Core):** el editor multiescena usa `SceneDefinition` + rutas a mapa/objetos/UI. `Scene` en Core es otro modelo (`TilemapLayers`, listas de objetos/luces/triggers) poco acoplado al editor actual.

**`LuaScriptRuntime`:** entornos por instancia, `CreateInstance`, `Tick` (y sobrecarga con `activeGameObjects` para `ChunkEntitySleep`), `BeginTick` / `InvokeOnUpdates` / `InvokeOnLateUpdates` / `EndTick`, `NotifyScripts`, `GetScriptInstancesFor`, `GetActiveScriptCount`, `GetLuaMemoryKb`, `ReloadScript`, breakpoints (`SetBreakpoints`, `IsBreakpointHit`, …), eventos y APIs inyectadas. `ScriptPropertyEntry` → `Set` en el entorno; `ParsePropertyValue`: `int`/`float`/`bool` o string.

**`PlayModeRunner` (proyecto FUEngine):** construye `GameObject`s desde `ObjectLayer`, copia o carga `TileMap`, registra `LuaScriptRuntime`, `WorldApi`, `NativeProtagonistController`, triggers (enter/exit), UI (`UIRuntimeBackend`), streaming de chunks según proyecto, `ScriptHotReloadWatcher`, audio **`PlayNaudioAudioEngine`** / `WpfPlayAudioApi`.

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
- **Grid / mapa:** `TileSize`, `MapWidth`, `MapHeight`, `Infinite`, `ChunkSize`, `InitialChunksW/H`, `TileHeight`, `AutoTiling`, `Fps`, `AnimationSpeedMultiplier`.
- **Streaming de chunks:** `ChunkLoadRadius`, `ChunkStreamEvictMargin`, `ChunkStreamSpillRuntimeEmpty`, `ChunkUnloadFar`, `ChunkSaveByChunk`, `ChunkEntitySleep`, `ChunkStreaming`, `ShowChunkBounds`.
- **Render juego:** `GameResolutionWidth/Height`, `CameraSizeWidth/Height`, `PixelPerfect`, `InitialZoom`, `DefaultFirstSceneBackgroundColor`, `HUDColor`, `HUDStyle`, `GameFontFamily`, `GameFontSize`, exportación de formatos, `AssetsRootFolder`, `ProjectGridSnapPx`.
- **Cámara / jugador:** `CameraLimits`, `CameraEffects`, `ProtagonistInstanceId`, `UseNativeInput`, `UseNativeCameraFollow`, `NativeCameraSmoothing`, `NativeMoveSpeedTilesPerSecond`, `AutoFlipSprite`, `UseNativeAutoAnimation`.
- **Audio:** `StartupMusicPath`, `StartupSoundPath`, `AudioManifestPath`, `MasterVolume`, `MusicVolume`, `SfxVolume`.
- **Física / gameplay:** `PhysicsEnabled`, `PhysicsGravity`, `DefaultCollisionEnabled`, `DefaultAnimationFps`, `FearMeterEnabled`, `DangerMeterEnabled`, `LightShadowDefault`, **`RuntimeRandomSeed`** (opcional; fija el RNG de **`GameApi`** en Lua al iniciar Play).
- **Scripts:** `ScriptingLanguage`, `BootstrapScriptId`, `DebugMode`, `ScriptNodes`.
- **Autoguardado:** `AutoSaveIntervalSeconds`, `AutoSaveEnabled`, `AutoSaveIntervalMinutes`, `AutoSaveMaxBackupsPerType`, `AutoSaveFolder`, `AutoSaveOnClose`, `AutoSaveOnlyWhenDirty`.
- **Capas:** `LayerNames` (índice = `LayerId` lógico en datos de tile).
- **Rutas:** `ProjectDirectory`, `MapPathRelative`, `MainMapPath`, `MainObjectsPath`, `Scenes` (`SceneDefinition`), `DefaultTilesetPath`, propiedades calculadas `MapPath`, `ObjectsPath`, `MainSceneMapPath`, `MainSceneObjectsPath`, rutas a `animaciones.json`, `scripts.json`, `triggerZones.json`, `seeds.json`, `AudioManifestAbsolutePath`.
- **Plugins:** `ProjectEnabledPlugins`.
- **Motor:** `EngineVersion` en proyecto cargado.

### 10.1 `GameViewportMath`

Utilidades estáticas para alinear resolución lógica del juego con el visor:

- **`GetEffectiveResolutionPixels`:** si `GameResolutionWidth/Height` > 0, son los píxeles lógicos; si son 0 (“Auto”), deriva un tamaño a partir de ~**12×10 casillas** en unidades de `TileSize` (mínimos aplicados en código).
- **`GetViewportSizeInWorldTiles`:** ancho/alto visible en **casillas mundo** (float).

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
| `game` | `GameApi` — `loadScene`, `quit`, **`setRandomSeed`**, **`randomInt`**, **`randomDouble`** |
| `Debug` | `DebugDrawApi` — depuración dibujada (si se inyecta) |

**`WorldApi` (métodos destacados):** `SetWorldContext`, `ConfigurePlayTilemap`, `getTile` / `setTile`, `findObject` / `getObjectByName`, `findByTag`, `getObjects`, `findByPath`, `instantiate` / `spawn`, `destroy`, `setPosition`, `getPlayer`, `raycast`, `raycastTiles`, `raycastCombined`.

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
| **Plugins/** `PluginInterface.cs`, `PluginLoader.cs` | Extensión por plugins. |
| **Project/** `GameViewportMath.cs` | Matemáticas vista/resolución. |
| | `ProjectInfo.cs` | Configuración completa del proyecto. |
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
| `LuaEnvironment.cs` | Estado NLua y entornos. |
| `LuaScriptRuntime.cs` | Runtime principal de scripts. |
| `LuaTileGenerator.cs` | Generación de tiles vía Lua. |
| `Mathematics/PerlinNoise.cs` | Ruido Perlin. |
| `PlayScenePhysicsApi.cs` | Tabla Lua `physics` (raycast / overlapCircle sobre colliders). |
| `RaycastHitInfo.cs` | Golpe raycast objetos. |
| `SceneManager.cs` | Escenas. |
| `ScriptBindings.cs` | `WorldApi`, `InputApi`, `TimeApi`, `AudioApi`, `PhysicsApi`, `UiApi`, `GameApi`, constantes. |
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

Ruta: `FUEngine/FUEngine/`. Listado de fuentes **excluyendo** `obj` y `bin`.

### App y ensamblado
- `App/App.xaml`, `App.xaml.cs` — arranque WPF, recursos globales.
- `AssemblyInfo.cs` — metadatos.

### Controles
- `CollisionShapesCanvasControl` — edición visual de formas de colisión.
- `DrawingCanvasControl` — lienzo de dibujo (paint pipeline).
- `MinimapControl` — minimapa.
- `NewProjectPanel` — UI de nuevo proyecto en pantalla de inicio.
- `ScriptEditorControl` — editor de texto de scripts embebido.

### Converters
- `BoolToPinLabelConverter`, `NullToVisibilityConverter`, `PathToFileNameConverter`, `StringToBrushConverter`.

### Dialogs
- `AboutWindow`, `BuildExportWindow`, `CleanOrphansDialog`, `ConfirmDeleteProjectDialog`, `CreateFromTemplateDialog`, `ExportPartialWindow`, `GlobalLibraryBrowserWindow`, `ImportSceneAssetScanDialog`, `NewProjectDialog`, `PixelEditWindow`, `ProjectConfigWindow`, `ScriptEditorWindow`, `SettingsPage`/`SettingsWindow`, `ShortcutsHelpContent`, `ShortcutsWindow`, `SimulateWindow`, `SnapshotPickerWindow`, `TemplatePickerWindow`, `TilesetEditorWindow`, `UnusedAssetsDialog`.
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
- `AnimationInspectorPanel`, `DefaultInspectorPanel`, `LayerInspectorPanel`, `LayersPanel`, `LogPanel`, `MapHierarchyPanel`, `MapPropertiesInspectorPanel`, `MultiObjectInspectorPanel`, `ObjectInspectorPanel`, `PlayHierarchyPanel`, `ProjectExplorerPanel`, `QuickPropertiesPanel`, `TileInspectorPanel`, `TriggerZoneInspectorPanel`, `UIElementInspectorPanel`.

### Rendering
- `GameViewportRenderer.cs`, `MapRenderContext.cs`, `MapRenderer.cs`, `PlayTileBitmapCompositor.cs`.

### Services (lógica de aplicación)
- `AudioAssetRegistry.cs`, `AudioSystem.cs`, `AutoSaveService.cs`, `CanvasControllerLuaTemplate.cs`, `CreativeSuiteMetadata.cs`, `DefaultLuaScriptTemplate.cs`, `EditorAudioBackend.cs`, `ExplorerMetadataService.cs`, `GlobalAssetLibraryService.cs`, `IAudioBackend.cs`, `IAudioHandle.cs`, `MapSnapshotService.cs`, `NativeAutoAnimationApplier.cs`, `NativeProtagonistController.cs`, `PlayerLaunchArgs.cs`, `PlayModeRunner.cs`, `PlayNaudioAudioEngine.cs`, `ProjectBuildService.cs`, `ProjectExportHelper.cs`, `ProjectIntegrityChecker.cs`, `SceneAssetReferenceCollector.cs`, `ScriptHotReloadWatcher.cs`, `ScriptRegistryProjectWriter.cs`, `SelectionManager.cs`, `StartupService.cs`, `TemplateProvider.cs`, `TextureAssetCache.cs`, `TileDataFile.cs`, `TilePaintService.cs`, `UnusedAssetScanner.cs`, `WpfPlayAudioApi.cs`, `ZoneClipboardService.cs`.

### Settings
- `EngineFontPresets.cs`, `EngineSettings.cs`, `EngineTypography.cs`.

### Tabs (contenido del editor central)
- `AnimationsTabContent`, `AudioTabContent`, `CollisionsEditorTabContent`, `ConsoleTabContent`, `DebugTabContent`, `ExplorerTabContent`, `GameTabContent`, `ObjectsTabContent`, `PaintCreatorTabContent`, `PaintEditorTabContent`, `PlaceholderTabContent`, `ScriptableTileTabContent`, `ScriptsTabContent`, `TileCreatorTabContent`, `TileEditorTabContent`, `TilesTabContent`, `UITabContent`.

### Tools
- `IMapEditorToolContext.cs`, `ITool.cs`, `PaintTool.cs`, `PlaceholderGenerator.cs`, `ToolController.cs`.

### Windows
- `DocumentationWindow`, `EditorWindow` (+ `EditorWindow.Menus.cs`, `EditorWindow.Inspector.cs`), `GamePlayWindow`, `MainWindow`, `PlayerWindow`, `SplashScreenWindow`, `StartupWindow`.

### Otros
- `TileImageLoader.cs` — carga de bitmaps para tiles.

---

## 17. Archivos JSON típicos

| Archivo (relativo al proyecto) | Contenido |
|-------------------------------|-----------|
| `proyecto.json` / **`Project.FUE`** / nombre configurado | `ProjectDto` / `ProjectInfo` |
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

**Proyectos nuevos (`NewProjectStructure`):** además de rutas legacy (`mapa.json` en algunos flujos), la plantilla moderna usa **`Project.FUE`** como archivo de proyecto, **`Settings.json`**, extensiones **`.map`** / **`.objects`** por escena (p. ej. `Maps/Start/map.map`, `Objects/Start/objects.objects`), carpetas `Assets/*`, `Scripts`, `Seeds`, `Autoguardados/Mapa|Objetos|Escenas`, escenas por defecto **Start** y **End** con `SceneDefinition` (`DefaultTabKinds`, `UIFolderRelative` por escena). Constantes útiles: `MapFileExtension`, `ObjectsFileExtension`, `DefaultScenes`.

---

## 18. Buscar por tema

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
| Autoguardado | `AutoSaveService` (`.tmp` → definitivo, subcarpetas Mapa/Objetos) |
| UI runtime Play | `UIRuntimeBackend` (`Show`/`Hide`/`SetFocus`, `pushState`/`popState` máx. 16 niveles, `bind`, `CallbackError`) |
| Parser variables script | `LuaScriptVariableParser` |

---

## 19. Paquetes NuGet por proyecto

| Proyecto | Paquetes |
|----------|----------|
| **FUEngine** | **AvalonEdit** (editor de scripts / resaltado; `Lua.xshd` embebido), **NAudio** (audio en editor y Play). **WPF** + **Windows Forms** habilitados en el `.csproj`. |
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
- **`PluginLoader`:** `LoadFromDirectory` vacío (stub). **`ProjectEngineCompatibilityChecker`:** compara `EngineVersion` del proyecto con `EngineVersion.Current`.
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
- (Ver `EngineSettings.cs` para el resto de propiedades JSON: rutas recientes, preferencias de ventana, etc.)

---

## Convenciones

- **Partials:** `EditorWindow` dividido en `.xaml.cs`, `.Menus.cs`, `.Inspector.cs`.
- **Coordenadas:** `(tx, ty)` celda mundo; `(cx, cy)` chunk; `(lx, ly)` local al chunk; **capa** por índice en `TileMap.Layers`.
- **Idioma:** comentarios y UI en español en muchos sitios; identificadores API a veces en inglés (`onStart`, `world.raycast`).

---

*Actualizar inventarios (12–16), §3.1, §8–11, §17–18 y §20–21 cuando cambie el código.*
