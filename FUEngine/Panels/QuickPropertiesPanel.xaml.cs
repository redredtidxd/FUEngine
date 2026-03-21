using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FUEngine.Core;
using FUEngine.Editor;

namespace FUEngine;

public partial class QuickPropertiesPanel : System.Windows.Controls.UserControl
{
    private static readonly System.Windows.Media.Brush LabelBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3));
    private static readonly System.Windows.Media.Brush ValueBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x8b, 0x94, 0x9e));
    private ProjectExplorerItem? _currentItem;

    public event EventHandler<ProjectExplorerItem>? RequestOpenInEditor;
    public event EventHandler<ProjectExplorerItem>? RequestDuplicate;
    public event EventHandler<ProjectExplorerItem>? RequestRename;
    public event EventHandler<ProjectExplorerItem>? RequestShowInFolder;
    public event EventHandler<ProjectExplorerItem>? RequestDelete;

    public QuickPropertiesPanel()
    {
        InitializeComponent();
    }

    public void SetItem(ProjectExplorerItem? item, ProjectInfo? project, TileMap? tileMap, ObjectLayer? objectLayer, ScriptRegistry? scriptRegistry)
    {
        _currentItem = item;
        if (ImgPreview != null) { ImgPreview.Source = null; }
        if (PreviewSection != null) PreviewSection.Visibility = Visibility.Collapsed;
        if (item == null)
        {
            TxtTitle.Text = "—";
            TxtPath.Text = "";
            TxtType.Text = "";
            TxtHint.Visibility = Visibility.Collapsed;
            SummaryPanel.Visibility = Visibility.Collapsed;
            return;
        }
        TxtTitle.Text = item.Name;
        TxtPath.Text = item.FullPath ?? "";
        TxtType.Text = item.IsFolder ? "Carpeta" : item.FileType.ToString();
        SummaryStack.Children.Clear();
        SummaryPanel.Visibility = Visibility.Collapsed;
        TxtHint.Visibility = Visibility.Collapsed;

        var fullPathForPreview = item.FullPath ?? "";
        var isImage = item.FileType == ProjectFileType.Sprite || item.FileType == ProjectFileType.TileSet ||
                      (fullPathForPreview.Length > 0 && new[] { ".png", ".jpg", ".jpeg" }.Contains(Path.GetExtension(fullPathForPreview).ToLowerInvariant()));
        if (isImage && !item.IsFolder && File.Exists(fullPathForPreview) && ImgPreview != null && PreviewSection != null)
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(fullPathForPreview, UriKind.Absolute);
                bmp.DecodePixelWidth = 96;
                bmp.EndInit();
                bmp.Freeze();
                ImgPreview.Source = bmp;
                PreviewSection.Visibility = Visibility.Visible;
            }
            catch { }
        }

        if (item.IsFolder)
        {
            AddSummaryLine("Elementos", item.Children.Count.ToString());
            SummaryPanel.Visibility = Visibility.Visible;
            return;
        }

        var fullPath = item.FullPath ?? "";
        var fileName = Path.GetFileName(fullPath);
        var ext = Path.GetExtension(fullPath).ToLowerInvariant();
        bool isEditable = ext == ".json" || ext == ".cs" || ext == ".txt" || ext == ".js" || item.FileType == ProjectFileType.Scripts || item.FileType == ProjectFileType.Project || item.FileType == ProjectFileType.Map || item.FileType == ProjectFileType.Objects || item.FileType == ProjectFileType.Animations;
        if (isEditable)
        {
            TxtHint.Text = "Doble clic en la jerarquía para abrir en el editor de código.";
            TxtHint.Visibility = Visibility.Visible;
        }

        if ((string.Equals(fileName, "proyecto.json", StringComparison.OrdinalIgnoreCase) || string.Equals(fileName, FUEngine.Editor.NewProjectStructure.ProjectFileName, StringComparison.OrdinalIgnoreCase)) && project != null)
        {
            AddSummaryLine("Nombre", project.Nombre);
            AddSummaryLine("Descripción", project.Descripcion ?? "");
            AddSummaryLine("Autor", project.Author ?? "—");
            AddSummaryLine("Versión", project.Version ?? "0.0.1");
            AddSummaryLine("Tile size (px)", project.TileSize.ToString());
            AddSummaryLine("Mapa ancho", project.MapWidth.ToString());
            AddSummaryLine("Mapa alto", project.MapHeight.ToString());
            AddSummaryLine("Infinito", project.Infinite ? "Sí" : "No");
            AddSummaryLine("Chunk size", project.ChunkSize.ToString());
            AddSummaryLine("Chunks iniciales W×H", $"{project.InitialChunksW}×{project.InitialChunksH}");
            AddSummaryLine("FPS", project.Fps.ToString());
            AddSummaryLine("Capas", project.LayerNames != null ? string.Join(", ", project.LayerNames) : "Suelo");
            if (!string.IsNullOrEmpty(project.EngineVersion))
                AddSummaryLine("Motor (guardado)", project.EngineVersion);
            SummaryPanel.Visibility = Visibility.Visible;
        }
        else if (string.Equals(fileName, "mapa.json", StringComparison.OrdinalIgnoreCase) && tileMap != null)
        {
            var chunks = tileMap.EnumerateChunkCoords().ToList();
            AddSummaryLine("Chunks", chunks.Count.ToString());
            AddSummaryLine("Tamaño chunk", tileMap.ChunkSize.ToString());
            int tileCount = 0;
            foreach (var (cx, cy) in chunks)
            {
                var ch = tileMap.GetChunk(cx, cy);
                if (ch != null)
                    tileCount += tileMap.ChunkSize * tileMap.ChunkSize;
            }
            AddSummaryLine("Tiles (aprox.)", tileCount.ToString());
            if (chunks.Count > 0 && chunks.Count <= 20)
            {
                AddSummaryHeader("Coordenadas de chunks");
                foreach (var (cx, cy) in chunks.Take(15))
                    AddSummaryLine($"  Chunk", $"({cx}, {cy})");
                if (chunks.Count > 15)
                    AddSummaryLine("  …", $"+{chunks.Count - 15} más");
            }
            SummaryPanel.Visibility = Visibility.Visible;
        }
        else if (string.Equals(fileName, "objetos.json", StringComparison.OrdinalIgnoreCase) && objectLayer != null)
        {
            AddSummaryLine("Definiciones", objectLayer.Definitions.Count.ToString());
            AddSummaryLine("Instancias", objectLayer.Instances.Count.ToString());
            AddSummaryHeader("Objetos definidos");
            foreach (var def in objectLayer.Definitions.Values.Take(25))
            {
                AddSummaryLine($"  {def.Id}", def.Nombre);
                AddSummaryLine("    Colisión", def.Colision ? "Sí" : "No");
                AddSummaryLine("    Interactivo", def.Interactivo ? "Sí" : "No");
                AddSummaryLine("    Script", def.ScriptId ?? "—");
                AddSummaryLine("    Tamaño", $"{def.Width}×{def.Height}");
            }
            if (objectLayer.Definitions.Count > 25)
                AddSummaryLine("  …", $"+{objectLayer.Definitions.Count - 25} más");
            SummaryPanel.Visibility = Visibility.Visible;
        }
        else if (string.Equals(fileName, "scripts.json", StringComparison.OrdinalIgnoreCase) && scriptRegistry != null)
        {
            var all = scriptRegistry.GetAll() ?? new List<ScriptDefinition>();
            AddSummaryLine("Scripts", all.Count.ToString());
            AddSummaryHeader("Scripts");
            foreach (var s in all.Take(20))
            {
                AddSummaryLine($"  {s.Id}", s.Nombre);
                var evts = s.Eventos != null ? string.Join(", ", s.Eventos) : "—";
                AddSummaryLine("    Eventos", evts.Length > 40 ? evts.Substring(0, 40) + "…" : evts);
            }
            if (all.Count > 20)
                AddSummaryLine("  …", $"+{all.Count - 20} más");
            SummaryPanel.Visibility = Visibility.Visible;
        }
        else if (string.Equals(fileName, "animaciones.json", StringComparison.OrdinalIgnoreCase) && project != null)
        {
            try
            {
                var path = Path.Combine(project.ProjectDirectory ?? "", "animaciones.json");
                var anims = File.Exists(path) ? AnimationSerialization.Load(path) : new List<AnimationDefinition>();
                AddSummaryLine("Animaciones", anims.Count.ToString());
                AddSummaryHeader("Animaciones");
                foreach (var a in anims.Take(15))
                {
                    AddSummaryLine($"  {a.Id}", a.Nombre);
                    AddSummaryLine("    Frames", a.Frames?.Count.ToString() ?? "0");
                    AddSummaryLine("    FPS", a.Fps.ToString());
                }
                if (anims.Count > 15)
                    AddSummaryLine("  …", $"+{anims.Count - 15} más");
                SummaryPanel.Visibility = Visibility.Visible;
            }
            catch { AddSummaryLine("Animaciones", "0"); SummaryPanel.Visibility = Visibility.Visible; }
        }
        else         if (item.FileType == ProjectFileType.Sprite || item.FileType == ProjectFileType.Sound)
        {
            AddSummaryLine("Tipo", item.FileType == ProjectFileType.Sprite ? "Sprite / imagen" : "Sonido");
            if (File.Exists(fullPath))
            {
                var fi = new FileInfo(fullPath);
                AddSummaryLine("Tamaño", $"{fi.Length} bytes");
            }
            SummaryPanel.Visibility = Visibility.Visible;
        }
        else if (ext == ".json" || item.FileType == ProjectFileType.Generic)
        {
            AddSummaryLine("Archivo", "JSON u otro");
            if (File.Exists(fullPath))
            {
                var fi = new FileInfo(fullPath);
                AddSummaryLine("Tamaño", $"{fi.Length} bytes");
            }
            SummaryPanel.Visibility = Visibility.Visible;
        }
    }

    private void AddSummaryHeader(string text)
    {
        var tb = new TextBlock { Text = text, FontWeight = FontWeights.SemiBold, Foreground = LabelBrush, FontSize = 11, Margin = new Thickness(0, 6, 0, 2) };
        SummaryStack.Children.Add(tb);
    }

    private void AddSummaryLine(string label, string value)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var lbl = new TextBlock { Text = label + ":", Foreground = LabelBrush, FontSize = 11, Margin = new Thickness(0, 0, 8, 2), TextTrimming = TextTrimming.CharacterEllipsis };
        var val = new TextBlock { Text = value, Foreground = ValueBrush, FontSize = 11, TextWrapping = TextWrapping.NoWrap };
        Grid.SetColumn(lbl, 0);
        Grid.SetColumn(val, 1);
        grid.Children.Add(lbl);
        grid.Children.Add(val);
        SummaryStack.Children.Add(grid);
    }

    private void BtnOpen_OnClick(object sender, RoutedEventArgs e)
    {
        if (_currentItem != null && !_currentItem.IsFolder) RequestOpenInEditor?.Invoke(this, _currentItem);
    }

    private void BtnDuplicate_OnClick(object sender, RoutedEventArgs e)
    {
        if (_currentItem != null) RequestDuplicate?.Invoke(this, _currentItem);
    }

    private void BtnRename_OnClick(object sender, RoutedEventArgs e)
    {
        if (_currentItem != null) RequestRename?.Invoke(this, _currentItem);
    }

    private void BtnShowInFolder_OnClick(object sender, RoutedEventArgs e)
    {
        if (_currentItem != null) RequestShowInFolder?.Invoke(this, _currentItem);
    }

    private void BtnDelete_OnClick(object sender, RoutedEventArgs e)
    {
        if (_currentItem != null) RequestDelete?.Invoke(this, _currentItem);
    }
}
