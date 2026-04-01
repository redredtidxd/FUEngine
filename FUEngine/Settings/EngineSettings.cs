using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace FUEngine;


/// <summary>
/// Configuración global del motor (preferencias del editor en <c>%LocalAppData%/FUEngine/Config/user_preferences.json</c>).
/// </summary>
public class EngineSettings
{
    // 1. General / UI
    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "Oscuro"; // Oscuro, Claro, Personalizado

    [JsonPropertyName("uiScalePercent")]
    public int UiScalePercent { get; set; } = 100;

    [JsonPropertyName("language")]
    public string Language { get; set; } = "es"; // es, en, etc.

    [JsonPropertyName("showTipsOnStartup")]
    public bool ShowTipsOnStartup { get; set; } = true;

    /// <summary>Si true, el overlay "Bienvenido a FUEngine" no se vuelve a mostrar (el usuario lo cerró al menos una vez).</summary>
    [JsonPropertyName("welcomeOverlayDismissedOnce")]
    public bool WelcomeOverlayDismissedOnce { get; set; } = false;

    [JsonPropertyName("editorFontSize")]
    public int EditorFontSize { get; set; } = 12;

    [JsonPropertyName("editorFontFamily")]
    public string EditorFontFamily { get; set; } = "Segoe UI";

    [JsonPropertyName("defaultProjectFps")]
    public int DefaultProjectFps { get; set; } = 60;

    [JsonPropertyName("autoUpdateCheckEnabled")]
    public bool AutoUpdateCheckEnabled { get; set; } = true;

    [JsonPropertyName("autoUpdateChannel")]
    public string AutoUpdateChannel { get; set; } = "Stable"; // Stable, Beta

    /// <summary>Si true, la carpeta <c>Data/</c> (índices JSON del sistema) no aparece en el Explorador del editor; sigue visible en el disco.</summary>
    [JsonPropertyName("hideDataFolderInExplorer")]
    public bool HideDataFolderInExplorer { get; set; } = true;

    /// <summary>Hub, LastProject, NewProject.</summary>
    [JsonPropertyName("startupBehavior")]
    public string StartupBehavior { get; set; } = "Hub";

    [JsonPropertyName("hardwareAccelerationEnabled")]
    public bool HardwareAccelerationEnabled { get; set; } = true;

    /// <summary>Id de acción → texto mostrado (ej. Ctrl+S). Solo claves conocidas en <see cref="EditorShortcutBindings"/>.</summary>
    [JsonPropertyName("shortcutBindings")]
    public Dictionary<string, string> ShortcutBindings { get; set; } = new();

    /// <summary>Default, Unity, Photoshop o Custom (atajos editados a mano).</summary>
    [JsonPropertyName("shortcutPreset")]
    public string ShortcutPreset { get; set; } = "Default";

    /// <summary>Si true, el editor usa intervalo/activado de autoguardado del motor en lugar del proyecto.</summary>
    [JsonPropertyName("useEngineAutoSaveSettings")]
    public bool UseEngineAutoSaveSettings { get; set; }

    [JsonPropertyName("engineAutoSaveEnabled")]
    public bool EngineAutoSaveEnabled { get; set; } = true;

    [JsonPropertyName("engineAutoSaveIntervalMinutes")]
    public int EngineAutoSaveIntervalMinutes { get; set; } = 5;

    /// <summary>Multiplicador del paso de zoom con rueda (1 = predeterminado).</summary>
    [JsonPropertyName("mapZoomWheelSensitivity")]
    public double MapZoomWheelSensitivity { get; set; } = 1.0;

    /// <summary>Multiplicador del desplazamiento WASD/flechas en el mapa.</summary>
    [JsonPropertyName("mapPanKeyboardStepScale")]
    public double MapPanKeyboardStepScale { get; set; } = 1.0;

    [JsonPropertyName("externalCodeEditorPath")]
    public string ExternalCodeEditorPath { get; set; } = "";

    [JsonPropertyName("physicsCollisionMode")]
    public string PhysicsCollisionMode { get; set; } = "Simple";

    [JsonPropertyName("globalGravity")]
    public double GlobalGravity { get; set; } = 9.8;

    [JsonPropertyName("buildExeIconPath")]
    public string BuildExeIconPath { get; set; } = "";

    [JsonPropertyName("buildGameVersion")]
    public string BuildGameVersion { get; set; } = "1.0.0";

    /// <summary>Windowed o Fullscreen (build/export).</summary>
    [JsonPropertyName("buildDefaultWindowMode")]
    public string BuildDefaultWindowMode { get; set; } = "Windowed";

    /// <summary>Unidad de coordenadas: Tiles, SubTiles, Pixels.</summary>
    [JsonPropertyName("coordinateUnit")]
    public string CoordinateUnit { get; set; } = "Tiles";

    // 2. Rutas y archivos
    /// <summary>Ruta raíz donde se crean proyectos nuevos (ej: C:\Users\Usuario\MisProyectosEditor). Si está vacía se usa la ruta por defecto del usuario.</summary>
    [JsonPropertyName("defaultProjectsPath")]
    public string DefaultProjectsPath { get; set; } = "";

    [JsonPropertyName("sharedAssetsPath")]
    public string SharedAssetsPath { get; set; } = "";

    [JsonPropertyName("buildExportPath")]
    public string BuildExportPath { get; set; } = "";

    [JsonPropertyName("defaultExportFormatImage")]
    public string DefaultExportFormatImage { get; set; } = "PNG"; // PNG, WebP

    [JsonPropertyName("defaultExportFormatAudio")]
    public string DefaultExportFormatAudio { get; set; } = "OGG"; // OGG, WAV, MP3

    [JsonPropertyName("defaultExportFormatData")]
    public string DefaultExportFormatData { get; set; } = "JSON";

    [JsonPropertyName("defaultExportFormatSeed")]
    public string DefaultExportFormatSeed { get; set; } = ".seed";

    // 3. Render / Motor gráfico
    [JsonPropertyName("defaultTileScale")]
    public int DefaultTileScale { get; set; } = 16;

    [JsonPropertyName("renderMode")]
    public string RenderMode { get; set; } = "PixelPerfect"; // PixelPerfect, Subpixel, AntiAlias, CRTEffect

    [JsonPropertyName("editorFps")]
    public int EditorFps { get; set; } = 60;

    [JsonPropertyName("defaultLightingMode")]
    public string DefaultLightingMode { get; set; } = "Desactivar"; // Desactivar, Activar, Avanzado

    [JsonPropertyName("defaultPaletteId")]
    public string DefaultPaletteId { get; set; } = "default";

    /// <summary>Intensidad de luz global (0-100).</summary>
    [JsonPropertyName("lightingIntensity")]
    public int LightingIntensity { get; set; } = 80;

    /// <summary>Dirección de luz en grados (0-360).</summary>
    [JsonPropertyName("lightingDirectionDegrees")]
    public int LightingDirectionDegrees { get; set; } = 45;

    /// <summary>Dithering automático opcional (efecto retro).</summary>
    [JsonPropertyName("ditheringEnabled")]
    public bool DitheringEnabled { get; set; } = false;

    /// <summary>Velocidad de parallax global (0.0-2.0, 1.0 = normal).</summary>
    [JsonPropertyName("parallaxSpeed")]
    public double ParallaxSpeed { get; set; } = 1.0;

    /// <summary>FPS por defecto para animaciones.</summary>
    [JsonPropertyName("defaultAnimationFps")]
    public int DefaultAnimationFps { get; set; } = 12;

    /// <summary>Interpolación de animación: Ninguna, Lineal, Suave.</summary>
    [JsonPropertyName("animationInterpolation")]
    public string AnimationInterpolation { get; set; } = "Lineal";

    // 4. Tilemap y mundo
    [JsonPropertyName("defaultChunkSize")]
    public int DefaultChunkSize { get; set; } = 16;

    [JsonPropertyName("defaultTileSize")]
    public int DefaultTileSize { get; set; } = 16;

    [JsonPropertyName("maxTileHeight")]
    public int MaxTileHeight { get; set; } = 1;

    [JsonPropertyName("autoTilingByDefault")]
    public bool AutoTilingByDefault { get; set; } = false;

    // 5. Objetos / Scripts
    [JsonPropertyName("defaultCommonScriptIds")]
    public List<string> DefaultCommonScriptIds { get; set; } = new();

    [JsonPropertyName("scriptEditorMode")]
    public string ScriptEditorMode { get; set; } = "Nodos"; // Nodos, Codigo

    [JsonPropertyName("defaultPlaceholderAssetsPath")]
    public string DefaultPlaceholderAssetsPath { get; set; } = "";

    // 6. Editor / Herramientas
    [JsonPropertyName("editorZoomMin")]
    public double EditorZoomMin { get; set; } = 0.25;

    [JsonPropertyName("editorZoomMax")]
    public double EditorZoomMax { get; set; } = 4.0;

    /// <summary>Activar por defecto la máscara «solo tiles con colisión» al abrir el editor.</summary>
    [JsonPropertyName("collisionMaskVisibleByDefault")]
    public bool CollisionMaskVisibleByDefault { get; set; } = false;

    [JsonPropertyName("gridSnapPx")]
    public int GridSnapPx { get; set; } = 0; // 0 = Tile size, 1, 2, 4

    [JsonPropertyName("gridVisibleByDefault")]
    public bool GridVisibleByDefault { get; set; } = true;

    [JsonPropertyName("rulersVisibleByDefault")]
    public bool RulersVisibleByDefault { get; set; } = true;

    [JsonPropertyName("defaultVisiblePanels")]
    public List<string> DefaultVisiblePanels { get; set; } = new() { "Consola", "Inspector", "Scene", "Map" };

    [JsonPropertyName("defaultSceneBackgroundColor")]
    public string DefaultSceneBackgroundColor { get; set; } = "#FFFFFF";

    /// <summary>
    /// Orden de carpetas en la raíz al marcar «Generar jerarquía estándar» en el asistente de proyecto.
    /// <c>default</c> = <see cref="DefaultNewProjectRootFolderOrder"/>; <c>custom</c> = <see cref="CustomNewProjectRootFolderOrder"/>.
    /// </summary>
    [JsonPropertyName("newProjectRootFolderOrderPresetId")]
    public string NewProjectRootFolderOrderPresetId { get; set; } = "default";

    /// <summary>Una carpeta por entrada; solo se usa si el preset es <c>custom</c>.</summary>
    [JsonPropertyName("customNewProjectRootFolderOrder")]
    public List<string> CustomNewProjectRootFolderOrder { get; set; } = new();

    /// <summary>
    /// Carpetas **adicionales** en la raíz al crear proyecto con «Generar jerarquía estándar».
    /// No sustituyen el orden Sprites/Maps/…; se crean además (nombres de una sola carpeta, sin rutas).
    /// </summary>
    [JsonPropertyName("extraNewProjectRootFolders")]
    public List<string> ExtraNewProjectRootFolders { get; set; } = new();

    /// <summary>
    /// Tema que añade carpetas extra predefinidas (se combina con <see cref="ExtraNewProjectRootFolders"/>):
    /// <c>none</c>, <c>ui</c>, <c>jam</c>, <c>content</c>.
    /// </summary>
    [JsonPropertyName("newProjectExplorerThemeId")]
    public string NewProjectExplorerThemeId { get; set; } = "none";

    /// <summary>«Modo depuración» marcado por defecto en el asistente de nuevo proyecto.</summary>
    [JsonPropertyName("defaultNewProjectDebugMode")]
    public bool DefaultNewProjectDebugMode { get; set; } = true;

    /// <summary>Orden predefinido del motor (raíz del proyecto).</summary>
    public static readonly string[] DefaultNewProjectRootFolderOrder = { "Sprites", "Maps", "Scripts", "Audio", "Seeds" };

    /// <summary>Resuelve la lista ordenada de carpetas raíz para proyectos nuevos según esta configuración.</summary>
    public IReadOnlyList<string> GetResolvedNewProjectStandardRootFolders()
    {
        if (string.Equals(NewProjectRootFolderOrderPresetId?.Trim(), "custom", StringComparison.OrdinalIgnoreCase))
        {
            var list = CustomNewProjectRootFolderOrder?
                .Select(s => s?.Trim() ?? "")
                .Where(s => s.Length > 0)
                .ToList() ?? new List<string>();
            if (list.Count > 0) return list;
        }
        return DefaultNewProjectRootFolderOrder;
    }

    /// <summary>Carpetas extra (tema + lista manual) para proyectos nuevos; sin duplicados.</summary>
    public IReadOnlyList<string> GetResolvedExtraNewProjectRootFolders()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void TryAdd(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            var t = name.Trim();
            if (t.Length == 0 || t is "." or ".." || t.Contains('/') || t.Contains('\\')) return;
            if (t.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0) return;
            set.Add(t);
        }
        foreach (var x in ExtraNewProjectRootFolders ?? new List<string>())
            TryAdd(x);
        switch ((NewProjectExplorerThemeId ?? "none").Trim().ToLowerInvariant())
        {
            case "ui":
                TryAdd("UI");
                TryAdd("Prefabs");
                break;
            case "jam":
                TryAdd("Screenshots");
                TryAdd("Build");
                break;
            case "content":
                TryAdd("UI");
                TryAdd("Plugins");
                TryAdd("StreamingAssets");
                break;
        }
        return set.ToList();
    }

    [JsonPropertyName("thumbnailPreviewEnabled")]
    public bool ThumbnailPreviewEnabled { get; set; } = true;

    [JsonPropertyName("thumbnailPreviewTypes")]
    public List<string> ThumbnailPreviewTypes { get; set; } = new() { "sprites", "animaciones", "audio" };

    [JsonPropertyName("defaultEnabledPlugins")]
    public List<string> DefaultEnabledPlugins { get; set; } = new(); // ej: Animador, TileEditor

    [JsonPropertyName("assetCacheMaxMb")]
    public int AssetCacheMaxMb { get; set; } = 0; // 0 = sin límite

    [JsonPropertyName("assetCacheEnabled")]
    public bool AssetCacheEnabled { get; set; } = true;

    [JsonPropertyName("lightingPreviewEnabled")]
    public bool LightingPreviewEnabled { get; set; } = true;

    // 7. Compilación / Exportación
    [JsonPropertyName("splashCreatedWithFUEngine")]
    public bool SplashCreatedWithFUEngine { get; set; } = true;

    [JsonPropertyName("splashDurationMs")]
    public int SplashDurationMs { get; set; } = 2500;

    [JsonPropertyName("splashFadeIn")]
    public bool SplashFadeIn { get; set; } = true;

    [JsonPropertyName("splashFadeOut")]
    public bool SplashFadeOut { get; set; } = true;

    [JsonPropertyName("splashLogoPath")]
    public string SplashLogoPath { get; set; } = "assets/logo_fuengine.png";

    [JsonPropertyName("tempBuildPath")]
    public string TempBuildPath { get; set; } = "";

    [JsonPropertyName("autoOptimizeOnExport")]
    public bool AutoOptimizeOnExport { get; set; } = false;

    /// <summary>Confirmar antes de sobrescribir al guardar (protección tipo permadeath/FNAF).</summary>
    [JsonPropertyName("saveOverwriteProtection")]
    public bool SaveOverwriteProtection { get; set; } = false;

    [JsonPropertyName("defaultBuildRuntime")]
    public string DefaultBuildRuntime { get; set; } = "net8.0-windows";

    [JsonPropertyName("defaultBuildResolution")]
    public string DefaultBuildResolution { get; set; } = "1920x1080";

    [JsonPropertyName("defaultTileScalingBuild")]
    public int DefaultTileScalingBuild { get; set; } = 1;

    // 8. Avanzado / Experimental
    [JsonPropertyName("debugShowFps")]
    public bool DebugShowFps { get; set; } = false;

    /// <summary>Overlay de debug: coordenadas, colisión, Z-index en editor.</summary>
    [JsonPropertyName("debugShowOverlay")]
    public bool DebugShowOverlay { get; set; } = false;

    [JsonPropertyName("debugShowDrawCalls")]
    public bool DebugShowDrawCalls { get; set; } = false;

    [JsonPropertyName("debugShowMemory")]
    public bool DebugShowMemory { get; set; } = false;

    [JsonPropertyName("testRenderSpriteStacking")]
    public bool TestRenderSpriteStacking { get; set; } = false;

    [JsonPropertyName("testRenderDepthBlending")]
    public bool TestRenderDepthBlending { get; set; } = false;

    [JsonPropertyName("testRenderDynamicLight")]
    public bool TestRenderDynamicLight { get; set; } = false;

    [JsonPropertyName("aiIntegrationPath")]
    public string AiIntegrationPath { get; set; } = "";

    [JsonPropertyName("autoLogsEnabled")]
    public bool AutoLogsEnabled { get; set; } = true;

    [JsonPropertyName("autoLogsPath")]
    public string AutoLogsPath { get; set; } = "";

    /// <summary>IDs de objetos marcados como favoritos para acceso rápido en el editor.</summary>
    [JsonPropertyName("favoriteObjectIds")]
    public List<string> FavoriteObjectIds { get; set; } = new();

    /// <summary>Tipos de tile favoritos (por nombre o índice) para la paleta rápida.</summary>
    [JsonPropertyName("favoriteTileTypes")]
    public List<string> FavoriteTileTypes { get; set; } = new();

    private static string SettingsPath => FUEngineAppPaths.UserPreferencesPath;

    /// <summary>Nombre de la carpeta por defecto para proyectos del usuario (dentro del perfil).</summary>
    public const string DefaultProjectsFolderName = "MisProyectosEditor";

    /// <summary>Obtiene la ruta raíz donde crear proyectos: DefaultProjectsPath si está definida, si no la carpeta por defecto en el perfil del usuario. No crea la carpeta.</summary>
    public static string GetDefaultProjectsRoot()
    {
        var settings = Load();
        if (!string.IsNullOrWhiteSpace(settings.DefaultProjectsPath))
            return settings.DefaultProjectsPath.Trim();
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            DefaultProjectsFolderName);
    }

    /// <summary>Asegura que la carpeta raíz de proyectos exista y devuelve su ruta (usa GetDefaultProjectsRoot).</summary>
    public static string EnsureDefaultProjectsRoot()
    {
        var root = GetDefaultProjectsRoot();
        if (!Directory.Exists(root))
            Directory.CreateDirectory(root);
        return root;
    }

    /// <summary>Carga la configuración guardada o devuelve valores por defecto.</summary>
    public static EngineSettings Load()
    {
        FUEngineAppPaths.EnsureLayout();
        try
        {
            var path = SettingsPath;
            if (!File.Exists(path) && File.Exists(FUEngineAppPaths.LegacySettingsPath))
            {
                try
                {
                    var dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir))
                        Directory.CreateDirectory(dir);
                    File.Copy(FUEngineAppPaths.LegacySettingsPath, path, overwrite: false);
                }
                catch { path = FUEngineAppPaths.LegacySettingsPath; }
            }

            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                };
                var loaded = System.Text.Json.JsonSerializer.Deserialize<EngineSettings>(json, options) ?? new EngineSettings();
                if (string.Equals(path, FUEngineAppPaths.LegacySettingsPath, StringComparison.OrdinalIgnoreCase))
                    Save(loaded);
                return loaded;
            }
        }
        catch { /* use default */ }
        return new EngineSettings();
    }

    /// <summary>Guarda la configuración en Config/user_preferences.json (crea la carpeta si no existe).</summary>
    public static void Save(EngineSettings settings)
    {
        try
        {
            FUEngineAppPaths.EnsureLayout();
            var dir = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var options = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };
            var json = System.Text.Json.JsonSerializer.Serialize(settings, options);
            File.WriteAllText(SettingsPath, json);
        }
        catch { /* ignore */ }
    }
}
