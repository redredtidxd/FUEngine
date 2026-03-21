using System.Windows;
using System.Windows.Controls;
using FUEngine.Core;
using FUEngine.Editor;

namespace FUEngine;

public partial class MapPropertiesInspectorPanel : System.Windows.Controls.UserControl
{
    public event EventHandler? RequestOpenProjectConfig;

    public MapPropertiesInspectorPanel()
    {
        InitializeComponent();
    }

    public void SetProject(ProjectInfo? project)
    {
        if (project == null) return;
        TxtTileSize.Text = $"{project.TileSize} px";
        TxtMapSize.Text = $"{project.MapWidth} × {project.MapHeight}";
        TxtChunkSize.Text = project.ChunkSize.ToString();
        TxtLayers.Text = project.LayerNames != null && project.LayerNames.Count > 0
            ? string.Join(", ", project.LayerNames)
            : "Suelo";
    }

    private void BtnOpenProjectConfig_OnClick(object sender, RoutedEventArgs e)
    {
        RequestOpenProjectConfig?.Invoke(this, EventArgs.Empty);
    }
}
