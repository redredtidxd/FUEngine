using System.Text.Json;
using System.Text.Json.Serialization;

namespace FUEngine.Core;

/// <summary>Serialización JSON de <see cref="Tileset"/> (archivos <c>.tileset.json</c> o <c>.fuetileset</c>, mismo formato).</summary>
public static class TilesetPersistence
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static Tileset? Load(string absolutePath)
    {
        if (string.IsNullOrWhiteSpace(absolutePath) || !File.Exists(absolutePath))
            return null;
        try
        {
            var json = File.ReadAllText(absolutePath);
            var dto = JsonSerializer.Deserialize<TilesetFileDto>(json, Options);
            if (dto == null) return null;
            var ts = new Tileset
            {
                Id = dto.Id ?? "",
                Name = dto.Name ?? "",
                TexturePath = (dto.TexturePath ?? "").Replace('\\', '/'),
                TileWidth = dto.TileWidth > 0 ? dto.TileWidth : 16,
                TileHeight = dto.TileHeight > 0 ? dto.TileHeight : 16
            };
            if (dto.Tiles != null)
            {
                foreach (var t in dto.Tiles)
                {
                    ts.SetTile(new Tile
                    {
                        Id = t.Id,
                        Collision = t.Collision,
                        LightBlock = t.LightBlock,
                        Material = t.Material,
                        Friction = t.Friction,
                        AnimationSpeed = t.AnimationSpeed,
                        AnimationId = t.AnimationId,
                        CustomData = t.CustomData,
                        Tags = t.Tags ?? new List<string>()
                    });
                }
            }
            return ts;
        }
        catch
        {
            return null;
        }
    }

    public static void Save(string absolutePath, Tileset tileset)
    {
        var dir = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        var tileList = new List<TileFileEntryDto>();
        foreach (var (id, tile) in tileset.EnumerateTiles())
        {
            tileList.Add(new TileFileEntryDto
            {
                Id = id,
                Collision = tile.Collision,
                LightBlock = tile.LightBlock,
                Material = tile.Material,
                Friction = tile.Friction,
                AnimationSpeed = tile.AnimationSpeed,
                AnimationId = tile.AnimationId,
                CustomData = tile.CustomData,
                Tags = tile.Tags.Count > 0 ? tile.Tags : null
            });
        }
        var dto = new TilesetFileDto
        {
            Id = tileset.Id,
            Name = tileset.Name,
            TexturePath = tileset.TexturePath.Replace('\\', '/'),
            TileWidth = tileset.TileWidth,
            TileHeight = tileset.TileHeight,
            Tiles = tileList
        };
        File.WriteAllText(absolutePath, JsonSerializer.Serialize(dto, Options));
    }

    private sealed class TilesetFileDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? TexturePath { get; set; }
        public int TileWidth { get; set; } = 16;
        public int TileHeight { get; set; } = 16;
        public List<TileFileEntryDto>? Tiles { get; set; }
    }

    private sealed class TileFileEntryDto
    {
        public int Id { get; set; }
        public bool Collision { get; set; }
        public bool LightBlock { get; set; }
        public string? Material { get; set; }
        public float Friction { get; set; } = 0.5f;
        public float AnimationSpeed { get; set; }
        public string? AnimationId { get; set; }
        public string? CustomData { get; set; }
        public List<string>? Tags { get; set; }
    }
}
