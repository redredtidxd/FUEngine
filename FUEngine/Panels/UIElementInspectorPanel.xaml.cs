using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using FUEngine.Core;
using UIElementCore = FUEngine.Core.UIElement;

namespace FUEngine;

public partial class UIElementInspectorPanel : System.Windows.Controls.UserControl
{
    private UIRoot? _root;
    private UICanvas? _canvas;
    private UIElementCore? _target;
    private bool _updating;

    public event EventHandler? PropertyChanged;

    public UIElementInspectorPanel()
    {
        InitializeComponent();
        CmbKind.Items.Clear();
        foreach (var kind in new[] { UIElementKind.Button, UIElementKind.Text, UIElementKind.Image, UIElementKind.Panel, UIElementKind.TabControl })
            CmbKind.Items.Add(new ComboBoxItem { Content = kind.ToString(), Tag = kind });
    }

    public void SetTarget(UICanvas? canvas, UIElementCore? element, UIRoot? root)
    {
        _canvas = canvas;
        _target = element;
        _root = root;
        _updating = true;

        if (element == null)
        {
            TxtNoSelection.Visibility = Visibility.Visible;
            PanelElement.Visibility = Visibility.Collapsed;
            _updating = false;
            return;
        }

        TxtNoSelection.Visibility = Visibility.Collapsed;
        PanelElement.Visibility = Visibility.Visible;
        TxtCanvasInfo.Text = "Canvas: " + (string.IsNullOrWhiteSpace(canvas?.Name) ? canvas?.Id ?? "—" : canvas!.Name);
        TxtId.Text = element.Id ?? "";
        TxtSeedId.Text = element.SeedId ?? "";
        TxtText.Text = element.Text ?? "";
        TxtImagePath.Text = element.ImagePath ?? "";
        ChkBlocksInput.IsChecked = element.BlocksInput;
        TxtRectX.Text = element.Rect.X.ToString(CultureInfo.InvariantCulture);
        TxtRectY.Text = element.Rect.Y.ToString(CultureInfo.InvariantCulture);
        TxtRectW.Text = element.Rect.Width.ToString(CultureInfo.InvariantCulture);
        TxtRectH.Text = element.Rect.Height.ToString(CultureInfo.InvariantCulture);
        TxtMinX.Text = element.Anchors.MinX.ToString(CultureInfo.InvariantCulture);
        TxtMinY.Text = element.Anchors.MinY.ToString(CultureInfo.InvariantCulture);
        TxtMaxX.Text = element.Anchors.MaxX.ToString(CultureInfo.InvariantCulture);
        TxtMaxY.Text = element.Anchors.MaxY.ToString(CultureInfo.InvariantCulture);

        CmbKind.SelectedIndex = -1;
        for (var i = 0; i < CmbKind.Items.Count; i++)
        {
            if (CmbKind.Items[i] is ComboBoxItem item && item.Tag is UIElementKind kind && kind == element.Kind)
            {
                CmbKind.SelectedIndex = i;
                break;
            }
        }
        if (CmbKind.SelectedIndex < 0 && CmbKind.Items.Count > 0) CmbKind.SelectedIndex = 0;
        UpdatePrefabUiState();
        _updating = false;
    }

    private void TxtId_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        var previous = _target.Id ?? "";
        var next = (TxtId.Text ?? "").Trim();
        if (string.Equals(previous, next, StringComparison.Ordinal)) return;
        _target.Id = next;
        if (_canvas != null && !UIRoot.IsIdUniqueInCanvas(_canvas, next, _target))
        {
            _target.Id = previous;
            _updating = true;
            TxtId.Text = previous;
            _updating = false;
            EditorLog.Toast("Ya existe otro elemento UI con ese Id en el canvas.", LogLevel.Warning, "UI");
            return;
        }
        OnElementChanged();
    }

    private void TxtSeedId_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        _target.SeedId = (TxtSeedId.Text ?? "").Trim();
        OnElementChanged();
    }

    private void CmbKind_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        if (CmbKind.SelectedItem is not ComboBoxItem item || item.Tag is not UIElementKind kind) return;
        _target.Kind = kind;
        OnElementChanged();
    }

    private void ChkBlocksInput_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_updating || _target == null) return;
        _target.BlocksInput = ChkBlocksInput.IsChecked == true;
        OnElementChanged();
    }

    private void TxtRect_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        if (!TryParseDouble(TxtRectX.Text, out var x) || !TryParseDouble(TxtRectY.Text, out var y) ||
            !TryParseDouble(TxtRectW.Text, out var w) || !TryParseDouble(TxtRectH.Text, out var h))
        {
            RefreshRectFromTarget();
            return;
        }
        w = Math.Max(0, w);
        h = Math.Max(0, h);
        var rect = _target.Rect;
        bool changed = rect.X != x || rect.Y != y || rect.Width != w || rect.Height != h;
        _target.Rect = new UIRect { X = x, Y = y, Width = w, Height = h };
        if (changed) OnElementChanged();
    }

    private void RefreshRectFromTarget()
    {
        if (_target == null) return;
        _updating = true;
        TxtRectX.Text = _target.Rect.X.ToString(CultureInfo.InvariantCulture);
        TxtRectY.Text = _target.Rect.Y.ToString(CultureInfo.InvariantCulture);
        TxtRectW.Text = _target.Rect.Width.ToString(CultureInfo.InvariantCulture);
        TxtRectH.Text = _target.Rect.Height.ToString(CultureInfo.InvariantCulture);
        _updating = false;
    }

    private void TxtAnchors_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        if (!TryParseDouble(TxtMinX.Text, out var minX) || !TryParseDouble(TxtMinY.Text, out var minY) ||
            !TryParseDouble(TxtMaxX.Text, out var maxX) || !TryParseDouble(TxtMaxY.Text, out var maxY))
        {
            RefreshAnchorsFromTarget();
            return;
        }
        if (minX > maxX) (minX, maxX) = (maxX, minX);
        if (minY > maxY) (minY, maxY) = (maxY, minY);
        var anchors = _target.Anchors;
        bool changed = anchors.MinX != minX || anchors.MinY != minY || anchors.MaxX != maxX || anchors.MaxY != maxY;
        _target.Anchors = new UIAnchors { MinX = minX, MinY = minY, MaxX = maxX, MaxY = maxY };
        if (changed) OnElementChanged();
    }

    private void RefreshAnchorsFromTarget()
    {
        if (_target == null) return;
        _updating = true;
        TxtMinX.Text = _target.Anchors.MinX.ToString(CultureInfo.InvariantCulture);
        TxtMinY.Text = _target.Anchors.MinY.ToString(CultureInfo.InvariantCulture);
        TxtMaxX.Text = _target.Anchors.MaxX.ToString(CultureInfo.InvariantCulture);
        TxtMaxY.Text = _target.Anchors.MaxY.ToString(CultureInfo.InvariantCulture);
        _updating = false;
    }

    private void TxtText_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        _target.Text = TxtText.Text ?? "";
        OnElementChanged();
    }

    private void TxtImagePath_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        _target.ImagePath = TxtImagePath.Text ?? "";
        OnElementChanged();
    }

    private void BtnApplyPrefab_OnClick(object sender, RoutedEventArgs e)
    {
        if (_target == null) return;
        var prefab = ResolvePrefab();
        if (prefab == null)
        {
            EditorLog.Toast("No se encontró prefab para este SeedId.", LogLevel.Warning, "UI");
            return;
        }
        UIPrefabPolicy.ApplyFromPrefab(_target, prefab, keepOverrides: true);
        SetTarget(_canvas, _target, _root);
        PropertyChanged?.Invoke(this, EventArgs.Empty);
        EditorLog.Toast("Prefab aplicado manteniendo overrides.", LogLevel.Info, "UI");
    }

    private void BtnResetOverrides_OnClick(object sender, RoutedEventArgs e)
    {
        if (_target == null) return;
        var prefab = ResolvePrefab();
        if (prefab == null)
        {
            EditorLog.Toast("No se encontró prefab para este SeedId.", LogLevel.Warning, "UI");
            return;
        }
        UIPrefabPolicy.ResetOverrides(_target, prefab);
        SetTarget(_canvas, _target, _root);
        PropertyChanged?.Invoke(this, EventArgs.Empty);
        EditorLog.Toast("Overrides reseteados al prefab.", LogLevel.Info, "UI");
    }

    private UIElementCore? ResolvePrefab() => UIPrefabPolicy.FindPrefabBySeedId(_root, _target);

    private void OnElementChanged()
    {
        RefreshOverridesFromPrefabIfNeeded();
        UpdatePrefabUiState();
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshOverridesFromPrefabIfNeeded()
    {
        if (_target == null) return;
        if (string.IsNullOrWhiteSpace(_target.SeedId))
        {
            _target.PropertyOverrides.Clear();
            return;
        }
        var prefab = ResolvePrefab();
        if (prefab != null)
            UIPrefabPolicy.RefreshOverridesFromPrefab(_target, prefab);
    }

    private void UpdatePrefabUiState()
    {
        var hasSeed = _target != null && !string.IsNullOrWhiteSpace(_target.SeedId);
        var hasPrefab = hasSeed && ResolvePrefab() != null;
        BtnApplyPrefab.IsEnabled = hasPrefab;
        BtnResetOverrides.IsEnabled = hasPrefab;
        var count = _target?.PropertyOverrides?.Count ?? 0;
        TxtOverridesInfo.Text = $"Overrides: {count}";
    }

    private static bool TryParseDouble(string? value, out double parsed) =>
        double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed);
}
