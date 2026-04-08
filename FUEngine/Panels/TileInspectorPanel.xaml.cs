using System.IO;
using FUEngine.Core;

namespace FUEngine;

public partial class TileInspectorPanel : System.Windows.Controls.UserControl
{
    public TileInspectorPanel()
    {
        InitializeComponent();
    }

    public void SetTile(ProjectInfo? project, TileMap? map, int activeLayerIndex, int? tileId, string? tilesetPathFromContext)
    {
        if (tileId == null || project == null || map == null || activeLayerIndex < 0 || activeLayerIndex >= map.Layers.Count)
        {
            TxtTileId.Text = "—";
            TxtTilesetPath.Text = "—";
            TxtLayerKind.Text = "—";
            TxtCollisionTileset.Text = "—";
            TxtMaterialTags.Text = "—";
            return;
        }

        var rel = !string.IsNullOrWhiteSpace(tilesetPathFromContext)
            ? tilesetPathFromContext
            : map.Layers[activeLayerIndex].TilesetAssetPath;
        if (string.IsNullOrWhiteSpace(rel))
        {
            TxtTileId.Text = tileId.Value.ToString();
            TxtTilesetPath.Text = "La capa no tiene tileset asignado.";
            TxtLayerKind.Text = "—";
            TxtCollisionTileset.Text = "—";
            TxtMaterialTags.Text = "—";
            return;
        }

        var dir = project.ProjectDirectory ?? "";
        var abs = Path.Combine(dir, rel.Trim().Replace('/', Path.DirectorySeparatorChar));
        var ts = TilesetPersistence.Load(abs);
        var layer = map.Layers[activeLayerIndex];
        var def = ts?.GetTile(tileId.Value);

        TxtTileId.Text = tileId.Value.ToString();
        TxtTilesetPath.Text = rel;
        TxtLayerKind.Text = LayerKindDisplay(layer.LayerType);
        TxtCollisionTileset.Text = def == null
            ? "(sin entrada en JSON; hereda según tipo de capa)"
            : (def.Collision ? "Sí (tileset)" : "No (tileset)");
        var mat = string.IsNullOrWhiteSpace(def?.Material) ? "—" : def!.Material!;
        var tags = def?.Tags == null || def.Tags.Count == 0 ? "—" : string.Join(", ", def.Tags);
        TxtMaterialTags.Text = $"Material: {mat}  ·  Tags: {tags}";
    }

    private static string LayerKindDisplay(LayerType t) => t switch
    {
        LayerType.Background => "Suelo / fondo (Background)",
        LayerType.Solid => "Pared / sólido (Solid)",
        LayerType.Objects => "Objetos / decoración (Objects)",
        LayerType.Foreground => "Primer plano (Foreground)",
        _ => t.ToString()
    };
}
