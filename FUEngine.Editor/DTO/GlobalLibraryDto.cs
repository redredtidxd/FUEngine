using System.Text.Json.Serialization;

namespace FUEngine.Editor;

/// <summary>Índice de la biblioteca global bajo la carpeta de assets compartidos (Library/library.json).</summary>
public sealed class GlobalLibraryManifestDto
{
    public int Version { get; set; } = 1;
    public List<GlobalLibraryEntryDto> Entries { get; set; } = new();
}

public sealed class GlobalLibraryEntryDto
{
    public string Id { get; set; } = "";
    /// <summary>Ruta relativa a la raíz de assets compartidos (ej. Library/files/abc.png).</summary>
    public string RelativePath { get; set; } = "";
    public string DisplayName { get; set; } = "";
    /// <summary>Tipo confirmado por el usuario: tileset, sprite, ui, audio, image, other.</summary>
    public string Kind { get; set; } = GlobalLibraryKinds.Image;
    public string? SuggestedKind { get; set; }
    public List<string>? Tags { get; set; }

    /// <summary>Solo UI (DataGrid); no se serializa.</summary>
    [JsonIgnore]
    public string TagsDisplay
    {
        get => Tags == null || Tags.Count == 0 ? "" : string.Join(", ", Tags);
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                Tags = new List<string>();
            else
                Tags = value.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        }
    }

    public int? TileWidth { get; set; }
    public int? TileHeight { get; set; }
    public string? ImportedAt { get; set; }
}

/// <summary>Valores persistidos en JSON (camelCase).</summary>
public static class GlobalLibraryKinds
{
    public const string Tileset = "tileset";
    public const string Sprite = "sprite";
    public const string Ui = "ui";
    public const string Audio = "audio";
    public const string Image = "image";
    public const string Other = "other";

    public static IReadOnlyList<string> All { get; } = new[] { Tileset, Sprite, Ui, Audio, Image, Other };
}
