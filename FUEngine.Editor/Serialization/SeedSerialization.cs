using System.IO;
using System.Text.Json;
using FUEngine.Core;

namespace FUEngine.Editor;

/// <summary>Serialización de seeds (seeds.json). Carga desde "seeds" o "prefabs" en JSON para compatibilidad.
/// Validar: cargar proyectos antiguos, crear seeds nuevos, guardar y volver a cargar; comprobar que Id, Nombre, Objects (DefinitionId, OffsetX/Y, Rotation) se preserven.</summary>
public static class SeedSerialization
{
    public static void Save(List<SeedDefinition> seeds, string path)
    {
        var dto = new SeedsDto
        {
            Seeds = seeds.Select(p => new SeedItemDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                Objects = (p.Objects ?? new List<SeedObjectEntry>()).Select(o => new SeedObjectEntryDto
                {
                    DefinitionId = o.DefinitionId,
                    OffsetX = o.OffsetX,
                    OffsetY = o.OffsetY,
                    Rotation = o.Rotation,
                    Nombre = o.Nombre,
                    SerializedInstanceJson = o.SerializedInstanceJson
                }).ToList(),
                Tags = p.Tags ?? new List<string>()
            }).ToList()
        };
        var json = JsonSerializer.Serialize(dto, SerializationDefaults.Options);
        var parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(parent))
            Directory.CreateDirectory(parent);
        File.WriteAllText(path, json);
    }

    public static List<SeedDefinition> Load(string path)
    {
        if (!File.Exists(path)) return new List<SeedDefinition>();
        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (System.IO.IOException ex)
        {
            throw new InvalidOperationException($"No se pudo leer el archivo de seeds (archivo en uso, sin permisos o no accesible): {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error inesperado al leer el archivo de seeds: {ex.Message}", ex);
        }
        if (string.IsNullOrWhiteSpace(json)) return new List<SeedDefinition>();
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return new List<SeedDefinition>();
        }
        using (doc)
        {
        var root = doc.RootElement;
        JsonElement arrayElement;
        if (root.TryGetProperty("seeds", out var seedsProp))
            arrayElement = seedsProp;
        else if (root.TryGetProperty("prefabs", out var prefabsProp))
            arrayElement = prefabsProp;
        else
            return new List<SeedDefinition>();

        var list = JsonSerializer.Deserialize<List<SeedItemDto>>(arrayElement.GetRawText(), SerializationDefaults.Options);
        if (list == null) return new List<SeedDefinition>();
        return list.Select(p => new SeedDefinition
        {
            Id = p.Id,
            Nombre = p.Nombre,
            Descripcion = p.Descripcion,
            Objects = (p.Objects ?? new List<SeedObjectEntryDto>()).Select(o => new SeedObjectEntry
            {
                DefinitionId = o.DefinitionId,
                OffsetX = o.OffsetX,
                OffsetY = o.OffsetY,
                Rotation = o.Rotation,
                Nombre = o.Nombre,
                SerializedInstanceJson = o.SerializedInstanceJson
            }).ToList(),
            Tags = p.Tags ?? new List<string>()
        }).ToList();
        }
    }
}
