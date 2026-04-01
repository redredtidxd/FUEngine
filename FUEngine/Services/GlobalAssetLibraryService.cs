using System.IO;
using System.Windows.Media.Imaging;
using FUEngine.Editor;

namespace FUEngine;

/// <summary>Biblioteca global de assets bajo la carpeta compartida del motor (settings).</summary>
public static class GlobalAssetLibraryService
{
    public static string ResolveSharedAssetsRoot(EngineSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.SharedAssetsPath))
            return Path.GetFullPath(settings.SharedAssetsPath.Trim());
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FUEngine", "SharedAssets");
    }

    public static string GetLibraryFilesDirectory(string sharedRoot) =>
        Path.Combine(sharedRoot, GlobalLibrarySerialization.FilesSubfolder);

    public static string GetIndexPath(string sharedRoot) =>
        GlobalLibrarySerialization.GetIndexPath(sharedRoot);

    public static GlobalLibraryManifestDto LoadManifest(string sharedRoot)
    {
        var path = GetIndexPath(sharedRoot);
        return GlobalLibrarySerialization.LoadOrEmpty(path);
    }

    public static void SaveManifest(string sharedRoot, GlobalLibraryManifestDto manifest)
    {
        GlobalLibrarySerialization.Save(GetIndexPath(sharedRoot), manifest);
    }

    /// <summary>Importa archivos al almacén Library/files y devuelve entradas nuevas (sin guardar el manifiesto).</summary>
    public static List<GlobalLibraryEntryDto> ImportFiles(string sharedRoot, IEnumerable<string> sourcePaths, int defaultTileSize)
    {
        var manifest = LoadManifest(sharedRoot);
        var filesDir = GetLibraryFilesDirectory(sharedRoot);
        Directory.CreateDirectory(filesDir);

        var existingIds = new HashSet<string>(manifest.Entries.Select(e => e.Id), StringComparer.OrdinalIgnoreCase);
        var list = new List<GlobalLibraryEntryDto>();
        foreach (var src in sourcePaths)
        {
            if (string.IsNullOrWhiteSpace(src) || !File.Exists(src)) continue;
            var ext = Path.GetExtension(src);
            var id = MakeUniqueId(existingIds);
            var safeName = SanitizeFileName(Path.GetFileName(src));
            var destName = $"{id}_{safeName}";
            var destAbs = Path.Combine(filesDir, destName);
            File.Copy(src, destAbs, overwrite: false);

            var rel = Path.Combine(GlobalLibrarySerialization.FilesSubfolder.Replace('/', Path.DirectorySeparatorChar), destName)
                .Replace(Path.DirectorySeparatorChar, '/');

            var suggested = SuggestKind(destAbs, ext, defaultTileSize);
            var display = Path.GetFileNameWithoutExtension(src);
            var entry = new GlobalLibraryEntryDto
            {
                Id = id,
                RelativePath = rel,
                DisplayName = display,
                Kind = suggested,
                SuggestedKind = suggested,
                Tags = new List<string>(),
                ImportedAt = DateTime.UtcNow.ToString("o")
            };

            ApplyTileHints(entry, destAbs, ext, defaultTileSize);
            list.Add(entry);
            manifest.Entries.Add(entry);
        }

        SaveManifest(sharedRoot, manifest);
        return list;
    }

    public static void UpdateEntry(string sharedRoot, GlobalLibraryEntryDto entry)
    {
        var manifest = LoadManifest(sharedRoot);
        var idx = manifest.Entries.FindIndex(e => string.Equals(e.Id, entry.Id, StringComparison.OrdinalIgnoreCase));
        if (idx < 0) return;
        manifest.Entries[idx] = entry;
        SaveManifest(sharedRoot, manifest);
    }

    public static void RemoveEntry(string sharedRoot, string entryId, bool deleteFile)
    {
        var manifest = LoadManifest(sharedRoot);
        var e = manifest.Entries.FirstOrDefault(x => string.Equals(x.Id, entryId, StringComparison.OrdinalIgnoreCase));
        if (e == null) return;
        manifest.Entries.Remove(e);
        SaveManifest(sharedRoot, manifest);
        if (deleteFile && !string.IsNullOrWhiteSpace(e.RelativePath))
        {
            var abs = Path.Combine(sharedRoot, e.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            try
            {
                if (File.Exists(abs))
                    File.Delete(abs);
            }
            catch { /* ignore */ }
        }
    }

    public static string? GetAbsolutePath(string sharedRoot, GlobalLibraryEntryDto entry)
    {
        if (string.IsNullOrWhiteSpace(entry.RelativePath)) return null;
        var abs = Path.Combine(sharedRoot, entry.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(abs) ? abs : null;
    }

    /// <summary>Copia el archivo de la biblioteca a la carpeta Assets del proyecto. Devuelve ruta relativa al proyecto o null.</summary>
    public static string? CopyEntryToProject(GlobalLibraryEntryDto entry, string sharedRoot, string projectDirectory, string assetsFolderName, bool registerInAudioManifest, string audioManifestRelative)
    {
        var src = GetAbsolutePath(sharedRoot, entry);
        if (src == null || string.IsNullOrWhiteSpace(projectDirectory)) return null;
        var assetsRoot = Path.Combine(projectDirectory, string.IsNullOrWhiteSpace(assetsFolderName) ? "Assets" : assetsFolderName);
        var sub = SubfolderForKind(entry.Kind);
        var destDir = Path.Combine(assetsRoot, sub);
        Directory.CreateDirectory(destDir);
        var baseName = Path.GetFileName(src);
        var destPath = Path.Combine(destDir, baseName);
        destPath = EnsureUniquePath(destPath);
        File.Copy(src, destPath, overwrite: false);
        var rel = Path.GetRelativePath(projectDirectory, destPath).Replace(Path.DirectorySeparatorChar, '/');

        if (registerInAudioManifest && string.Equals(entry.Kind, GlobalLibraryKinds.Audio, StringComparison.OrdinalIgnoreCase))
            TryAppendAudioManifest(projectDirectory, entry, rel, string.IsNullOrWhiteSpace(audioManifestRelative) ? "audio.json" : audioManifestRelative.Trim());

        return rel;
    }

    private static void TryAppendAudioManifest(string projectDirectory, GlobalLibraryEntryDto entry, string relativeToProject, string audioManifestRelative)
    {
        try
        {
            var audioPath = Path.Combine(projectDirectory, audioManifestRelative.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(audioPath))
            {
                var manifest = new AudioManifestDto { Sounds = new List<AudioManifestSoundDto>() };
                var id = SanitizeAudioId(entry.DisplayName);
                manifest.Sounds!.Add(new AudioManifestSoundDto
                {
                    Id = id,
                    Path = relativeToProject,
                    Volume = 1f
                });
                var json = System.Text.Json.JsonSerializer.Serialize(manifest, SerializationDefaults.Options);
                File.WriteAllText(audioPath, json);
                return;
            }

            var jsonExisting = File.ReadAllText(audioPath);
            var dto = System.Text.Json.JsonSerializer.Deserialize<AudioManifestDto>(jsonExisting, SerializationDefaults.Options) ?? new AudioManifestDto();
            dto.Sounds ??= new List<AudioManifestSoundDto>();
            var newId = SanitizeAudioId(entry.DisplayName);
            if (dto.Sounds.Any(s => string.Equals(s.Id, newId, StringComparison.OrdinalIgnoreCase)))
                newId = newId + "_" + Guid.NewGuid().ToString("N")[..6];
            dto.Sounds.Add(new AudioManifestSoundDto { Id = newId, Path = relativeToProject, Volume = 1f });
            File.WriteAllText(audioPath, System.Text.Json.JsonSerializer.Serialize(dto, SerializationDefaults.Options));
        }
        catch { /* ignore */ }
    }

    private static string SanitizeAudioId(string? name)
    {
        var s = string.IsNullOrWhiteSpace(name) ? "sound" : name.Trim();
        foreach (var c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        s = s.Replace(' ', '_');
        return string.IsNullOrEmpty(s) ? "sound" : s;
    }

    private static string SubfolderForKind(string kind)
    {
        return kind.ToLowerInvariant() switch
        {
            GlobalLibraryKinds.Tileset => "Tilesets",
            GlobalLibraryKinds.Sprite => "Sprites",
            GlobalLibraryKinds.Ui => "UI",
            GlobalLibraryKinds.Audio => "Audio",
            GlobalLibraryKinds.Image => "Images",
            _ => "Misc"
        };
    }

    private static string EnsureUniquePath(string path)
    {
        if (!File.Exists(path)) return path;
        var dir = Path.GetDirectoryName(path) ?? "";
        var name = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);
        for (var i = 1; i < 9999; i++)
        {
            var p = Path.Combine(dir, $"{name}_{i}{ext}");
            if (!File.Exists(p)) return p;
        }
        return path;
    }

    private static string MakeUniqueId(HashSet<string> existing)
    {
        string id;
        do
        {
            id = "lib_" + Guid.NewGuid().ToString("N")[..12];
        } while (!existing.Add(id));
        return id;
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return string.IsNullOrEmpty(name) ? "asset" : name;
    }

    private static string SuggestKind(string absolutePath, string ext, int defaultTileSize)
    {
        ext = ext.ToLowerInvariant();
        if (IsAudioExt(ext))
            return GlobalLibraryKinds.Audio;
        if (!IsImageExt(ext))
            return GlobalLibraryKinds.Other;

        var dims = TryGetImageDimensions(absolutePath);
        if (dims == null)
            return GlobalLibraryKinds.Image;

        var (w, h) = dims.Value;
        var ts = Math.Max(8, defaultTileSize);
        if (ts > 0 && w % ts == 0 && h % ts == 0 && w >= ts && h >= ts)
            return GlobalLibraryKinds.Tileset;
        if (h == ts && w >= ts * 2)
            return GlobalLibraryKinds.Tileset;
        var lower = Path.GetFileNameWithoutExtension(absolutePath).ToLowerInvariant();
        if (lower.Contains("_idle") || lower.Contains("_walk") || lower.Contains("_run"))
            return GlobalLibraryKinds.Sprite;
        if (lower.StartsWith("ui_", StringComparison.OrdinalIgnoreCase) || lower.Contains("button") || lower.Contains("panel"))
            return GlobalLibraryKinds.Ui;
        return GlobalLibraryKinds.Image;
    }

    private static void ApplyTileHints(GlobalLibraryEntryDto entry, string absolutePath, string ext, int defaultTileSize)
    {
        if (!IsImageExt(ext.ToLowerInvariant())) return;
        var dims = TryGetImageDimensions(absolutePath);
        if (dims == null) return;
        var (w, h) = dims.Value;
        var ts = Math.Max(8, defaultTileSize);
        if (string.Equals(entry.Kind, GlobalLibraryKinds.Tileset, StringComparison.OrdinalIgnoreCase))
        {
            entry.TileWidth = ts;
            entry.TileHeight = ts;
        }
    }

    private static bool IsAudioExt(string ext) =>
        ext is ".wav" or ".mp3" or ".ogg" or ".flac" or ".m4a" or ".aac";

    private static bool IsImageExt(string ext) =>
        ext is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".webp";

    private static (int w, int h)? TryGetImageDimensions(string path)
    {
        try
        {
            using var fs = File.OpenRead(path);
            var decoder = BitmapDecoder.Create(fs, BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.OnLoad);
            var frame = decoder.Frames[0];
            return (frame.PixelWidth, frame.PixelHeight);
        }
        catch
        {
            return null;
        }
    }
}
