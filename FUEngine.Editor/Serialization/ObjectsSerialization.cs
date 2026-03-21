using System.Text.Json;
using FUEngine.Core;

namespace FUEngine.Editor;

public static class ObjectsSerialization
{
    public static void Save(ObjectLayer layer, string path)
    {
        var dto = ToDto(layer);
        var json = JsonSerializer.Serialize(dto, SerializationDefaults.Options);
        File.WriteAllText(path, json);
    }

    /// <summary>Copia la capa de objetos (útil para sandbox del tab Juego).</summary>
    public static ObjectLayer Clone(ObjectLayer layer)
    {
        if (layer == null) return new ObjectLayer();
        return FromDto(ToDto(layer));
    }

    public static ObjectLayer Load(string path)
    {
        if (!File.Exists(path))
            return new ObjectLayer();
        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"No se pudo leer el archivo de objetos: {ex.Message}", ex);
        }
        ObjectsDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<ObjectsDto>(json, SerializationDefaults.Options);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"JSON de objetos inválido (línea {ex.LineNumber}, posición {ex.BytePositionInLine}): {ex.Message}", ex);
        }
        if (dto == null)
            throw new InvalidOperationException("El archivo de objetos está vacío o mal formado.");
        return FromDto(dto);
    }

    public static ObjectsDto ToDto(ObjectLayer layer)
    {
        var definitions = layer.Definitions.Values
            .Select(d => new ObjectDefinitionDto
            {
                Id = d.Id,
                Nombre = d.Nombre,
                SpritePath = d.SpritePath,
                Colision = d.Colision,
                Interactivo = d.Interactivo,
                Destructible = d.Destructible,
                ScriptId = d.ScriptId,
                AnimacionId = d.AnimacionId,
                Width = d.Width,
                Height = d.Height,
                AnimatronicType = d.AnimatronicType,
                MovementPattern = d.MovementPattern,
                Personality = d.Personality,
                CanDetectPlayer = d.CanDetectPlayer,
                Tags = d.Tags ?? new List<string>(),
                EnableInGameDrawing = d.EnableInGameDrawing
            }).ToList();
        var instances = layer.Instances
            .Select(i => new ObjectInstanceDto
            {
                InstanceId = i.InstanceId,
                DefinitionId = i.DefinitionId,
                X = i.X,
                Y = i.Y,
                Rotation = i.Rotation,
                ScaleX = i.ScaleX,
                ScaleY = i.ScaleY,
                LayerOrder = i.LayerOrder,
                Nombre = i.Nombre,
                ColisionOverride = i.ColisionOverride,
                CollisionType = i.CollisionType,
                InteractivoOverride = i.InteractivoOverride,
                DestructibleOverride = i.DestructibleOverride,
                ScriptIdOverride = i.ScriptIdOverride,
                ScriptIds = i.ScriptIds != null ? new List<string>(i.ScriptIds) : new List<string>(),
                ScriptProperties = (i.ScriptProperties ?? new List<ScriptInstancePropertySet>()).Select(sp => new ScriptInstancePropertySetDto
                {
                    ScriptId = sp.ScriptId,
                    Properties = (sp.Properties ?? new List<ScriptPropertyEntry>()).Select(p => new ScriptPropertyEntryDto { Key = p.Key, Type = p.Type ?? "string", Value = p.Value ?? "" }).ToList()
                }).ToList(),
                Tags = i.Tags != null ? new List<string>(i.Tags) : new List<string>(),
                Visible = i.Visible
            }).ToList();
        return new ObjectsDto { Definitions = definitions, Instances = instances };
    }

    public static ObjectLayer FromDto(ObjectsDto dto)
    {
        if (dto == null) return new ObjectLayer();
        var layer = new ObjectLayer();
        foreach (var d in dto.Definitions ?? new List<ObjectDefinitionDto>())
        {
            layer.RegisterDefinition(new ObjectDefinition
            {
                Id = d.Id,
                Nombre = d.Nombre,
                SpritePath = d.SpritePath,
                Colision = d.Colision,
                Interactivo = d.Interactivo,
                Destructible = d.Destructible,
                ScriptId = d.ScriptId,
                AnimacionId = d.AnimacionId,
                Width = d.Width,
                Height = d.Height,
                AnimatronicType = d.AnimatronicType,
                MovementPattern = d.MovementPattern,
                Personality = d.Personality,
                CanDetectPlayer = d.CanDetectPlayer,
                Tags = d.Tags ?? new List<string>(),
                EnableInGameDrawing = d.EnableInGameDrawing
            });
        }
        foreach (var i in dto.Instances ?? new List<ObjectInstanceDto>())
        {
            layer.AddInstance(new ObjectInstance
            {
                InstanceId = i.InstanceId,
                DefinitionId = i.DefinitionId,
                X = i.X,
                Y = i.Y,
                Rotation = i.Rotation,
                ScaleX = i.ScaleX != 0 ? i.ScaleX : 1.0,
                ScaleY = i.ScaleY != 0 ? i.ScaleY : 1.0,
                LayerOrder = i.LayerOrder,
                Nombre = i.Nombre,
                ColisionOverride = i.ColisionOverride,
                CollisionType = i.CollisionType,
                InteractivoOverride = i.InteractivoOverride,
                DestructibleOverride = i.DestructibleOverride,
                ScriptIdOverride = i.ScriptIdOverride,
                ScriptIds = i.ScriptIds != null ? new List<string>(i.ScriptIds) : new List<string>(),
                ScriptProperties = (i.ScriptProperties ?? new List<ScriptInstancePropertySetDto>()).Select(sp => new ScriptInstancePropertySet
                {
                    ScriptId = sp.ScriptId,
                    Properties = (sp.Properties ?? new List<ScriptPropertyEntryDto>()).Select(p => new ScriptPropertyEntry { Key = p.Key, Type = p.Type ?? "string", Value = p.Value ?? "" }).ToList()
                }).ToList(),
                Tags = i.Tags != null ? new List<string>(i.Tags) : new List<string>(),
                Visible = i.Visible
            });
        }
        return layer;
    }
}
