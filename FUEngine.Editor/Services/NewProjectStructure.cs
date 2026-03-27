using System.Collections.Generic;
using System.Linq;
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
            DefaultTabKinds = new List<string> { "Mapa" }
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

    /// <summary>
    /// Chunks por lado del mapa inicial por defecto (4×4 = <strong>16</strong> chunks materializados en el .map).
    /// El ancho/alto en casillas mundo es <c>InitialChunksW × <see cref="ProjectInfo.ChunkSize"/></c> (y lo mismo en H).
    /// </summary>
    public const int DefaultMapChunksPerSide = 4;

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
    /// Orden por defecto de carpetas en la raíz cuando «Generar jerarquía estándar» está activo (coincide con la opción predeterminada del motor).
    /// </summary>
    public static readonly IReadOnlyList<string> DefaultStandardRootFolderNames = new[] { "Sprites", "Maps", "Scripts", "Audio", "Seeds" };

    /// <summary>True si <paramref name="name"/> es un nombre de carpeta válido en Windows (sin * ? : etc.).</summary>
    public static bool IsValidProjectFolderName(string name) =>
        !string.IsNullOrWhiteSpace(name) && name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;

    /// <summary>Comprueba si se puede crear el proyecto en <paramref name="path"/> (ruta absoluta o relativa normalizable).</summary>
    public static bool TryValidateProjectOutputPath(string path, bool createFolderIfMissing, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(path))
        {
            error = "empty";
            return false;
        }
        if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            error = "invalidPathChars";
            return false;
        }
        try
        {
            var full = Path.GetFullPath(path);
            var leaf = Path.GetFileName(full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (string.IsNullOrEmpty(leaf) || !IsValidProjectFolderName(leaf))
            {
                error = "invalidFolderName";
                return false;
            }
            if (Directory.Exists(full))
                return true;
            if (!createFolderIfMissing)
            {
                error = "missing";
                return false;
            }
            var parent = Path.GetDirectoryName(full);
            if (string.IsNullOrEmpty(parent))
            {
                error = "noParent";
                return false;
            }
            return Directory.Exists(parent);
        }
        catch
        {
            error = "exception";
            return false;
        }
    }

    /// <summary>
    /// Crea la estructura completa del proyecto en <paramref name="projectDir"/> y guarda
    /// el proyecto con <paramref name="project"/> (nombre, tile size, etc.).
    /// Opcional: <paramref name="logoSourcePath"/> se copia como logo.png; <paramref name="proyectoConfig"/> se guarda en proyecto.config.
    /// <paramref name="generateStandardRootFolders"/> crea en raíz las carpetas del orden configurado en el motor (Sprites, Maps, …).
    /// <paramref name="standardRootFolderNames"/> sustituye el orden (null = <see cref="DefaultStandardRootFolderNames"/>).
    /// <paramref name="extraRootFolderNames"/> carpetas adicionales en raíz (además del orden; no sustituyen Sprites/Maps salvo lista personalizada completa).
    /// Devuelve la ruta a Project.json para poder cargar el proyecto.
    /// </summary>
    public static string Create(string projectDir, ProjectInfo project, string? logoSourcePath = null, ProyectoConfigDto? proyectoConfig = null, bool generateStandardRootFolders = true, IReadOnlyList<string>? standardRootFolderNames = null, IReadOnlyList<string>? extraRootFolderNames = null)
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
        CreateDir(Path.Combine(dir, "Assets", "Textures"));
        CreateDir(Path.Combine(dir, "Assets", "Sprites"));
        CreateDir(Path.Combine(dir, "Assets", "Sonidos"));
        CreateDir(Path.Combine(dir, "Assets", "Animations"));
        CreateDir(Path.Combine(dir, "Assets", "Logos"));
        CreateDir(Path.Combine(dir, "Assets", "Placeholders"));
        CreateDir(Path.Combine(dir, "Maps"));
        CreateDir(Path.Combine(dir, "Scenes"));
        CreateDir(Path.Combine(dir, "Scripts"));
        CreateDir(Path.Combine(dir, "Seeds"));
        CreateDir(Path.Combine(dir, ProjectIndexPaths.DataFolderName));
        CreateDir(Path.Combine(dir, "Assets", "Audio"));

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

        // Mapa finito: InitialChunksW/H × ChunkSize = casillas mundo (el editor muestra bordes y «+» por chunk).
        int cs = Math.Max(1, project.ChunkSize);
        int chunksW = project.InitialChunksW > 0 ? Math.Clamp(project.InitialChunksW, 1, 64) : DefaultMapChunksPerSide;
        int chunksH = project.InitialChunksH > 0 ? Math.Clamp(project.InitialChunksH, 1, 64) : DefaultMapChunksPerSide;
        int sideTilesW = chunksW * cs;
        int sideTilesH = chunksH * cs;
        foreach (var scene in DefaultScenes)
        {
            var sceneMapPath = Path.Combine(dir, scene.MapPathRelative);
            var sceneObjectsPath = Path.Combine(dir, scene.ObjectsPathRelative);
            CreateDir(Path.GetDirectoryName(sceneMapPath)!);
            CreateDir(Path.GetDirectoryName(sceneObjectsPath)!);
            // Solo Start: el demo del cuadrado que rebota en los bordes del viewport está íntegramente en Scripts/main.lua (script de capa Ground). demo_square no lleva .lua propio. End sin script de capa.
            string? layerScript = string.Equals(scene.Id, "Start", StringComparison.OrdinalIgnoreCase)
                ? "Scripts/main.lua"
                : null;
            MapSerialization.Save(CreateDefaultSceneTileMap(project.ChunkSize, project.LayerNames, sideTilesW, sideTilesH, layerScript), sceneMapPath);
            var sceneObjectLayer = new ObjectLayer();
            sceneObjectLayer.RegisterDefinition(defaultDef);
            ObjectsSerialization.Save(sceneObjectLayer, sceneObjectsPath);
        }

        // Manifiesto de audio vacío bajo Data/ (índices de sistema separados de la raíz)
        var audioDto = new AudioManifestDto { Sounds = new List<AudioManifestSoundDto>() };
        File.WriteAllText(Path.Combine(dir, ProjectIndexPaths.DataFolderName, "audio.json"), JsonSerializer.Serialize(audioDto, SerializationDefaults.Options));

        // Tileset por defecto: textura BMP 64×64 (cuadrícula 16×16) + descriptor .tileset.json
        var bmpRel = "Assets/Tilesets/default_tileset.bmp";
        var bmpAbs = Path.Combine(dir, "Assets", "Tilesets", "default_tileset.bmp");
        WriteCheckerboardBmp64x64(bmpAbs);
        var defaultTileset = new Tileset
        {
            Id = "default",
            Name = "Predeterminado",
            TexturePath = bmpRel.Replace('\\', '/'),
            TileWidth = 16,
            TileHeight = 16
        };
        defaultTileset.SetTile(new Tile { Id = 0 });
        defaultTileset.SetTile(new Tile { Id = 1 });
        defaultTileset.SetTile(new Tile { Id = 2 });
        defaultTileset.SetTile(new Tile { Id = 3 });
        var tilesetJsonAbs = Path.Combine(dir, "Assets", "Tilesets", "default.tileset.json");
        TilesetPersistence.Save(tilesetJsonAbs, defaultTileset);
        project.DefaultTilesetPath = "Assets/Tilesets/default.tileset.json";

        if (string.IsNullOrWhiteSpace(project.DefaultFirstSceneBackgroundColor))
            project.DefaultFirstSceneBackgroundColor = "#FFFFFF";
        else
            project.DefaultFirstSceneBackgroundColor = project.DefaultFirstSceneBackgroundColor.Trim();
        if (string.IsNullOrWhiteSpace(project.EditorMapCanvasBackgroundColor))
            project.EditorMapCanvasBackgroundColor = "#21262d";
        else
            project.EditorMapCanvasBackgroundColor = project.EditorMapCanvasBackgroundColor.Trim();
        project.AudioManifestPath = "Data/audio.json";
        project.MapWidth = sideTilesW;
        project.MapHeight = sideTilesH;
        project.InitialChunksW = chunksW;
        project.InitialChunksH = chunksH;
        // Centro del mapa en casillas mundo (visor azul / cámara; el HUD usa origen 0,0 en el centro del mapa).
        project.EditorViewportCenterWorldX = sideTilesW * 0.5;
        project.EditorViewportCenterWorldY = sideTilesH * 0.5;

        if (project.ProjectFormatVersion <= 0)
            project.ProjectFormatVersion = ProjectSchema.CurrentFormatVersion;

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

        // Único .lua del proyecto nuevo (plantilla Blank): registro scripts.json = solo esta entrada vía EnsureDefaultScriptsJsonIfMissing.
        // Límites = rectángulo de vista lógica (mismo que marco azul / resolución en Play).
        var mainLuaContent = @"-- FUEngine · script de capa (Ground, escena Start)
-- Demo predeterminado: todo el rebote está aquí (no hay otro .lua en el objeto). demo_square = seed; viewport: world:getPlayViewport*.

local left, top, vw, vh = 0, 0, 20, 11
local half = 0.5
local square = nil
local posX, posY = 10, 5.5
local vx, vy = 0, 0

local function bumpTint()
  if square == nil then return end
  square:setSpriteTint(math.random(), math.random(), math.random())
end

function onStart()
  for _ = 1, 4 do _ = math.random() end
  left = world:getPlayViewportLeft()
  top = world:getPlayViewportTop()
  vw = world:getPlayViewportWidthTiles()
  vh = world:getPlayViewportHeightTiles()
  posX = left + vw * 0.5
  posY = top + vh * 0.5
  square = world:instantiate(""demo_square"", posX, posY)
  if square == nil then return end
  square:setSpriteTint(0, 0, 0)
  local angle = math.random() * math.pi * 2
  local speed = 9
  vx = math.cos(angle) * speed
  vy = math.sin(angle) * speed
end

function onLayerUpdate(dt)
  if square == nil then return end
  left = world:getPlayViewportLeft()
  top = world:getPlayViewportTop()
  vw = world:getPlayViewportWidthTiles()
  vh = world:getPlayViewportHeightTiles()
  local right = left + vw
  local bottom = top + vh
  local nx = posX + vx * dt
  local ny = posY + vy * dt
  if nx - half < left then vx = -vx; nx = left + half; bumpTint() end
  if nx + half > right then vx = -vx; nx = right - half; bumpTint() end
  if ny - half < top then vy = -vy; ny = top + half; bumpTint() end
  if ny + half > bottom then vy = -vy; ny = bottom - half; bumpTint() end
  posX = nx
  posY = ny
  world:setPosition(square, posX, posY)
end
";
        var scriptsPath = Path.Combine(dir, "Scripts", "main.lua");
        File.WriteAllText(scriptsPath, mainLuaContent);

        // scripts.json y animaciones.json: se crean al abrir el proyecto (EnsureProjectFolders) si faltan.

        var seedsDir = Path.Combine(dir, "Seeds");
        var demoSquareSeed = new SeedDefinition
        {
            Id = "demo_square",
            Nombre = "Demo Square",
            Descripcion = "Seed por defecto: instancia obj_default (cuadrado lógico, no tile). world:instantiate(\"demo_square\", x, y).",
            Objects = new List<SeedObjectEntry>
            {
                new SeedObjectEntry { DefinitionId = "obj_default", OffsetX = 0, OffsetY = 0, Nombre = "DemoSquare" }
            }
        };
        var seedList = new List<SeedDefinition> { demoSquareSeed };
        SeedSerialization.Save(seedList, Path.Combine(seedsDir, "demo_square.seed"));
        SeedSerialization.Save(seedList, Path.Combine(dir, ProjectIndexPaths.DataFolderName, "seeds.json"));

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

        EnsureDefaultScriptsJsonIfMissing(dir);
        EnsureDefaultAnimacionesIfMissing(dir);

        return projectPath;
    }

    /// <summary>BMP 64×64 RGB, patrón en damero para ver celdas de 16×16 (WPF carga BMP sin dependencias extra).</summary>
    private static void WriteCheckerboardBmp64x64(string absolutePath)
    {
        const int w = 64, h = 64;
        int stride = (w * 3 + 3) & ~3;
        int pixelDataSize = stride * h;
        int fileSize = 14 + 40 + pixelDataSize;
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write((byte)'B');
        bw.Write((byte)'M');
        bw.Write(fileSize);
        bw.Write((ushort)0);
        bw.Write((ushort)0);
        bw.Write(14 + 40);
        bw.Write(40);
        bw.Write(w);
        bw.Write(h);
        bw.Write((ushort)1);
        bw.Write((ushort)24);
        bw.Write(0);
        bw.Write(pixelDataSize);
        bw.Write(0);
        bw.Write(0);
        bw.Write(0);
        bw.Write(0);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                byte b = (byte)(((x / 16) + (y / 16)) % 2 == 0 ? 210 : 130);
                bw.Write(b);
                bw.Write(b);
                bw.Write(b);
            }
            for (int pad = w * 3; pad < stride; pad++)
                bw.Write((byte)0);
        }
        var dir = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllBytes(absolutePath, ms.ToArray());
    }

    /// <summary>
    /// Si falta scripts.json y existe Scripts/main.lua, crea el registro con una sola entrada <c>main</c> (proyecto Blank: no hay más .lua por defecto).
    /// </summary>
    public static void EnsureDefaultScriptsJsonIfMissing(string projectDirectory)
    {
        if (string.IsNullOrWhiteSpace(projectDirectory)) return;
        var path = ProjectIndexPaths.ResolveScriptsJson(projectDirectory);
        if (File.Exists(path)) return;
        var mainLua = Path.Combine(projectDirectory, "Scripts", "main.lua");
        if (!File.Exists(mainLua)) return;
        var parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(parent))
            Directory.CreateDirectory(parent);
        File.WriteAllText(path, "{\"scripts\":[{\"id\":\"main\",\"nombre\":\"Main\",\"path\":\"Scripts/main.lua\"}]}");
    }

    /// <summary>Asegura animaciones.json vacío si no existe.</summary>
    public static void EnsureDefaultAnimacionesIfMissing(string projectDirectory)
    {
        if (string.IsNullOrWhiteSpace(projectDirectory)) return;
        var path = ProjectIndexPaths.ResolveAnimacionesJson(projectDirectory);
        if (File.Exists(path)) return;
        var parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(parent))
            Directory.CreateDirectory(parent);
        File.WriteAllText(path, "{\"animations\":[]}");
    }

    /// <summary>Asegura audio.json vacío si no existe.</summary>
    public static void EnsureDefaultAudioIfMissing(string projectDirectory)
    {
        if (string.IsNullOrWhiteSpace(projectDirectory)) return;
        var path = ProjectIndexPaths.Resolve(projectDirectory, "audio.json");
        if (File.Exists(path)) return;
        var parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(parent))
            Directory.CreateDirectory(parent);
        var audioDto = new AudioManifestDto { Sounds = new List<AudioManifestSoundDto>() };
        File.WriteAllText(path, JsonSerializer.Serialize(audioDto, SerializationDefaults.Options));
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

    /// <summary>Mapa inicial: capas vacías y un chunk vacío por celda que cubre el rectángulo mundo (p. ej. 4×4 = 16 chunks); opcionalmente script de capa en Ground.</summary>
    private static TileMap CreateDefaultSceneTileMap(int chunkSize, IReadOnlyList<string> layerNames, int mapW, int mapH, string? layerScriptRelativePath)
    {
        var map = new TileMap(chunkSize);
        while (map.Layers.Count > 0)
            map.RemoveLayerAt(0);
        int order = 0;
        foreach (var name in layerNames)
        {
            var lt = name.Equals("Objects", StringComparison.OrdinalIgnoreCase) ? LayerType.Objects
                : name.Equals("Foreground", StringComparison.OrdinalIgnoreCase) ? LayerType.Foreground
                : LayerType.Background;
            var desc = new MapLayerDescriptor { Name = name, LayerType = lt, SortOrder = order++ };
            if (!string.IsNullOrEmpty(layerScriptRelativePath) &&
                string.Equals(name, "Ground", StringComparison.OrdinalIgnoreCase))
            {
                desc.LayerScriptEnabled = true;
                desc.LayerScriptId = layerScriptRelativePath;
            }
            map.AddLayer(desc);
        }
        int cs = Math.Max(1, map.ChunkSize);
        if (mapW > 0 && mapH > 0)
        {
            int maxCx = (mapW - 1) / cs;
            int maxCy = (mapH - 1) / cs;
            for (int cy = 0; cy <= maxCy; cy++)
            {
                for (int cx = 0; cx <= maxCx; cx++)
                    map.EnsureEmptyChunksAllLayers(cx, cy);
            }
        }
        return map;
    }

    private static void EnsureStandardRootFolders(string projectDir, IReadOnlyList<string>? names)
    {
        var list = names ?? DefaultStandardRootFolderNames;
        foreach (var raw in list)
        {
            var seg = SanitizeRootFolderSegment(raw);
            if (string.IsNullOrEmpty(seg)) continue;
            CreateDir(Path.Combine(projectDir, seg));
        }
    }

    /// <summary>Orden estándar + carpetas extra (sin duplicar por nombre, insensible a mayúsculas).</summary>
    private static IReadOnlyList<string> MergeStandardAndExtraRootFolders(IReadOnlyList<string>? standardRootFolderNames, IReadOnlyList<string>? extraRootFolderNames)
    {
        var baseList = (standardRootFolderNames ?? DefaultStandardRootFolderNames).ToList();
        foreach (var ex in extraRootFolderNames ?? Array.Empty<string>())
        {
            var s = SanitizeRootFolderSegment(ex);
            if (string.IsNullOrEmpty(s)) continue;
            if (!baseList.Contains(s, StringComparer.OrdinalIgnoreCase))
                baseList.Add(s);
        }
        return baseList;
    }

    /// <summary>Un solo segmento de carpeta bajo la raíz; evita rutas y caracteres inválidos.</summary>
    private static string SanitizeRootFolderSegment(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        var t = raw.Trim().Trim('\\', '/');
        if (t is "." or ".." || t.Contains('\\') || t.Contains('/')) return "";
        if (t.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return "";
        return t;
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
        CreateDir(Path.Combine(dir, "Assets", "Audio"));
        CreateDir(Path.Combine(dir, "Assets", "Animations"));
        CreateDir(Path.Combine(dir, "Assets", "Logos"));
        CreateDir(Path.Combine(dir, "Assets", "Tilesets"));
        CreateDir(Path.Combine(dir, "Assets", "Textures"));
        CreateDir(Path.Combine(dir, "Assets", "Placeholders"));
        CreateDir(Path.Combine(dir, "Maps"));
        CreateDir(Path.Combine(dir, "Scenes"));
        CreateDir(Path.Combine(dir, "Scripts"));
        CreateDir(Path.Combine(dir, "Seeds"));
        CreateDir(Path.Combine(dir, ProjectIndexPaths.DataFolderName));
        EnsureDefaultScriptsJsonIfMissing(dir);
        EnsureDefaultAnimacionesIfMissing(dir);
        EnsureDefaultAudioIfMissing(dir);
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
