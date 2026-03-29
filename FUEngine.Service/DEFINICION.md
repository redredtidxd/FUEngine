# FUEngine.Service — Definición y propósito

## ¿Qué es?

`FUEngine.Service` es la **capa de contratos de servicio** del motor. Define **interfaces** (no implementaciones) que representan las capacidades transversales del editor y del runtime: audio, autoguardado, gestión de proyectos, builds, scripting, logging, etc.

## ¿Por qué existe?

Hoy el código de servicios está repartido en dos sitios con roles poco definidos:

| Ubicación actual | Archivos | Problema |
|---|---|---|
| `FUEngine/Services/` | ~40+ clases | Mezcla lógica de negocio con WPF (timers de `DispatcherTimer`, `ImageSource`, NAudio). Todo está en namespace `FUEngine`, acoplado al ejecutable del editor. |
| `FUEngine.Editor/Services/` | 2 clases estáticas | Solo helpers de estructura de proyecto en disco. |

Esto genera tres problemas concretos:

1. **Acoplamiento circular**: `PlayModeRunner` (servicio) depende de WPF (`Dispatcher`), y los paneles WPF dependen de `PlayModeRunner`. No se pueden testear servicios sin levantar WPF.
2. **No hay contratos**: todo es clase concreta. No se puede sustituir un `AudioSystem` por un mock en tests, ni cambiar la implementación de `StartupService` sin tocar los 15+ sitios que lo llaman directamente.
3. **Confusión de capas**: `FUEngine.Core` es el dominio, `FUEngine` es la app… ¿dónde va la lógica de negocio que no es dominio ni es UI? No hay respuesta clara.

`FUEngine.Service` resuelve esto siendo la **capa intermedia de contratos**:

```
FUEngine.Core          (dominio: tipos, modelos, abstracciones gráficas)
    ↑
FUEngine.Service       (contratos: interfaces de servicios de aplicación) ← NUEVO
    ↑
FUEngine.Editor        (serialización, formato de proyecto)
FUEngine.Runtime       (game loop, Lua, Vulkan)
FUEngine               (app WPF: implementa las interfaces, inyecta en paneles)
```

## ¿Qué va aquí?

### SÍ pertenece a FUEngine.Service

- **Interfaces de servicio** (`IAudioSystem`, `IAutosaveService`, `IBuildService`, etc.)
- **DTOs puros** que los servicios necesitan (`RecentProjectEntry`, `ProjectIssue`, `UnusedAssetInfo`)
- **Enums de servicio** (`ProjectIssueSeverity`)
- **Service locator** (registro simple sin IoC pesado, coherente con el estilo actual del proyecto)

### NO pertenece a FUEngine.Service

| Tipo | Va en… |
|---|---|
| Tipos de dominio (`TileMap`, `GameObject`, `ProjectInfo`) | `FUEngine.Core` |
| Serialización JSON, DTOs de disco | `FUEngine.Editor` |
| Implementaciones concretas (NAudio, WPF timers, Discord RPC) | `FUEngine` (la app) |
| Game loop, Lua, Vulkan | `FUEngine.Runtime` / `FUEngine.Graphics.Vulkan` |

## Estructura de carpetas

```
FUEngine.Service/
├── FUEngine.Service.csproj
├── DEFINICION.md              ← este archivo
├── ServiceLocator.cs          ← registro de servicios (sin IoC)
├── IEditorLog.cs              ← logging centralizado
├── ISelectionService.cs       ← estado de selección del editor
├── IDiscordPresenceService.cs ← Discord Rich Presence
├── Audio/
│   ├── IAudioBackend.cs       ← backend de reproducción (editor vs runtime)
│   ├── IAudioSystem.cs        ← fachada de audio de alto nivel
│   └── IAudioAssetRegistry.cs ← catálogo ID → ruta de archivo
├── Project/
│   ├── IStartupService.cs     ← proyectos recientes y Hub
│   ├── IAppPaths.cs           ← rutas de AppData
│   └── IProjectIntegrityChecker.cs ← validación de integridad
├── Autosave/
│   └── IAutosaveService.cs    ← autoguardado periódico
├── Build/
│   └── IBuildService.cs       ← exportar build del juego
├── Assets/
│   ├── ITextureAssetCache.cs  ← caché de metadatos de texturas
│   └── IAssetScanner.cs       ← detección de assets no usados
└── Scripting/
    ├── IScriptHotReloadWatcher.cs ← hot reload de .lua
    └── IScriptRegistryWriter.cs   ← sync de scripts.json
```

## Cómo se usa (ejemplo de migración gradual)

### Paso 1: Las implementaciones existentes implementan las interfaces

```csharp
// En FUEngine/Services/AudioSystem.cs (ya existe)
public sealed class AudioSystem : IAudioSystem  // ← añadir la interfaz
{
    // ... código existente sin cambios ...
}
```

### Paso 2: Registro en el arranque

```csharp
// En App.xaml.cs o StartupWindow
ServiceLocator.Register<IAudioSystem>(new AudioSystem(backend, registry));
ServiceLocator.Register<IEditorLog>(new ConsoleEditorLog());
```

### Paso 3: Los consumidores piden la interfaz

```csharp
// Antes:
AudioSystem.Instance.Play("explosion");

// Después:
ServiceLocator.Get<IAudioSystem>().Play("explosion");
```

### Paso 4 (opcional futuro): Tests sin WPF

```csharp
[Test]
public void PlayModeRunner_StartsCorrectly()
{
    ServiceLocator.Register<IAudioSystem>(new MockAudioSystem());
    ServiceLocator.Register<IEditorLog>(new NullEditorLog());
    // ... test sin necesitar WPF ni NAudio ...
}
```

## Dependencias del proyecto

- **Depende de**: `FUEngine.Core` (para tipos de dominio como `ProjectInfo`)
- **No depende de**: WPF, NAudio, Discord RPC, Lua, Vulkan, ni ninguna implementación concreta
- **Es referenciado por**: todos los demás proyectos que necesiten consumir o implementar servicios

## Convenciones

1. **Solo interfaces y DTOs** — nunca implementaciones con lógica de negocio
2. **Prefijo `I`** para interfaces (estándar C#)
3. **Documentación XML** en todas las interfaces (el contrato es la documentación)
4. **Namespace**: `FUEngine.Service` + subcarpeta (ej. `FUEngine.Service.Audio`)
5. **Sin dependencias externas** — solo `FUEngine.Core` y BCL de .NET
