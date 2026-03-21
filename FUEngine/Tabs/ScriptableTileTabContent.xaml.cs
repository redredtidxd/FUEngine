using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using FUEngine.Runtime;

namespace FUEngine;

public partial class ScriptableTileTabContent : System.Windows.Controls.UserControl
{
    private string? _currentScriptPath;
    private string _projectDirectory = "";
    private byte[]? _lastBgra;
    private int _lastWidth;
    private int _lastHeight;
    private readonly Dictionary<string, double> _propertyValues = new();
    private string? _lastPropertiesSource;
    private System.Windows.Threading.DispatcherTimer? _propertiesDebounce;

    public ScriptableTileTabContent()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (CodeEditor != null)
            {
                CodeEditor.SyntaxHighlighting = LoadLuaHighlighting();
                if (string.IsNullOrWhiteSpace(CodeEditor.Text))
                    CodeEditor.Text = DefaultScriptTemplate;
                CodeEditor.TextChanged += CodeEditor_TextChanged;
            }
            if (CmbSize != null && CmbSize.Items.Count > 0)
                CmbSize.SelectedIndex = 0;
        };
    }

    private void CodeEditor_TextChanged(object? sender, EventArgs e)
    {
        _propertiesDebounce?.Stop();
        _propertiesDebounce = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(600)
        };
        _propertiesDebounce.Tick += (_, _) =>
        {
            _propertiesDebounce.Stop();
            RefreshPropertiesPanel();
        };
        _propertiesDebounce.Start();
    }

    public void SetProjectDirectory(string projectDirectory) => _projectDirectory = projectDirectory ?? "";

    private const string DefaultScriptTemplate = @"-- Parámetros (aparecen como sliders en el editor)
property(""Scale"", 0.08, 0.01, 0.5)
property(""Seed"", 0, 0, 9999)

-- Ruido: noise(x, y) o math.noise(x, y) — Perlin 2D en [-1, 1]
function onGenerateTile(canvas, w, h)
  for y = 0, h - 1 do
    for x = 0, w - 1 do
      local v = noise(x * Scale, y * Scale)
      v = (v + 1) * 0.5
      local g = math.floor(v * 255)
      canvas:SetPixel(x, y, g, g, 200, 255)
    end
  end
end
";

    private static IHighlightingDefinition? LoadLuaHighlighting()
    {
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            var name = asm.GetName().Name + ".Resources.Lua.xshd";
            using var stream = asm.GetManifestResourceStream(name);
            if (stream == null) return null;
            using var reader = XmlReader.Create(stream);
            return HighlightingLoader.Load(reader, HighlightingManager.Instance);
        }
        catch
        {
            return null;
        }
    }

    public void LoadScript(string fullPath)
    {
        _currentScriptPath = fullPath;
        if (TxtPath != null)
            TxtPath.Text = "Tile por script — " + (string.IsNullOrEmpty(fullPath) ? "Sin script" : Path.GetFileName(fullPath));

        if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
        {
            if (CodeEditor != null) CodeEditor.Text = "";
            return;
        }

        try
        {
            var source = File.ReadAllText(fullPath, Encoding.UTF8);
            if (CodeEditor != null) CodeEditor.Text = source;
            if (TxtError != null) { TxtError.Text = ""; TxtError.Visibility = Visibility.Collapsed; }
            RefreshPropertiesPanel();
        }
        catch (Exception ex)
        {
            if (TxtError != null) { TxtError.Text = "Error al cargar: " + ex.Message; TxtError.Visibility = Visibility.Visible; }
        }
    }

    /// <summary>Reconstruye los sliders a partir del script. Solo se llama cuando el código cambia o al cargar; al mover un slider solo se regenera (sin tocar la UI).</summary>
    private void RefreshPropertiesPanel()
    {
        if (PropertiesPanel == null || CodeEditor == null) return;
        var source = CodeEditor.Text ?? "";
        var (properties, error) = LuaTileGenerator.GetProperties(source, _currentScriptPath ?? "tilegen");
        _lastPropertiesSource = source;
        PropertiesPanel.Children.Clear();
        if (properties.Count == 0 && !string.IsNullOrEmpty(error))
        {
            var errBlock = new TextBlock { Text = "(Error en script: " + error + ")", Foreground = System.Windows.Media.Brushes.OrangeRed, FontSize = 10, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 4) };
            PropertiesPanel.Children.Add(errBlock);
            return;
        }
        foreach (var prop in properties)
        {
            var row = new StackPanel { Orientation = System.Windows.Controls.Orientation.Vertical, Margin = new Thickness(0, 0, 0, 10) };
            var label = new TextBlock
            {
                Text = prop.Name + ": " + (_propertyValues.TryGetValue(prop.Name, out var cur) ? cur.ToString("G4") : prop.Default.ToString("G4")),
                Foreground = System.Windows.Media.Brushes.LightGray,
                FontSize = 11
            };
            var value = _propertyValues.TryGetValue(prop.Name, out var v) ? v : prop.Default;
            value = Math.Clamp(value, prop.Min, prop.Max);
            _propertyValues[prop.Name] = value;
            var slider = new Slider
            {
                Minimum = prop.Min,
                Maximum = prop.Max,
                Value = value,
                Width = 180,
                VerticalAlignment = VerticalAlignment.Center
            };
            slider.ValueChanged += (_, _) =>
            {
                var val = slider.Value;
                _propertyValues[prop.Name] = val;
                label.Text = prop.Name + ": " + val.ToString("G4");
                // Solo regenerar; no RefreshPropertiesPanel() para no reconstruir la UI
                RegenerateFromCurrentState();
            };
            row.Children.Add(label);
            row.Children.Add(slider);
            PropertiesPanel.Children.Add(row);
        }
    }

    private void BtnOpen_OnClick(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Scripts Lua|*.lua|Todos|*.*",
            Title = "Abrir script de generación de tile"
        };
        if (!string.IsNullOrEmpty(_projectDirectory) && Directory.Exists(_projectDirectory))
            dlg.InitialDirectory = _projectDirectory;
        if (dlg.ShowDialog() != true) return;
        LoadScript(dlg.FileName);
    }

    private void CmbSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Size used on Regenerate
    }

    private int GetSelectedSize()
    {
        if (CmbSize?.SelectedItem is ComboBoxItem item && item.Tag is string s && int.TryParse(s, out var n))
            return Math.Clamp(n, 16, 128);
        return 16;
    }

    private void BtnRegenerate_OnClick(object sender, RoutedEventArgs e)
    {
        var source = CodeEditor?.Text ?? "";
        if (source != _lastPropertiesSource)
            RefreshPropertiesPanel();
        RegenerateFromCurrentState();
    }

    private void RegenerateFromCurrentState()
    {
        var source = CodeEditor?.Text ?? "";
        var size = GetSelectedSize();

        if (string.IsNullOrWhiteSpace(source))
        {
            if (TxtError != null) { TxtError.Text = "Escribe o abre un script que defina onGenerateTile(canvas, width, height)."; TxtError.Visibility = Visibility.Visible; }
            if (PreviewImage != null) PreviewImage.Source = null;
            return;
        }

        var (bgra, w, h, err) = LuaTileGenerator.RunFromSource(
            source,
            size,
            size,
            _currentScriptPath ?? "tilegen",
            msg => { },
            _propertyValues);

        if (err != null)
        {
            if (TxtError != null) { TxtError.Text = err; TxtError.Visibility = Visibility.Visible; }
            if (PreviewImage != null) PreviewImage.Source = null;
            _lastBgra = null;
            return;
        }

        if (TxtError != null) { TxtError.Text = ""; TxtError.Visibility = Visibility.Collapsed; }
        _lastBgra = bgra;
        _lastWidth = w;
        _lastHeight = h;

        var bmp = new WriteableBitmap(w, h, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
        bmp.WritePixels(new System.Windows.Int32Rect(0, 0, w, h), bgra!, w * 4, 0);
        if (PreviewImage != null) PreviewImage.Source = bmp;
    }

    private void BtnExport_OnClick(object sender, RoutedEventArgs e)
    {
        if (_lastBgra == null || _lastWidth <= 0 || _lastHeight <= 0)
        {
            System.Windows.MessageBox.Show("Genera primero el tile con Regenerar.", "Exportar", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PNG|*.png|Todos|*.*",
            Title = "Exportar tile a PNG"
        };
        if (!string.IsNullOrEmpty(_projectDirectory) && Directory.Exists(_projectDirectory))
            dlg.InitialDirectory = Path.Combine(_projectDirectory, "Assets", "Tiles");
        if (string.IsNullOrEmpty(dlg.InitialDirectory) || !Directory.Exists(dlg.InitialDirectory))
            dlg.InitialDirectory = _projectDirectory;
        if (dlg.ShowDialog() != true) return;

        var pngPath = dlg.FileName;
        try
        {
            using var stream = File.Create(pngPath);
            var encoder = new PngBitmapEncoder();
            var bmp = new WriteableBitmap(_lastWidth, _lastHeight, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
            bmp.WritePixels(new System.Windows.Int32Rect(0, 0, _lastWidth, _lastHeight), _lastBgra, _lastWidth * 4, 0);
            bmp.Freeze();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            encoder.Save(stream);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("No se pudo guardar: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var tiledataPath = TileDataFile.GetTileDataPath(pngPath);
        var dto = new TileDataDto
        {
            GridSize = _lastWidth,
            FrameCount = 1
        };
        TileDataFile.Save(tiledataPath, dto);
        CreativeSuiteMetadata.Write(pngPath, CreativeSuiteMetadata.SourceTile);

        System.Windows.MessageBox.Show("Guardado: " + pngPath, "Tile exportado", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
