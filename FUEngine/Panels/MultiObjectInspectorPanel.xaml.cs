using System.Windows;
using System.Windows.Controls;
using FUEngine.Core;

namespace FUEngine;

public partial class MultiObjectInspectorPanel : System.Windows.Controls.UserControl
{
    private List<ObjectInstance> _objects = new();
    private ObjectLayer? _layer;
    public event EventHandler? PropertyChanged;
    public event EventHandler? RequestClearSelection;

    public MultiObjectInspectorPanel()
    {
        InitializeComponent();
    }

    public void SetTarget(List<ObjectInstance> objects, ObjectLayer layer)
    {
        _objects = objects ?? new List<ObjectInstance>();
        _layer = layer;
        TxtCount.Text = _objects.Count + " objetos seleccionados";
        if (TxtCommonInfo != null)
            TxtCommonInfo.Text = _objects.Count > 1
                ? "Cambios de rotación y acciones se aplican a todos los objetos seleccionados."
                : "Seleccione varios objetos en el mapa (Ctrl+clic) para edición masiva.";
    }

    private void BtnRotate_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn || btn.Tag is not string tag || !int.TryParse(tag, out int delta)) return;
        foreach (var obj in _objects)
        {
            obj.Rotation = (obj.Rotation + delta) % 360;
            if (obj.Rotation < 0) obj.Rotation += 360;
        }
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void BtnDeselectAll_OnClick(object sender, RoutedEventArgs e)
    {
        RequestClearSelection?.Invoke(this, EventArgs.Empty);
    }
}
