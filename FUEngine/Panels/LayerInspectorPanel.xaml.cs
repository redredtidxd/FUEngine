using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using FUEngine.Core;

namespace FUEngine;

public partial class LayerInspectorPanel : System.Windows.Controls.UserControl
{
    private MapLayerDescriptor? _descriptor;
    private bool _updating;

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

    public void SetDescriptor(MapLayerDescriptor? descriptor)
    {
        _descriptor = descriptor;
        _updating = true;
        try
        {
            if (descriptor == null)
            {
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
                Visibility = Visibility.Collapsed;
                return;
            }
            Visibility = Visibility.Visible;
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

    public event EventHandler? PropertyChanged;
}
