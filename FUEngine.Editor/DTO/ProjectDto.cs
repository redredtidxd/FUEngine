namespace FUEngine.Editor;

/// <summary>
/// DTO para proyecto (proyecto.json).
/// </summary>
public class ProjectDto
{
    /// <summary>ID único del proyecto (ej. proj_001). Editable y visible en Propiedades.</summary>
    public string? Id { get; set; }
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
    public int InitialChunksW { get; set; } = 4;
    public int InitialChunksH { get; set; } = 4;
    public int ChunkLoadRadius { get; set; } = 2;
    /// <summary>Si null al cargar JSON antiguo, se usa 4.</summary>
    public int? ChunkStreamEvictMargin { get; set; }
    /// <summary>Si null al cargar JSON antiguo, se usa true (volcar vacíos tocados).</summary>
    public bool? ChunkStreamSpillRuntimeEmpty { get; set; }
    public bool ChunkUnloadFar { get; set; } = true;
    public bool ChunkSaveByChunk { get; set; } = true;
    public bool ChunkEntitySleep { get; set; } = true;
    public bool ChunkStreaming { get; set; } = true;
    public bool ShowChunkBounds { get; set; } = false;
    public int TileHeight { get; set; } = 1;
    public bool AutoTiling { get; set; } = false;
    public int Fps { get; set; } = 60;
    public double AnimationSpeedMultiplier { get; set; } = 1.0;
    public int GameResolutionWidth { get; set; } = 320;
    public int GameResolutionHeight { get; set; } = 180;
    public string? AssetsRootFolder { get; set; } = "Assets";
    public int ProjectGridSnapPx { get; set; } = 0;
    public string? DefaultFirstSceneBackgroundColor { get; set; }
    public string? HUDColor { get; set; }
    public string? HUDStyle { get; set; }
    public string? GameFontFamily { get; set; }
    public int GameFontSize { get; set; } = 16;
    public string? ExportFormatImage { get; set; }
    public string? ExportFormatAudio { get; set; }
    public string? NamingRuleObjects { get; set; }
    public string? NamingRuleSeeds { get; set; }
    public int CameraSizeWidth { get; set; } = 320;
    public int CameraSizeHeight { get; set; } = 180;
    public string? CameraLimits { get; set; }
    public string? CameraEffects { get; set; }
    public string? DefaultInputScheme { get; set; }
    public string? ProtagonistInstanceId { get; set; }
    public bool UseNativeInput { get; set; }
    public bool UseNativeCameraFollow { get; set; }
    public float NativeCameraSmoothing { get; set; } = 8f;
    public float NativeMoveSpeedTilesPerSecond { get; set; } = 4f;
    public bool AutoFlipSprite { get; set; }
    public bool UseNativeAutoAnimation { get; set; }
    public string? StartupMusicPath { get; set; }
    public string? StartupSoundPath { get; set; }
    public string? AudioManifestPath { get; set; } = "audio.json";
    /// <summary>Si falta en JSON antiguo, <see cref="ProjectSerialization.FromDto"/> usa 1.</summary>
    public float? MasterVolume { get; set; }
    /// <summary>Si falta en JSON antiguo, se usa 0,7.</summary>
    public float? MusicVolume { get; set; }
    /// <summary>Si falta en JSON antiguo, se usa 1.</summary>
    public float? SfxVolume { get; set; }
    public List<string>? ProjectEnabledPlugins { get; set; }
    public int DefaultAnimationFps { get; set; } = 12;
    public bool DefaultCollisionEnabled { get; set; } = true;
    public double PhysicsGravity { get; set; } = 0;
    public bool PhysicsEnabled { get; set; } = false;
    /// <summary>Semilla RNG en Lua (<c>game.setRandomSeed</c> / reproducibilidad). Opcional.</summary>
    public int? RuntimeRandomSeed { get; set; }
    public string? BootstrapScriptId { get; set; }
    public bool PixelPerfect { get; set; } = true;
    public double InitialZoom { get; set; } = 1.0;
    public bool LightShadowDefault { get; set; } = false;
    public bool DebugMode { get; set; } = false;
    public bool ScriptNodes { get; set; } = false;
    /// <summary>Guardado automático cada N segundos (0 = desactivado).</summary>
    public int AutoSaveIntervalSeconds { get; set; }
    /// <summary>Activar autoguardado.</summary>
    public bool AutoSaveEnabled { get; set; } = true;
    /// <summary>Intervalo de autoguardado en minutos (por defecto 5).</summary>
    public int AutoSaveIntervalMinutes { get; set; } = 5;
    /// <summary>Máximo de backups por tipo a conservar (mapa, objetos).</summary>
    public int AutoSaveMaxBackupsPerType { get; set; } = 10;
    /// <summary>Carpeta de autoguardados (relativa al proyecto o absoluta).</summary>
    public string? AutoSaveFolder { get; set; } = "Autoguardados";
    /// <summary>Autoguardar al cerrar si hay cambios pendientes.</summary>
    public bool AutoSaveOnClose { get; set; } = true;
    /// <summary>Autoguardar solo si hubo cambios.</summary>
    public bool AutoSaveOnlyWhenDirty { get; set; } = true;
    /// <summary>Activa medidor de miedo (cercanía a animatrónicos, mirada).</summary>
    public bool FearMeterEnabled { get; set; }
    /// <summary>Activa medidor de peligro (luz, ruido, situación).</summary>
    public bool DangerMeterEnabled { get; set; }
    /// <summary>Tileset JSON por defecto para API de tiles (ruta relativa al proyecto).</summary>
    public string? DefaultTilesetPath { get; set; }

    /// <summary>Nombres de capas (suelo, decorativo, interactivo…).</summary>
    public List<string> LayerNames { get; set; } = new() { "Suelo" };
    public string? MapPath { get; set; }
    /// <summary>Ruta al mapa de la escena Start (estructura del nivel).</summary>
    public string? MainMapPath { get; set; } = "mapa.json";
    /// <summary>Ruta al archivo de objetos de la escena Start (instancias).</summary>
    public string? MainObjectsPath { get; set; } = "objetos.json";
    /// <summary>Obsoleto: proyectos antiguos guardaban solo esto. Al cargar se mapea a MainObjectsPath.</summary>
    public string? MainSceneRelative { get; set; }
    public string? ObjectsPath { get; set; }
    public bool CreatedWithFUEngine { get; set; } = true;
    /// <summary>Versión de FUEngine con la que se guardó el proyecto (ej: 0.0.1).</summary>
    public string? EngineVersion { get; set; }
    /// <summary>Lenguaje de scripting (ej: "Lua"). Por defecto "Lua".</summary>
    public string ScriptingLanguage { get; set; } = "Lua";
    public SplashConfigDto? Splash { get; set; }
    /// <summary>Lista de escenas del proyecto (Start, End por defecto en proyecto nuevo).</summary>
    public List<SceneDto>? Scenes { get; set; }
}

/// <summary>DTO de una escena del proyecto.</summary>
public class SceneDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string MapPathRelative { get; set; } = "";
    public string ObjectsPathRelative { get; set; } = "";
    public List<string>? DefaultTabKinds { get; set; }
}

public class SplashConfigDto
{
    public string LogoPath { get; set; } = "assets/logo_fuengine.png";
    public int DurationMs { get; set; } = 2500;
    public bool FadeIn { get; set; } = true;
    public bool FadeOut { get; set; } = true;
    public int FadeInMs { get; set; } = 500;
    public int FadeOutMs { get; set; } = 500;
    public string? SoundPath { get; set; }
}
