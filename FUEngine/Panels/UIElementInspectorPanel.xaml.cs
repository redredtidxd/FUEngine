using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;
using FUEngine.Core;
using UIElementCore = FUEngine.Core.UIElement;

namespace FUEngine;

public partial class UIElementInspectorPanel : System.Windows.Controls.UserControl
{
    private UIRoot? _root;
    private UICanvas? _canvas;
    private UIElementCore? _target;
    private string? _projectDirectory;
    private bool _updating;
    private bool _fontListPopulated;

    public event EventHandler? PropertyChanged;

    public UIElementInspectorPanel()
    {
        InitializeComponent();
        CmbKind.Items.Clear();
        foreach (var kind in new[] { UIElementKind.Button, UIElementKind.Text, UIElementKind.Image, UIElementKind.Panel, UIElementKind.TabControl })
            CmbKind.Items.Add(new ComboBoxItem { Content = kind.ToString(), Tag = kind });

        CmbFontUnit.Items.Clear();
        CmbFontUnit.Items.Add(new ComboBoxItem { Content = "Pixels", Tag = UITextFontUnit.Pixels });
        CmbFontUnit.Items.Add(new ComboBoxItem { Content = "Points", Tag = UITextFontUnit.Points });
        CmbTextAlign.Items.Clear();
        CmbTextAlign.Items.Add(new ComboBoxItem { Content = "Left", Tag = UITextAlignmentKind.Left });
        CmbTextAlign.Items.Add(new ComboBoxItem { Content = "Center", Tag = UITextAlignmentKind.Center });
        CmbTextAlign.Items.Add(new ComboBoxItem { Content = "Right", Tag = UITextAlignmentKind.Right });
        CmbTextAlign.Items.Add(new ComboBoxItem { Content = "Justify", Tag = UITextAlignmentKind.Justify });
        CmbOverflow.Items.Clear();
        CmbOverflow.Items.Add(new ComboBoxItem { Content = "Clip", Tag = UITextOverflowMode.Clip });
        CmbOverflow.Items.Add(new ComboBoxItem { Content = "ScaleToFit", Tag = UITextOverflowMode.ScaleToFit });
        CmbOverflow.Items.Add(new ComboBoxItem { Content = "Ellipsis", Tag = UITextOverflowMode.Ellipsis });
        CmbTwSoundTrigger.Items.Clear();
        CmbTwSoundTrigger.Items.Add(new ComboBoxItem { Content = "Cada carácter", Tag = UITypewriterSoundTrigger.EachCharacter });
        CmbTwSoundTrigger.Items.Add(new ComboBoxItem { Content = "Solo espacios", Tag = UITypewriterSoundTrigger.SpacesOnly });
        BuildTextPivotPresetCombo();
        BuildTextAnchorPresetGrid();
    }

    public void SetTarget(UICanvas? canvas, UIElementCore? element, UIRoot? root, string? projectDirectory = null)
    {
        _canvas = canvas;
        _target = element;
        _root = root;
        _projectDirectory = projectDirectory;
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
        TxtLocalizationKey.Text = element.LocalizationKey ?? "";
        TxtTextStyleProfile.Text = element.TextStyleProfilePath ?? "";
        TxtTypewriterProfile.Text = element.TypewriterProfilePath ?? "";
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
        UpdateTextTypographyVisibility();
        RefreshTextTypographyFromTarget();
        RefreshTextAnchorFromTarget();
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
        UpdateTextTypographyVisibility();
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

    private void TxtLocalizationKey_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        _target.LocalizationKey = (TxtLocalizationKey.Text ?? "").Trim();
        OnElementChanged();
    }

    private void TxtTextStyleProfile_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        _target.TextStyleProfilePath = (TxtTextStyleProfile.Text ?? "").Trim().Replace('/', Path.DirectorySeparatorChar);
        OnElementChanged();
    }

    private void TxtTypewriterProfile_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        _target.TypewriterProfilePath = (TxtTypewriterProfile.Text ?? "").Trim().Replace('/', Path.DirectorySeparatorChar);
        OnElementChanged();
    }

    private void ChkGlyphFx_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_updating || _target == null) return;
        PushTextStyleFromUi();
    }

    private void SliderGlyphFx_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating || _target == null) return;
        PushTextStyleFromUi();
    }

    private void GlyphFxText_OnChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        PushTextStyleFromUi();
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
        SetTarget(_canvas, _target, _root, _projectDirectory);
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
        SetTarget(_canvas, _target, _root, _projectDirectory);
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

    private static bool TryParseInt(string? value, out int parsed) =>
        int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed);

    private void UpdateTextTypographyVisibility()
    {
        if (_target == null)
        {
            PanelTextTypography.Visibility = Visibility.Collapsed;
            return;
        }
        PanelTextTypography.Visibility = UIElementCore.SupportsRichTextComponents(_target.Kind)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void RefreshTextTypographyFromTarget()
    {
        if (_target == null) return;
        var s = _target.TextStyle ?? new UITextStyle();
        var l = _target.TextLayout ?? new UITextLayoutSettings();
        var tw = _target.Typewriter ?? new UITypewriterSettings();
        CmbFontFamily.Text = s.FontFamily;
        TxtFontSize.Text = s.FontSize.ToString(CultureInfo.InvariantCulture);
        SelectComboByTag(CmbFontUnit, s.FontSizeUnit);
        TxtTextColor.Text = s.Color;
        SliderTextOpacity.Value = Math.Clamp(s.Opacity, 0, 1);
        SelectComboByTag(CmbTextAlign, s.Alignment);
        ChkKerning.IsChecked = s.EnableKerning;
        SliderOutline.Value = Math.Clamp(s.OutlineThickness, 0, 6);
        TxtOutlineColor.Text = s.OutlineColor;
        TxtShadowX.Text = s.ShadowOffsetX.ToString(CultureInfo.InvariantCulture);
        TxtShadowY.Text = s.ShadowOffsetY.ToString(CultureInfo.InvariantCulture);
        TxtShadowColor.Text = s.ShadowColor;
        SliderShadowBlur.Value = Math.Clamp(s.ShadowBlur, 0, 24);
        TxtLineSpacing.Text = s.LineSpacing.ToString(CultureInfo.InvariantCulture);
        TxtLetterSpacing.Text = s.LetterSpacing.ToString(CultureInfo.InvariantCulture);
        ChkRichText.IsChecked = s.RichTextEnabled;
        ChkWordWrap.IsChecked = l.WordWrap;
        ChkHyphenation.IsChecked = l.HyphenationEnabled;
        TxtHyphenMin.Text = l.HyphenMinPrefixChars.ToString(CultureInfo.InvariantCulture);
        SelectComboByTag(CmbOverflow, l.OverflowMode);
        ChkTwEnabled.IsChecked = tw.Enabled;
        SliderTwCps.Value = Math.Clamp(tw.CharsPerSecond, 1, 120);
        ChkTwFade.IsChecked = tw.FadeInPerChar;
        TxtTwFadeDur.Text = tw.FadeInDurationSeconds.ToString(CultureInfo.InvariantCulture);
        ChkTwPause.IsChecked = tw.PunctuationPausesEnabled;
        TxtTwPauseComma.Text = tw.PauseAfterCommaSeconds.ToString(CultureInfo.InvariantCulture);
        TxtTwPausePeriod.Text = tw.PauseAfterPeriodSeconds.ToString(CultureInfo.InvariantCulture);
        TxtTwPauseQuestion.Text = tw.PauseAfterQuestionSeconds.ToString(CultureInfo.InvariantCulture);
        TxtTwPauseExcl.Text = tw.PauseAfterExclamationSeconds.ToString(CultureInfo.InvariantCulture);
        TxtTwSoundPath.Text = tw.SoundPath;
        SelectComboByTag(CmbTwSoundTrigger, tw.SoundTrigger);
        SliderTwSoundVol.Value = Math.Clamp(tw.SoundVolume, 0, 2);
        var g = s.GlyphEffects;
        if (g == null)
        {
            ChkGeShake.IsChecked = false;
            SliderGeShake.Value = 2;
            ChkGeWave.IsChecked = false;
            SliderGeWaveAmp.Value = 4;
            TxtGeWaveFreq.Text = "1.2";
            ChkGeRainbow.IsChecked = false;
            SliderGeRainbow.Value = 0.25;
        }
        else
        {
            ChkGeShake.IsChecked = g.ShakeEnabled;
            SliderGeShake.Value = Math.Clamp(g.ShakeIntensityPixels, 0, 8);
            ChkGeWave.IsChecked = g.WaveEnabled;
            SliderGeWaveAmp.Value = Math.Clamp(g.WaveAmplitudePixels, 0, 16);
            TxtGeWaveFreq.Text = g.WaveFrequency.ToString(CultureInfo.InvariantCulture);
            ChkGeRainbow.IsChecked = g.RainbowEnabled;
            SliderGeRainbow.Value = Math.Clamp(g.RainbowCyclesPerSecond, 0, 2);
        }
    }

    private static void SelectComboByTag(System.Windows.Controls.ComboBox c, object tag)
    {
        for (var i = 0; i < c.Items.Count; i++)
        {
            if (c.Items[i] is ComboBoxItem item && item.Tag?.Equals(tag) == true)
            {
                c.SelectedIndex = i;
                return;
            }
        }
        if (c.Items.Count > 0) c.SelectedIndex = 0;
    }

    private void BuildTextPivotPresetCombo()
    {
        CmbTextPivotPreset.Items.Clear();
        foreach (UITextPivotPreset p in Enum.GetValues<UITextPivotPreset>())
        {
            var label = p switch
            {
                UITextPivotPreset.TopLeft => "Arriba-izquierda",
                UITextPivotPreset.TopCenter => "Arriba-centro",
                UITextPivotPreset.TopRight => "Arriba-derecha",
                UITextPivotPreset.MiddleLeft => "Centro-izquierda",
                UITextPivotPreset.Center => "Centro",
                UITextPivotPreset.MiddleRight => "Centro-derecha",
                UITextPivotPreset.BottomLeft => "Abajo-izquierda",
                UITextPivotPreset.BottomCenter => "Abajo-centro",
                UITextPivotPreset.BottomRight => "Abajo-derecha",
                _ => p.ToString()
            };
            CmbTextPivotPreset.Items.Add(new ComboBoxItem { Content = label, Tag = p });
        }
        if (CmbTextPivotPreset.Items.Count > 0) CmbTextPivotPreset.SelectedIndex = 0;
    }

    private void BuildTextAnchorPresetGrid()
    {
        GridAnchorPresets.Children.Clear();
        var presets = new[]
        {
            UITextAnchorPreset.TopLeft, UITextAnchorPreset.TopCenter, UITextAnchorPreset.TopRight,
            UITextAnchorPreset.MiddleLeft, UITextAnchorPreset.Center, UITextAnchorPreset.MiddleRight,
            UITextAnchorPreset.BottomLeft, UITextAnchorPreset.BottomCenter, UITextAnchorPreset.BottomRight
        };
        foreach (var pr in presets)
        {
            var btn = new System.Windows.Controls.Button
            {
                Tag = pr,
                Content = AnchorPresetGlyph(pr),
                Margin = new Thickness(2),
                Padding = new Thickness(4, 2, 4, 2),
                Background = new SolidColorBrush(MediaColor.FromRgb(0x21, 0x26, 0x2d)),
                Foreground = new SolidColorBrush(MediaColor.FromRgb(0xe6, 0xed, 0xf3)),
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(0x30, 0x36, 0x3d)),
                ToolTip = pr.ToString()
            };
            btn.Click += TextAnchorPresetButton_OnClick;
            GridAnchorPresets.Children.Add(btn);
        }
    }

    private static string AnchorPresetGlyph(UITextAnchorPreset p) => p switch
    {
        UITextAnchorPreset.TopLeft => "↖",
        UITextAnchorPreset.TopCenter => "↑",
        UITextAnchorPreset.TopRight => "↗",
        UITextAnchorPreset.MiddleLeft => "←",
        UITextAnchorPreset.Center => "●",
        UITextAnchorPreset.MiddleRight => "→",
        UITextAnchorPreset.BottomLeft => "↙",
        UITextAnchorPreset.BottomCenter => "↓",
        UITextAnchorPreset.BottomRight => "↘",
        _ => "·"
    };

    private void RefreshTextAnchorFromTarget()
    {
        if (_target == null || !UIElementCore.SupportsRichTextComponents(_target.Kind)) return;
        _target.TextAnchor ??= new UITextAnchorSettings();
        var ta = _target.TextAnchor;
        SelectComboByTag(CmbTextPivotPreset, ta.PivotPreset);
        ApplyTextAnchorPresetHighlights();
    }

    private void ApplyTextAnchorPresetHighlights()
    {
        var sel = _target?.TextAnchor?.AnchorPreset;
        foreach (var child in GridAnchorPresets.Children.OfType<System.Windows.Controls.Button>())
        {
            var isSel = child.Tag is UITextAnchorPreset p && p == sel;
            child.Background = new SolidColorBrush(isSel
                ? MediaColor.FromRgb(0x38, 0x8b, 0xfd)
                : MediaColor.FromRgb(0x21, 0x26, 0x2d));
        }
    }

    private void TextAnchorPresetButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_updating || _target == null) return;
        if (sender is not System.Windows.Controls.Button b || b.Tag is not UITextAnchorPreset pr) return;
        _target.TextAnchor ??= new UITextAnchorSettings();
        _target.TextAnchor.AnchorPreset = pr;
        ApplyTextAnchorPresetHighlights();
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void CmbTextPivotPreset_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        if (CmbTextPivotPreset.SelectedItem is not ComboBoxItem item || item.Tag is not UITextPivotPreset pv) return;
        _target.TextAnchor ??= new UITextAnchorSettings();
        _target.TextAnchor.PivotPreset = pv;
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void BtnSyncTextAnchorsFromPreset_OnClick(object sender, RoutedEventArgs e)
    {
        if (_target == null) return;
        _target.TextAnchor ??= new UITextAnchorSettings();
        var pr = _target.TextAnchor.AnchorPreset;
        if (pr == UITextAnchorPreset.None) return;
        _target.Anchors = UITextAnchorSettings.ToAnchors(pr);
        _updating = true;
        TxtMinX.Text = _target.Anchors.MinX.ToString(CultureInfo.InvariantCulture);
        TxtMinY.Text = _target.Anchors.MinY.ToString(CultureInfo.InvariantCulture);
        TxtMaxX.Text = _target.Anchors.MaxX.ToString(CultureInfo.InvariantCulture);
        TxtMaxY.Text = _target.Anchors.MaxY.ToString(CultureInfo.InvariantCulture);
        _updating = false;
        PropertyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void CmbFontFamily_OnDropDownOpened(object sender, EventArgs e)
    {
        if (_fontListPopulated) return;
        _fontListPopulated = true;
        var current = (CmbFontFamily.Text ?? "").Trim();
        CmbFontFamily.Items.Clear();
        foreach (var ff in Fonts.SystemFontFamilies.OrderBy(x => x.Source))
            CmbFontFamily.Items.Add(ff.Source);
        CmbFontFamily.Text = current;
    }

    private void CmbFontFamily_OnLostFocus(object sender, RoutedEventArgs e) => PushTextStyleFromUi();

    private void TextStyleField_OnChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        PushTextStyleFromUi();
    }

    private void PushTextStyleFromUi()
    {
        if (_updating || _target == null || !UIElementCore.SupportsRichTextComponents(_target.Kind)) return;
        _target.TextStyle ??= new UITextStyle();
        var s = _target.TextStyle;
        s.FontFamily = (CmbFontFamily.Text ?? "").Trim();
        if (TryParseDouble(TxtFontSize.Text, out var fs)) s.FontSize = fs;
        if (CmbFontUnit.SelectedItem is ComboBoxItem fu && fu.Tag is UITextFontUnit u) s.FontSizeUnit = u;
        s.Color = (TxtTextColor.Text ?? "").Trim();
        s.Opacity = SliderTextOpacity.Value;
        if (CmbTextAlign.SelectedItem is ComboBoxItem al && al.Tag is UITextAlignmentKind ak) s.Alignment = ak;
        s.EnableKerning = ChkKerning.IsChecked == true;
        s.OutlineThickness = SliderOutline.Value;
        s.OutlineColor = (TxtOutlineColor.Text ?? "").Trim();
        if (TryParseDouble(TxtShadowX.Text, out var sx)) s.ShadowOffsetX = sx;
        if (TryParseDouble(TxtShadowY.Text, out var sy)) s.ShadowOffsetY = sy;
        s.ShadowColor = (TxtShadowColor.Text ?? "").Trim();
        s.ShadowBlur = SliderShadowBlur.Value;
        if (TryParseDouble(TxtLineSpacing.Text, out var ls) && ls > 0.1) s.LineSpacing = ls;
        if (TryParseDouble(TxtLetterSpacing.Text, out var lt)) s.LetterSpacing = lt;
        s.RichTextEnabled = ChkRichText.IsChecked == true;
        PushGlyphEffectsInto(s);
        OnElementChanged();
    }

    private void PushGlyphEffectsInto(UITextStyle s)
    {
        var shake = ChkGeShake.IsChecked == true;
        var wave = ChkGeWave.IsChecked == true;
        var rainbow = ChkGeRainbow.IsChecked == true;
        if (!shake && !wave && !rainbow)
        {
            s.GlyphEffects = null;
            return;
        }
        s.GlyphEffects ??= new UITextGlyphEffects();
        var g = s.GlyphEffects;
        g.ShakeEnabled = shake;
        g.ShakeIntensityPixels = SliderGeShake.Value;
        g.WaveEnabled = wave;
        g.WaveAmplitudePixels = SliderGeWaveAmp.Value;
        if (TryParseDouble(TxtGeWaveFreq.Text, out var wf))
            g.WaveFrequency = Math.Max(0.05, wf);
        g.RainbowEnabled = rainbow;
        g.RainbowCyclesPerSecond = SliderGeRainbow.Value;
    }

    private void TextLayoutField_OnChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        PushTextLayoutFromUi();
    }

    private void PushTextLayoutFromUi()
    {
        if (_updating || _target == null || !UIElementCore.SupportsRichTextComponents(_target.Kind)) return;
        _target.TextLayout ??= new UITextLayoutSettings();
        var l = _target.TextLayout;
        if (TryParseInt(TxtHyphenMin.Text, out var hm)) l.HyphenMinPrefixChars = Math.Max(1, hm);
        OnElementChanged();
    }

    private void CmbFontUnit_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        PushTextStyleFromUi();
    }

    private void CmbTextAlign_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        PushTextStyleFromUi();
    }

    private void SliderTextOpacity_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating || _target == null) return;
        PushTextStyleFromUi();
    }

    private void SliderOutline_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating || _target == null) return;
        PushTextStyleFromUi();
    }

    private void SliderShadowBlur_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating || _target == null) return;
        PushTextStyleFromUi();
    }

    private void ChkKerning_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_updating || _target == null) return;
        PushTextStyleFromUi();
    }

    private void ChkRichText_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_updating || _target == null) return;
        PushTextStyleFromUi();
    }

    private void ChkWordWrap_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_updating || _target == null) return;
        _target.TextLayout ??= new UITextLayoutSettings();
        _target.TextLayout.WordWrap = ChkWordWrap.IsChecked == true;
        OnElementChanged();
    }

    private void ChkHyphenation_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_updating || _target == null) return;
        _target.TextLayout ??= new UITextLayoutSettings();
        _target.TextLayout.HyphenationEnabled = ChkHyphenation.IsChecked == true;
        OnElementChanged();
    }

    private void CmbOverflow_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        if (CmbOverflow.SelectedItem is not ComboBoxItem item || item.Tag is not UITextOverflowMode mode) return;
        _target.TextLayout ??= new UITextLayoutSettings();
        _target.TextLayout.OverflowMode = mode;
        OnElementChanged();
    }

    private void TypewriterField_OnChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        PushTypewriterFromUi();
    }

    private void PushTypewriterFromUi()
    {
        if (_updating || _target == null || !UIElementCore.SupportsRichTextComponents(_target.Kind)) return;
        _target.Typewriter ??= new UITypewriterSettings();
        var tw = _target.Typewriter;
        if (TryParseDouble(TxtTwFadeDur.Text, out var fd) && fd >= 0) tw.FadeInDurationSeconds = fd;
        if (TryParseDouble(TxtTwPauseComma.Text, out var pc) && pc >= 0) tw.PauseAfterCommaSeconds = pc;
        if (TryParseDouble(TxtTwPausePeriod.Text, out var pp) && pp >= 0) tw.PauseAfterPeriodSeconds = pp;
        if (TryParseDouble(TxtTwPauseQuestion.Text, out var pq) && pq >= 0) tw.PauseAfterQuestionSeconds = pq;
        if (TryParseDouble(TxtTwPauseExcl.Text, out var pe) && pe >= 0) tw.PauseAfterExclamationSeconds = pe;
        tw.SoundPath = (TxtTwSoundPath.Text ?? "").Trim();
        OnElementChanged();
    }

    private void ChkTw_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_updating || _target == null) return;
        _target.Typewriter ??= new UITypewriterSettings();
        var tw = _target.Typewriter;
        tw.Enabled = ChkTwEnabled.IsChecked == true;
        tw.FadeInPerChar = ChkTwFade.IsChecked == true;
        tw.PunctuationPausesEnabled = ChkTwPause.IsChecked == true;
        OnElementChanged();
    }

    private void SliderTwCps_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating || _target == null) return;
        _target.Typewriter ??= new UITypewriterSettings();
        _target.Typewriter.CharsPerSecond = SliderTwCps.Value;
        OnElementChanged();
    }

    private void CmbTwSoundTrigger_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || _target == null) return;
        if (CmbTwSoundTrigger.SelectedItem is not ComboBoxItem item || item.Tag is not UITypewriterSoundTrigger tr) return;
        _target.Typewriter ??= new UITypewriterSettings();
        _target.Typewriter.SoundTrigger = tr;
        OnElementChanged();
    }

    private void SliderTwSoundVol_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating || _target == null) return;
        _target.Typewriter ??= new UITypewriterSettings();
        _target.Typewriter.SoundVolume = SliderTwSoundVol.Value;
        OnElementChanged();
    }

    private void BtnBrowseFont_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_projectDirectory))
        {
            EditorLog.Toast("No hay carpeta de proyecto para copiar la fuente.", LogLevel.Warning, "UI");
            return;
        }
        var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Fuente|*.ttf;*.otf|Todos|*.*" };
        if (dlg.ShowDialog() != true) return;
        var destDir = Path.Combine(_projectDirectory, "Assets", "Fonts");
        try
        {
            Directory.CreateDirectory(destDir);
            var name = Path.GetFileName(dlg.FileName);
            var dest = Path.Combine(destDir, name);
            File.Copy(dlg.FileName, dest, overwrite: true);
            var rel = $"Assets/Fonts/{name}".Replace('\\', '/');
            _updating = true;
            CmbFontFamily.Text = rel;
            _updating = false;
            PushTextStyleFromUi();
            EditorLog.Toast($"Fuente copiada a {rel}", LogLevel.Info, "UI");
        }
        catch (Exception ex)
        {
            EditorLog.Toast($"No se pudo copiar la fuente: {ex.Message}", LogLevel.Warning, "UI");
        }
    }

    private void BtnBrowseTwSound_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_projectDirectory))
        {
            EditorLog.Toast("No hay carpeta de proyecto.", LogLevel.Warning, "UI");
            return;
        }
        var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Audio|*.wav;*.ogg|Todos|*.*" };
        if (dlg.ShowDialog() != true) return;
        var destDir = Path.Combine(_projectDirectory, "Assets", "UI", "Sounds");
        try
        {
            Directory.CreateDirectory(destDir);
            var name = Path.GetFileName(dlg.FileName);
            var dest = Path.Combine(destDir, name);
            File.Copy(dlg.FileName, dest, overwrite: true);
            var rel = $"Assets/UI/Sounds/{name}".Replace('\\', '/');
            _updating = true;
            TxtTwSoundPath.Text = rel;
            _updating = false;
            PushTypewriterFromUi();
        }
        catch (Exception ex)
        {
            EditorLog.Toast($"No se pudo copiar el audio: {ex.Message}", LogLevel.Warning, "UI");
        }
    }
}
