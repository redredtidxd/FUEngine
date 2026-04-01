# Checklist de revisión completa – Motor FUEngine (Seeds + configuradores)

Revisión realizada según el checklist. Se indican estado (OK / Corregido / Pendiente / N/A) y acciones tomadas.

---

## 1. Estructura del proyecto y archivos

| Punto | Estado | Detalle |
|-------|--------|---------|
| Carpetas base (Assets, Maps, Scripts, Seeds) | OK | `NewProjectStructure` crea `Seeds` (ya no Prefabs). Paths consistentes. |
| Nombres JSON (seeds.json, prefabs.json) | OK | Proyecto usa `seeds.json`; migración desde `prefabs.json` con backup. |
| UnusedAssetScanner incluye Seeds y Prefabs | OK | `GetAllReferencedPaths` añade tanto `seeds.json` como `prefabs.json` para no marcar como no usados. |

---

## 2. Core y tipos base

| Punto | Estado | Detalle |
|-------|--------|---------|
| SeedDefinition / PrefabDefinition | OK | Solo existen tipos Seed (`SeedDefinition`, `SeedObjectEntry`, `SeedInstance`). Prefab eliminado del Core. |
| Propiedades y valores por defecto | OK | Id, Nombre, Descripcion, Objects, Tags con inicialización correcta. Offsets y Rotation en SeedObjectEntry. |
| ObjectInstance, clases compartidas | N/A | No hay campos redundantes por la migración; ObjectInstance sigue siendo el tipo de instancia en mapa. |
| IWorldContext / SelfProxy Instantiate | OK | `Instantiate(string prefabName, ...)` documentado como “seed”; parámetro se mantiene por compatibilidad de API. Sin duplicación de lógica (un solo camino). |

---

## 3. Serialización / DTOs

| Punto | Estado | Detalle |
|-------|--------|---------|
| SeedSerialization FromDto/ToDto | OK | Save mapea SeedDefinition → SeedItemDto; Load mapea desde "seeds" o "prefabs" y rellena SeedDefinition. |
| Archivos vacíos o inexistentes | Corregido | `Load`: si el archivo no existe devuelve lista vacía; si el JSON es `null`/whitespace devuelve lista vacía; si `JsonDocument.Parse` falla se captura y se devuelve lista vacía (no excepción). |
| Compatibilidad hacia atrás | OK | Load acepta clave JSON "prefabs" además de "seeds". Proyectos antiguos cargan correctamente. |

---

## 4. Editor y UI

| Punto | Estado | Detalle |
|-------|--------|---------|
| EditorWindow tabs: Prefabs vs Seeds | OK | Solo existe el tab "Seeds"; usa `CreateObjectsTabContent()`. Comentario en código: si Seeds necesita lógica distinta, crear `CreateSeedsTabContent()`. |
| OptionalTabKindsOrder y bindings | OK | Orden incluye "Seeds"; TabDisplayNames, TabIcons, TabCategory tienen entrada "Seeds". |
| ObjectInspectorPanel eventos | OK | Un solo evento `RequestConvertToSeed` (no duplicado con Prefab). Desuscripción en `CleanupCachedPanels` con manejadores nombrados para evitar fugas. |
| ProjectExplorerPanel filtros y rutas | OK | Filtro "Seed"; rutas con `IndexOf("/Seeds/", StringComparison.OrdinalIgnoreCase)` para multiplataforma. Solo `CreateNewSeed` (no CreateNewPrefab). |

---

## 5. Settings / Engine

| Punto | Estado | Detalle |
|-------|--------|---------|
| EngineSettings nuevas propiedades | OK | GridVisibleByDefault, RulersVisibleByDefault, formatos de exportación, DefaultSeedsPath, etc. definidos con JsonPropertyName. |
| Bindings SettingsWindow | OK | BindToUi / ReadFromUi con comprobaciones null para controles opcionales; ComboBoxes para tema (PixelStyle, RetroCRT), idioma (es, en, ja), canal de actualizaciones, etc. |
| Persistencia al reiniciar | OK | Settings se guardan en `%LocalAppData%/FUEngine/settings.json`; Load() al iniciar. |

---

## 6. Funcionalidad del mundo y mapa

| Punto | Estado | Detalle |
|-------|--------|---------|
| Instanciación Seeds en mapa | N/A | Colocación en mapa por ObjectDefinition; Lua `world.instantiate(seedName, x, y)` en Play expande `seeds.json` vía `TryExpandPrefab` en `WorldContextFromList`. |
| Posiciones, rotaciones, layer order | OK | SeedObjectEntry tiene OffsetX, OffsetY, Rotation; ObjectInstance en mapa tiene sus propios X, Y, Rotation, LayerOrder. |
| Scripts asociados a objetos/Seeds | N/A | NotifyMissingScripts avisa scripts inexistentes en objetos; seeds son plantillas de objetos, no añaden lógica nueva. |

---

## 7. Migración y compatibilidad

| Punto | Estado | Detalle |
|-------|--------|---------|
| Migración prefabs → seeds sin pérdida | OK | Si existe `prefabs.json` y no `seeds.json`, se carga desde prefabs, se hace backup a `prefabs.json.backup`, y se escribe `seeds.json`. |
| Backups automáticos en migración | OK | Implementado en EditorWindow (LoadProjectData). |
| Convivencia Seeds y Prefabs | OK | No hay convivencia de tipos: solo Seeds en código; archivos antiguos se leen por clave "prefabs" en JSON y se guardan como seeds. |

---

## 8. Bugs típicos

| Punto | Estado | Detalle |
|-------|--------|---------|
| Funciones duplicadas | Pendiente | CreateObjectsTabContent compartido para Seeds; posible refactor futuro (ObjectTypeHandler<T>) si se añaden más tipos. |
| Eventos que no se desuscriben | Corregido | CleanupCachedPanels desuscribe RequestConvertToSeed, PropertyChanged, RequestDuplicate, RequestDelete, RequestRename del ObjectInspectorPanel con manejadores nombrados. |
| Mayúsculas/minúsculas en paths | Corregido | ProjectExplorerPanel usa OrdinalIgnoreCase en rutas "/Seeds/". |
| JSON vacío o inexistente | Corregido | SeedSerialization.Load: archivo inexistente → lista vacía; JSON vacío o inválido → lista vacía sin lanzar. |
| Eliminar Seeds en uso en mapa | N/A | Los seeds son plantillas; las instancias en mapa son ObjectInstance. Borrar un seed no borra instancias ya colocadas (queda referencia por DefinitionId). |

---

## 9. Documentación / onboarding

| Punto | Estado | Detalle |
|-------|--------|---------|
| AI-ONBOARDING.md y STATUS.md | Corregido | Título del bloque de código corregido de "PrefabDefinition.cs" a "SeedDefinition.cs". Resto de referencias ya actualizadas a Seeds en revisión anterior. |
| Estructura, paths y serialización | OK | Docs describen seeds.json, SeedDefinition, SeedObjectEntry, SeedSerialization. |

---

## 10. Mejoras y extras

| Punto | Estado | Detalle |
|-------|--------|---------|
| Duplicación de código / métodos genéricos | Pendiente | Opcional: ObjectTypeHandler<T> para reducir repetición entre tipos reutilizables (Seeds, Tiles, etc.). |
| Consistencia UI (iconos, tabs, tooltips) | OK | Tab "Seeds" con nombre e icono coherentes; botón "Convertir a seed" y textos actualizados. |
| Performance carga/guardado Seeds | OK | Misma estructura que antes (lista en un JSON); sin cambios que degraden rendimiento. |
| Tests unitarios | Pendiente | No hay proyecto de tests en el repo. SeedSerialization tiene comentario con criterios de validación (carga proyectos antiguos, roundtrip, preservar offsets/rotación). |

---

## Cambios realizados en esta revisión

1. **docs/AI-ONBOARDING.md**: Título "PrefabDefinition.cs" → "SeedDefinition.cs".
2. **SeedSerialization.Load**: Manejo seguro de JSON vacío o inválido (devuelve lista vacía en lugar de lanzar).
3. **ProjectConfigWindow**: `ChkGuardarSoloCambios.IsEnabled` actualizado en `UpdateAutosaveEnabledState` cuando autoguardado está desactivado.
4. **ProjectConfigWindow**: Comprobaciones de null para `ChkGuardarSoloCambios` en LoadFromProject, UpdateAutosaveEnabledState y TryApply (`ChkGuardarSoloCambios?.IsChecked`) para evitar NullReferenceException.
5. **ObjectInspectorPanel**: Obtención de `scriptPath` mediante variable intermedia `script` (FirstOrDefault) y uso de `script.Path` para claridad; el valor por defecto del tuple evita null ref cuando no hay coincidencia.

---

## Resumen

- **Estructura, Core, serialización, UI, Settings, migración y documentación** están alineados con Seeds y con el checklist.
- **Correcciones aplicadas**: documentación (título), carga de seeds (JSON vacío/inválido), desuscripción de eventos (ya hecha antes), rutas con OrdinalIgnoreCase (ya hechas antes), backup en migración (ya hecho), estado de ChkGuardarSoloCambios.
- **Pendiente opcional**: refactor genérico para tipos reutilizables, proyecto de tests automáticos para SeedSerialization y migración.
