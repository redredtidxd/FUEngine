using System.Windows;
using System.Windows.Controls;
using FUEngine.Core;

namespace FUEngine;

public partial class TriggerZoneInspectorPanel : System.Windows.Controls.UserControl
{
    private TriggerZone? _target;
    private bool _updating;
    private List<(string Id, string Nombre, string? Path)> _scripts = new();

    public event EventHandler? PropertyChanged;
    public event EventHandler<TriggerZone>? RequestDuplicate;
    public event EventHandler<TriggerZone>? RequestDelete;

    public TriggerZoneInspectorPanel()
    {
        InitializeComponent();
    }

    public void SetAvailableScripts(IEnumerable<(string Id, string Nombre, string? Path)> scripts)
    {
        _scripts = scripts?.ToList() ?? new List<(string, string, string?)>();
    }

    public void SetTarget(TriggerZone? zone)
    {
        _target = zone;
        _updating = true;
        if (zone == null)
        {
            Visibility = Visibility.Collapsed;
            _updating = false;
            return;
        }
        Visibility = Visibility.Visible;
        TxtNombre.Text = zone.Nombre;
        TxtDescripcion.Text = zone.Descripcion ?? "";
        CmbTriggerType.Items.Clear();
        CmbTriggerType.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "Al entrar", Tag = "OnEnter" });
        CmbTriggerType.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "Al salir", Tag = "OnExit" });
        CmbTriggerType.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "Temporal", Tag = "Temporal" });
        CmbTriggerType.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "Persistente", Tag = "Persistent" });
        var tt = zone.TriggerType ?? "OnEnter";
        for (int k = 0; k < CmbTriggerType.Items.Count; k++)
        {
            if (CmbTriggerType.Items[k] is System.Windows.Controls.ComboBoxItem tti && (tti.Tag as string) == tt)
            { CmbTriggerType.SelectedIndex = k; break; }
        }
        if (CmbTriggerType.SelectedIndex < 0) CmbTriggerType.SelectedIndex = 0;
        TxtLayerId.Text = zone.LayerId.ToString();
        TxtX.Text = zone.X.ToString();
        TxtY.Text = zone.Y.ToString();
        TxtWidth.Text = zone.Width.ToString();
        TxtHeight.Text = zone.Height.ToString();
        TxtTags.Text = zone.Tags != null && zone.Tags.Count > 0 ? string.Join(", ", zone.Tags) : "";

            void FillCombo(System.Windows.Controls.ComboBox cb, string? currentId)
            {
                cb.Items.Clear();
                cb.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "(ninguno)", Tag = (string?)null });
            foreach (var (id, nombre, _) in _scripts)
                cb.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = nombre, Tag = id });
            for (int i = 0; i < cb.Items.Count; i++)
            {
                if (cb.Items[i] is System.Windows.Controls.ComboBoxItem item && (item.Tag as string) == currentId)
                { cb.SelectedIndex = i; break; }
            }
            if (cb.SelectedIndex < 0) cb.SelectedIndex = 0;
        }
        FillCombo(CmbScriptEnter, zone.ScriptIdOnEnter);
        FillCombo(CmbScriptExit, zone.ScriptIdOnExit);
        FillCombo(CmbScriptTick, zone.ScriptIdOnTick);
        _updating = false;
    }

    private void TxtNombre_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        _target.Nombre = TxtNombre.Text ?? "";
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void TxtPositionSize_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        if (int.TryParse(TxtX.Text, out int x)) _target.X = x;
        if (int.TryParse(TxtY.Text, out int y)) _target.Y = y;
        if (int.TryParse(TxtWidth.Text, out int w) && w > 0) _target.Width = w;
        if (int.TryParse(TxtHeight.Text, out int h) && h > 0) _target.Height = h;
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void CmbScript_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        if (CmbScriptEnter.SelectedItem is System.Windows.Controls.ComboBoxItem enterItem)
            _target.ScriptIdOnEnter = enterItem.Tag as string;
        if (CmbScriptExit.SelectedItem is System.Windows.Controls.ComboBoxItem exitItem)
            _target.ScriptIdOnExit = exitItem.Tag as string;
        if (CmbScriptTick.SelectedItem is System.Windows.Controls.ComboBoxItem tickItem)
            _target.ScriptIdOnTick = tickItem.Tag as string;
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void TxtDescripcion_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        _target.Descripcion = TxtDescripcion.Text ?? "";
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void CmbTriggerType_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || _target == null || CmbTriggerType.SelectedItem is not System.Windows.Controls.ComboBoxItem item) return;
        _target.TriggerType = item.Tag as string ?? "OnEnter";
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void TxtLayerId_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        if (int.TryParse(TxtLayerId.Text, out int id)) _target.LayerId = id;
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void TxtTags_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        var t = TxtTags?.Text ?? "";
        _target.Tags = t.Split(',', StringSplitOptions.TrimEntries).Where(s => s.Length > 0).ToList();
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void BtnDuplicate_OnClick(object sender, RoutedEventArgs e)
    {
        if (_target != null) RequestDuplicate?.Invoke(this, _target);
    }

    private void BtnDelete_OnClick(object sender, RoutedEventArgs e)
    {
        if (_target != null) RequestDelete?.Invoke(this, _target);
    }
}
