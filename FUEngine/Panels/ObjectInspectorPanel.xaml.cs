using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FUEngine.Core;
using FUEngine.Dialogs;
using FUEngine.Editor;
using SWC = System.Windows.Controls;

namespace FUEngine;

public partial class ObjectInspectorPanel : SWC.UserControl
{
    public const string DataFormatObjectInstanceId = "FUEngine.ObjectInstanceId";

    private ObjectInstance? _target;
    private ObjectLayer? _layer;
    private bool _updating;
    private List<(string Id, string Nombre, string? Path)> _scripts = new();
    private string? _projectDirectory;
    private int _tileSize = 32;
    private DispatcherTimer? _liveVarsTimer;
    private readonly List<(ScriptPropertyEntry prop, SWC.TextBox box)> _liveValueBindings = new();
    private readonly List<(ScriptPropertyEntry prop, SWC.CheckBox chk)> _liveBoolBindings = new();

    /// <summary>Si Play está activo, devuelve snapshot de variables Lua por (instanceId, scriptId).</summary>
    public Func<string, string, IReadOnlyDictionary<string, string>?>? LiveVariablesProvider { get; set; }

    /// <summary>Escribe en el runtime Lua (instanceId, scriptId, clave, tipo, valor texto).</summary>
    public Action<string, string, string, string, string>? LiveVariableWriter { get; set; }

    public event EventHandler? PropertyChanged;
    public event EventHandler<ObjectInstance>? RequestDuplicate;
    public event EventHandler<ObjectInstance>? RequestDelete;
    public event EventHandler<ObjectInstance>? RequestRename;
    public event EventHandler<ObjectInstance>? RequestConvertToSeed;
    public event EventHandler<ObjectInstance>? RequestApplyToSeed;

    public ObjectInspectorPanel()
    {
        InitializeComponent();
        Loaded += (_, _) => EnsureLiveTimer();
        Unloaded += (_, _) => { _liveVarsTimer?.Stop(); };
        InitPivotCombo();
        InitLayerOrderCombo();
        InitEngineColliderCombo();
    }

    private void InitEngineColliderCombo()
    {
        if (CmbColliderShape == null) return;
        CmbColliderShape.Items.Clear();
        CmbColliderShape.Items.Add(new SWC.ComboBoxItem { Content = "Box", Tag = "Box" });
        CmbColliderShape.Items.Add(new SWC.ComboBoxItem { Content = "Circle", Tag = "Circle" });
    }

    private void InitPivotCombo()
    {
        if (CmbPivot == null) return;
        CmbPivot.Items.Clear();
        CmbPivot.Items.Add(new SWC.ComboBoxItem { Content = "(predeterminado)", Tag = "" });
        foreach (var s in new[] { "Center", "Feet", "Top" })
            CmbPivot.Items.Add(new SWC.ComboBoxItem { Content = s, Tag = s });
    }

    private void InitLayerOrderCombo()
    {
        if (CmbLayerOrder == null) return;
        CmbLayerOrder.Items.Clear();
        for (var z = -32; z <= 32; z++)
            CmbLayerOrder.Items.Add(new SWC.ComboBoxItem { Content = z == 0 ? "0 (base)" : z.ToString(), Tag = z });
    }

    public void SetTileSize(int tileSize) => _tileSize = Math.Max(1, tileSize);

    private void EnsureLiveTimer()
    {
        if (_liveVarsTimer != null) return;
        _liveVarsTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        _liveVarsTimer.Tick += LiveVarsTimer_Tick;
    }

    private void LiveVarsTimer_Tick(object? sender, EventArgs e)
    {
        if (LiveVariablesProvider == null || _target == null) return;
        var scriptId = GetSelectedScriptId();
        if (string.IsNullOrEmpty(scriptId)) return;
        var snap = LiveVariablesProvider(_target.InstanceId ?? "", scriptId);
        if (snap == null) return;
        foreach (var (prop, box) in _liveValueBindings)
        {
            if (box.IsKeyboardFocused) continue;
            if (!snap.TryGetValue(prop.Key, out var live)) continue;
            if (string.Equals(box.Text, live, StringComparison.Ordinal)) continue;
            _updating = true;
            box.Text = live;
            prop.Value = live;
            _updating = false;
        }

        foreach (var (prop, chk) in _liveBoolBindings)
            {
            if (chk.IsKeyboardFocused) continue;
            if (!snap.TryGetValue(prop.Key, out var live)) continue;
            var on = live.Equals("true", StringComparison.OrdinalIgnoreCase);
            if (chk.IsChecked == on) continue;
            _updating = true;
            chk.IsChecked = on;
            prop.Value = on ? "true" : "false";
            _updating = false;
        }
    }

    private void RestartLiveTimer()
    {
        EnsureLiveTimer();
        var run = LiveVariablesProvider != null && _target != null && GetSelectedScriptId() is { Length: > 0 };
        if (run) _liveVarsTimer?.Start();
        else _liveVarsTimer?.Stop();
    }

    public void SetProjectDirectory(string? directory)
    {
        _projectDirectory = directory;
    }

    public void SetAvailableScripts(IEnumerable<(string Id, string Nombre, string? Path)> scripts)
    {
        _scripts = scripts?.ToList() ?? new List<(string, string, string?)>();
    }

    private static bool IsHexN(char c) =>
        c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';

    private static string FormatGuidDisplay(string? id)
    {
        if (string.IsNullOrEmpty(id)) return "";
        if (id.Length == 32 && id.All(IsHexN))
            return $"{id[..8]}-{id.Substring(8, 4)}-{id.Substring(12, 4)}-{id.Substring(16, 4)}-{id.Substring(20, 12)}";
        return id;
    }

    private static string ShortId(string? id)
    {
        if (string.IsNullOrEmpty(id)) return "—";
        return id.Length > 12 ? id[..8] + "…" : id;
    }

    private void UpdateGridCoords()
    {
        if (TxtGridCoords == null || _target == null) return;
        var ts = (double)_tileSize;
        var tx = (int)Math.Floor(_target.X / ts);
        var ty = (int)Math.Floor(_target.Y / ts);
        TxtGridCoords.Text = $"tx: {tx}   ty: {ty}   (tile {_tileSize}px)";
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
            if (TxtInstanceId != null) TxtInstanceId.Text = FormatGuidDisplay(instance.InstanceId ?? "");
            if (TxtHeaderId != null) TxtHeaderId.Text = "ID: " + ShortId(instance.InstanceId);
            if (TxtDefinitionId != null) TxtDefinitionId.Text = instance.DefinitionId ?? "";
            TxtNombre.Text = instance.Nombre;
            TxtDefinitionName.Text = "Tipo: " + (def?.Nombre ?? instance.DefinitionId);
            TxtPosX.Text = instance.X.ToString(System.Globalization.CultureInfo.InvariantCulture);
            TxtPosY.Text = instance.Y.ToString(System.Globalization.CultureInfo.InvariantCulture);
            TxtSize.Text = def != null ? $"Tamaño: {def.Width}×{def.Height}" : "Tamaño: —";
            UpdateGridCoords();

            CmbRotation.Items.Clear();
            foreach (var d in new[] { 0, 90, 180, 270 })
                CmbRotation.Items.Add(new SWC.ComboBoxItem { Content = d + "°", Tag = (double)d });
            var rot = (int)Math.Round(instance.Rotation) % 360;
            if (rot < 0) rot += 360;
            var idx = rot switch { 0 => 0, 90 => 1, 180 => 2, 270 => 3, _ => -1 };
            if (idx >= 0) CmbRotation.SelectedIndex = idx;
            else { CmbRotation.SelectedIndex = -1; TxtRotationCustom.Text = instance.Rotation.ToString(System.Globalization.CultureInfo.InvariantCulture); }
            if (TxtRotationCustom != null) TxtRotationCustom.Text = idx >= 0 ? "" : instance.Rotation.ToString(System.Globalization.CultureInfo.InvariantCulture);

            TxtScaleX.Text = instance.ScaleX.ToString(System.Globalization.CultureInfo.InvariantCulture);
            TxtScaleY.Text = instance.ScaleY.ToString(System.Globalization.CultureInfo.InvariantCulture);

            if (CmbLayerOrder != null)
            {
                var lo = instance.LayerOrder;
                var found = false;
                for (var i = 0; i < CmbLayerOrder.Items.Count; i++)
                {
                    if (CmbLayerOrder.Items[i] is SWC.ComboBoxItem ci && ci.Tag is int z && z == lo)
                    {
                        CmbLayerOrder.SelectedIndex = i;
                        found = true;
                        break;
                    }
                }

                if (!found && CmbLayerOrder.Items.Count > 0)
                    CmbLayerOrder.SelectedIndex = Math.Clamp(lo + 32, 0, CmbLayerOrder.Items.Count - 1);
            }

            TxtTags.Text = instance.Tags != null && instance.Tags.Count > 0 ? string.Join(", ", instance.Tags) : "";

            if (ChkEnabled != null) ChkEnabled.IsChecked = instance.Enabled;

            if (CmbPivot != null)
            {
                var p = instance.Pivot ?? "";
                CmbPivot.SelectedIndex = 0;
                for (var i = 0; i < CmbPivot.Items.Count; i++)
                {
                    if (CmbPivot.Items[i] is SWC.ComboBoxItem ci && (ci.Tag as string ?? "") == p)
                    {
                        CmbPivot.SelectedIndex = i;
                        break;
                    }
                }
            }

            if (ChkPointLight != null)
            {
                ChkPointLight.IsChecked = instance.PointLightEnabled;
                TxtPointLightRadius.Text = instance.PointLightRadius.ToString(System.Globalization.CultureInfo.InvariantCulture);
                TxtPointLightIntensity.Text = instance.PointLightIntensity.ToString(System.Globalization.CultureInfo.InvariantCulture);
                TxtPointLightColor.Text = instance.PointLightColorHex ?? "#ffffff";
            }

            LoadEngineComponentsToUi(instance);

            CmbCollisionType.Items.Clear();
            CmbCollisionType.Items.Add(new SWC.ComboBoxItem { Content = "Sólido", Tag = "Solid" });
            CmbCollisionType.Items.Add(new SWC.ComboBoxItem { Content = "Trigger", Tag = "Trigger" });
            CmbCollisionType.Items.Add(new SWC.ComboBoxItem { Content = "Superficie", Tag = "Surface" });
            var ct = instance.CollisionType ?? "Solid";
            for (int i = 0; i < CmbCollisionType.Items.Count; i++)
            {
                if (CmbCollisionType.Items[i] is SWC.ComboBoxItem cti && (cti.Tag as string) == ct)
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
                var name = _scripts.FirstOrDefault(s => s.Id == sid).Nombre ?? sid;
                LstScripts.Items.Add(new SWC.ListBoxItem { Content = name, Tag = sid });
            }
            CmbScriptAdd.Items.Clear();
            CmbScriptAdd.Items.Add(new SWC.ComboBoxItem { Content = "(seleccionar…)", Tag = (string?)null });
            foreach (var (id, nombre, _) in _scripts)
                CmbScriptAdd.Items.Add(new SWC.ComboBoxItem { Content = nombre, Tag = id });
            CmbScriptAdd.SelectedIndex = 0;
            if (LstScripts.Items.Count > 0) LstScripts.SelectedIndex = 0;
            RefreshScriptPropertiesPanel();

            if (ImgPreview != null && PreviewBorder != null)
            {
                ImgPreview.Source = null;
                PreviewBorder.Visibility = Visibility.Visible;
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
                        }
                        catch { /* ignore */ }
                    }
                }
            }
            if (BtnApplyToSeed != null)
                BtnApplyToSeed.Visibility = !string.IsNullOrEmpty(instance.SourceSeedId) ? Visibility.Visible : Visibility.Collapsed;
        }
        _updating = false;
    }

    private void BtnApplyToSeed_OnClick(object sender, RoutedEventArgs e)
    {
        if (_target != null) RequestApplyToSeed?.Invoke(this, _target);
    }

    private void TxtNombre_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        _target.Nombre = TxtNombre.Text ?? "";
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
                TxtInstanceId.Text = FormatGuidDisplay(candidate);
                if (TxtHeaderId != null) TxtHeaderId.Text = "ID: " + ShortId(candidate);
                _updating = false;
                PropertyChanged?.Invoke(this, EventArgs.Empty);
                EditorLog.Toast("ID generado: " + candidate, LogLevel.Info, "Inspector");
                return;
            }
        }
        var fallback = prefix + "_" + Guid.NewGuid().ToString("N")[..8];
        _updating = true;
        _target.InstanceId = fallback;
        TxtInstanceId.Text = FormatGuidDisplay(fallback);
        if (TxtHeaderId != null) TxtHeaderId.Text = "ID: " + ShortId(fallback);
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

    private void ChkEnabled_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_updating || _target == null) return;
        _target.Enabled = ChkEnabled?.IsChecked != false;
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void CmbPivot_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || _target == null || CmbPivot.SelectedItem is not ComboBoxItem ci) return;
        _target.Pivot = ci.Tag as string ?? "";
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void CmbLayerOrder_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || _target == null || CmbLayerOrder.SelectedItem is not ComboBoxItem ci || ci.Tag is not int z) return;
        _target.LayerOrder = z;
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ChkPointLight_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_updating || _target == null) return;
        _target.PointLightEnabled = ChkPointLight?.IsChecked == true;
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void TxtPointLight_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        if (float.TryParse(TxtPointLightRadius?.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var r) && r > 0)
            _target.PointLightRadius = r;
        if (float.TryParse(TxtPointLightIntensity?.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var i) && i > 0)
            _target.PointLightIntensity = i;
        _target.PointLightColorHex = (TxtPointLightColor?.Text ?? "#ffffff").Trim();
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
        if (_updating || _target == null || CmbRotation.SelectedItem is not ComboBoxItem item || item.Tag is not double d) return;
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

    private void TxtTags_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        var t = TxtTags?.Text ?? "";
        _target.Tags = t.Split(',', StringSplitOptions.TrimEntries).Where(s => s.Length > 0).ToList();
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void CmbCollisionType_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || _target == null || CmbCollisionType.SelectedItem is not ComboBoxItem item) return;
        _target.CollisionType = item.Tag as string ?? "Solid";
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void AddScriptIdToTarget(string id)
    {
        if (_target == null || string.IsNullOrEmpty(id)) return;
        if (_target.ScriptIds == null) _target.ScriptIds = new List<string>();
        if (_target.ScriptIds.Contains(id)) return;
        _target.ScriptIds.Add(id);
        if (_target.ScriptProperties == null) _target.ScriptProperties = new List<ScriptInstancePropertySet>();
        if (_target.ScriptProperties.All(sp => sp.ScriptId != id))
            _target.ScriptProperties.Add(new ScriptInstancePropertySet { ScriptId = id, Properties = new List<ScriptPropertyEntry>() });
        if (_target.ScriptIds.Count == 1) _target.ScriptIdOverride = id;
        var name = _scripts.FirstOrDefault(s => s.Id == id).Nombre ?? id;
        LstScripts.Items.Add(new SWC.ListBoxItem { Content = name, Tag = id });
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void BtnAddScript_OnClick(object sender, RoutedEventArgs e)
    {
        if (_updating || _target == null || CmbScriptAdd.SelectedItem is not ComboBoxItem item) return;
        var id = item.Tag as string;
        if (string.IsNullOrEmpty(id)) return;
        AddScriptIdToTarget(id);
    }

    private void BtnRemoveScript_OnClick(object sender, RoutedEventArgs e)
    {
        if (_updating || _target == null || LstScripts.SelectedItem is not ListBoxItem li) return;
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
        if (LstScripts?.SelectedItem is SWC.ListBoxItem li && li.Tag is string id) return id;
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
                TxtScriptValidation.Text = "Script no encontrado en el proyecto.";
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
        _liveValueBindings.Clear();
        _liveBoolBindings.Clear();
        ItemsScriptProps.Items.Clear();
        TxtScriptPropsHint.Visibility = Visibility.Visible;
        var scriptId = GetSelectedScriptId();
        if (string.IsNullOrEmpty(scriptId) || _target == null)
        {
            if (TxtScriptPropsHint != null)
                TxtScriptPropsHint.Text = "Seleccione un script. Use -- @prop en el .lua o variables globales en la raíz (sin local). Con Play activo, sincronización en caliente. Tablas del motor: " + string.Join(", ", LuaScriptVariableDiscovery.MotorGlobalNames) + ".";
            RestartLiveTimer();
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
                    var parsed = LuaScriptVariableParser.ParseMergedForInspector(code);
                    foreach (var (name, type, defaultValue) in parsed)
                    {
                        if (set.Properties.Any(p => string.Equals(p.Key, name, StringComparison.OrdinalIgnoreCase)))
                            continue;
                        set.Properties.Add(new ScriptPropertyEntry { Key = name, Type = type, Value = defaultValue });
                    }
                }
                catch { /* ignore */ }
            }
        }
        var types = new[] { "string", "int", "float", "bool", "Vector2", "Color", "object" };
        foreach (var prop in set.Properties)
        {
            var row = new SWC.Grid();
            row.Margin = new Thickness(0, 0, 0, 4);
            row.ColumnDefinitions.Add(new SWC.ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new SWC.ColumnDefinition { Width = new GridLength(72) });
            row.ColumnDefinitions.Add(new SWC.ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new SWC.ColumnDefinition { Width = GridLength.Auto });
            var keyBox = new SWC.TextBox
            {
                Text = prop.Key,
                Padding = new Thickness(4, 2, 4, 2),
                Background = System.Windows.Media.Brushes.Transparent,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x30, 0x36, 0x3d)),
                Tag = prop,
                ToolTip = "Nombre de la propiedad"
            };
            keyBox.TextChanged += (_, _) => { if (!_updating && keyBox.Tag is ScriptPropertyEntry pe) { pe.Key = keyBox.Text ?? ""; PropertyChanged?.Invoke(this, EventArgs.Empty); } };
            var typeCombo = new SWC.ComboBox
            {
                Padding = new Thickness(4, 2, 4, 2),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x0d, 0x11, 0x17)),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x30, 0x36, 0x3d)),
                Tag = prop,
                ToolTip = "Tipo"
            };
            foreach (var t in types) typeCombo.Items.Add(new SWC.ComboBoxItem { Content = t, Tag = t });
            var typeIdx = Array.FindIndex(types, x => string.Equals(x, prop.Type, StringComparison.OrdinalIgnoreCase));
            typeCombo.SelectedIndex = typeIdx >= 0 ? typeIdx : 0;
            typeCombo.SelectionChanged += (_, _) =>
            {
                if (_updating) return;
                if (typeCombo.SelectedItem is SWC.ComboBoxItem ci && ci.Tag is string ty && typeCombo.Tag is ScriptPropertyEntry pe)
                {
                    pe.Type = ty;
                    RefreshScriptPropertiesPanel();
                    PropertyChanged?.Invoke(this, EventArgs.Empty);
                }
            };
            var tyLower = (prop.Type ?? "string").ToLowerInvariant();
            System.Windows.UIElement valueControl;
            if (tyLower == "bool")
            {
                var chk = new SWC.CheckBox
                {
                    Content = "",
                    IsChecked = prop.Value.Equals("true", StringComparison.OrdinalIgnoreCase),
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3)),
                    Tag = prop,
                    VerticalAlignment = VerticalAlignment.Center
                };
                chk.Checked += (_, _) => { if (!_updating && chk.Tag is ScriptPropertyEntry pe) { pe.Value = chk.IsChecked == true ? "true" : "false"; PropertyChanged?.Invoke(this, EventArgs.Empty); PushLive(pe); } };
                chk.Unchecked += (_, _) => { if (!_updating && chk.Tag is ScriptPropertyEntry pe) { pe.Value = "false"; PropertyChanged?.Invoke(this, EventArgs.Empty); PushLive(pe); } };
                _liveBoolBindings.Add((prop, chk));
                valueControl = chk;
            }
            else
            {
                var valBox = new SWC.TextBox
                {
                    Text = prop.Value,
                    Padding = new Thickness(4, 2, 4, 2),
                    Background = System.Windows.Media.Brushes.Transparent,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3)),
                    BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x30, 0x36, 0x3d)),
                    Tag = prop,
                    ToolTip = tyLower == "object" ? "InstanceId del objeto (arrastre desde jerarquía)" : "Valor"
                };
                if (tyLower == "object")
                {
                    valBox.AllowDrop = true;
                    valBox.PreviewDragOver += (_, de) =>
                    {
                        if (de.Data.GetDataPresent(DataFormatObjectInstanceId)) { de.Effects = System.Windows.DragDropEffects.Copy; de.Handled = true; }
                    };
                    valBox.Drop += (_, de) =>
                    {
                        if (de.Data.GetData(DataFormatObjectInstanceId) is string iid && !string.IsNullOrEmpty(iid))
                        {
                            valBox.Text = iid;
                            prop.Value = iid;
                            PropertyChanged?.Invoke(this, EventArgs.Empty);
                            PushLive(prop);
                        }
                    };
                }
                valBox.TextChanged += (_, _) => { if (!_updating && valBox.Tag is ScriptPropertyEntry pe) { pe.Value = valBox.Text ?? ""; PropertyChanged?.Invoke(this, EventArgs.Empty); } };
                _liveValueBindings.Add((prop, valBox));
                valBox.LostFocus += (_, _) => PushLive(prop);
                valueControl = valBox;
            }

            void PushLive(ScriptPropertyEntry pe)
            {
                if (_updating || _target == null || LiveVariableWriter == null) return;
                var sid = GetSelectedScriptId();
                if (string.IsNullOrEmpty(sid)) return;
                LiveVariableWriter(_target.InstanceId ?? "", sid, pe.Key, pe.Type ?? "string", pe.Value ?? "");
            }

            var removeBtn = new SWC.Button
            {
                Content = "✕",
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(4, 0, 0, 0),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x21, 0x26, 0x2d)),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3)),
                Tag = prop,
                ToolTip = "Quitar"
            };
            removeBtn.Click += (_, _) =>
            {
                if (removeBtn.Tag is ScriptPropertyEntry entry && set.Properties != null)
                {
                    set.Properties.Remove(entry);
                    RefreshScriptPropertiesPanel();
                    PropertyChanged?.Invoke(this, EventArgs.Empty);
                }
            };
            SWC.Grid.SetColumn(keyBox, 0);
            SWC.Grid.SetColumn(typeCombo, 1);
            SWC.Grid.SetColumn(valueControl, 2);
            SWC.Grid.SetColumn(removeBtn, 3);
            row.Children.Add(keyBox);
            row.Children.Add(typeCombo);
            row.Children.Add(valueControl);
            row.Children.Add(removeBtn);
            ItemsScriptProps.Items.Add(row);
        }
        RestartLiveTimer();
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

    private void LoadEngineComponentsToUi(ObjectInstance instance)
    {
        if (TxtSpriteTint == null) return;
        TxtSpriteTint.Text = instance.SpriteColorTintHex ?? "#ffffff";
        ChkSpriteFlipX!.IsChecked = instance.SpriteFlipX;
        ChkSpriteFlipY!.IsChecked = instance.SpriteFlipY;
        TxtSpriteSortOffset!.Text = instance.SpriteSortOffset.ToString(CultureInfo.InvariantCulture);
        TxtDefaultAnimClip!.Text = instance.DefaultAnimationClipId ?? "";
        ChkAnimAutoPlay!.IsChecked = instance.AnimationAutoPlay;
        TxtAnimSpeedMult!.Text = instance.AnimationSpeedMultiplier.ToString(CultureInfo.InvariantCulture);
        var shape = (instance.ColliderShape ?? "Box").Trim();
        if (CmbColliderShape != null)
        {
            var found = false;
            for (var i = 0; i < CmbColliderShape.Items.Count; i++)
            {
                if (CmbColliderShape.Items[i] is SWC.ComboBoxItem ci && string.Equals(ci.Tag as string, shape, StringComparison.OrdinalIgnoreCase))
                {
                    CmbColliderShape.SelectedIndex = i;
                    found = true;
                    break;
                }
            }
            if (!found) CmbColliderShape.SelectedIndex = 0;
        }
        TxtColliderBoxW!.Text = instance.ColliderBoxWidthTiles.ToString(CultureInfo.InvariantCulture);
        TxtColliderBoxH!.Text = instance.ColliderBoxHeightTiles.ToString(CultureInfo.InvariantCulture);
        TxtColliderRadius!.Text = instance.ColliderCircleRadiusTiles.ToString(CultureInfo.InvariantCulture);
        TxtColliderOffX!.Text = instance.ColliderOffsetX.ToString(CultureInfo.InvariantCulture);
        TxtColliderOffY!.Text = instance.ColliderOffsetY.ToString(CultureInfo.InvariantCulture);
        ChkRigidbody!.IsChecked = instance.RigidbodyEnabled;
        TxtRbMass!.Text = instance.RigidbodyMass.ToString(CultureInfo.InvariantCulture);
        TxtRbGravScale!.Text = instance.RigidbodyGravityScale.ToString(CultureInfo.InvariantCulture);
        TxtRbDrag!.Text = instance.RigidbodyDrag.ToString(CultureInfo.InvariantCulture);
        ChkCameraTarget!.IsChecked = instance.CameraTargetEnabled;
        ChkProximity!.IsChecked = instance.ProximitySensorEnabled;
        TxtProxRange!.Text = instance.ProximityDetectionRangeTiles.ToString(CultureInfo.InvariantCulture);
        TxtProxTag!.Text = instance.ProximityTargetTag ?? "player";
        ChkHealth!.IsChecked = instance.HealthEnabled;
        TxtHealthMax!.Text = instance.HealthMax.ToString(CultureInfo.InvariantCulture);
        TxtHealthCurrent!.Text = instance.HealthCurrent.ToString(CultureInfo.InvariantCulture);
        ChkAudioSource!.IsChecked = instance.AudioSourceEnabled;
        TxtAudioClipId!.Text = instance.AudioClipId ?? "";
        ChkParticleEmitter!.IsChecked = instance.ParticleEmitterEnabled;
        TxtParticleTexture!.Text = instance.ParticleTexturePath ?? "";
    }

    private void EngineComponents_Any(object sender, EventArgs e) => CommitEngineComponentsFromUi();

    private void CommitEngineComponentsFromUi()
    {
        if (_updating || _target == null) return;
        _target.SpriteColorTintHex = (TxtSpriteTint?.Text ?? "#ffffff").Trim();
        if (string.IsNullOrWhiteSpace(_target.SpriteColorTintHex)) _target.SpriteColorTintHex = "#ffffff";
        _target.SpriteFlipX = ChkSpriteFlipX?.IsChecked == true;
        _target.SpriteFlipY = ChkSpriteFlipY?.IsChecked == true;
        if (int.TryParse(TxtSpriteSortOffset?.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var soff))
            _target.SpriteSortOffset = soff;
        _target.DefaultAnimationClipId = string.IsNullOrWhiteSpace(TxtDefaultAnimClip?.Text) ? null : TxtDefaultAnimClip.Text.Trim();
        _target.AnimationAutoPlay = ChkAnimAutoPlay?.IsChecked != false;
        if (float.TryParse(TxtAnimSpeedMult?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var am) && am > 0)
            _target.AnimationSpeedMultiplier = am;
        if (CmbColliderShape?.SelectedItem is SWC.ComboBoxItem csp && csp.Tag is string cst)
            _target.ColliderShape = cst;
        if (float.TryParse(TxtColliderBoxW?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var cw))
            _target.ColliderBoxWidthTiles = cw;
        if (float.TryParse(TxtColliderBoxH?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var ch))
            _target.ColliderBoxHeightTiles = ch;
        if (float.TryParse(TxtColliderRadius?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var cr))
            _target.ColliderCircleRadiusTiles = cr > 0 ? cr : 0.5f;
        if (float.TryParse(TxtColliderOffX?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var ox))
            _target.ColliderOffsetX = ox;
        if (float.TryParse(TxtColliderOffY?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var oy))
            _target.ColliderOffsetY = oy;
        _target.RigidbodyEnabled = ChkRigidbody?.IsChecked == true;
        if (float.TryParse(TxtRbMass?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var mass) && mass > 0)
            _target.RigidbodyMass = mass;
        if (float.TryParse(TxtRbGravScale?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var gs))
            _target.RigidbodyGravityScale = gs;
        if (float.TryParse(TxtRbDrag?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var drag))
            _target.RigidbodyDrag = drag;
        _target.CameraTargetEnabled = ChkCameraTarget?.IsChecked == true;
        _target.ProximitySensorEnabled = ChkProximity?.IsChecked == true;
        if (float.TryParse(TxtProxRange?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var pr) && pr > 0)
            _target.ProximityDetectionRangeTiles = pr;
        _target.ProximityTargetTag = string.IsNullOrWhiteSpace(TxtProxTag?.Text) ? "player" : TxtProxTag.Text.Trim();
        _target.HealthEnabled = ChkHealth?.IsChecked == true;
        if (float.TryParse(TxtHealthMax?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var hm) && hm > 0)
            _target.HealthMax = hm;
        if (float.TryParse(TxtHealthCurrent?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var hc))
            _target.HealthCurrent = hc;
        _target.AudioSourceEnabled = ChkAudioSource?.IsChecked == true;
        _target.AudioClipId = string.IsNullOrWhiteSpace(TxtAudioClipId?.Text) ? null : TxtAudioClipId.Text.Trim();
        _target.ParticleEmitterEnabled = ChkParticleEmitter?.IsChecked == true;
        _target.ParticleTexturePath = string.IsNullOrWhiteSpace(TxtParticleTexture?.Text) ? null : TxtParticleTexture.Text.Trim().Replace('\\', '/');
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void BtnAddComponent_OnClick(object sender, RoutedEventArgs e)
    {
        if (_target == null) return;
        var items = new List<AddComponentPickerWindow.ComponentPickItem>
        {
            new() { Category = "Rendering", Id = "sprite_visibility", Title = "Visibilidad / sprite", Description = "Activa el checkbox «Visible en mapa».", Enabled = true },
            new() { Category = "Rendering", Id = "sprite_renderer", Title = "SpriteRenderer (tinte / flip)", Description = "Expander Sprite, física y gameplay.", Enabled = true },
            new() { Category = "Physics", Id = "collision", Title = "Colisión básica", Description = "Marca colisión y tipo sólido/trigger.", Enabled = true },
            new() { Category = "Physics", Id = "rigidbody", Title = "Rigidbody", Description = "Gravedad del proyecto + velocidad.", Enabled = true },
            new() { Category = "Lights", Id = "point_light", Title = "Luz puntual", Description = "Añade LightComponent en Play.", Enabled = true },
            new() { Category = "Scripts", Id = "lua_script", Title = "Asignar script Lua", Description = "Enfoca la lista de scripts.", Enabled = true },
            new() { Category = "Audio", Id = "audio_source", Title = "AudioSource", Description = "Metadatos + audio.play en Lua.", Enabled = true },
            new() { Category = "Rendering", Id = "anim_player", Title = "AnimationPlayer", Description = "Clip por defecto y self.playAnimation.", Enabled = true },
            new() { Category = "Gameplay", Id = "proximity", Title = "ProximitySensor", Description = "Distancia y etiqueta objetivo.", Enabled = true },
            new() { Category = "Gameplay", Id = "health", Title = "Health", Description = "Vida máxima / actual.", Enabled = true },
            new() { Category = "Rendering", Id = "camera_target", Title = "CameraTarget", Description = "La cámara sigue este objeto.", Enabled = true },
            new() { Category = "Rendering", Id = "particles", Title = "ParticleEmitter (datos)", Description = "Persistencia; visor ampliación.", Enabled = true },
        };
        var w = new AddComponentPickerWindow(items) { Owner = Window.GetWindow(this) };
        if (w.ShowDialog() != true || string.IsNullOrEmpty(w.SelectedId)) return;
        switch (w.SelectedId)
        {
            case "sprite_visibility":
                ChkVisible.IsChecked = true;
                ExpanderTransform.IsExpanded = true;
                ChkVisible.Focus();
                break;
            case "collision":
                ChkColision.IsChecked = true;
                ChkColision.Focus();
                break;
            case "point_light":
                _target.PointLightEnabled = true;
                if (ChkPointLight != null) ChkPointLight.IsChecked = true;
                ExpanderPointLight.IsExpanded = true;
                PropertyChanged?.Invoke(this, EventArgs.Empty);
                break;
            case "lua_script":
                ExpanderScripts.IsExpanded = true;
                CmbScriptAdd.Focus();
                break;
            case "sprite_renderer":
                ExpanderEngineComponents.IsExpanded = true;
                TxtSpriteTint?.Focus();
                break;
            case "rigidbody":
                if (ChkRigidbody != null) ChkRigidbody.IsChecked = true;
                ExpanderEngineComponents.IsExpanded = true;
                break;
            case "audio_source":
                if (ChkAudioSource != null) ChkAudioSource.IsChecked = true;
                ExpanderEngineComponents.IsExpanded = true;
                TxtAudioClipId?.Focus();
                break;
            case "anim_player":
                ExpanderEngineComponents.IsExpanded = true;
                TxtDefaultAnimClip?.Focus();
                break;
            case "proximity":
                if (ChkProximity != null) ChkProximity.IsChecked = true;
                ExpanderEngineComponents.IsExpanded = true;
                break;
            case "health":
                if (ChkHealth != null) ChkHealth.IsChecked = true;
                ExpanderEngineComponents.IsExpanded = true;
                break;
            case "camera_target":
                if (ChkCameraTarget != null) ChkCameraTarget.IsChecked = true;
                ExpanderEngineComponents.IsExpanded = true;
                break;
            case "particles":
                if (ChkParticleEmitter != null) ChkParticleEmitter.IsChecked = true;
                ExpanderEngineComponents.IsExpanded = true;
                break;
        }
    }

    private void ObjectInspector_OnPreviewDragOver(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormatObjectInstanceId))
            e.Effects = System.Windows.DragDropEffects.Copy;
        else
            e.Effects = System.Windows.DragDropEffects.None;
        e.Handled = true;
    }

    private void ObjectInspector_OnDrop(object sender, System.Windows.DragEventArgs e)
    {
        if (_target == null) return;
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop) && e.Data.GetData(System.Windows.DataFormats.FileDrop) is string[] files)
        {
            foreach (var f in files)
            {
                if (!f.EndsWith(".lua", StringComparison.OrdinalIgnoreCase)) continue;
                TryAddScriptFromLuaFile(f);
            }
            e.Handled = true;
            return;
        }
    }

    private void TryAddScriptFromLuaFile(string fullPath)
    {
        if (string.IsNullOrEmpty(_projectDirectory)) return;
        string rel;
        try { rel = Path.GetRelativePath(_projectDirectory, fullPath); }
        catch { return; }
        rel = rel.Replace('\\', '/');
        var match = _scripts.FirstOrDefault(s => s.Path != null && s.Path.Replace('\\', '/').Equals(rel, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(match.Id))
        {
            EditorLog.Toast("El .lua no está registrado en scripts.json.", LogLevel.Warning, "Inspector");
            return;
        }
        AddScriptIdToTarget(match.Id);
        EditorLog.Toast("Script añadido: " + match.Nombre, LogLevel.Info, "Inspector");
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
        UpdateGridCoords();
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
