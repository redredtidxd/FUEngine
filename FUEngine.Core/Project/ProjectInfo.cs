namespace FUEngine.Core;

/// <summary>
/// Información del proyecto cargado (nombre, resolución, rutas, metadata).
/// </summary>
public class ProjectInfo
{
    /// <summary>ID único del proyecto (ej. proj_001). Visible en Propiedades.</summary>
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string? Author { get; set; }
    public string? Copyright { get; set; }
    public string Version { get; set; } = "0.0.1";
    public string? IconPath { get; set; }
    public string? PaletteId { get; set; }
    public string? TemplateType { get; set; }
    public int TileSize { get; set; } = 16;
    public int MapWidth { get; set; } = 64;
    public int MapHeight { get; set; } = 64;
    public bool Infinite { get; set; } = true;
    public int ChunkSize { get; set; } = 32;
    /// <summary>Cantidad de chunks en X para plantillas y límites de integridad; casillas mapa ≈ esto × <see cref="ChunkSize"/> (si el mapa finito coincide con el proyecto).</summary>
    public int InitialChunksW { get; set; } = 4;
    /// <summary>Cantidad de chunks en Y; casillas ≈ esto × <see cref="ChunkSize"/>.</summary>
    public int InitialChunksH { get; set; } = 4;
    /// <summary>Radio de chunks alrededor de la cámara a cargar (1-4).</summary>
    public int ChunkLoadRadius { get; set; } = 2;
    /// <summary>
    /// Márgen extra (Chebyshev) sumado a <see cref="ChunkLoadRadius"/> para mantener chunks en memoria antes de descargar
    /// y para precargar desde caché de Play. Mayor = menos tirones al moverse rápido; más RAM.
    /// </summary>
    public int ChunkStreamEvictMargin { get; set; } = 4;
    /// <summary>Si true, los chunks vacíos modificados en runtime se vuelcan a disco al descargar y se restauran al volver (streaming).</summary>
    public bool ChunkStreamSpillRuntimeEmpty { get; set; } = true;
    /// <summary>Descargar chunks lejanos de memoria.</summary>
    public bool ChunkUnloadFar { get; set; } = true;
    /// <summary>Guardar mapa por chunks (chunk_0_0.map, etc.).</summary>
    public bool ChunkSaveByChunk { get; set; } = true;
    /// <summary>Dormir entidades en chunks fuera del radio (mejor rendimiento).</summary>
    public bool ChunkEntitySleep { get; set; } = true;
    /// <summary>Cargar chunks en segundo plano (streaming).</summary>
    public bool ChunkStreaming { get; set; } = true;
    /// <summary>Mostrar límites de chunk en el editor (solo debug).</summary>
    public bool ShowChunkBounds { get; set; } = false;
    public int TileHeight { get; set; } = 1;
    public bool AutoTiling { get; set; } = false;
    public int Fps { get; set; } = 60;
    public double AnimationSpeedMultiplier { get; set; } = 1.0;
    public int GameResolutionWidth { get; set; } = 320;
    public int GameResolutionHeight { get; set; } = 180;
    public string AssetsRootFolder { get; set; } = "Assets";
    public int ProjectGridSnapPx { get; set; } = 0;
    public string DefaultFirstSceneBackgroundColor { get; set; } = "#FFFFFF";
    /// <summary>Color de fondo del lienzo del mapa en el editor (hex #RRGGBB).</summary>
    public string EditorMapCanvasBackgroundColor { get; set; } = "#21262d";
    public string? HUDColor { get; set; }
    public string HUDStyle { get; set; } = "Minimal";
    public string? GameFontFamily { get; set; }
    public int GameFontSize { get; set; } = 16;
    public string ExportFormatImage { get; set; } = "PNG";
    public string ExportFormatAudio { get; set; } = "OGG";
    public string NamingRuleObjects { get; set; } = "libre";
    public string NamingRuleSeeds { get; set; } = "libre";
    public int CameraSizeWidth { get; set; } = 320;
    public int CameraSizeHeight { get; set; } = 180;
    public string? CameraLimits { get; set; }
    public string? CameraEffects { get; set; }
    public string DefaultInputScheme { get; set; } = "Keyboard";
    /// <summary>InstanceId de la instancia protagonista en objetos.json (un solo marcador por proyecto).</summary>
    public string? ProtagonistInstanceId { get; set; }
    /// <summary>WASD/flechas mueven al protagonista en Play (antes de Lua).</summary>
    public bool UseNativeInput { get; set; }
    /// <summary>La cámara del visor sigue al protagonista con suavizado (tras Lua/física).</summary>
    public bool UseNativeCameraFollow { get; set; }

    /// <summary>Centro de la cámara en casillas mundo (esquina sup.-izq. del mundo = 0); el marco azul se centra aquí. Mapas finitos: suele iniciarse en el centro geométrico del mapa (<c>MapWidth/2</c>, <c>MapHeight/2</c>).</summary>
    public double EditorViewportCenterWorldX { get; set; }

    /// <summary>Centro de la cámara en casillas mundo (eje Y).</summary>
    public double EditorViewportCenterWorldY { get; set; }

    /// <summary>Si true, el sandbox del tab Juego no se pausa al pasar a la pestaña Mapa (edición + vista Play a la vez).</summary>
    public bool KeepEmbeddedPlayRunningWithMapTab { get; set; } = true;
    /// <summary>Factor de suavizado de cámara (≈ velocidad hacia el objetivo; 0 = instantáneo).</summary>
    public float NativeCameraSmoothing { get; set; } = 8f;
    /// <summary>Velocidad de movimiento nativo en casillas por segundo.</summary>
    public float NativeMoveSpeedTilesPerSecond { get; set; } = 4f;
    /// <summary>Invertir sprite en X según la dirección del movimiento (ScaleX negativo en render).</summary>
    public bool AutoFlipSprite { get; set; }
    /// <summary>Si true, el protagonista usa clips <c>Idle</c> / <c>Walk</c> de <c>animaciones.json</c> (Id o Nombre) según la intención de movimiento con teclas.</summary>
    public bool UseNativeAutoAnimation { get; set; }
    public string? StartupMusicPath { get; set; }
    public string? StartupSoundPath { get; set; }
    /// <summary>Ruta relativa al catálogo de audio (id → archivo). Por defecto audio.json.</summary>
    public string AudioManifestPath { get; set; } = "audio.json";
    /// <summary>Volumen maestro 0–1 aplicado a música y SFX.</summary>
    public float MasterVolume { get; set; } = 1f;
    /// <summary>Volumen del bus música 0–1 (tras master).</summary>
    public float MusicVolume { get; set; } = 0.7f;
    /// <summary>Volumen del bus efectos 0–1 (tras master).</summary>
    public float SfxVolume { get; set; } = 1f;
    public List<string> ProjectEnabledPlugins { get; set; } = new();
    public int DefaultAnimationFps { get; set; } = 12;
    public bool DefaultCollisionEnabled { get; set; } = true;
    public double PhysicsGravity { get; set; } = 0;
    public bool PhysicsEnabled { get; set; } = false;
    /// <summary>Semilla fija del RNG expuesto a Lua en Play. Null = no forzar (comportamiento no determinista al arrancar).</summary>
    public int? RuntimeRandomSeed { get; set; }
    public string? BootstrapScriptId { get; set; }
    public bool PixelPerfect { get; set; } = true;

    /// <summary>Antialiasing global previsto para el visor/juego: <c>none</c>, <c>fxaa</c>, <c>msaa</c>. Pixel art: <c>none</c>.</summary>
    public string RenderAntiAliasMode { get; set; } = ProjectRenderSettings.AntiAliasNone;

    /// <summary>Muestras MSAA cuando <see cref="RenderAntiAliasMode"/> es <c>msaa</c>: 0, 2, 4 u 8.</summary>
    public int MsaaSampleCount { get; set; }

    /// <summary>Filtrado de texturas: <c>nearest</c> (pixel art) o <c>bilinear</c>.</summary>
    public string TextureFilterMode { get; set; } = ProjectRenderSettings.FilterNearest;

    public double InitialZoom { get; set; } = 1.0;
    public bool LightShadowDefault { get; set; } = false;
    public bool DebugMode { get; set; } = false;
    public bool ScriptNodes { get; set; } = false;
    public int AutoSaveIntervalSeconds { get; set; }
    /// <summary>Activar autoguardado en carpeta separada (por defecto Autoguardados).</summary>
    public bool AutoSaveEnabled { get; set; } = true;
    /// <summary>Intervalo en minutos (por defecto 5). Si 0, usa AutoSaveIntervalSeconds.</summary>
    public int AutoSaveIntervalMinutes { get; set; } = 5;
    /// <summary>Máximo de archivos de autoguardado por tipo (mapa, objetos) a conservar.</summary>
    public int AutoSaveMaxBackupsPerType { get; set; } = 10;
    /// <summary>Carpeta de autoguardados relativa al proyecto o ruta absoluta (por defecto "Autoguardados").</summary>
    public string AutoSaveFolder { get; set; } = "Autoguardados";
    /// <summary>Autoguardar al cerrar la ventana si hay cambios pendientes.</summary>
    public bool AutoSaveOnClose { get; set; } = true;
    /// <summary>Autoguardar solo si hubo cambios desde el último guardado.</summary>
    public bool AutoSaveOnlyWhenDirty { get; set; } = true;
    public bool FearMeterEnabled { get; set; }
    public bool DangerMeterEnabled { get; set; }
    /// <summary>Lenguaje de scripting del proyecto (ej: "Lua"). Permite cambiar en el futuro (ej: "RedLanguage").</summary>
    public string ScriptingLanguage { get; set; } = "Lua";
    /// <summary>Proveedor de anuncios para metadatos de export (Android/iOS): null o "simulated" = editor; "google_mobile_ads" = integración nativa prevista.</summary>
    public string? AdsExportProvider { get; set; }
    /// <summary>Nombres de capas (suelo, decorativo, interactivo, animatrónicos…). El índice es LayerId.</summary>
    public List<string> LayerNames { get; set; } = new() { "Suelo" };
    public string ProjectDirectory { get; set; } = "";
    /// <summary>Ruta relativa opcional al JSON de tileset por defecto (ej. Assets/Tilesets/terrain.tileset.json) para world.setTile en Lua.</summary>
    public string? DefaultTilesetPath { get; set; }

    /// <summary>Ruta relativa al mapa principal (ej: "mapa.json" o "Maps/map_001.json"). Usado cuando no hay Scenes (proyectos legacy).</summary>
    public string MapPathRelative { get; set; } = "mapa.json";
    /// <summary>Ruta relativa al MAPA de la escena Start (modo Play). Estructura del nivel. Por defecto mapa.json.</summary>
    public string MainMapPath { get; set; } = "mapa.json";
    /// <summary>Ruta relativa al archivo de OBJETOS (instancias) de la escena Start (modo Play). Por defecto objetos.json.</summary>
    public string MainObjectsPath { get; set; } = "objetos.json";
    /// <summary>Lista de escenas del proyecto. Si es null o vacía, se trata como un solo mapa (legacy).</summary>
    public List<SceneDefinition>? Scenes { get; set; }
    /// <summary>Versión de FUEngine con la que se guardó el proyecto (si está en proyecto.json).</summary>
    public string? EngineVersion { get; set; }

    /// <summary>Versión de esquema del archivo de proyecto (<see cref="ProjectSchema.CurrentFormatVersion"/>). 0 = JSON sin campo (proyectos anteriores).</summary>
    public int ProjectFormatVersion { get; set; }
    /// <summary>Ruta del mapa principal: con Scenes usa el primero; si no, MapPathRelative.</summary>
    public string MapPath => GetSceneMapPath(0);
    /// <summary>Ruta de objetos principal: con Scenes usa el primero; si no, objetos.json.</summary>
    public string ObjectsPath => GetSceneObjectsPath(0);
    /// <summary>Ruta absoluta del mapa de la escena Start. Usar para Play (escena principal).</summary>
    public string MainSceneMapPath => Path.Combine(ProjectDirectory ?? "", (MainMapPath?.Trim()?.Length ?? 0) > 0 ? MainMapPath!.Trim() : "mapa.json");
    /// <summary>Ruta absoluta del archivo de objetos de la escena Start. Usar para Play (escena principal).</summary>
    public string MainSceneObjectsPath => Path.Combine(ProjectDirectory ?? "", (MainObjectsPath?.Trim()?.Length ?? 0) > 0 ? MainObjectsPath!.Trim() : "objetos.json");

    /// <summary>Ruta absoluta del mapa de la escena en el índice dado. Si no hay Scenes, usa MapPathRelative.</summary>
    public string GetSceneMapPath(int sceneIndex)
    {
        if (Scenes != null && sceneIndex >= 0 && sceneIndex < Scenes.Count)
            return Scenes[sceneIndex].GetMapPath(ProjectDirectory ?? "");
        return Path.Combine(ProjectDirectory ?? "", MapPathRelative ?? "mapa.json");
    }

    /// <summary>Ruta absoluta de objetos de la escena en el índice dado. Si no hay Scenes, usa objetos.json.</summary>
    public string GetSceneObjectsPath(int sceneIndex)
    {
        if (Scenes != null && sceneIndex >= 0 && sceneIndex < Scenes.Count)
            return Scenes[sceneIndex].GetObjectsPath(ProjectDirectory ?? "");
        return Path.Combine(ProjectDirectory ?? "", "objetos.json");
    }
    public string AnimacionesPath => Path.Combine(ProjectDirectory ?? "", "animaciones.json");
    /// <summary>Ruta absoluta del manifiesto de audio.</summary>
    public string AudioManifestAbsolutePath => Path.Combine(ProjectDirectory ?? "", string.IsNullOrWhiteSpace(AudioManifestPath) ? "audio.json" : AudioManifestPath.Trim());
    public string ScriptsPath => Path.Combine(ProjectDirectory ?? "", "scripts.json");
    public string TriggerZonesPath => Path.Combine(ProjectDirectory ?? "", "triggerZones.json");
    /// <summary>Ruta del archivo de seeds (seeds.json). Para proyectos antiguos, cargar desde prefabs.json si no existe seeds.json y guardar en seeds.json.</summary>
    public string SeedsPath => Path.Combine(ProjectDirectory ?? "", "seeds.json");
}
