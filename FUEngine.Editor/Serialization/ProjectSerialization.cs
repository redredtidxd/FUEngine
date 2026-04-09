using System.Text.Json;
using FUEngine.Core;
using System;

namespace FUEngine.Editor;

public static class ProjectSerialization
{
    private static string GenerateProjectId(ProjectInfo project)
    {
        return "proj_" + Guid.NewGuid().ToString("N")[..8];
    }

    public static void Save(ProjectInfo project, string path)
    {
        var dto = new ProjectDto
        {
            Id = string.IsNullOrWhiteSpace(project.Id) ? GenerateProjectId(project) : project.Id,
            Nombre = project.Nombre ?? "",
            Descripcion = project.Descripcion ?? "",
            Author = project.Author,
            Copyright = project.Copyright,
            Version = project.Version ?? "0.0.1",
            IconPath = project.IconPath,
            PaletteId = project.PaletteId,
            TemplateType = project.TemplateType,
            TileSize = project.TileSize,
            MapWidth = project.MapWidth,
            MapHeight = project.MapHeight,
            MapBoundsOriginWorldTileX = project.MapBoundsOriginWorldTileX,
            MapBoundsOriginWorldTileY = project.MapBoundsOriginWorldTileY,
            Infinite = project.Infinite,
            ChunkSize = project.ChunkSize,
            InitialChunksW = project.InitialChunksW,
            InitialChunksH = project.InitialChunksH,
            ChunkLoadRadius = project.ChunkLoadRadius,
            ChunkStreamEvictMargin = project.ChunkStreamEvictMargin,
            ChunkStreamSpillRuntimeEmpty = project.ChunkStreamSpillRuntimeEmpty,
            ChunkUnloadFar = project.ChunkUnloadFar,
            ChunkSaveByChunk = project.ChunkSaveByChunk,
            ChunkEntitySleep = project.ChunkEntitySleep,
            ChunkStreaming = project.ChunkStreaming,
            ShowChunkBounds = project.ShowChunkBounds,
            TileHeight = project.TileHeight,
            AutoTiling = project.AutoTiling,
            Fps = project.Fps,
            AnimationSpeedMultiplier = project.AnimationSpeedMultiplier,
            GameResolutionWidth = project.GameResolutionWidth,
            GameResolutionHeight = project.GameResolutionHeight,
            AssetsRootFolder = project.AssetsRootFolder ?? "Assets",
            ProjectGridSnapPx = project.ProjectGridSnapPx,
            DefaultFirstSceneBackgroundColor = project.DefaultFirstSceneBackgroundColor,
            EditorMapCanvasBackgroundColor = project.EditorMapCanvasBackgroundColor,
            HUDColor = project.HUDColor,
            HUDStyle = project.HUDStyle ?? "Minimal",
            GameFontFamily = project.GameFontFamily,
            GameFontSize = project.GameFontSize,
            ExportFormatImage = project.ExportFormatImage ?? "PNG",
            ExportFormatAudio = project.ExportFormatAudio ?? "OGG",
            NamingRuleObjects = project.NamingRuleObjects ?? "libre",
            NamingRuleSeeds = project.NamingRuleSeeds ?? "libre",
            CameraSizeWidth = project.CameraSizeWidth,
            CameraSizeHeight = project.CameraSizeHeight,
            CameraLimits = project.CameraLimits,
            CameraEffects = project.CameraEffects,
            DefaultInputScheme = project.DefaultInputScheme ?? "Keyboard",
            ProtagonistInstanceId = project.ProtagonistInstanceId,
            UseNativeInput = project.UseNativeInput,
            UseNativeCameraFollow = project.UseNativeCameraFollow,
            EditorViewportCenterWorldX = project.EditorViewportCenterWorldX,
            EditorViewportCenterWorldY = project.EditorViewportCenterWorldY,
            KeepEmbeddedPlayRunningWithMapTab = project.KeepEmbeddedPlayRunningWithMapTab,
            NativeCameraSmoothing = project.NativeCameraSmoothing,
            NativeMoveSpeedTilesPerSecond = project.NativeMoveSpeedTilesPerSecond,
            AutoFlipSprite = project.AutoFlipSprite,
            UseNativeAutoAnimation = project.UseNativeAutoAnimation,
            StartupMusicPath = project.StartupMusicPath,
            StartupSoundPath = project.StartupSoundPath,
            AudioManifestPath = string.IsNullOrWhiteSpace(project.AudioManifestPath) ? "audio.json" : project.AudioManifestPath,
            MasterVolume = Clamp01(project.MasterVolume),
            MusicVolume = Clamp01(project.MusicVolume),
            SfxVolume = Clamp01(project.SfxVolume),
            ProjectEnabledPlugins = project.ProjectEnabledPlugins?.Count > 0 ? project.ProjectEnabledPlugins : null,
            DefaultAnimationFps = project.DefaultAnimationFps,
            DefaultCollisionEnabled = project.DefaultCollisionEnabled,
            PhysicsGravity = project.PhysicsGravity,
            PhysicsEnabled = project.PhysicsEnabled,
            RuntimeRandomSeed = project.RuntimeRandomSeed,
            BootstrapScriptId = project.BootstrapScriptId,
            PixelPerfect = project.PixelPerfect,
            RenderAntiAliasMode = project.RenderAntiAliasMode ?? ProjectRenderSettings.AntiAliasNone,
            MsaaSampleCount = project.MsaaSampleCount,
            TextureFilterMode = project.TextureFilterMode ?? ProjectRenderSettings.FilterNearest,
            InitialZoom = project.InitialZoom,
            LightShadowDefault = project.LightShadowDefault,
            DebugMode = project.DebugMode,
            ScriptNodes = project.ScriptNodes,
            AutoSaveIntervalSeconds = project.AutoSaveIntervalSeconds,
            AutoSaveEnabled = project.AutoSaveEnabled,
            AutoSaveIntervalMinutes = project.AutoSaveIntervalMinutes,
            AutoSaveMaxBackupsPerType = project.AutoSaveMaxBackupsPerType,
            AutoSaveFolder = project.AutoSaveFolder ?? "Autoguardados",
            AutoSaveOnClose = project.AutoSaveOnClose,
            AutoSaveOnlyWhenDirty = project.AutoSaveOnlyWhenDirty,
            FearMeterEnabled = project.FearMeterEnabled,
            DangerMeterEnabled = project.DangerMeterEnabled,
            DefaultTilesetPath = project.DefaultTilesetPath,
            LayerNames = project.LayerNames ?? new List<string> { "Suelo" },
            MapPath = project.MapPathRelative ?? "mapa.json",
            MainMapPath = project.MainMapPath ?? "mapa.json",
            MainObjectsPath = project.MainObjectsPath ?? "objetos.json",
            ObjectsPath = "objetos.json",
            Scenes = project.Scenes?.Select(s => new SceneDto
            {
                Id = s.Id,
                Name = s.Name,
                MapPathRelative = s.MapPathRelative,
                ObjectsPathRelative = s.ObjectsPathRelative,
                DefaultTabKinds = s.DefaultTabKinds?.Count > 0 ? s.DefaultTabKinds : null
            }).ToList(),
            CreatedWithFUEngine = true,
            EngineVersion = FUEngine.Core.EngineVersion.Current,
            ProjectFormatVersion = project.ProjectFormatVersion,
            ScriptingLanguage = project.ScriptingLanguage ?? "Lua",
            AdsExportProvider = project.AdsExportProvider,
            Splash = new SplashConfigDto()
        };
        var json = JsonSerializer.Serialize(dto, SerializationDefaults.Options);
        File.WriteAllText(path, json);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            SceneDescriptorSync.WriteAll(project, dir);
    }

    public static ProjectInfo Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("La ruta del proyecto no puede estar vacía.", nameof(path));
        if (!File.Exists(path))
            throw new FileNotFoundException("No se encontró el archivo del proyecto.", path);
        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            var readErr = $"No se pudo leer el archivo del proyecto: {ex.Message}";
            EditorJsonLoadDiagnostics.ReportJsonError?.Invoke(readErr, "Proyecto", path);
            throw new InvalidOperationException(readErr, ex);
        }
        ProjectDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<ProjectDto>(json, SerializationDefaults.Options);
        }
        catch (JsonException ex)
        {
            var msg = $"JSON del proyecto inválido o corrupto (línea {ex.LineNumber}, posición {ex.BytePositionInLine}): {ex.Message}";
            EditorJsonLoadDiagnostics.ReportJsonError?.Invoke(msg, "Proyecto", path);
            throw new InvalidOperationException(msg, ex);
        }
        if (dto == null)
        {
            var msg = "El archivo del proyecto está vacío o mal formado.";
            EditorJsonLoadDiagnostics.ReportJsonError?.Invoke(msg, "Proyecto", path);
            throw new InvalidOperationException(msg);
        }
        var dir = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(dir))
            dir = Path.GetDirectoryName(Path.GetFullPath(path)) ?? "";
        return FromDto(dto, dir ?? "");
    }

    public static ProjectInfo FromDto(ProjectDto dto, string projectDirectory)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));
        if (dto.TileSize <= 0)
            throw new InvalidOperationException($"TileSize inválido: {dto.TileSize}. Debe ser > 0.");
        if (dto.Fps is < 1 or > 1000)
            throw new InvalidOperationException($"FPS inválido: {dto.Fps}. Rango válido: 1-1000.");
        if (dto.ChunkSize <= 0)
            throw new InvalidOperationException($"ChunkSize inválido: {dto.ChunkSize}. Debe ser > 0.");
        var dir = projectDirectory ?? "";
        return new ProjectInfo
        {
            Id = string.IsNullOrWhiteSpace(dto.Id) ? "proj_" + Guid.NewGuid().ToString("N")[..8] : dto.Id,
            Nombre = dto.Nombre ?? "",
            Descripcion = dto.Descripcion ?? "",
            Author = dto.Author,
            Copyright = dto.Copyright,
            Version = dto.Version ?? "0.0.1",
            IconPath = dto.IconPath,
            PaletteId = dto.PaletteId,
            TemplateType = dto.TemplateType,
            TileSize = dto.TileSize,
            MapWidth = dto.MapWidth,
            MapHeight = dto.MapHeight,
            MapBoundsOriginWorldTileX = dto.MapBoundsOriginWorldTileX,
            MapBoundsOriginWorldTileY = dto.MapBoundsOriginWorldTileY,
            Infinite = dto.Infinite,
            ChunkSize = dto.ChunkSize,
            InitialChunksW = dto.InitialChunksW,
            InitialChunksH = dto.InitialChunksH,
            ChunkLoadRadius = dto.ChunkLoadRadius,
            ChunkStreamEvictMargin = dto.ChunkStreamEvictMargin ?? 4,
            ChunkStreamSpillRuntimeEmpty = dto.ChunkStreamSpillRuntimeEmpty ?? true,
            ChunkUnloadFar = dto.ChunkUnloadFar,
            ChunkSaveByChunk = dto.ChunkSaveByChunk,
            ChunkEntitySleep = dto.ChunkEntitySleep,
            ChunkStreaming = dto.ChunkStreaming,
            ShowChunkBounds = dto.ShowChunkBounds,
            TileHeight = dto.TileHeight,
            AutoTiling = dto.AutoTiling,
            Fps = dto.Fps,
            AnimationSpeedMultiplier = dto.AnimationSpeedMultiplier,
            GameResolutionWidth = dto.GameResolutionWidth,
            GameResolutionHeight = dto.GameResolutionHeight,
            AssetsRootFolder = dto.AssetsRootFolder ?? "Assets",
            ProjectGridSnapPx = dto.ProjectGridSnapPx,
            DefaultFirstSceneBackgroundColor = dto.DefaultFirstSceneBackgroundColor ?? "#FFFFFF",
            EditorMapCanvasBackgroundColor = dto.EditorMapCanvasBackgroundColor ?? "#21262d",
            HUDColor = dto.HUDColor,
            HUDStyle = dto.HUDStyle ?? "Minimal",
            GameFontFamily = dto.GameFontFamily,
            GameFontSize = dto.GameFontSize > 0 ? dto.GameFontSize : 16,
            ExportFormatImage = dto.ExportFormatImage ?? "PNG",
            ExportFormatAudio = dto.ExportFormatAudio ?? "OGG",
            NamingRuleObjects = dto.NamingRuleObjects ?? "libre",
            NamingRuleSeeds = dto.NamingRuleSeeds ?? "libre",
            CameraSizeWidth = dto.CameraSizeWidth,
            CameraSizeHeight = dto.CameraSizeHeight,
            CameraLimits = dto.CameraLimits,
            CameraEffects = dto.CameraEffects,
            DefaultInputScheme = dto.DefaultInputScheme ?? "Keyboard",
            ProtagonistInstanceId = dto.ProtagonistInstanceId,
            UseNativeInput = dto.UseNativeInput,
            UseNativeCameraFollow = dto.UseNativeCameraFollow,
            EditorViewportCenterWorldX = dto.EditorViewportCenterWorldX,
            EditorViewportCenterWorldY = dto.EditorViewportCenterWorldY,
            KeepEmbeddedPlayRunningWithMapTab = dto.KeepEmbeddedPlayRunningWithMapTab,
            NativeCameraSmoothing = dto.NativeCameraSmoothing < 0 ? 8f : dto.NativeCameraSmoothing,
            NativeMoveSpeedTilesPerSecond = dto.NativeMoveSpeedTilesPerSecond > 0 ? dto.NativeMoveSpeedTilesPerSecond : 4f,
            AutoFlipSprite = dto.AutoFlipSprite,
            UseNativeAutoAnimation = dto.UseNativeAutoAnimation,
            StartupMusicPath = dto.StartupMusicPath,
            StartupSoundPath = dto.StartupSoundPath,
            AudioManifestPath = string.IsNullOrWhiteSpace(dto.AudioManifestPath) ? "audio.json" : dto.AudioManifestPath,
            MasterVolume = Clamp01(dto.MasterVolume ?? 1f),
            MusicVolume = Clamp01(dto.MusicVolume ?? 0.7f),
            SfxVolume = Clamp01(dto.SfxVolume ?? 1f),
            ProjectEnabledPlugins = dto.ProjectEnabledPlugins ?? new List<string>(),
            DefaultAnimationFps = dto.DefaultAnimationFps,
            DefaultCollisionEnabled = dto.DefaultCollisionEnabled,
            PhysicsGravity = dto.PhysicsGravity,
            PhysicsEnabled = dto.PhysicsEnabled,
            RuntimeRandomSeed = dto.RuntimeRandomSeed,
            BootstrapScriptId = dto.BootstrapScriptId,
            PixelPerfect = dto.PixelPerfect,
            RenderAntiAliasMode = ProjectRenderSettings.NormalizeAntiAliasMode(dto.RenderAntiAliasMode),
            MsaaSampleCount = ProjectRenderSettings.NormalizeMsaaSamples(dto.MsaaSampleCount),
            TextureFilterMode = ProjectRenderSettings.NormalizeTextureFilter(dto.TextureFilterMode),
            InitialZoom = dto.InitialZoom,
            LightShadowDefault = dto.LightShadowDefault,
            DebugMode = dto.DebugMode,
            ScriptNodes = dto.ScriptNodes,
            AutoSaveIntervalSeconds = dto.AutoSaveIntervalSeconds,
            AutoSaveEnabled = dto.AutoSaveEnabled,
            AutoSaveIntervalMinutes = dto.AutoSaveIntervalMinutes,
            AutoSaveMaxBackupsPerType = dto.AutoSaveMaxBackupsPerType,
            AutoSaveFolder = dto.AutoSaveFolder ?? "Autoguardados",
            AutoSaveOnClose = dto.AutoSaveOnClose,
            AutoSaveOnlyWhenDirty = dto.AutoSaveOnlyWhenDirty,
            FearMeterEnabled = dto.FearMeterEnabled,
            DangerMeterEnabled = dto.DangerMeterEnabled,
            DefaultTilesetPath = dto.DefaultTilesetPath,
            LayerNames = dto.LayerNames != null && dto.LayerNames.Count > 0 ? dto.LayerNames : new List<string> { "Suelo" },
            ProjectDirectory = dir,
            MapPathRelative = dto.MapPath ?? "mapa.json",
            MainMapPath = dto.MainMapPath ?? "mapa.json",
            MainObjectsPath = dto.MainObjectsPath ?? dto.MainSceneRelative ?? "objetos.json",
            Scenes = dto.Scenes?.Select(s => new SceneDefinition
            {
                Id = s.Id ?? "",
                Name = s.Name ?? s.Id ?? "",
                MapPathRelative = s.MapPathRelative ?? "mapa.json",
                ObjectsPathRelative = s.ObjectsPathRelative ?? "objetos.json",
                DefaultTabKinds = s.DefaultTabKinds ?? new List<string>()
            }).ToList(),
            EngineVersion = dto.EngineVersion,
            ProjectFormatVersion = dto.ProjectFormatVersion,
            ScriptingLanguage = dto.ScriptingLanguage ?? "Lua",
            AdsExportProvider = dto.AdsExportProvider
        };
    }

    private static float Clamp01(float v) => Math.Clamp(v, 0f, 1f);
}
