using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using FUEngine.Core;
using FUEngine.Dialogs;

namespace FUEngine;

public partial class LayerInspectorPanel : System.Windows.Controls.UserControl
{
    private MapLayerDescriptor? _descriptor;
    private bool _updating;
    private string? _projectDirectory;

    public LayerInspectorPanel()
    {
        InitializeComponent();
        CmbLayerType.Items.Add("Suelo (Background)");
        CmbLayerType.Items.Add("Paredes (Solid)");
        CmbLayerType.Items.Add("Objetos");
        CmbLayerType.Items.Add("Superposición (Foreground)");
        CmbBlendMode.Items.Add("Normal");
        CmbBlendMode.Items.Add("Aditivo");
        CmbBlendMode.Items.Add("Multiplicar");
    }

    public void SetProjectDirectory(string? projectDirectory) => _projectDirectory = projectDirectory;

    public void SetDescriptor(MapLayerDescriptor? descriptor)
    {
        _descriptor = descriptor;
        _updating = true;
        try
        {
            if (descriptor == null)
            {
                TxtLayerSummary.Text = "";
                TxtId.Text = "";
                TxtName.Text = "";
                CmbLayerType.SelectedIndex = -1;
                SliderOpacity.Value = 100;
                TxtOpacityValue.Text = "100";
                CmbBlendMode.SelectedIndex = 0;
                TxtParallaxX.Text = "1";
                TxtParallaxY.Text = "1";
                TxtOffsetX.Text = "0";
                TxtOffsetY.Text = "0";
                TxtCollisionLayer.Text = "1";
                TxtCollisionMask.Text = "65535";
                ChkRenderAbovePlayer.IsChecked = false;
                ChkLayerScriptEnabled.IsChecked = true;
                TxtLayerScriptPath.Text = "";
                Visibility = Visibility.Collapsed;
                return;
            }
            Visibility = Visibility.Visible;
            TxtLayerSummary.Text = descriptor.LayerType switch
            {
                LayerType.Background => "Capa de tiles (suelo). Cada celda tiene datos de tile: tipo, colisión e interactividad. Edita con el pincel en la capa activa del mapa.",
                LayerType.Solid => "Capa de tiles sólida: las celdas ocupadas pueden bloquear paso según reglas de colisión del motor y la máscara de la capa.",
                LayerType.Objects => "Capa de tiles orientada a decoración u objetos en rejilla. Las entidades con lógica (instancias) están en la jerarquía del mapa y en objetos.json; las semillas son plantillas reutilizables.",
                LayerType.Foreground => "Capa de tiles por encima del jugador según orden de capas y «Dibujar encima del jugador». Útil para tejados, vegetación alta, etc.",
                _ => "Capa de tiles del mapa."
            };
            TxtId.Text = descriptor.Id;
            TxtName.Text = descriptor.Name;
            CmbLayerType.SelectedIndex = (int)descriptor.LayerType;
            SliderOpacity.Value = descriptor.Opacity;
            TxtOpacityValue.Text = descriptor.Opacity.ToString(CultureInfo.InvariantCulture);
            CmbBlendMode.SelectedIndex = (int)descriptor.BlendMode;
            TxtParallaxX.Text = descriptor.ParallaxX.ToString(CultureInfo.InvariantCulture);
            TxtParallaxY.Text = descriptor.ParallaxY.ToString(CultureInfo.InvariantCulture);
            TxtOffsetX.Text = descriptor.OffsetX.ToString(CultureInfo.InvariantCulture);
            TxtOffsetY.Text = descriptor.OffsetY.ToString(CultureInfo.InvariantCulture);
            TxtCollisionLayer.Text = descriptor.CollisionLayer.ToString(CultureInfo.InvariantCulture);
            TxtCollisionMask.Text = descriptor.CollisionMask.ToString(CultureInfo.InvariantCulture);
            ChkRenderAbovePlayer.IsChecked = descriptor.RenderAbovePlayer;
            ChkLayerScriptEnabled.IsChecked = descriptor.LayerScriptEnabled;
            TxtLayerScriptPath.Text = descriptor.LayerScriptId ?? "";
        }
        finally
        {
            _updating = false;
        }
    }

    private void TxtName_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _descriptor == null) return;
        _descriptor.Name = TxtName.Text ?? "";
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void CmbLayerType_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || _descriptor == null || CmbLayerType.SelectedIndex < 0) return;
        _descriptor.LayerType = (LayerType)CmbLayerType.SelectedIndex;
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SliderOpacity_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating || _descriptor == null) return;
        int v = (int)SliderOpacity.Value;
        _descriptor.Opacity = v;
        TxtOpacityValue.Text = v.ToString(CultureInfo.InvariantCulture);
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void CmbBlendMode_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || _descriptor == null || CmbBlendMode.SelectedIndex < 0) return;
        _descriptor.BlendMode = (LayerBlendMode)CmbBlendMode.SelectedIndex;
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void TxtParallax_OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (_descriptor == null) return;
        if (float.TryParse(TxtParallaxX.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float px))
            _descriptor.ParallaxX = px;
        if (float.TryParse(TxtParallaxY.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float py))
            _descriptor.ParallaxY = py;
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void TxtOffset_OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (_descriptor == null) return;
        if (float.TryParse(TxtOffsetX.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float ox))
            _descriptor.OffsetX = ox;
        if (float.TryParse(TxtOffsetY.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float oy))
            _descriptor.OffsetY = oy;
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void TxtCollision_OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (_descriptor == null) return;
        if (uint.TryParse(TxtCollisionLayer.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint cl))
            _descriptor.CollisionLayer = cl;
        if (uint.TryParse(TxtCollisionMask.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint cm))
            _descriptor.CollisionMask = cm;
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ChkRenderAbovePlayer_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_updating || _descriptor == null) return;
        _descriptor.RenderAbovePlayer = ChkRenderAbovePlayer.IsChecked == true;
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ChkLayerScriptEnabled_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_updating || _descriptor == null) return;
        _descriptor.LayerScriptEnabled = ChkLayerScriptEnabled.IsChecked == true;
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void TxtLayerScriptPath_OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (_descriptor == null) return;
        var t = (TxtLayerScriptPath.Text ?? "").Trim();
        _descriptor.LayerScriptId = string.IsNullOrEmpty(t) ? null : t;
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void BtnBrowseLayerScript_OnClick(object sender, RoutedEventArgs e)
    {
        if (_descriptor == null) return;
        var root = _projectDirectory;
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            System.Windows.MessageBox.Show("No hay directorio de proyecto válido.", "Script de capa", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Lua (*.lua)|*.lua|Todos (*.*)|*.*",
            InitialDirectory = root
        };
        if (dlg.ShowDialog() != true || string.IsNullOrEmpty(dlg.FileName)) return;
        try
        {
            var rel = Path.GetRelativePath(root, dlg.FileName).Replace('\\', '/');
            if (rel.StartsWith("..", StringComparison.Ordinal))
            {
                System.Windows.MessageBox.Show("Elige un archivo dentro de la carpeta del proyecto.", "Script de capa", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _updating = true;
            TxtLayerScriptPath.Text = rel;
            _descriptor.LayerScriptId = rel;
            _updating = false;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Script de capa", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    public event EventHandler? PropertyChanged;
    /// <summary>Tras elegir un componente de capa en el catálogo (p. ej. script Lua); el descriptor ya está actualizado.</summary>
    public event EventHandler<MapLayerDescriptor>? LayerComponentRequested;

    private void BtnAddComponent_OnClick(object sender, RoutedEventArgs e)
    {
        if (_descriptor == null) return;
        var dlg = new AddComponentPickerWindow(new[]
        {
            new AddComponentPickerWindow.ComponentPickItem { Category = "Capa", Id = "layer_script", Title = "Script Lua de capa", Description = "onLayerUpdate(dt); tabla layer", Enabled = true },
            new AddComponentPickerWindow.ComponentPickItem { Category = "Visual", Id = "parallax_dynamic", Title = "Parallax dinámico", Description = "animar offset/parallax por tiempo (próximamente)", Enabled = false },
            new AddComponentPickerWindow.ComponentPickItem { Category = "Visual", Id = "color_grading", Title = "Color grading / tint", Description = "tinte global en la capa (noche, cueva) (próximamente)", Enabled = false },
            new AddComponentPickerWindow.ComponentPickItem { Category = "Visual", Id = "tilt_shift_blur", Title = "Tilt-shift / desenfoque", Description = "DOF o blur selectivo en la capa (próximamente)", Enabled = false },
            new AddComponentPickerWindow.ComponentPickItem { Category = "Visual", Id = "weather_layer", Title = "Efecto climático (capa)", Description = "lluvia, nieve, polvo acotados a la capa (próximamente)", Enabled = false },
            new AddComponentPickerWindow.ComponentPickItem { Category = "Visual", Id = "fxaa_layer", Title = "FXAA (anti-aliasing capa)", Description = "post-proceso suave de bordes; opcional vs pixel art (próximamente)", Enabled = false },
            new AddComponentPickerWindow.ComponentPickItem { Category = "Física", Id = "wind_force", Title = "Viento / campo de fuerza", Description = "empuje en tiles de esta capa (próximamente)", Enabled = false },
            new AddComponentPickerWindow.ComponentPickItem { Category = "Física", Id = "liquid_buoyancy", Title = "Agua / flotabilidad", Description = "flotación automática en capa tipo agua (próximamente)", Enabled = false },
            new AddComponentPickerWindow.ComponentPickItem { Category = "Tiles", Id = "auto_tile_solver", Title = "Auto-tile solver (bitmask)", Description = "reglas de vecinos al pintar (próximamente)", Enabled = false },
            new AddComponentPickerWindow.ComponentPickItem { Category = "Gameplay", Id = "damage_layer", Title = "Capa de daño", Description = "tiles sólidos como peligro (lava/pinchos) (próximamente)", Enabled = false },
        })
        {
            Owner = Window.GetWindow(this)
        };
        if (dlg.ShowDialog() != true || string.IsNullOrEmpty(dlg.SelectedId)) return;
        if (dlg.SelectedId != "layer_script") return;
        _descriptor.LayerScriptEnabled = true;
        if (string.IsNullOrWhiteSpace(_descriptor.LayerScriptId))
            _descriptor.LayerScriptId = "";
        _updating = true;
        ChkLayerScriptEnabled.IsChecked = true;
        _updating = false;
        PropertyChanged?.Invoke(this, EventArgs.Empty);
        LayerComponentRequested?.Invoke(this, _descriptor);
    }
}
