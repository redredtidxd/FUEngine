using System.Text.Json;
using FUEngine.Core;

namespace FUEngine.Editor;

/// <summary>
/// Crea la estructura inicial de un proyecto nuevo: Assets, Maps, Scripts, Seeds,
/// Project.json, Settings.json, map_001.json, main.json, etc.
/// </summary>
public static class NewProjectStructure
{
    /// <summary>Extensión de archivo de mapa (contenido JSON con extensión propia).</summary>
    public const string MapFileExtension = ".map";
    /// <summary>Extensión de archivo de instancias de objetos por escena (contenido JSON con extensión propia).</summary>
    public const string ObjectsFileExtension = ".objects";
    /// <summary>Nombre del archivo de mapa dentro de la subcarpeta de escena.</summary>
    public const string MapFileName = "map.map";
    /// <summary>Nombre del archivo de objetos dentro de la subcarpeta de escena.</summary>
    public const string ObjectsFileName = "objects.objects";

    public const string DefaultMapRelative = "Maps/map_001/map.map";
    /// <summary>Escenas por defecto en proyecto nuevo: subcarpeta por escena (Maps/Id/map.map, Objects/Id/objects.objects).</summary>
    public static readonly IReadOnlyList<SceneDefinition> DefaultScenes = new[]
    {
        new SceneDefinition
        {
            Id = "Start",
            Name = "Start",
            MapPathRelative = "Maps/Start/map.map",
            ObjectsPathRelative = "Objects/Start/objects.objects",
            DefaultTabKinds = new List<string> { "Scripts" }
        },
        new SceneDefinition
        {
            Id = "End",
            Name = "End",
            MapPathRelative = "Maps/End/map.map",
            ObjectsPathRelative = "Objects/End/objects.objects",
            DefaultTabKinds = new List<string> { "Explorador" }
        }
    };
    /// <summary>Extensión de archivo de proyecto del motor (.FUE = FUEngine project).</summary>
    public const string ProjectFileExtension = ".FUE";
    /// <summary>Nombre del archivo de proyecto por defecto (nuevos proyectos).</summary>
    public const string ProjectFileName = "Project.FUE";
    public const string SettingsFileName = "Settings.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Capas por defecto para la jerarquía del mapa.
    /// </summary>
    public static readonly IReadOnlyList<string> DefaultLayerNames = new[]
    {
        "Background",
        "Ground",
        "Objects",
        "Foreground"
    };

    /// <summary>
    /// Crea la estructura completa del proyecto en <paramref name="projectDir"/> y guarda
    /// el proyecto con <paramref name="project"/> (nombre, tile size, etc.).
    /// Opcional: <paramref name="logoSourcePath"/> se copia como logo.png; <paramref name="proyectoConfig"/> se guarda en proyecto.config.
    /// Devuelve la ruta a Project.json para poder cargar el proyecto.
    /// </summary>
    public static string Create(string projectDir, ProjectInfo project, string? logoSourcePath = null, ProyectoConfigDto? proyectoConfig = null)
    {
        if (string.IsNullOrWhiteSpace(projectDir))
            throw new ArgumentException("Project directory is required.", nameof(projectDir));

        var dir = projectDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // Carpetas estándar (Mapa, Objetos, Escenas, Autoguardados con subcarpetas, Assets)
        CreateDir(Path.Combine(dir, "Mapa"));
        CreateDir(Path.Combine(dir, "Objetos"));
        CreateDir(Path.Combine(dir, "Escenas"));
        CreateDir(Path.Combine(dir, "Autoguardados"));
        CreateDir(Path.Combine(dir, "Autoguardados", "Mapa"));
        CreateDir(Path.Combine(dir, "Autoguardados", "Objetos"));
        CreateDir(Path.Combine(dir, "Autoguardados", "Escenas"));
        CreateDir(Path.Combine(dir, "Assets", "Tilesets"));
        CreateDir(Path.Combine(dir, "Assets", "Sprites"));
        CreateDir(Path.Combine(dir, "Assets", "Sonidos"));
        CreateDir(Path.Combine(dir, "Assets", "Animations"));
        CreateDir(Path.Combine(dir, "Assets", "Logos"));
        CreateDir(Path.Combine(dir, "Assets", "Placeholders"));
        CreateDir(Path.Combine(dir, "Maps"));
        CreateDir(Path.Combine(dir, "Scripts"));
        CreateDir(Path.Combine(dir, "Seeds"));

        // Escenas por defecto: Start y End (cada una con su mapa y objetos)
        project.Scenes = DefaultScenes.ToList();
        project.MapPathRelative = DefaultScenes[0].MapPathRelative;
        project.LayerNames = DefaultLayerNames.ToList();
        CreateDir(Path.Combine(dir, "Objects"));

        var defaultDef = new ObjectDefinition
        {
            Id = "obj_default",
            Nombre = "Objeto",
            Colision = true,
            Interactivo = false,
            Destructible = false,
            Width = 1,
            Height = 1
        };

        foreach (var scene in DefaultScenes)
        {
            var sceneMapPath = Path.Combine(dir, scene.MapPathRelative);
            var sceneObjectsPath = Path.Combine(dir, scene.ObjectsPathRelative);
            CreateDir(Path.GetDirectoryName(sceneMapPath)!);
            CreateDir(Path.GetDirectoryName(sceneObjectsPath)!);
            MapSerialization.Save(new TileMap(project.ChunkSize), sceneMapPath);
            var sceneObjectLayer = new ObjectLayer();
            sceneObjectLayer.RegisterDefinition(defaultDef);
            ObjectsSerialization.Save(sceneObjectLayer, sceneObjectsPath);
        }

        var projectPath = Path.Combine(dir, ProjectFileName);
        ProjectSerialization.Save(project, projectPath);

        // Settings.json (grid, layers, paleta por defecto)
        var settings = new
        {
            gridVisible = true,
            defaultTileSize = project.TileSize,
            layerNames = project.LayerNames,
            defaultPalette = project.PaletteId ?? "default"
        };
        File.WriteAllText(Path.Combine(dir, SettingsFileName), JsonSerializer.Serialize(settings, JsonOptions));

        // Scripts/main.lua (script por defecto: cuadrado que rebota en los bordes, compatible con world:instantiate y world:setPosition)
        var mainLuaContent = @"-- FUEngine Default Startup Script
-- Se ejecuta al iniciar el proyecto. Cuadrado pixel art que rebota en los bordes.

local square = nil
local posX = 40
local posY = 40
local speedX = 40
local speedY = 25
local mapWidth = 320
local mapHeight = 180
local size = 8

function onStart()
    square = world:instantiate(""demo_square"", posX, posY)
end

function onUpdate(dt)
    if square == nil then return end
    posX = posX + speedX * dt
    posY = posY + speedY * dt
    if posX < 0 then posX = 0; speedX = -speedX end
    if posX + size > mapWidth then posX = mapWidth - size; speedX = -speedX end
    if posY < 0 then posY = 0; speedY = -speedY end
    if posY + size > mapHeight then posY = mapHeight - size; speedY = -speedY end
    world:setPosition(square, posX, posY)
end
";
        var scriptsPath = Path.Combine(dir, "Scripts", "main.lua");
        File.WriteAllText(scriptsPath, mainLuaContent);

        // scripts.json en raíz (registro del editor; los archivos son .lua)
        File.WriteAllText(Path.Combine(dir, "scripts.json"), "{\"scripts\":[{\"id\":\"main\",\"nombre\":\"Main\",\"path\":\"Scripts/main.lua\"}]}");

        // animaciones.json
        File.WriteAllText(Path.Combine(dir, "animaciones.json"), "{\"animations\":[]}");

        var seedsDir = Path.Combine(dir, "Seeds");
        var demoSquareSeed = new SeedDefinition
        {
            Id = "demo_square",
            Nombre = "Demo Square",
            Descripcion = "Cuadrado pixel art por defecto para el script main.lua (rebote en bordes).",
            Objects = new List<SeedObjectEntry>()
        };
        SeedSerialization.Save(new List<SeedDefinition> { demoSquareSeed }, Path.Combine(seedsDir, "demo_square.seed"));

        // Logo: copiar a Assets/Logos/logo.png (ruta relativa en config) para no saturar la raíz
        var logosDir = Path.Combine(dir, "Assets", "Logos");
        var logoDest = Path.Combine(logosDir, "logo.png");
        if (!string.IsNullOrWhiteSpace(logoSourcePath) && File.Exists(logoSourcePath))
        {
            try
            {
                if (!Directory.Exists(logosDir)) Directory.CreateDirectory(logosDir);
                File.Copy(logoSourcePath, logoDest, overwrite: true);
            }
            catch { /* ignore */ }
        }

        // proyecto.config (nombre, logo como ruta relativa, fechas, plantilla, autoguardado, historial)
        if (proyectoConfig != null)
        {
            proyectoConfig.UltimaRuta = dir;
            proyectoConfig.CreadoEn ??= DateTime.UtcNow.ToString("O");
            proyectoConfig.UltimaModificacion = proyectoConfig.CreadoEn;
            if (File.Exists(logoDest))
                proyectoConfig.Logo = "Assets/Logos/logo.png";
            else
                proyectoConfig.Logo = "logo.png";
            proyectoConfig.HistorialCambios ??= new List<EntradaHistorialDto>();
            proyectoConfig.HistorialCambios.Insert(0, new EntradaHistorialDto
            {
                Version = proyectoConfig.Version ?? "0.1",
                Fecha = proyectoConfig.CreadoEn,
                Descripcion = "Proyecto creado"
            });
            ProyectoConfigSerialization.Save(dir, proyectoConfig);
        }

        ApplyTemplateContents(dir, proyectoConfig?.Plantilla ?? "Blank");

        return projectPath;
    }

    /// <summary>
    /// Añade archivos de ejemplo o readmes según la plantilla (TileBased, RPG, etc.).
    /// </summary>
    private static void ApplyTemplateContents(string projectDir, string template)
    {
        if (string.IsNullOrWhiteSpace(projectDir)) return;
        var t = template.Trim();
        if (string.IsNullOrEmpty(t) || string.Equals(t, "Blank", StringComparison.OrdinalIgnoreCase)) return;
        try
        {
            if (string.Equals(t, "TileBased", StringComparison.OrdinalIgnoreCase))
            {
                var tilesetsReadme = Path.Combine(projectDir, "Assets", "Tilesets", "README.txt");
                File.WriteAllText(tilesetsReadme, "Plantilla TileBased: coloca aquí tus tilesets (imágenes de tiles).\nEl mapa usará estos recursos para pintar por capas.");
                var spritesReadme = Path.Combine(projectDir, "Assets", "Sprites", "README.txt");
                File.WriteAllText(spritesReadme, "Sprites para objetos y personajes. Referencia desde objetos.json.");
            }
            else if (string.Equals(t, "RPG", StringComparison.OrdinalIgnoreCase))
            {
                var spritesReadme = Path.Combine(projectDir, "Assets", "Sprites", "README.txt");
                File.WriteAllText(spritesReadme, "Plantilla RPG: sprites de personajes, NPCs y enemigos.\nOrganiza por carpetas: Personaje, Enemigos, Objetos.");
                var animReadme = Path.Combine(projectDir, "Assets", "Animations", "README.txt");
                File.WriteAllText(animReadme, "Animaciones (idle, walk, attack, etc.) para usar en objetos y personajes.");
            }
            else if (!string.Equals(t, "General", StringComparison.OrdinalIgnoreCase))
            {
                var readme = Path.Combine(projectDir, "Assets", "README.txt");
                File.WriteAllText(readme, $"Plantilla: {t}. Organiza aquí tus assets (Sprites, Sonidos, Animations).");
            }
        }
        catch { /* ignore */ }
    }

    private static void CreateDir(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    private static void WritePlaceholder(string filePath, string comment)
    {
        File.WriteAllText(filePath, string.Empty);
    }

    /// <summary>
    /// Resuelve la ruta del mapa: si existe el archivo .map o legacy .json, devuelve esa ruta; si no, la ruta preferida (.map).
    /// </summary>
    public static string ResolveMapPath(string projectDirectory, string mapPathRelative)
    {
        if (string.IsNullOrWhiteSpace(projectDirectory) || string.IsNullOrWhiteSpace(mapPathRelative))
            return Path.Combine(projectDirectory ?? "", mapPathRelative ?? "");
        var primary = Path.Combine(projectDirectory, mapPathRelative);
        if (File.Exists(primary)) return primary;
        var dir = Path.GetDirectoryName(primary);
        var name = Path.GetFileNameWithoutExtension(primary);
        if (!string.IsNullOrEmpty(dir))
        {
            var legacySameFolder = Path.Combine(dir, name + ".json");
            if (File.Exists(legacySameFolder)) return legacySameFolder;
        }
        var sceneId = Path.GetFileName(dir);
        if (!string.IsNullOrEmpty(sceneId))
        {
            var flatLegacy = Path.Combine(projectDirectory, "Maps", sceneId + ".json");
            if (File.Exists(flatLegacy)) return flatLegacy;
        }
        return primary;
    }

    /// <summary>
    /// Resuelve la ruta de objetos: si existe el archivo .objects o legacy .json, devuelve esa ruta; si no, la ruta preferida (.objects).
    /// </summary>
    public static string ResolveObjectsPath(string projectDirectory, string objectsPathRelative)
    {
        if (string.IsNullOrWhiteSpace(projectDirectory) || string.IsNullOrWhiteSpace(objectsPathRelative))
            return Path.Combine(projectDirectory ?? "", objectsPathRelative ?? "");
        var primary = Path.Combine(projectDirectory, objectsPathRelative);
        if (File.Exists(primary)) return primary;
        var dir = Path.GetDirectoryName(primary);
        var name = Path.GetFileNameWithoutExtension(primary);
        if (!string.IsNullOrEmpty(dir))
        {
            var legacySameFolder = Path.Combine(dir, name + ".json");
            if (File.Exists(legacySameFolder)) return legacySameFolder;
        }
        var sceneId = Path.GetFileName(dir);
        if (!string.IsNullOrEmpty(sceneId))
        {
            var flatLegacy = Path.Combine(projectDirectory, "Objects", sceneId + ".json");
            if (File.Exists(flatLegacy)) return flatLegacy;
        }
        return primary;
    }

    /// <summary>
    /// Indica si la carpeta del proyecto usa la estructura nueva (Project.json + Maps/).
    /// </summary>
    public static bool UsesNewStructure(string projectDirectory)
    {
        if (string.IsNullOrWhiteSpace(projectDirectory)) return false;
        return File.Exists(Path.Combine(projectDirectory, ProjectFileName));
    }

    /// <summary>
    /// Crea las carpetas estándar del proyecto si no existen (Mapa, Objetos, Escenas, Autoguardados, Assets/Sprites, etc.).
    /// Llamar al abrir un proyecto para sincronizar la estructura.
    /// </summary>
    public static void EnsureProjectFolders(string projectDirectory)
    {
        if (string.IsNullOrWhiteSpace(projectDirectory)) return;
        var dir = projectDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        CreateDir(Path.Combine(dir, "Mapa"));
        CreateDir(Path.Combine(dir, "Objetos"));
        CreateDir(Path.Combine(dir, "Escenas"));
        CreateDir(Path.Combine(dir, "Autoguardados"));
        CreateDir(Path.Combine(dir, "Autoguardados", "Mapa"));
        CreateDir(Path.Combine(dir, "Autoguardados", "Objetos"));
        CreateDir(Path.Combine(dir, "Autoguardados", "Escenas"));
        CreateDir(Path.Combine(dir, "Assets", "Sprites"));
        CreateDir(Path.Combine(dir, "Assets", "Sonidos"));
        CreateDir(Path.Combine(dir, "Assets", "Animations"));
        CreateDir(Path.Combine(dir, "Assets", "Logos"));
        CreateDir(Path.Combine(dir, "Assets", "Tilesets"));
        CreateDir(Path.Combine(dir, "Assets", "Placeholders"));
        CreateDir(Path.Combine(dir, "Maps"));
        CreateDir(Path.Combine(dir, "Scripts"));
        CreateDir(Path.Combine(dir, "Seeds"));
    }

    /// <summary>Nombres reservados de Windows (carpetas/archivos no permitidos).</summary>
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    /// <summary>
    /// Convierte el nombre del proyecto en un nombre de carpeta válido (sin caracteres inválidos ni nombres reservados de Windows).
    /// </summary>
    public static string SanitizeFolderName(string projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName)) return "NuevoProyecto";
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(projectName.Trim().Where(c => !invalid.Contains(c)).ToArray());
        if (string.IsNullOrWhiteSpace(sanitized)) return "NuevoProyecto";
        if (ReservedNames.Contains(sanitized))
            return sanitized + "_";
        return sanitized;
    }

    /// <summary>
    /// Devuelve una ruta de proyecto única bajo <paramref name="root"/>: si la carpeta ya existe, añade (1), (2), etc.
    /// </summary>
    public static string GetUniqueProjectPath(string root, string baseFolderName)
    {
        if (string.IsNullOrWhiteSpace(root)) return baseFolderName;
        var baseName = string.IsNullOrWhiteSpace(baseFolderName) ? "NuevoProyecto" : baseFolderName;
        var path = Path.Combine(root, baseName);
        if (!Directory.Exists(path)) return path;
        for (var i = 1; i <= 1000; i++)
        {
            var candidate = Path.Combine(root, $"{baseName} ({i})");
            if (!Directory.Exists(candidate)) return candidate;
        }
        return Path.Combine(root, $"{baseName} ({DateTime.Now:yyyyMMddHHmmss})");
    }
}
