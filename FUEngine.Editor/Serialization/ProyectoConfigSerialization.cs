using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FUEngine.Editor;

/// <summary>
/// Lee y escribe proyecto.config (JSON) en la raíz del proyecto.
/// </summary>
public static class ProyectoConfigSerialization
{
    public const string ConfigFileName = "proyecto.config";

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string GetConfigPath(string projectDirectory)
    {
        return Path.Combine(projectDirectory ?? "", ConfigFileName);
    }

    public static bool Exists(string projectDirectory)
    {
        var path = GetConfigPath(projectDirectory);
        return !string.IsNullOrEmpty(projectDirectory) && File.Exists(path);
    }

    public static ProyectoConfigDto? Load(string projectDirectory)
    {
        var path = GetConfigPath(projectDirectory);
        if (string.IsNullOrEmpty(projectDirectory) || !File.Exists(path))
            return null;
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ProyectoConfigDto>(json, Options);
        }
        catch
        {
            return null;
        }
    }

    public static void Save(string projectDirectory, ProyectoConfigDto config)
    {
        var path = GetConfigPath(projectDirectory);
        if (string.IsNullOrEmpty(projectDirectory)) return;
        try
        {
            var json = JsonSerializer.Serialize(config, Options);
            File.WriteAllText(path, json);
        }
        catch
        {
            // ignore
        }
    }
}
