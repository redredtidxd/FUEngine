using System.Text.Json;

namespace FUEngine.Editor;

public static class TriggerZoneSerialization
{
    public static void Save(List<FUEngine.Core.TriggerZone> zones, string path)
    {
        var dto = new TriggerZonesDto
        {
            Zones = zones.Select(z => new TriggerZoneItemDto
            {
                Id = z.Id,
                Nombre = z.Nombre,
                Descripcion = z.Descripcion,
                TriggerType = z.TriggerType ?? "OnEnter",
                LayerId = z.LayerId,
                X = z.X,
                Y = z.Y,
                Width = z.Width,
                Height = z.Height,
                ScriptIdOnEnter = z.ScriptIdOnEnter,
                ScriptIdOnExit = z.ScriptIdOnExit,
                ScriptIdOnTick = z.ScriptIdOnTick,
                Tags = z.Tags ?? new List<string>()
            }).ToList()
        };
        var json = JsonSerializer.Serialize(dto, SerializationDefaults.Options);
        File.WriteAllText(path, json);
    }

    public static List<FUEngine.Core.TriggerZone> Load(string path)
    {
        if (!File.Exists(path)) return new List<FUEngine.Core.TriggerZone>();
        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"No se pudo leer el archivo de zonas trigger: {ex.Message}", ex);
        }
        TriggerZonesDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<TriggerZonesDto>(json, SerializationDefaults.Options);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"JSON de zonas trigger inválido (línea {ex.LineNumber}, posición {ex.BytePositionInLine}): {ex.Message}", ex);
        }
        if (dto?.Zones == null) return new List<FUEngine.Core.TriggerZone>();
        return dto.Zones.Select(z => new FUEngine.Core.TriggerZone
        {
            Id = z.Id,
            Nombre = z.Nombre,
            Descripcion = z.Descripcion,
            TriggerType = z.TriggerType ?? "OnEnter",
            LayerId = z.LayerId,
            X = z.X,
            Y = z.Y,
            Width = z.Width,
            Height = z.Height,
            ScriptIdOnEnter = z.ScriptIdOnEnter,
            ScriptIdOnExit = z.ScriptIdOnExit,
            ScriptIdOnTick = z.ScriptIdOnTick,
            Tags = z.Tags ?? new List<string>()
        }).ToList();
    }
}
