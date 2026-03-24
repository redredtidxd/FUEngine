using System.Text.Json;
using FUEngine.Core;

namespace FUEngine.Editor;

/// <summary>
/// Escribe un archivo <c>.scene</c> por escena en <c>Scenes/</c> (metadatos legibles; la fuente de verdad sigue siendo Project.FUE).
/// </summary>
public static class SceneDescriptorSync
{
    public const string SceneFileExtension = ".scene";
    public const string ScenesFolderName = "Scenes";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void WriteAll(ProjectInfo project, string projectDirectory)
    {
        if (string.IsNullOrWhiteSpace(projectDirectory) || !Directory.Exists(projectDirectory)) return;
        var scenesDir = Path.Combine(projectDirectory, ScenesFolderName);
        Directory.CreateDirectory(scenesDir);
        if (project.Scenes == null || project.Scenes.Count == 0) return;

        foreach (var s in project.Scenes)
        {
            var id = string.IsNullOrWhiteSpace(s.Id) ? "scene" : s.Id.Trim();
            var fileName = SanitizeFileName(id) + SceneFileExtension;
            var path = Path.Combine(scenesDir, fileName);
            var dto = new SceneDescriptorDto
            {
                Id = s.Id,
                Name = s.Name,
                MapPathRelative = s.MapPathRelative,
                ObjectsPathRelative = s.ObjectsPathRelative,
                UIFolderRelative = string.IsNullOrWhiteSpace(s.UIFolderRelative) ? "UI" : s.UIFolderRelative,
                DefaultTabKinds = s.DefaultTabKinds?.Count > 0 ? s.DefaultTabKinds : null
            };
            File.WriteAllText(path, JsonSerializer.Serialize(dto, JsonOptions));
        }
    }

    private static string SanitizeFileName(string id)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = id.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        var name = new string(chars).Trim();
        return string.IsNullOrEmpty(name) ? "scene" : name;
    }

    private sealed class SceneDescriptorDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? MapPathRelative { get; set; }
        public string? ObjectsPathRelative { get; set; }
        public string? UIFolderRelative { get; set; }
        public List<string>? DefaultTabKinds { get; set; }
    }
}
