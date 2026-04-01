using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FUEngine.Editor;

namespace FUEngine;

public partial class TemplatePickerWindow : Window
{
    public TemplateItem? SelectedTemplate { get; private set; }
    private List<TemplateItem> _allTemplates = new();

    public TemplatePickerWindow()
    {
        InitializeComponent();
        _allTemplates = TemplateProvider.GetAllTemplates();
        FilterCategory.ItemsSource = TemplateProvider.GetAllCategories();
        FilterCategory.SelectedIndex = 0;
        ApplyFilter();
    }

    /// <summary>Preselecciona una plantilla por id (para acceso rápido desde el HUD).</summary>
    public void SelectTemplateById(int templateId)
    {
        var item = _allTemplates.FirstOrDefault(t => t.Id == templateId);
        if (item != null)
            TemplateList.SelectedItem = item;
    }

    private void FilterCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var cat = FilterCategory.SelectedItem as string;
        if (cat == null || cat == "Todas")
            TemplateList.ItemsSource = _allTemplates;
        else
            TemplateList.ItemsSource = _allTemplates.Where(t => t.Category == cat).ToList();
    }

    private void TemplateList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var item = TemplateList.SelectedItem as TemplateItem;
        BtnUse.IsEnabled = item != null;
        if (item == null)
        {
            PreviewCanvas.Children.Clear();
            TxtScriptsIncluded.Text = "Selecciona una plantilla para ver los scripts incluidos.";
            return;
        }
        var data = TemplateProvider.GetTemplateData(item.Id);
        DrawPreview(data.Map);
        var scriptNames = TemplateProvider.MergeWithCommonModules(data.Scripts).Scripts.Select(s => s.Nombre).ToList();
        TxtScriptsIncluded.Text = "Scripts incluidos (módulos reutilizables fusionados): " + (scriptNames.Count > 0 ? string.Join(", ", scriptNames.Take(8)) + (scriptNames.Count > 8 ? "…" : "") : "ninguno");
    }

    private void DrawPreview(MapDto map)
    {
        PreviewCanvas.Children.Clear();
        if (map.Chunks == null || map.Chunks.Count == 0) return;
        int minCx = map.Chunks.Min(c => c.Cx);
        int maxCx = map.Chunks.Max(c => c.Cx);
        int minCy = map.Chunks.Min(c => c.Cy);
        int maxCy = map.Chunks.Max(c => c.Cy);
        int cols = (maxCx - minCx + 1) * map.ChunkSize;
        int rows = (maxCy - minCy + 1) * map.ChunkSize;
        if (cols <= 0 || rows <= 0) return;
        double cellW = Math.Max(1, Math.Floor(PreviewCanvas.Width / cols));
        double cellH = Math.Max(1, Math.Floor(PreviewCanvas.Height / rows));
        var brushes = new Dictionary<int, System.Windows.Media.Brush>
        {
            [0] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(80, 80, 80)),
            [1] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(120, 80, 60)),
            [2] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(90, 90, 120)),
            [3] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 60, 100))
        };
        foreach (var chunk in map.Chunks)
        {
            int baseX = (chunk.Cx - minCx) * map.ChunkSize;
            int baseY = (chunk.Cy - minCy) * map.ChunkSize;
            foreach (var t in chunk.Tiles ?? new List<TileDto>())
            {
                var rect = new System.Windows.Shapes.Rectangle
                {
                    Width = cellW,
                    Height = cellH,
                    Fill = brushes.GetValueOrDefault(t.TipoTile, brushes[0])
                };
                System.Windows.Controls.Canvas.SetLeft(rect, (baseX + t.X) * cellW);
                System.Windows.Controls.Canvas.SetTop(rect, (baseY + t.Y) * cellH);
                PreviewCanvas.Children.Add(rect);
            }
        }
    }

    private void BtnUse_OnClick(object sender, RoutedEventArgs e)
    {
        SelectedTemplate = TemplateList.SelectedItem as TemplateItem;
        DialogResult = true;
        Close();
    }

    private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

public class TemplateItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
}
