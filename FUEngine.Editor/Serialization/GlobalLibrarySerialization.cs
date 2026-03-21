using System.Text.Json;

namespace FUEngine.Editor;

public static class GlobalLibrarySerialization
{
    public static string LibrarySubfolder => "Library";
    public static string FilesSubfolder => Path.Combine(LibrarySubfolder, "files");
    public const string IndexFileName = "library.json";

    public static string GetIndexPath(string sharedAssetsRoot) =>
        Path.Combine(sharedAssetsRoot, LibrarySubfolder, IndexFileName);

    public static GlobalLibraryManifestDto LoadOrEmpty(string indexPath)
    {
        if (!File.Exists(indexPath))
            return new GlobalLibraryManifestDto();
        try
        {
            var json = File.ReadAllText(indexPath);
            var dto = JsonSerializer.Deserialize<GlobalLibraryManifestDto>(json, SerializationDefaults.Options);
            return dto ?? new GlobalLibraryManifestDto();
        }
        catch
        {
            return new GlobalLibraryManifestDto();
        }
    }

    public static void Save(string indexPath, GlobalLibraryManifestDto manifest)
    {
        var dir = Path.GetDirectoryName(indexPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(manifest, SerializationDefaults.Options);
        File.WriteAllText(indexPath, json);
    }
}
