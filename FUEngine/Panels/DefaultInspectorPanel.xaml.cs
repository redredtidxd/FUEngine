using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FUEngine.Core;
using FUEngine.Editor;

namespace FUEngine;

public partial class DefaultInspectorPanel : System.Windows.Controls.UserControl
{
    public event EventHandler? CreateObjectClicked;
    public event EventHandler? AddTriggerClicked;
    public event EventHandler? OpenMapConfigClicked;
    public event EventHandler? CenterCameraClicked;

    private static readonly System.Windows.Media.Brush PreviewBrushSuelo = new SolidColorBrush(System.Windows.Media.Color.FromRgb(80, 80, 80));
    private static readonly System.Windows.Media.Brush PreviewBrushPared = new SolidColorBrush(System.Windows.Media.Color.FromRgb(120, 80, 60));
    private static readonly System.Windows.Media.Brush PreviewBrushObjeto = new SolidColorBrush(System.Windows.Media.Color.FromRgb(90, 90, 120));
    private static readonly System.Windows.Media.Brush PreviewBrushEspecial = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 60, 100));

    public DefaultInspectorPanel()
    {
        InitializeComponent();
    }

    public void SetData(
        ProjectInfo? project,
        TileMap? tileMap,
        ObjectLayer? objectLayer,
        ScriptRegistry? scriptRegistry,
        string toolName,
        string toolDetail,
        string? layerName,
        int rotationDegrees,
        int tilesCount,
        int objectsCount,
        int triggersCount,
        int scriptsCount,
        int selectedTileType,
        ObjectDefinition? selectedObjectDef)
    {
        if (project == null) return;

        TxtMapName.Text = !string.IsNullOrEmpty(project.MapPathRelative)
            ? System.IO.Path.GetFileNameWithoutExtension(project.MapPathRelative)
            : "Mapa principal";
        int chunkCount = tileMap?.EnumerateChunkCoords().Count() ?? 0;
        int cs = tileMap?.ChunkSize ?? 16;
        TxtMapSize.Text = $"{project.MapWidth} × {project.MapHeight} tiles";
        TxtTileSize.Text = $"Tile size: {project.TileSize} px";
        TxtLayers.Text = $"Capas: {project.LayerNames?.Count ?? 1}";

        TxtProjectName.Text = project.Nombre;
        TxtProjectPath.Text = project.ProjectDirectory ?? "";
        TxtEngineVersion.Text = $"Motor: v{(string.IsNullOrEmpty(project.EngineVersion) ? EngineVersion.Current : project.EngineVersion)}";

        TxtToolName.Text = toolName;
        TxtToolDetail.Text = toolDetail;
        if (!string.IsNullOrEmpty(layerName) || rotationDegrees != 0)
        {
            TxtLayerRotation.Visibility = Visibility.Visible;
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(layerName)) parts.Add($"Capa: {layerName}");
            if (rotationDegrees != 0) parts.Add($"Rotación: {rotationDegrees}°");
            TxtLayerRotation.Text = string.Join("  ·  ", parts);
        }
        else
            TxtLayerRotation.Visibility = Visibility.Collapsed;

        TxtStatsTiles.Text = $"Tiles: {tilesCount}";
        TxtStatsObjects.Text = $"Objetos: {objectsCount}";
        TxtStatsTriggers.Text = $"Triggers: {triggersCount}";
        TxtStatsScripts.Text = $"Scripts: {scriptsCount}";

        if (selectedObjectDef != null)
        {
            PreviewBox.Background = PreviewBrushObjeto;
            TxtPreviewType.Text = selectedObjectDef.Nombre;
            TxtPreviewProps.Text = $"Colisión: {(selectedObjectDef.Colision ? "Sí" : "No")}  ·  {selectedObjectDef.Width}×{selectedObjectDef.Height}";
        }
        else if (!string.IsNullOrEmpty(toolDetail) && (toolDetail.Contains("Catálogo", StringComparison.Ordinal) || toolDetail.Contains("tileset", StringComparison.OrdinalIgnoreCase)))
        {
            PreviewBox.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x30, 0x36, 0x3d));
            TxtPreviewType.Text = "Pincel (atlas)";
            TxtPreviewProps.Text = toolDetail;
        }
        else
        {
            var brush = selectedTileType switch
            {
                1 => PreviewBrushPared,
                2 => PreviewBrushObjeto,
                3 => PreviewBrushEspecial,
                _ => PreviewBrushSuelo
            };
            PreviewBox.Background = brush;
            var typeName = selectedTileType switch { 0 => "Suelo", 1 => "Pared", 2 => "Objeto", 3 => "Especial", _ => "Suelo" };
            TxtPreviewType.Text = typeName;
            TxtPreviewProps.Text = selectedTileType == 0 ? "Colisión: No" : "Colisión: Sí";
        }

        TxtTip.Text = "Para configurar y editar: (1) Seleccione un objeto en la Jerarquía o en el mapa → se muestra en el Inspector con posición, scripts, etc. (2) Seleccione un trigger en Triggers → edite zona y scripts. (3) Use «Config mapa» para tamaño y opciones. (4) Herramienta Seleccionar por defecto: clic+arrastrar = selección de tiles, Del = borrar. Ctrl+Z deshace.";
    }

    private void BtnCreateObject_OnClick(object sender, RoutedEventArgs e) => CreateObjectClicked?.Invoke(this, EventArgs.Empty);
    private void BtnAddTrigger_OnClick(object sender, RoutedEventArgs e) => AddTriggerClicked?.Invoke(this, EventArgs.Empty);
    private void BtnConfigMap_OnClick(object sender, RoutedEventArgs e) => OpenMapConfigClicked?.Invoke(this, EventArgs.Empty);
    private void BtnCenterCamera_OnClick(object sender, RoutedEventArgs e) => CenterCameraClicked?.Invoke(this, EventArgs.Empty);
}
