# FUEngine – Estado de la especificación

Checklist según la especificación Motor / Editor / Explorador / Extras.

**Nota sobre tamaño del repo:** El peso “real” del motor es solo el código fuente. Las carpetas `bin/`, `obj/`, `.vs/` y `publish/` (no versionadas con `.gitignore`) pueden ocupar GB en disco; es normal. Un build publicado (ejecutable) suele quedar en ~100–200 MB.

---

## ⚙️ Motor / Backend (7)

| # | Requisito | Estado | Notas |
|---|-----------|--------|--------|
| 1 | **Gestión de tiles y chunks** – Crear, cargar y guardar mapas infinitos por chunks. | ✅ | `TileMap`, `Chunk`, `TileData`; `MapSerialization`; chunks 16x16/32x32. |
| 2 | **Sistema de objetos** – Colisiones, interactividad, rotación y scripts asociados. | ✅ | `ObjectDefinition`, `ObjectInstance`, `ObjectLayer`; colisión, interactivo, rotación, ScriptId. |
| 3 | **Sistema de scripts básicos** – Registrar scripts, módulos comunes y eventos (onCollision, onInteract, onTrigger…). | ✅ | `ScriptRegistry`, `ScriptDefinition`; `KnownEvents` (onCollision, onInteract, onTrigger, etc.). |
| 4 | **Animaciones / placeholders** – Reproducción mínima, control de frames y FPS. | ✅ | `AnimationDefinition`, `AnimationController`; serialización en `animaciones.json`. |
| 5 | **Colisiones y física ligera** – Tiles con colisión, objetos sólidos, detección jugador/animatrónicos. | ✅ | `TileData.Colision`, `IsCollisionAt`; objetos con colisión; `ObjectDefinition.CanDetectPlayer`. |
| 6 | **Serialización robusta** – Guardado y carga de proyecto, mapa, objetos y scripts en JSON. | ✅ | `ProjectSerialization`, `MapSerialization`, `ObjectsSerialization`, `ScriptSerialization`, `AnimationSerialization`. |
| 7 | **Soporte de tiles y sprites dinámicos** – Cambiar textura o paleta por tile, objeto o zona. | 🔶 | `ProjectInfo.PaletteId`, `TileData` con Height/Transparente; paleta por zona pendiente de UI. |

---

## 🖌 Editor / UX (6)

| # | Requisito | Estado | Notas |
|---|-----------|--------|--------|
| 1 | **Canvas de mapa editable** – Pintar, borrar, seleccionar tiles y objetos con zoom y scroll. | ✅ | `MapCanvas`, zoom con botones y teclado; scroll; pintar/borrar tile; seleccionar y mover objeto. |
| 2 | **Panel de propiedades / inspector** – Mostrar y editar propiedades de tile/objeto/animación/script. | ✅ | `ObjectInspectorPanel` (objeto); `QuickPropertiesPanel` (archivo en explorador). |
| 3 | **Herramientas del editor** – Pintar, borrar, seleccionar, mover, rotar y duplicar. | ✅ | Pintar, seleccionar, colocar; mover objeto arrastrando; rotar (R); borrar objeto (Del / menú). Duplicar objeto: pendiente. |
| 4 | **Drag & Drop** – Arrastrar archivos del explorador al canvas o entre carpetas. | ✅ | Soltar archivos desde el escritorio al árbol del proyecto (importar). Arrastrar desde explorador al canvas: pendiente. |
| 5 | **Previsualización en miniatura** – Tiles, objetos, animaciones y scripts. | ✅ | Panel de vista previa en explorador; propiedades rápidas por tipo de archivo. |
| 6 | **Atajos y controles rápidos** – Teclas para herramientas, rotar, layers y duplicar. | ✅ | Ctrl+S, Ctrl+Shift+S, Del, R, 1/2/3. **Deshacer/Rehacer** (Ctrl+Z / Ctrl+Y) con historial de comandos. **Estructura de proyecto nuevo:** al crear proyecto se genera Assets (Tilesets, Sprites, Animations, Placeholders), Maps (map_001.json), Scripts (main.json), Seeds, Project.json, Settings.json; jerarquía con Layers (Background, Ground, Objects, Foreground), Objetos, Groups (DefaultGroup), Triggers; se puede abrir Project.json o proyecto.json. |

---

## 🗂 Explorador de proyecto / UI (4)

| # | Requisito | Estado | Notas |
|---|-----------|--------|--------|
| 1 | **Jerarquía de proyecto** – Carpetas y archivos: mapa.json, objetos.json, scripts.json… | ✅ | `ProjectExplorerPanel` con árbol; raíz = proyecto; archivos y subcarpetas. |
| 2 | **Mini-previews e iconos** – Tipo de archivo y miniatura de sprite/animación. | ✅ | Iconos por tipo (carpeta, proyecto, mapa, objetos, scripts, animaciones, sprite, genérico); panel de vista previa. |
| 3 | **Filtrado y búsqueda** – Por nombre, tipo, scripts o propiedades. | ✅ | Barra de búsqueda por nombre; combo de filtro (Todo, Mapa/Tiles, Objetos, Scripts, Animaciones). |
| 4 | **Menú contextual** – Crear, renombrar, eliminar, duplicar, abrir en editor externo. | ✅ | Clic derecho: Nuevo archivo, Renombrar, Duplicar, Eliminar, Copiar ruta, Abrir en explorador, Usar como plantilla (algunos placeholder). |

---

## 💡 Extras / Configuración / Futuro (3)

| # | Requisito | Estado | Notas |
|---|-----------|--------|--------|
| 1 | **Configuración global del motor** – Paleta por defecto, tamaño de tile, tema, ruta por defecto de proyectos. | ✅ | `EngineSettings`, `SettingsWindow` (General, Rutas, Editor, Motor, Scripts/Assets, Compilación, Avanzado). |
| 2 | **Sistema de logs y errores** – Mostrar advertencias o errores de mapa/objetos/animaciones. | ✅ | `EditorLog` (Info, Warning, Error); `LogPanel` en el editor (expander abajo); mensajes en carga y guardado. |
| 3 | **Soporte de plantillas** – Previsualización, categorías, parámetros opcionales y fusión de scripts. | ✅ | Plantillas de proyecto; diálogo de creación con plantilla; parámetros opcionales. Previsualización de plantilla y fusión de scripts: mejorable. |

---

## 🔥 Bonus (para después)

| Idea | Estado |
|------|--------|
| Sistema de iluminación básica y sombras pixel-perfect. | Pendiente |
| Paleta dinámica y blending por zonas. | Pendiente |
| Soporte de nodos para scripts visuales. | Pendiente |
| Renderizado de profundidad / pseudo 2.5D. | Pendiente |
| Guardado automático incremental y undo/redo. | ✅ Undo/redo implementado (hasta 100 pasos). Autoguardado: pendiente. |

---

## 🎁 Extras / Nice-to-have (fundamentos listos)

| Requisito | Estado | Notas |
|-----------|--------|--------|
| **Sistema de capas** – Múltiples capas de tiles/objetos. | ✅ Base | `ProjectInfo.LayerNames`, `TileData.LayerId`; serialización en mapa y proyecto. |
| **Eventos globales del mundo** – OnDayStart, OnNightStart, OnPlayerMove. | ✅ | `KnownEvents`: OnDayStart, OnNightStart, OnPlayerMove, OnZoneEnter, OnZoneExit; array `WorldEvents`. |
| **Triggers por zona** – Áreas que ejecutan scripts al entrar/salir. | ✅ Base | `TriggerZone` (rect, ScriptIdOnEnter/OnExit); `TriggerZoneSerialization` → triggerZones.json. |
| **Sistema de tags** – Etiquetar objetos/tiles para filtros y búsqueda. | ✅ | `TileData.Tags`, `ObjectDefinition.Tags`; serialización en mapa y objetos. |
| **Undo/redo** – Historial de cambios de mapa y objetos. | ✅ | `IEditorCommand`, `EditorHistory`; PaintTile, RemoveTile, AddObject, RemoveObject. |
| **Bookmarks / favoritos** – Objetos o tiles más usados. | ✅ Base | `EngineSettings.FavoriteObjectIds`, `FavoriteTileTypes`; persistidos en settings.json. |
| **Notificaciones internas** – Avisos de colisiones, scripts faltantes. | ✅ | `EditorLog` + advertencias al cargar (objeto con script inexistente). |
| **Mini-mapa en tiempo real** – Vista del mapa actual en edición. | 🔶 | No integrado en el inspector (pendiente). |
| **Seeds / plantillas de objetos** – Configuraciones reutilizables. | ✅ Base | `SeedDefinition`, `SeedObjectEntry`; `SeedSerialization` → seeds.json. |
| **Preview colisiones / pathfinding** | 🔶 | Estructuras listas (objetos con colisión); vista de rutas IA pendiente. |
| **Duplicado masivo** | ✅ | Herramienta Zona (4): arrastrar para seleccionar área; Ctrl+C / Ctrl+V copiar/pegar zona (tiles + objetos). |
| **Paleta rápida drag-and-drop** | ✅ | Paleta de 4 tiles en la barra (Suelo, Pared, Objeto, Especial); arrastrar al canvas para pintar. |
| **Inspector múltiple** | ✅ | Ctrl+clic en modo Seleccionar añade/quita de la selección; panel "N objetos" con rotación masiva y Deseleccionar. |
| **Dashboard mini-preview** | ✅ | Proyectos recientes con miniatura 48x48 generada desde mapa.json (async). |
| **Temas y layout configurables** | 🔶 | Tema en EngineSettings; layout con splitters (pendiente guardar posiciones). |
| **Simulación ligera** | ✅ | Proyecto → Simular: ventana con mapa, jugador (flechas), colisiones, triggers por zona (log al entrar). |
| **Exportación parcial** | ✅ | Proyecto → Exportar parcial: elegir mapa/objetos/scripts/animaciones/proyecto y carpeta destino. |
| **IA / placeholder sprites** | ✅ | Explorador → clic derecho → Generar sprite placeholder (PNG 32x32 en assets/). |

---

## 🧱 Core – Estructura y sistemas base

Sistemas en **FUEngine.Core** (stubs o listos para uso):

| Carpeta | Contenido | Estado |
|---------|-----------|--------|
| **Map** | TileType, TileData, Chunk, TileMap, **Tileset**, **Tile** (+Friction), **TilemapLayer**, **TilemapChunk**, **TileAnimation**, **LayerOrder**, **TileMaterial**, **AutoTiling**, **PixelLayer** | ✅ |
| **Objects** | ObjectDefinition, ObjectInstance, ObjectLayer, **GameObject**, **Transform**, **Component**, SpriteComponent, ColliderComponent, ScriptComponent, LightComponent | ✅ |
| **Scripts** | ScriptDefinition, ScriptRegistry, KnownEvents | ✅ |
| **Animation** | AnimationDefinition, AnimationController | ✅ |
| **Triggers** | TriggerZone | ✅ |
| **Seeds** | SeedDefinition, SeedObjectEntry, **SeedInstance** | ✅ |
| **Project** | ProjectInfo, **Scene** (TilemapLayers, Objects, Lights, Triggers), **SceneObject** | ✅ |
| **Engine** | EngineVersion, GameTiming, SplashScreenConfig, **ProjectEngineCompatibilityChecker**, **DebugOverlay** | ✅ |
| **Assets** | AssetCache, **TextureAsset**, **SoundAsset**, **ScriptAsset**, **AssetDatabase**, **ResourceLoader** | ✅ |
| **Physics** | CollisionBody, PhysicsWorld | ✅ Stubs |
| **Audio** | SoundSource, AudioManager | ✅ Stubs |
| **Lighting** | LightSource, LightingManager | ✅ Stubs |
| **Rendering** | Renderer, SpriteRenderer, TileRenderer | ✅ Stubs (editor y runtime los usan) |
| **Input** | InputManager | ✅ Stub (editor y runtime) |
| **Plugins** | IPlugin, PluginLoader | ✅ Stubs |

**Runtime** usa `Core.Renderer` y `Core.InputManager`; no duplica lógica. **Undo/Redo** sigue en el proyecto WPF (Editor) con `IEditorCommand` y `EditorHistory`.

**Escena ≠ 1 tilemap.** Una escena contiene varias capas:
- **TilemapLayers**: cada capa usa un `Tileset` y guarda solo **tile IDs** (mapa ligero). Ej.: Tilemap_Background, Tilemap_Walls, Tilemap_Details.
- **Objects**: lista de `SceneObject` (puertas, cámaras, animatrónicos).
- **Lights**: lista de `LightSource`.
- **Triggers**: lista de `TriggerZone`.

**Tileset** = imagen + TileWidth/TileHeight + definiciones `Tile` (collision, material, lightBlock, animation). **TilemapLayer** almacena solo IDs; las propiedades se resuelven desde el Tileset. **LayerOrder** (Background=0, Ground=1, Walls=2, Decoration=3, Foreground=4) para orden de dibujado. **TileMap** (Chunk + TileData) se mantiene para compatibilidad con el editor actual; el modelo recomendado a largo plazo es Scene + TilemapLayer + Tileset.

---

## Resumen

- **Motor/Backend:** 6/7 completos; 1 parcial (paleta por zona).
- **Editor/UX:** 5/6 completos; undo/redo operativo; 1 pendiente (drag explorador → canvas; duplicar objeto).
- **Explorador:** 4/4 completos.
- **Extras:** 3/3 + fundamentos de capas, eventos mundo, triggers, tags, seeds, bookmarks, minimapa, notificaciones.

**Atajos:** Ctrl+S, Ctrl+Shift+S, **Ctrl+Z** (deshacer), **Ctrl+Y** (rehacer), Del, R, 1/2/3/4/5.  
**Log:** panel Log/Errores (expander abajo). **Mini-mapa:** encima del inspector, actualizado en tiempo real.

---

## 🚀 Extras finales (antes de crear y probar proyecto)

| Área | Requisito | Estado | Notas |
|------|-----------|--------|--------|
| **Motor/Runtime** | Logs / debug – Errores, eventos de scripts y colisiones en pruebas. | ✅ | `EditorLog`; simulación registra entrada a zonas (script asignado); verificador escribe al log. |
| **Motor/Runtime** | Verificador de integridad – Referencias rotas en mapa/objetos/scripts. | ✅ | `ProjectIntegrityChecker`: scripts inexistentes en tiles/objetos, archivos faltantes, chunk size, chunks fuera de rango. Proyecto → Verificar integridad. |
| **Motor/Runtime** | Caché de assets – Sprites/tiles/animaciones en memoria para render rápido. | ✅ Base | `AssetCache` en Core (por ruta e ID); listo para uso en runtime. |
| **Motor/Runtime** | FPS / delta time – Animaciones y lógica consistentes. | ✅ | `GameTiming` en Core (TargetFps, DeltaTime, TotalTime); simulación muestra FPS y Δt. |
| **Motor/Runtime** | Snapshots del mapa – Estados temporales sin sobreescribir proyecto. | ✅ | `MapSnapshotService`: Guardar snapshot / Cargar snapshot (carpeta `snapshots/` con timestamp). |
| **Editor/UX** | Herramienta de medición / grid – Distancias entre tiles/objetos. | ✅ | Herramienta **Medir**: dos clics; línea y distancia en tiles/px en barra de estado. |
| **Editor/UX** | Herramientas Brush, Rectángulo, Fill, Line, Eraser, Picker, Stamp. | ✅ | Pincel, Rectángulo (arrastrar), Línea (2 clics), Relleno (bucket), Goma, Cuentagotas, Stamp (pegar zona). Opciones: tamaño pincel 1–3, rotación 0°–270°. |
| **Editor/UX** | Mini editor de tiles (collision, friction, tag, animación). | ✅ | **Editor de Tileset** (pestaña Tiles → Abrir editor): por cada tile, colisión, bloqueo luz, fricción, material, etiquetas, animación. |
| **Editor/UX** | Capas activas/visibles – Encender/apagar capas para edición. | ✅ | Combo **Capa** en barra (Todas + nombres de `LayerNames`); solo se dibujan tiles de la capa seleccionada. |
| **Editor/UX** | Relleno / bucket – Áreas grandes del mismo tipo de tile. | ✅ | Modo Pintar + **Shift+clic**: relleno por inundación (máx. 2000 tiles); undo por operación. |
| **Editor/UX** | Resaltado de tiles interactivos – Ver cuáles tienen script o colisión. | ✅ | Checkbox **Resaltar interactivos**: borde amarillo en tiles con `Interactivo` o `ScriptId`. |
| **UI/Extras** | Tip de ayuda contextual – Info de herramienta o tile. | ✅ | Barra de estado: coordenadas (tile) + herramienta + tipo de tile actual. |
| **UI/Extras** | Panel de atajos configurables – Acceso rápido. | ✅ | Ayuda → **Atajos de teclado**: ventana con lista (guardar, deshacer, herramientas 1–5, R, Shift+clic). |
| **UI/Extras** | Validación de tamaño y límites – Avisar chunk/tile fuera de rango. | ✅ | Verificador de integridad advierte chunks fuera de rango sugerido (InitialChunksW/H). |
| **Preparación** | Placeholders de assets – Probar sin arte final. | ✅ | `PlaceholderGenerator`; Explorador → Generar sprite placeholder. |
| **Preparación** | Simulación jugador/entidad – Probar colisiones y triggers. | ✅ | Proyecto → Simular: jugador (flechas), colisiones con tiles, detección de zonas (log + EditorLog). |
