using System.Text.Json;
using FUEngine.Core;

namespace FUEngine.Editor;

public static class AnimationSerialization
{
    public static void Save(IEnumerable<AnimationDefinition> animations, string path)
    {
        var list = animations.Select(a => new AnimationItemDto
        {
            Id = a.Id,
            Nombre = a.Nombre,
            Frames = a.Frames?.ToList() ?? new List<string>(),
            Fps = a.Fps
        }).ToList();
        var dto = new AnimationsDto { Animations = list };
        var json = JsonSerializer.Serialize(dto, SerializationDefaults.Options);
        File.WriteAllText(path, json);
    }

    public static List<AnimationDefinition> Load(string path)
    {
        if (!File.Exists(path)) return new List<AnimationDefinition>();
        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"No se pudo leer el archivo de animaciones: {ex.Message}", ex);
        }
        AnimationsDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<AnimationsDto>(json, SerializationDefaults.Options);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"JSON de animaciones inválido (línea {ex.LineNumber}, posición {ex.BytePositionInLine}): {ex.Message}", ex);
        }
        if (dto == null) return new List<AnimationDefinition>();
        return dto.Animations.Select(a => new AnimationDefinition
        {
            Id = a.Id,
            Nombre = a.Nombre,
            Frames = a.Frames ?? new List<string>(),
            Fps = a.Fps
        }).ToList();
    }
}
