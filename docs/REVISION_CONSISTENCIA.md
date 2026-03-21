# Revisión de consistencia — FUEngine

Revisión de inconsistencias, elementos sin implementar o rotos y duplicados en el motor.

---

## 1. Funcionalidad "en desarrollo" (placeholders)

| Ubicación | Qué hace ahora | Recomendación |
|-----------|----------------|----------------|
| **EditorWindow.xaml.cs** ~2819 | "Ejecutar script: en desarrollo." (Log) | Implementar ejecución de script o quitar/ocultar la acción hasta que exista. |
| **EditorWindow.Inspector.cs** ~137-138 | "Crear objeto: diálogo en desarrollo" / "Añadir trigger: en desarrollo" (MessageBox) | Sustituir por Toast + log; cuando exista el flujo, implementar diálogo o enlace a objetos.json/triggerZones.json. |
| **ProjectExplorerPanel.xaml.cs** ~1218, 1223, 1228 | "Usar como plantilla", "Nuevo seed", "Nuevo script" → MessageBox "en desarrollo" | Unificar con Toast; priorizar implementación de "Nuevo script" y "Nuevo seed" si son críticos. |
| **AnimationsTabContent.xaml.cs** ~34 | "Crear animación: diálogo en desarrollo" | Igual: Toast + log; enlazar a animaciones.json o implementar diálogo. |
| **PlayModeRunner.cs** ~300 | TODO: AudioApi para play(id) | Dejar como TODO hasta tener backend de audio; no afecta a la consistencia de UI. |
| **ProjectExplorerPanel.xaml.cs** ~867 | "Crear grupo: usa la Jerarquía del mapa..." (MessageBox) | Mensaje informativo; valorar Toast. |
| **ExplorerTabContent.xaml.cs** ~61, 66 | "Vista Lista/Grid próximamente.", "Renombrar múltiple próximamente." | Unificar con Toast cuando exista. |
| **MapHierarchyPanel.xaml.cs** ~311 | Placeholder: "Próximamente." en menú contextual | Igual. |

**Resumen:** Hay varios puntos de entrada (menú/explorador/tabs) que solo muestran "en desarrollo" o "próximamente". Conviene sustituir MessageBox por Toast en todos y, con el tiempo, implementar o documentar el flujo (por ejemplo "editar en X.json").

---

## 2. Inconsistencia de nombres en UI: Scene vs Escena

**Corregido:** Títulos de MessageBox de errores unificados a "Crear escena", "Duplicar escena", "Importar escena" en EditorWindow.xaml.cs.

---

## 3. Rutas por defecto: mapa vs objetos (bug conceptual)

**Corregido:** Se separaron los conceptos. Ya no existe un solo campo "MainSceneRelative" que mezclaba mapa y objetos.
- **MainMapPath** = ruta al mapa de la escena Start (estructura del nivel). Por defecto `mapa.json`.
- **MainObjectsPath** = ruta al archivo de objetos de la escena Start (instancias). Por defecto `objetos.json`.
- En Configuración del proyecto la UI sigue siendo "selecciona escena" (dropdown); al elegir una escena se asignan internamente ambos paths desde esa escena.
- Compatibilidad: proyectos antiguos con `MainSceneRelative` en JSON se mapean a `MainObjectsPath` al cargar.

---

## 4. Bug: guardado de proyecto con ruta incorrecta (Project.json vs proyecto.json)

**Corregido:** Los tres sitios (MapHierarchy_OnRequestAddLayer, MapHierarchy_OnRequestReorderLayers, Explorer_OnRequestCreateObjectLayer) usan `GetProjectFilePath()`; si la ruta es null se muestra Toast de error y no se guarda.

---

## 5. Undo/Redo

- **MenuDeshacer** / **MenuRehacer** y **BtnUndo** / **BtnRedo** están conectados a `_history` y se habilitan/deshabilitan con `UpdateUndoRedoMenu()`.
- El historial se actualiza en acciones de mapa (pintar, etc.). No está verificado si cubre todas las operaciones que deberían ser deshacibles (objetos, triggers, etc.).

**Recomendación:** Revisar que todas las acciones editables (mapa, objetos, triggers) registren cambios en el mismo historial y que Deshacer/Rehacer sean coherentes en cada pestaña/contexto.

---

## 6. Duplicados o lógica repetida

- **Carga de proyecto/escena:** Hay rutas que asumen `mapa.json` / `objetos.json` en raíz y otras que usan `Scenes`, `MapPathRelative`, `ObjectsPathRelative`. La lógica está repartida entre `LoadProjectData`, `ProjectSerialization`, `ProjectInfo.GetSceneMapPath`/`GetSceneObjectsPath` y uso de `MainSceneRelative`. No hay duplicación de código grave, pero la convención "legacy vs multi-escena" debe estar clara en comentarios.
- **EditorWindow y StartupWindow:** Ambos pueden abrir proyectos; el flujo de "proyecto reciente" y "abrir proyecto" está en StartupWindow; el editor recibe `ProjectInfo` ya cargado. No hay duplicación innecesaria.

**Recomendación:** Mantener un único punto de verdad para "dónde está el mapa/objetos de la escena actual/principal" (ProjectInfo + SceneDefinition) y referenciarlo desde editor y serialización.

- **SettingsPage.xaml.cs y SettingsWindow.xaml.cs:** Lógica muy similar para leer/escribir campos de configuración (TxtEditorFontFamily, TxtProjectsPath, etc.). Posible duplicación; valorar extraer a un helper o vista compartida.

---

## 7. Posibles puntos frágiles

- **Import Scene:** Tras copiar mapa y objetos, las rutas de assets dentro de esos JSON siguen apuntando al proyecto de origen. El botón "Copiar encontrados al proyecto" copia a `Assets/` del proyecto actual; si la ruta en el JSON es `Assets/sprites/enemy.png`, al copiar se mantiene esa estructura bajo `Assets/`. Si en el proyecto de origen la ruta era relativa a la raíz (por ejemplo `sprites/enemy.png`), podría no coincidir. Revisar convención de rutas (siempre relativas a proyecto, o siempre bajo `Assets/`).
- **Limpiar huérfanos:** Solo considera `Maps/*.json` y `Objects/*.json`. No detecta otros JSON huérfanos (por ejemplo en Scripts o en otras carpetas). Está bien acotado; si en el futuro se añaden más tipos de "escena" o recursos, conviene extender la lista de carpetas o patrones de forma explícita.
- **Scan de assets en importación:** Solo se consideran `SpritePath` (objetos) y `SourceImagePath` (tiles). No se escanean referencias en scripts (por ejemplo rutas en Lua). Mejora futura: parser básico o convención de rutas en scripts para incluirlas en el informe.

---

## 8. Converters con ConvertBack no implementado

Los converters **BoolToPinLabelConverter**, **StringToBrushConverter**, **NullToVisibilityConverter** y **PathToFileNameConverter** implementan `ConvertBack` lanzando `NotImplementedException`. Si algún binding que los use es `TwoWay`, fallará en tiempo de ejecución al escribir.

**Recomendación:** Comprobar que no se usen en modo TwoWay; si se usan, implementar ConvertBack o usar `OneWay` explícitamente.

---

## 9. Código muerto o redundante

| Ubicación | Detalle |
|-----------|---------|
| **ProjectDto.ObjectsPath** | Se escribe en `ProjectSerialization.Save` (siempre `"objetos.json"`) pero en `FromDto` no se lee; la carga usa `MainObjectsPath` y `Scenes`. La propiedad en el DTO es redundante para la carga. |
| **ProjectSerialization.Save — Splash** | Siempre se asigna `Splash = new SplashConfigDto()`; no se copia desde `ProjectInfo` (que no tiene Splash; el splash está en EngineSettings). Al cargar, `dto.Splash` no se mapea a `ProjectInfo`. Coherente con diseño actual; documentar si en el futuro el proyecto tiene su propio splash. |

---

## 10. Catch vacíos que ocultan errores

**Corregido:** En EditorWindow.xaml.cs ~1881 (guardado tras añadir/reordenar escena), el catch ahora muestra Toast y registra el error con EditorLog.

| Ubicación | Contexto | Riesgo |
|-----------|----------|--------|
| **EditorWindow.xaml.cs** ~1837, 1842 | ReleaseMouseCapture en arrastre de escenas | Menor; fallos de captura. |
| **ProjectExplorerPanel.xaml.cs** ~1168 | `Process.Start("explorer.exe", ...)` para "Mostrar en explorador" | Si falla (ruta inválida, etc.) no se informa. Valorar log o mensaje. |
| **ObjectInspectorPanel / QuickPropertiesPanel** | Carga de imagen para preview | Aceptable; si la imagen no carga, simplemente no se muestra preview. |

---

## 11. SceneDefinition: rutas vacías

`SceneDefinition.GetMapPath` y `GetObjectsPath` hacen `Path.Combine(projectDirectory, MapPathRelative ?? "")`. Si `MapPathRelative` o `ObjectsPathRelative` están vacíos, el resultado es solo `projectDirectory` (ruta de carpeta, no de archivo). En la práctica, `FromDto` y los flujos de creación asignan siempre valores por defecto; pero si se instancia `SceneDefinition` manualmente sin asignar rutas, el comportamiento es frágil.

**Recomendación:** En los getters, usar fallback a `"mapa.json"` / `"objetos.json"` si la ruta relativa es null o vacía.

---

## 12. Toast no implementado

El documento recomienda sustituir MessageBox "en desarrollo" por Toast. En el código no existe actualmente un sistema Toast; solo se usa `MessageBox`. Para aplicar la recomendación hace falta implementar un mecanismo de notificación tipo Toast (o reutilizar uno existente si se añade).

---

## 13. Resumen de acciones recomendadas (prioridad)

1. **Alta:** Sustituir todos los MessageBox "en desarrollo" por Toast + log.
2. **Media:** Revisar que Undo/Redo cubra todas las operaciones editables que se quieran deshacer.
3. **Media:** Comprobar que los converters (BoolToPinLabel, StringToBrush, NullToVisibility, PathToFileName) no se usen en TwoWay; si se usan, implementar ConvertBack o fijar OneWay.
4. **Baja:** Documentar convención de rutas de assets (proyecto vs Assets/) para Import + "Copiar encontrados".
5. **Baja:** Cuando exista AudioApi, implementar el TODO en PlayModeRunner para play(id).
6. **Baja:** Valorar fallback en SceneDefinition.GetMapPath/GetObjectsPath cuando MapPathRelative/ObjectsPathRelative estén vacíos (p. ej. "mapa.json"/"objetos.json").

---

*Documento generado a partir de revisión de código. Actualizar cuando se implementen cambios.*
