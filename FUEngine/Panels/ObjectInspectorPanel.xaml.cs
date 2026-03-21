using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using FUEngine.Core;
using FUEngine.Editor;

namespace FUEngine;

public partial class ObjectInspectorPanel : System.Windows.Controls.UserControl
{
    private ObjectInstance? _target;
    private ObjectLayer? _layer;
    private bool _updating;
    private List<(string Id, string Nombre, string? Path)> _scripts = new();
    private string? _projectDirectory;

    public event EventHandler? PropertyChanged;
    public event EventHandler<ObjectInstance>? RequestDuplicate;
    public event EventHandler<ObjectInstance>? RequestDelete;
    public event EventHandler<ObjectInstance>? RequestRename;
    public event EventHandler<ObjectInstance>? RequestConvertToSeed;

    public ObjectInspectorPanel()
    {
        InitializeComponent();
    }

    public void SetProjectDirectory(string? directory)
    {
        _projectDirectory = directory;
    }

    public void SetAvailableScripts(IEnumerable<(string Id, string Nombre, string? Path)> scripts)
    {
        _scripts = scripts?.ToList() ?? new List<(string, string, string?)>();
    }

    public void SetTarget(ObjectInstance? instance, ObjectLayer? layer)
    {
        _target = instance;
        _layer = layer;
        _updating = true;
        if (instance == null || layer == null)
        {
            TxtNoSelection.Visibility = Visibility.Visible;
            PanelObject.Visibility = Visibility.Collapsed;
        }
        else
        {
            TxtNoSelection.Visibility = Visibility.Collapsed;
            PanelObject.Visibility = Visibility.Visible;
            var def = layer.GetDefinition(instance.DefinitionId);
            if (TxtInstanceId != null) TxtInstanceId.Text = instance.InstanceId ?? "";
            if (TxtDefinitionId != null) TxtDefinitionId.Text = instance.DefinitionId ?? "";
            TxtNombre.Text = instance.Nombre;
            TxtDefinitionName.Text = "Tipo: " + (def?.Nombre ?? instance.DefinitionId);
            TxtPosX.Text = instance.X.ToString(System.Globalization.CultureInfo.InvariantCulture);
            TxtPosY.Text = instance.Y.ToString(System.Globalization.CultureInfo.InvariantCulture);
            TxtSize.Text = def != null ? $"Tamaño: {def.Width}×{def.Height}" : "Tamaño: —";

            CmbRotation.Items.Clear();
            foreach (var d in new[] { 0, 90, 180, 270 })
                CmbRotation.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = d + "°", Tag = (double)d });
            var rot = (int)Math.Round(instance.Rotation) % 360;
            if (rot < 0) rot += 360;
            var idx = rot switch { 0 => 0, 90 => 1, 180 => 2, 270 => 3, _ => -1 };
            if (idx >= 0) CmbRotation.SelectedIndex = idx;
            else { CmbRotation.SelectedIndex = -1; TxtRotationCustom.Text = instance.Rotation.ToString(System.Globalization.CultureInfo.InvariantCulture); }
            if (TxtRotationCustom != null) TxtRotationCustom.Text = idx >= 0 ? "" : instance.Rotation.ToString(System.Globalization.CultureInfo.InvariantCulture);

            TxtScaleX.Text = instance.ScaleX.ToString(System.Globalization.CultureInfo.InvariantCulture);
            TxtScaleY.Text = instance.ScaleY.ToString(System.Globalization.CultureInfo.InvariantCulture);
            TxtLayerOrder.Text = instance.LayerOrder.ToString();
            TxtTags.Text = instance.Tags != null && instance.Tags.Count > 0 ? string.Join(", ", instance.Tags) : "";

            CmbCollisionType.Items.Clear();
            CmbCollisionType.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "Sólido", Tag = "Solid" });
            CmbCollisionType.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "Trigger", Tag = "Trigger" });
            CmbCollisionType.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "Superficie", Tag = "Surface" });
            var ct = instance.CollisionType ?? "Solid";
            for (int i = 0; i < CmbCollisionType.Items.Count; i++)
            {
                if (CmbCollisionType.Items[i] is System.Windows.Controls.ComboBoxItem cti && (cti.Tag as string) == ct)
                { CmbCollisionType.SelectedIndex = i; break; }
            }
            if (CmbCollisionType.SelectedIndex < 0) CmbCollisionType.SelectedIndex = 0;

            ChkColision.IsChecked = instance.ColisionOverride ?? def?.Colision ?? false;
            ChkInteractivo.IsChecked = instance.InteractivoOverride ?? def?.Interactivo ?? false;
            ChkDestructible.IsChecked = instance.DestructibleOverride ?? def?.Destructible ?? false;
            ChkVisible.IsChecked = instance.Visible;
            if (ChkEnableInGameDrawing != null)
                ChkEnableInGameDrawing.IsChecked = def?.EnableInGameDrawing ?? false;

            LstScripts.Items.Clear();
            var scriptIds = instance.ScriptIds != null && instance.ScriptIds.Count > 0 ? instance.ScriptIds : (instance.ScriptIdOverride != null ? new List<string> { instance.ScriptIdOverride } : new List<string>());
            foreach (var sid in scriptIds)
            {
                // FirstOrDefault devuelve tuple por defecto (Nombre null) si no hay coincidencia; ?? sid evita null. Mantener si Nombre deja de ser nullable.
                var name = _scripts.FirstOrDefault(s => s.Id == sid).Nombre ?? sid;
                LstScripts.Items.Add(new System.Windows.Controls.ListBoxItem { Content = name, Tag = sid });
            }
            CmbScriptAdd.Items.Clear();
            CmbScriptAdd.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "(seleccionar…)", Tag = (string?)null });
            foreach (var (id, nombre, _) in _scripts)
                CmbScriptAdd.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = nombre, Tag = id });
            CmbScriptAdd.SelectedIndex = 0;
            if (LstScripts.Items.Count > 0) LstScripts.SelectedIndex = 0;
            RefreshScriptPropertiesPanel();

            if (ImgPreview != null && PreviewBorder != null)
            {
                ImgPreview.Source = null;
                PreviewBorder.Visibility = Visibility.Collapsed;
                var spritePath = def?.SpritePath;
                if (!string.IsNullOrEmpty(spritePath) && !string.IsNullOrEmpty(_projectDirectory))
                {
                    var full = Path.Combine(_projectDirectory, spritePath);
                    if (File.Exists(full))
                    {
                        try
                        {
                            var bmp = new BitmapImage();
                            bmp.BeginInit();
                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                            bmp.UriSource = new Uri(full, UriKind.Absolute);
                            bmp.DecodePixelWidth = 64;
                            bmp.EndInit();
                            bmp.Freeze();
                            ImgPreview.Source = bmp;
                            PreviewBorder.Visibility = Visibility.Visible;
                        }
                        catch { }
                    }
                }
            }
        }
        _updating = false;
    }

    private void TxtNombre_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        _target.Nombre = TxtNombre.Text ?? "";
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void TxtInstanceId_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _target == null || TxtInstanceId == null || _layer == null) return;
        var newId = (TxtInstanceId.Text ?? "").Trim();
        var previousId = _target.InstanceId ?? "";
        if (string.Equals(newId, previousId, StringComparison.Ordinal)) return;
        _target.InstanceId = newId;
        var isDuplicate = _layer.Instances.Any(i => i != _target && string.Equals(i.InstanceId ?? "", newId, StringComparison.Ordinal));
        if (isDuplicate)
        {
            _target.InstanceId = previousId;
            _updating = true;
            TxtInstanceId.Text = previousId;
            _updating = false;
            EditorLog.Toast("Ya existe otro objeto con ese Instance ID. Los IDs deben ser únicos.", LogLevel.Warning, "Inspector");
            return;
        }
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void BtnGenerateInstanceId_OnClick(object sender, RoutedEventArgs e)
    {
        if (_updating || _target == null || _layer == null || TxtInstanceId == null) return;
        var existing = _layer.Instances.Select(i => i.InstanceId ?? "").Where(s => s.Length > 0).ToHashSet(StringComparer.Ordinal);
        var prefix = GetIdPrefixFromDefinition(_target.DefinitionId);
        for (var n = 1; n <= 9999; n++)
        {
            var candidate = prefix + "_" + n;
            if (!existing.Contains(candidate))
            {
                _updating = true;
                _target.InstanceId = candidate;
                TxtInstanceId.Text = candidate;
                _updating = false;
                PropertyChanged?.Invoke(this, EventArgs.Empty);
                EditorLog.Toast("ID generado: " + candidate, LogLevel.Info, "Inspector");
                return;
            }
        }
        var fallback = prefix + "_" + Guid.NewGuid().ToString("N")[..8];
        _updating = true;
        _target.InstanceId = fallback;
        TxtInstanceId.Text = fallback;
        _updating = false;
        PropertyChanged?.Invoke(this, EventArgs.Empty);
        EditorLog.Toast("ID generado: " + fallback, LogLevel.Info, "Inspector");
    }

    private static string GetIdPrefixFromDefinition(string? definitionId)
    {
        if (string.IsNullOrWhiteSpace(definitionId)) return "inst";
        var s = definitionId.Trim();
        var sb = new System.Text.StringBuilder();
        foreach (var c in s)
        {
            if (char.IsLetterOrDigit(c) || c == '_') sb.Append(c);
            else if (c == ' ' || c == '-') sb.Append('_');
        }
        var prefix = sb.ToString().Trim('_');
        return prefix.Length > 0 ? prefix : "inst";
    }

    private void TxtDefinitionId_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _target == null || TxtDefinitionId == null) return;
        _target.DefinitionId = TxtDefinitionId.Text ?? "";
        if (_layer != null)
        {
            var def = _layer.GetDefinition(_target.DefinitionId);
            TxtDefinitionName.Text = "Tipo: " + (def?.Nombre ?? _target.DefinitionId);
        }
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Chk_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_updating || _target == null) return;
        _target.ColisionOverride = ChkColision.IsChecked;
        _target.InteractivoOverride = ChkInteractivo.IsChecked;
        _target.DestructibleOverride = ChkDestructible.IsChecked;
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ChkVisible_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_updating || _target == null) return;
        _target.Visible = ChkVisible.IsChecked == true;
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ChkEnableInGameDrawing_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_updating || _target == null || _layer == null || ChkEnableInGameDrawing == null) return;
        var def = _layer.GetDefinition(_target.DefinitionId);
        if (def != null)
        {
            def.EnableInGameDrawing = ChkEnableInGameDrawing.IsChecked == true;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void CmbRotation_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || _target == null || CmbRotation.SelectedItem is not System.Windows.Controls.ComboBoxItem item || item.Tag is not double d) return;
        _target.Rotation = d;
        if (TxtRotationCustom != null) TxtRotationCustom.Text = "";
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void TxtRotation_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _target == null || string.IsNullOrWhiteSpace(TxtRotationCustom?.Text)) return;
        if (double.TryParse(TxtRotationCustom.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double r))
        {
            _target.Rotation = ((r % 360) + 360) % 360;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void TxtScale_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        if (double.TryParse(TxtScaleX?.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double sx) && sx > 0) _target.ScaleX = sx;
        if (double.TryParse(TxtScaleY?.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double sy) && sy > 0) _target.ScaleY = sy;
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void TxtLayerOrder_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        if (int.TryParse(TxtLayerOrder?.Text, out int o)) _target.LayerOrder = o;
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void TxtTags_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        var t = TxtTags?.Text ?? "";
        _target.Tags = t.Split(',', StringSplitOptions.TrimEntries).Where(s => s.Length > 0).ToList();
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void CmbCollisionType_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || _target == null || CmbCollisionType.SelectedItem is not System.Windows.Controls.ComboBoxItem item) return;
        _target.CollisionType = item.Tag as string ?? "Solid";
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void BtnAddScript_OnClick(object sender, RoutedEventArgs e)
    {
        if (_updating || _target == null || CmbScriptAdd.SelectedItem is not System.Windows.Controls.ComboBoxItem item) return;
        var id = item.Tag as string;
        if (string.IsNullOrEmpty(id)) return;
        if (_target.ScriptIds == null) _target.ScriptIds = new List<string>();
        if (_target.ScriptIds.Contains(id)) return;
        _target.ScriptIds.Add(id);
        if (_target.ScriptProperties == null) _target.ScriptProperties = new List<ScriptInstancePropertySet>();
        if (_target.ScriptProperties.All(sp => sp.ScriptId != id))
            _target.ScriptProperties.Add(new ScriptInstancePropertySet { ScriptId = id, Properties = new List<ScriptPropertyEntry>() });
        if (_target.ScriptIds.Count == 1) _target.ScriptIdOverride = id;
        var name = _scripts.FirstOrDefault(s => s.Id == id).Nombre ?? id;
        LstScripts.Items.Add(new System.Windows.Controls.ListBoxItem { Content = name, Tag = id });
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void BtnRemoveScript_OnClick(object sender, RoutedEventArgs e)
    {
        if (_updating || _target == null || LstScripts.SelectedItem is not System.Windows.Controls.ListBoxItem li) return;
        var id = li.Tag as string;
        if (id == null) return;
        _target.ScriptIds?.Remove(id);
        _target.ScriptProperties?.RemoveAll(sp => sp.ScriptId == id);
        if (_target.ScriptIds != null && _target.ScriptIds.Count > 0) _target.ScriptIdOverride = _target.ScriptIds[0];
        else _target.ScriptIdOverride = null;
        LstScripts.Items.Remove(li);
        RefreshScriptPropertiesPanel();
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private string? GetSelectedScriptId()
    {
        if (LstScripts?.SelectedItem is System.Windows.Controls.ListBoxItem li && li.Tag is string id) return id;
        return null;
    }

    private ScriptInstancePropertySet? GetOrCreatePropertySetForScript(string scriptId)
    {
        if (_target == null) return null;
        if (_target.ScriptProperties == null) _target.ScriptProperties = new List<ScriptInstancePropertySet>();
        var set = _target.ScriptProperties.FirstOrDefault(sp => sp.ScriptId == scriptId);
        if (set == null)
        {
            set = new ScriptInstancePropertySet { ScriptId = scriptId, Properties = new List<ScriptPropertyEntry>() };
            _target.ScriptProperties.Add(set);
        }
        return set;
    }

    private void LstScripts_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating) return;
        var id = GetSelectedScriptId();
        if (TxtScriptValidation != null)
        {
            if (!string.IsNullOrEmpty(id) && _scripts.All(s => s.Id != id))
            {
                TxtScriptValidation.Text = "⚠ Script no encontrado en el proyecto.";
                TxtScriptValidation.Visibility = Visibility.Visible;
            }
            else
                TxtScriptValidation.Visibility = Visibility.Collapsed;
        }
        RefreshScriptPropertiesPanel();
    }

    private void RefreshScriptPropertiesPanel()
    {
        if (ItemsScriptProps == null) return;
        ItemsScriptProps.Items.Clear();
        TxtScriptPropsHint.Visibility = Visibility.Visible;
        var scriptId = GetSelectedScriptId();
        if (string.IsNullOrEmpty(scriptId) || _target == null)
        {
            if (TxtScriptPropsHint != null)
                TxtScriptPropsHint.Text = "Seleccione un script. Las variables globales del .lua (asignaciones en la raíz del archivo, sin «local», fuera de function/if/for) se añaden aquí automáticamente; puede editar valores y tipos sin tocar el código.";
            return;
        }
        TxtScriptPropsHint.Visibility = Visibility.Collapsed;
        var set = GetOrCreatePropertySetForScript(scriptId);
        if (set?.Properties == null) return;
        var script = _scripts.FirstOrDefault(s => s.Id == scriptId);
        var scriptPath = script.Path;
        if (!string.IsNullOrEmpty(scriptPath) && scriptPath.EndsWith(".lua", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(_projectDirectory))
        {
            var fullPath = Path.Combine(_projectDirectory, scriptPath);
            if (File.Exists(fullPath))
            {
                try
                {
                    var code = File.ReadAllText(fullPath, System.Text.Encoding.UTF8);
                    var parsed = LuaScriptVariableParser.Parse(code);
                    foreach (var (name, type, defaultValue) in parsed)
                    {
                        if (set.Properties.Any(p => string.Equals(p.Key, name, StringComparison.OrdinalIgnoreCase)))
                            continue;
                        set.Properties.Add(new ScriptPropertyEntry { Key = name, Type = type, Value = defaultValue });
                    }
                }
                catch { /* ignorar errores de lectura/parse */ }
            }
        }
        var types = new[] { "string", "int", "float", "bool", "Vector2", "Color" };
        foreach (var prop in set.Properties)
        {
            var row = new System.Windows.Controls.Grid();
            row.Margin = new Thickness(0, 0, 0, 4);
            row.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(80) });
            row.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = GridLength.Auto });
            var keyBox = new System.Windows.Controls.TextBox
            {
                Text = prop.Key,
                Padding = new Thickness(4, 2, 4, 2),
                Background = System.Windows.Media.Brushes.Transparent,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x30, 0x36, 0x3d)),
                Tag = prop,
                ToolTip = "Nombre de la propiedad"
            };
            keyBox.TextChanged += (s, _) => { if (!_updating && s is System.Windows.Controls.TextBox tb && tb.Tag is ScriptPropertyEntry pe) { pe.Key = tb.Text ?? ""; PropertyChanged?.Invoke(this, EventArgs.Empty); } };
            var typeCombo = new System.Windows.Controls.ComboBox
            {
                Padding = new Thickness(4, 2, 4, 2),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x0d, 0x11, 0x17)),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x30, 0x36, 0x3d)),
                Tag = prop,
                ToolTip = "Tipo: string, int, float, bool, Vector2, Color"
            };
            foreach (var t in types) typeCombo.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = t, Tag = t });
            typeCombo.SelectedIndex = Math.Max(0, Array.IndexOf(types, prop.Type));
            typeCombo.SelectionChanged += (s, _) => { if (!_updating && s is System.Windows.Controls.ComboBox cb && cb.SelectedItem is System.Windows.Controls.ComboBoxItem ci && ci.Tag is string ty && cb.Tag is ScriptPropertyEntry pe) { pe.Type = ty; PropertyChanged?.Invoke(this, EventArgs.Empty); } };
            var valBox = new System.Windows.Controls.TextBox
            {
                Text = prop.Value,
                Padding = new Thickness(4, 2, 4, 2),
                Background = System.Windows.Media.Brushes.Transparent,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x30, 0x36, 0x3d)),
                Tag = prop,
                ToolTip = "Valor (según el tipo)"
            };
            valBox.TextChanged += (s, _) => { if (!_updating && s is System.Windows.Controls.TextBox tb && tb.Tag is ScriptPropertyEntry pe) { pe.Value = tb.Text ?? ""; PropertyChanged?.Invoke(this, EventArgs.Empty); } };
            var removeBtn = new System.Windows.Controls.Button
            {
                Content = "✕",
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(4, 0, 0, 0),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x21, 0x26, 0x2d)),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3)),
                Tag = prop,
                ToolTip = "Quitar esta propiedad"
            };
            removeBtn.Click += (s, _) =>
            {
                if (s is System.Windows.Controls.Button btn && btn.Tag is ScriptPropertyEntry entry && set.Properties != null)
                {
                    set.Properties.Remove(entry);
                    RefreshScriptPropertiesPanel();
                    PropertyChanged?.Invoke(this, EventArgs.Empty);
                }
            };
            System.Windows.Controls.Grid.SetColumn(keyBox, 0);
            System.Windows.Controls.Grid.SetColumn(typeCombo, 1);
            System.Windows.Controls.Grid.SetColumn(valBox, 2);
            System.Windows.Controls.Grid.SetColumn(removeBtn, 3);
            row.Children.Add(keyBox);
            row.Children.Add(typeCombo);
            row.Children.Add(valBox);
            row.Children.Add(removeBtn);
            ItemsScriptProps.Items.Add(row);
        }
    }

    private void BtnAddScriptProp_OnClick(object sender, RoutedEventArgs e)
    {
        var scriptId = GetSelectedScriptId();
        if (string.IsNullOrEmpty(scriptId) || _target == null) return;
        var set = GetOrCreatePropertySetForScript(scriptId);
        if (set?.Properties == null) return;
        set.Properties.Add(new ScriptPropertyEntry { Key = "nueva", Type = "string", Value = "" });
        RefreshScriptPropertiesPanel();
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void BtnConvertToSeed_OnClick(object sender, RoutedEventArgs e)
    {
        if (_target != null) RequestConvertToSeed?.Invoke(this, _target);
    }

    private void TxtPosition_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        if (double.TryParse(TxtPosX?.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double x)) _target.X = x;
        if (double.TryParse(TxtPosY?.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double y)) _target.Y = y;
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void BtnDuplicate_OnClick(object sender, RoutedEventArgs e)
    {
        if (_target != null) RequestDuplicate?.Invoke(this, _target);
    }

    private void BtnRename_OnClick(object sender, RoutedEventArgs e)
    {
        if (_target != null) RequestRename?.Invoke(this, _target);
    }

    private void BtnDelete_OnClick(object sender, RoutedEventArgs e)
    {
        if (_target != null) RequestDelete?.Invoke(this, _target);
    }
}
