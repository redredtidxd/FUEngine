using System.Windows.Controls;

namespace FUEngine;

public partial class TileInspectorPanel : System.Windows.Controls.UserControl
{
    public TileInspectorPanel()
    {
        InitializeComponent();
    }

    public void SetTile(int? tileId)
    {
        if (tileId == null)
        {
            TxtTileId.Text = "—";
            TxtCollision.Text = "—";
            return;
        }
        TxtTileId.Text = tileId.Value.ToString();
        TxtCollision.Text = "Configurable en editor de Tileset.";
    }
}
