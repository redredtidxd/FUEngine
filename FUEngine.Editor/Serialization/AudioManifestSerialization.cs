using System.Text.Json;

namespace FUEngine.Editor;

/// <summary>Carga <c>audio.json</c>: sonidos con rutas relativas al proyecto.</summary>
public static class AudioManifestSerialization
{
    /// <summary>Entrada resuelta a disco (clave = id normalizado).</summary>
    public sealed class SoundEntry
    {
        public string Id { get; init; } = "";
        public string RelativePath { get; init; } = "";
        public string AbsolutePath { get; init; } = "";
        public float Volume { get; init; } = 1f;
        public bool IsLoop { get; init; }
        public float PitchVar { get; init; }
    }

    public static IReadOnlyDictionary<string, SoundEntry> LoadOrEmpty(string manifestAbsolutePath, string projectDirectory)
    {
        var dict = new Dictionary<string, SoundEntry>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(manifestAbsolutePath) || !File.Exists(manifestAbsolutePath))
            return dict;

        string json;
        try
        {
            json = File.ReadAllText(manifestAbsolutePath);
        }
        catch
        {
            return dict;
        }

        AudioManifestDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<AudioManifestDto>(json, SerializationDefaults.Options);
        }
        catch (JsonException)
        {
            return dict;
        }

        var list = dto?.Sounds ?? dto?.Clips;
        if (list == null || list.Count == 0)
            return dict;

        var root = string.IsNullOrWhiteSpace(projectDirectory) ? Path.GetDirectoryName(manifestAbsolutePath) ?? "" : projectDirectory;

        foreach (var item in list)
        {
            var id = item.Id?.Trim();
            if (string.IsNullOrEmpty(id)) continue;
            var rel = item.Path?.Trim().Replace('/', Path.DirectorySeparatorChar);
            if (string.IsNullOrEmpty(rel)) continue;
            var abs = Path.GetFullPath(Path.Combine(root, rel));
            if (!File.Exists(abs))
                continue;

            var vol = item.Volume is > 0f ? item.Volume.Value : 1f;
            dict[id] = new SoundEntry
            {
                Id = id,
                RelativePath = rel,
                AbsolutePath = abs,
                Volume = Math.Clamp(vol, 0f, 2f),
                IsLoop = item.IsLoop == true,
                PitchVar = Math.Max(0f, item.PitchVar ?? 0f)
            };
        }

        return dict;
    }
}
