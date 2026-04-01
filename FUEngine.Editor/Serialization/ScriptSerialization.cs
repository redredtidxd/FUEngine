using System.IO;
using System.Text.Json;
using FUEngine.Core;

namespace FUEngine.Editor;

public static class ScriptSerialization
{
    public static void Save(ScriptRegistry registry, string path)
    {
        var parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(parent))
            Directory.CreateDirectory(parent);
        var list = registry.GetAll().Select(s => new ScriptItemDto
        {
            Id = s.Id,
            Nombre = s.Nombre,
            Path = s.Path,
            Eventos = s.Eventos?.ToList() ?? new List<string>()
        }).ToList();
        var dto = new ScriptsDto { Scripts = list };
        var json = JsonSerializer.Serialize(dto, SerializationDefaults.Options);
        File.WriteAllText(path, json);
    }

    public static ScriptRegistry Load(string path)
    {
        if (!File.Exists(path)) return new ScriptRegistry();
        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"No se pudo leer el archivo de scripts: {ex.Message}", ex);
        }
        ScriptsDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<ScriptsDto>(json, SerializationDefaults.Options);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"JSON de scripts inválido (línea {ex.LineNumber}, posición {ex.BytePositionInLine}): {ex.Message}", ex);
        }
        if (dto == null) return new ScriptRegistry();
        var registry = new ScriptRegistry();
        foreach (var s in dto.Scripts ?? new List<ScriptItemDto>())
            registry.Register(new ScriptDefinition { Id = s.Id, Nombre = s.Nombre, Path = s.Path, Eventos = s.Eventos ?? new List<string>() });
        return registry;
    }
}
