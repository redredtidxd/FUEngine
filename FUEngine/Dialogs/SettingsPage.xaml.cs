using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace FUEngine;

public partial class SettingsPage : Page
{
    private static string SettingsPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FUEngine", "settings.json");

    private EngineSettings _settings = new();
    private readonly Dictionary<string, TextBlock> _shortcutKeyLabels = new();
    private readonly Dictionary<string, System.Windows.Controls.Button> _shortcutCaptureButtons = new();
    private string? _capturingShortcutId;
    private bool _shortcutPresetComboSync;

    private static readonly SolidColorBrush ShortcutBtnIdleBg = new(System.Windows.Media.Color.FromRgb(0x21, 0x26, 0x2d));
    private static readonly SolidColorBrush ShortcutBtnIdleFg = new(System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3));
    private static readonly SolidColorBrush ShortcutBtnIdleBorder = new(System.Windows.Media.Color.FromRgb(0x30, 0x36, 0x3d));
    private static readonly SolidColorBrush ShortcutBtnCapturingBg = new(System.Windows.Media.Color.FromRgb(0xd2, 0x7d, 0x2e));
    private static readonly SolidColorBrush ShortcutBtnCapturingFg = new(System.Windows.Media.Color.FromRgb(0xff, 0xff, 0xff));
    private static readonly SolidColorBrush ShortcutBtnCapturingBorder = new(System.Windows.Media.Color.FromRgb(0xff, 0xb4, 0x4d));
    private Window? _hostWindow;

    public SettingsPage()
    {
        InitializeComponent();
        Loaded += OnPageLoaded;
        Unloaded += OnPageUnloaded;
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        PopulateComboBoxes();
        LoadSettings();
        WireSliders();
        AttachKeyCapture();
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        _capturingShortcutId = null;
        ApplyShortcutCaptureVisualState();
        DetachKeyCapture();
    }

    private void WireSliders()
    {
        if (SldEngineAutoSaveInterval != null)
            SldEngineAutoSaveInterval.ValueChanged += (_, _) => UpdateSliderLabels();
        if (SldZoomWheelSensitivity != null)
            SldZoomWheelSensitivity.ValueChanged += (_, _) => UpdateSliderLabels();
        if (SldPanKeyScale != null)
            SldPanKeyScale.ValueChanged += (_, _) => UpdateSliderLabels();
    }

    private void UpdateSliderLabels()
    {
        if (TblEngineAutoSaveInterval != null && SldEngineAutoSaveInterval != null)
            TblEngineAutoSaveInterval.Text = ((int)Math.Round(SldEngineAutoSaveInterval.Value)).ToString();
        if (TblZoomWheelSensitivity != null && SldZoomWheelSensitivity != null)
            TblZoomWheelSensitivity.Text = SldZoomWheelSensitivity.Value.ToString("0.##", CultureInfo.InvariantCulture);
        if (TblPanKeyScale != null && SldPanKeyScale != null)
            TblPanKeyScale.Text = SldPanKeyScale.Value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private void AttachKeyCapture()
    {
        _hostWindow = Window.GetWindow(this);
        if (_hostWindow != null)
            _hostWindow.PreviewKeyDown += Host_PreviewKeyDown;
    }

    private void DetachKeyCapture()
    {
        if (_hostWindow != null)
            _hostWindow.PreviewKeyDown -= Host_PreviewKeyDown;
        _hostWindow = null;
    }

    private void Host_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (string.IsNullOrEmpty(_capturingShortcutId)) return;
        if (e.Key == Key.Escape)
        {
            _capturingShortcutId = null;
            ApplyShortcutCaptureVisualState();
            if (TxtShortcutCaptureHint != null) TxtShortcutCaptureHint.Visibility = Visibility.Collapsed;
            e.Handled = true;
            return;
        }
        if (e.Key == Key.System || e.Key == Key.LeftShift || e.Key == Key.RightShift || e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl || e.Key == Key.LeftAlt || e.Key == Key.RightAlt)
            return;
        var mods = Keyboard.Modifiers;
        if (e.Key is Key.LeftShift or Key.RightShift or Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt)
            return;
        var formatted = EditorShortcutBindings.FormatKeyGesture(e.Key, mods);
        _settings.ShortcutBindings ??= new Dictionary<string, string>(StringComparer.Ordinal);
        _settings.ShortcutBindings[_capturingShortcutId] = formatted;
        if (_shortcutKeyLabels.TryGetValue(_capturingShortcutId, out var tb))
            tb.Text = formatted;
        _capturingShortcutId = null;
        _settings.ShortcutPreset = EditorShortcutPresets.CustomId;
        SyncShortcutPresetCombo();
        ApplyShortcutCaptureVisualState();
        if (TxtShortcutCaptureHint != null) TxtShortcutCaptureHint.Visibility = Visibility.Collapsed;
        e.Handled = true;
    }

    private void ApplyShortcutCaptureVisualState()
    {
        foreach (var kv in _shortcutCaptureButtons)
        {
            if (kv.Value == null) continue;
            if (_capturingShortcutId == kv.Key)
            {
                kv.Value.Content = "Presione tecla…";
                kv.Value.Background = ShortcutBtnCapturingBg;
                kv.Value.Foreground = ShortcutBtnCapturingFg;
                kv.Value.BorderBrush = ShortcutBtnCapturingBorder;
            }
            else
            {
                kv.Value.Content = "Cambiar…";
                kv.Value.Background = ShortcutBtnIdleBg;
                kv.Value.Foreground = ShortcutBtnIdleFg;
                kv.Value.BorderBrush = ShortcutBtnIdleBorder;
            }
        }
    }

    private void SyncShortcutPresetCombo()
    {
        if (CmbShortcutPreset == null) return;
        _shortcutPresetComboSync = true;
        try
        {
            var id = string.IsNullOrWhiteSpace(_settings.ShortcutPreset) ? EditorShortcutPresets.DefaultId : _settings.ShortcutPreset.Trim();
            if (string.Equals(id, EditorShortcutPresets.CustomId, StringComparison.OrdinalIgnoreCase))
            {
                CmbShortcutPreset.SelectedItem = null;
                if (TxtShortcutPresetCustomNote != null) TxtShortcutPresetCustomNote.Visibility = Visibility.Visible;
            }
            else
            {
                var found = EditorShortcutPresets.ComboChoices.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase));
                CmbShortcutPreset.SelectedItem = found ?? EditorShortcutPresets.ComboChoices[0];
                if (TxtShortcutPresetCustomNote != null) TxtShortcutPresetCustomNote.Visibility = Visibility.Collapsed;
            }
        }
        finally
        {
            _shortcutPresetComboSync = false;
        }
    }

    private void CmbShortcutPreset_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_shortcutPresetComboSync) return;
        if (CmbShortcutPreset?.SelectedItem is not EditorShortcutPresets.Choice choice) return;
        EditorShortcutPresets.Apply(_settings, choice.Id);
        if (TxtShortcutPresetCustomNote != null) TxtShortcutPresetCustomNote.Visibility = Visibility.Collapsed;
        RebuildShortcutRows();
    }

    private void RebuildShortcutRows()
    {
        if (PanelShortcutRows == null) return;
        PanelShortcutRows.Children.Clear();
        _shortcutKeyLabels.Clear();
        _shortcutCaptureButtons.Clear();
        _settings.ShortcutBindings ??= new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var def in EditorShortcutBindings.Definitions)
        {
            var grid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

            var desc = new TextBlock
            {
                Text = def.Description,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3)),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
                ToolTip = def.Description
            };
            Grid.SetColumn(desc, 0);

            var keysText = EditorShortcutBindings.GetDisplay(_settings, def.Id);
            var keysTb = new TextBlock
            {
                Text = keysText,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x8b, 0x94, 0x9e)),
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "Combinación actual guardada en settings.json."
            };
            Grid.SetColumn(keysTb, 1);
            if (!def.Id.StartsWith('_'))
                _shortcutKeyLabels[def.Id] = keysTb;

            var btn = new System.Windows.Controls.Button
            {
                Content = "Cambiar…",
                Tag = def.Id,
                IsEnabled = def.Rebindable,
                Margin = new Thickness(8, 0, 0, 0),
                Padding = new Thickness(8, 4, 8, 4),
                Background = ShortcutBtnIdleBg,
                Foreground = ShortcutBtnIdleFg,
                BorderBrush = ShortcutBtnIdleBorder,
                BorderThickness = new Thickness(1),
                ToolTip = def.Rebindable
                    ? "Pulsa para capturar una nueva tecla; Escape cancela."
                    : "Este atajo no se puede reasignar."
            };
            btn.Click += ShortcutCaptureBtn_OnClick;
            Grid.SetColumn(btn, 2);
            if (!def.Id.StartsWith('_'))
                _shortcutCaptureButtons[def.Id] = btn;

            grid.Children.Add(desc);
            grid.Children.Add(keysTb);
            grid.Children.Add(btn);
            PanelShortcutRows.Children.Add(grid);
        }
        ApplyShortcutCaptureVisualState();
    }

    private void ShortcutCaptureBtn_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button b || b.Tag is not string id) return;
        _capturingShortcutId = id;
        if (TxtShortcutCaptureHint != null)
        {
            TxtShortcutCaptureHint.Text = "Pulsa la nueva tecla o combinación… (Escape para cancelar)";
            TxtShortcutCaptureHint.Visibility = Visibility.Visible;
        }
        ApplyShortcutCaptureVisualState();
    }

    private void PopulateComboBoxes()
    {
        if (CmbTheme != null) CmbTheme.ItemsSource = new[] { "Oscuro", "Claro", "PixelStyle", "RetroCRT", "Personalizado" };
        EngineFontPresets.FillCombo(CmbEngineFont);
        if (CmbLanguage != null) CmbLanguage.ItemsSource = new[] { ("Español", "es"), ("English", "en"), ("日本語", "ja") }.Select(x => $"{x.Item1} ({x.Item2})").ToList();
        if (CmbCoordinateUnit != null) CmbCoordinateUnit.ItemsSource = new[] { "Tiles", "SubTiles", "Pixels" };
        if (CmbStartupBehavior != null)
            CmbStartupBehavior.ItemsSource = new[] { "Abrir Hub", "Abrir último proyecto", "Crear nuevo proyecto (asistente)" };
        if (CmbPhysicsCollisionMode != null) CmbPhysicsCollisionMode.ItemsSource = new[] { "Simple", "Precisa" };
        if (CmbBuildWindowMode != null) CmbBuildWindowMode.ItemsSource = new[] { "Windowed", "Fullscreen" };
        if (TxtEngineVersionLabel != null) TxtEngineVersionLabel.Text = "Versión del motor: " + FUEngine.Core.EngineVersion.Current;
        if (CmbUpdateChannel != null) CmbUpdateChannel.ItemsSource = new[] { "Stable", "Beta" };
        if (CmbExportFormatImage != null) CmbExportFormatImage.ItemsSource = new[] { "PNG", "WebP" };
        if (CmbExportFormatAudio != null) CmbExportFormatAudio.ItemsSource = new[] { "OGG", "WAV", "MP3" };
        if (CmbRenderMode != null) CmbRenderMode.ItemsSource = new[] { "PixelPerfect", "Subpixel", "AntiAlias", "CRTEffect" };
        if (CmbLightingMode != null) CmbLightingMode.ItemsSource = new[] { "Desactivar", "Activar", "Avanzado" };
        if (CmbDefaultPalette != null) CmbDefaultPalette.ItemsSource = new[] { "default", "grayscale", "gameboy", "warm" };
        if (CmbChunkSize != null) CmbChunkSize.ItemsSource = new[] { "16", "32", "64" };
        if (CmbScriptEditorMode != null) CmbScriptEditorMode.ItemsSource = new[] { "Nodos", "Codigo" };
        if (CmbGridSnap != null) CmbGridSnap.ItemsSource = new[] { "Tile size", "1px", "2px", "4px" };
        if (CmbAnimationInterpolation != null) CmbAnimationInterpolation.ItemsSource = new[] { "Ninguna", "Lineal", "Suave" };
        if (CmbBuildRuntime != null) CmbBuildRuntime.ItemsSource = new[] { "net8.0-windows", "net9.0-windows", "net6.0-windows" };
        if (CmbBuildResolution != null) CmbBuildResolution.ItemsSource = new[] { "1920x1080", "1280x720", "800x600", "640x360" };
        if (CmbShortcutPreset != null) CmbShortcutPreset.ItemsSource = EditorShortcutPresets.ComboChoices;
        if (CmbNewProjectFolderOrderPreset != null)
        {
            CmbNewProjectFolderOrderPreset.Items.Clear();
            CmbNewProjectFolderOrderPreset.Items.Add(new ComboBoxItem { Content = "Predeterminado (Sprites, Maps, Scripts, Audio, Seeds)", Tag = "default" });
            CmbNewProjectFolderOrderPreset.Items.Add(new ComboBoxItem { Content = "Personalizado (lista inferior)", Tag = "custom" });
        }
        if (CmbNewProjectExplorerTheme != null)
        {
            CmbNewProjectExplorerTheme.Items.Clear();
            CmbNewProjectExplorerTheme.Items.Add(new ComboBoxItem { Content = "Ninguno (solo lista manual abajo)", Tag = "none" });
            CmbNewProjectExplorerTheme.Items.Add(new ComboBoxItem { Content = "UI + Prefabs", Tag = "ui" });
            CmbNewProjectExplorerTheme.Items.Add(new ComboBoxItem { Content = "Jam: Screenshots + Build", Tag = "jam" });
            CmbNewProjectExplorerTheme.Items.Add(new ComboBoxItem { Content = "Contenido: UI + Plugins + StreamingAssets", Tag = "content" });
        }
    }

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                _settings = System.Text.Json.JsonSerializer.Deserialize<EngineSettings>(json,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new EngineSettings();
            }
        }
        catch { _settings = new EngineSettings(); }

        ApplyDefaultsIfEmpty();
        EditorShortcutPresets.NormalizeAfterLoad(_settings);
        BindToUi();
    }

    private static string DefaultSharedAssetsPath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FUEngine", "SharedAssets");

    private static string DefaultBuildExportPath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FUEngine", "BuildExport");

    private void ApplyDefaultsIfEmpty()
    {
        if (string.IsNullOrWhiteSpace(_settings.DefaultProjectsPath))
            _settings.DefaultProjectsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FUEngine");
        if (string.IsNullOrWhiteSpace(_settings.TempBuildPath))
            _settings.TempBuildPath = Path.Combine(Path.GetTempPath(), "FUEngine", "Builds");
        if (string.IsNullOrWhiteSpace(_settings.SharedAssetsPath))
            _settings.SharedAssetsPath = DefaultSharedAssetsPath();
        if (string.IsNullOrWhiteSpace(_settings.BuildExportPath))
            _settings.BuildExportPath = DefaultBuildExportPath();
        if (string.IsNullOrWhiteSpace(_settings.AutoLogsPath))
            _settings.AutoLogsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FUEngine", "Logs");
    }

    private static int StartupBehaviorIndex(string? v) => (v ?? "Hub") switch
    {
        "LastProject" => 1,
        "NewProject" => 2,
        _ => 0
    };

    private static string StartupBehaviorFromIndex(int i) => i switch
    {
        1 => "LastProject",
        2 => "NewProject",
        _ => "Hub"
    };

    private void BindToUi()
    {
        if (CmbStartupBehavior != null) CmbStartupBehavior.SelectedIndex = StartupBehaviorIndex(_settings.StartupBehavior);
        if (ChkHardwareAcceleration != null) ChkHardwareAcceleration.IsChecked = _settings.HardwareAccelerationEnabled;
        if (TxtAiPath != null) TxtAiPath.Text = _settings.AiIntegrationPath ?? "";

        if (CmbTheme != null) CmbTheme.SelectedItem = _settings.Theme ?? "Oscuro";
        if (CmbLanguage != null)
        {
            var langIdx = _settings.Language switch { "en" => 1, "ja" => 2, _ => 0 };
            if (langIdx < CmbLanguage.Items.Count) CmbLanguage.SelectedIndex = langIdx;
        }
        if (CmbCoordinateUnit != null) CmbCoordinateUnit.SelectedItem = _settings.CoordinateUnit ?? "Tiles";
        if (ChkShowTips != null) ChkShowTips.IsChecked = _settings.ShowTipsOnStartup;
        EngineFontPresets.SelectForSettings(CmbEngineFont, _settings);
        if (ChkAutoUpdate != null) ChkAutoUpdate.IsChecked = _settings.AutoUpdateCheckEnabled;
        if (CmbUpdateChannel != null) CmbUpdateChannel.SelectedItem = _settings.AutoUpdateChannel ?? "Stable";

        if (ChkUseEngineAutoSave != null) ChkUseEngineAutoSave.IsChecked = _settings.UseEngineAutoSaveSettings;
        if (ChkEngineAutoSaveEnabled != null) ChkEngineAutoSaveEnabled.IsChecked = _settings.EngineAutoSaveEnabled;
        if (SldEngineAutoSaveInterval != null)
            SldEngineAutoSaveInterval.Value = Math.Clamp(_settings.EngineAutoSaveIntervalMinutes > 0 ? _settings.EngineAutoSaveIntervalMinutes : 5, 1, 60);
        if (SldZoomWheelSensitivity != null)
            SldZoomWheelSensitivity.Value = Math.Clamp(_settings.MapZoomWheelSensitivity > 0 ? _settings.MapZoomWheelSensitivity : 1, 0.25, 3);
        if (SldPanKeyScale != null)
            SldPanKeyScale.Value = Math.Clamp(_settings.MapPanKeyboardStepScale > 0 ? _settings.MapPanKeyboardStepScale : 1, 0.25, 3);
        UpdateSliderLabels();

        if (TxtExternalCodeEditor != null) TxtExternalCodeEditor.Text = _settings.ExternalCodeEditorPath ?? "";

        if (TxtProjectsPath != null) TxtProjectsPath.Text = _settings.DefaultProjectsPath ?? "";
        if (TxtSharedAssetsPath != null) TxtSharedAssetsPath.Text = _settings.SharedAssetsPath ?? "";
        if (TxtBuildExportPath != null) TxtBuildExportPath.Text = _settings.BuildExportPath ?? "";
        if (ChkSaveOverwriteProtection != null) ChkSaveOverwriteProtection.IsChecked = _settings.SaveOverwriteProtection;
        if (CmbExportFormatImage != null) CmbExportFormatImage.SelectedItem = _settings.DefaultExportFormatImage ?? "PNG";
        if (CmbExportFormatAudio != null) CmbExportFormatAudio.SelectedItem = _settings.DefaultExportFormatAudio ?? "OGG";

        if (ChkCollisionMaskDefault != null) ChkCollisionMaskDefault.IsChecked = _settings.CollisionMaskVisibleByDefault;
        if (TxtZoomMin != null) TxtZoomMin.Text = _settings.EditorZoomMin.ToString(CultureInfo.InvariantCulture);
        if (TxtZoomMax != null) TxtZoomMax.Text = _settings.EditorZoomMax.ToString(CultureInfo.InvariantCulture);
        if (CmbGridSnap != null) CmbGridSnap.SelectedIndex = _settings.GridSnapPx switch { 1 => 1, 2 => 2, 4 => 3, _ => 0 };
        if (ChkGridVisible != null) ChkGridVisible.IsChecked = _settings.GridVisibleByDefault;
        if (ChkRulersVisible != null) ChkRulersVisible.IsChecked = _settings.RulersVisibleByDefault;
        if (TxtDefaultSceneBgColor != null) TxtDefaultSceneBgColor.Text = _settings.DefaultSceneBackgroundColor ?? "#FFFFFF";
        if (ChkThumbnailPreview != null) ChkThumbnailPreview.IsChecked = _settings.ThumbnailPreviewEnabled;
        if (TxtDefaultPlugins != null) TxtDefaultPlugins.Text = _settings.DefaultEnabledPlugins != null ? string.Join(", ", _settings.DefaultEnabledPlugins) : "";
        if (ChkLightingPreview != null) ChkLightingPreview.IsChecked = _settings.LightingPreviewEnabled;

        if (TxtDefaultTileScale != null) TxtDefaultTileScale.Text = _settings.DefaultTileScale.ToString();
        if (CmbRenderMode != null) CmbRenderMode.SelectedItem = _settings.RenderMode ?? "PixelPerfect";
        if (TxtDefaultProjectFps != null) TxtDefaultProjectFps.Text = _settings.DefaultProjectFps.ToString();
        if (CmbPhysicsCollisionMode != null) CmbPhysicsCollisionMode.SelectedItem = _settings.PhysicsCollisionMode ?? "Simple";
        if (TxtGlobalGravity != null) TxtGlobalGravity.Text = _settings.GlobalGravity.ToString(CultureInfo.InvariantCulture);
        if (CmbLightingMode != null) CmbLightingMode.SelectedItem = _settings.DefaultLightingMode ?? "Desactivar";
        if (CmbDefaultPalette != null) CmbDefaultPalette.SelectedItem = _settings.DefaultPaletteId ?? "default";
        if (TxtLightingIntensity != null) TxtLightingIntensity.Text = _settings.LightingIntensity.ToString();
        if (TxtLightingDirection != null) TxtLightingDirection.Text = _settings.LightingDirectionDegrees.ToString();
        if (ChkDithering != null) ChkDithering.IsChecked = _settings.DitheringEnabled;
        if (ChkTestDynamicLight != null) ChkTestDynamicLight.IsChecked = _settings.TestRenderDynamicLight;
        if (TxtParallaxSpeed != null) TxtParallaxSpeed.Text = _settings.ParallaxSpeed.ToString(CultureInfo.InvariantCulture);
        if (TxtDefaultAnimationFps != null) TxtDefaultAnimationFps.Text = _settings.DefaultAnimationFps.ToString();
        if (CmbAnimationInterpolation != null) CmbAnimationInterpolation.SelectedItem = _settings.AnimationInterpolation ?? "Lineal";
        if (CmbChunkSize != null) CmbChunkSize.SelectedItem = _settings.DefaultChunkSize.ToString();
        if (TxtDefaultTileSize != null) TxtDefaultTileSize.Text = _settings.DefaultTileSize.ToString();
        if (TxtMaxTileHeight != null) TxtMaxTileHeight.Text = _settings.MaxTileHeight.ToString();
        if (ChkAutoTiling != null) ChkAutoTiling.IsChecked = _settings.AutoTilingByDefault;

        if (TxtCommonScriptIds != null) TxtCommonScriptIds.Text = _settings.DefaultCommonScriptIds != null ? string.Join(", ", _settings.DefaultCommonScriptIds) : "";
        if (CmbScriptEditorMode != null) CmbScriptEditorMode.SelectedItem = _settings.ScriptEditorMode ?? "Nodos";
        if (TxtPlaceholderAssetsPath != null) TxtPlaceholderAssetsPath.Text = _settings.DefaultPlaceholderAssetsPath ?? "";

        if (ChkSplashCreatedWith != null) ChkSplashCreatedWith.IsChecked = _settings.SplashCreatedWithFUEngine;
        if (TxtSplashDuration != null) TxtSplashDuration.Text = _settings.SplashDurationMs.ToString();
        if (ChkSplashFadeIn != null) ChkSplashFadeIn.IsChecked = _settings.SplashFadeIn;
        if (ChkSplashFadeOut != null) ChkSplashFadeOut.IsChecked = _settings.SplashFadeOut;
        if (TxtSplashLogoPath != null) TxtSplashLogoPath.Text = _settings.SplashLogoPath ?? "";
        if (TxtBuildExeIcon != null) TxtBuildExeIcon.Text = _settings.BuildExeIconPath ?? "";
        if (TxtBuildGameVersion != null) TxtBuildGameVersion.Text = _settings.BuildGameVersion ?? "1.0.0";
        if (CmbBuildWindowMode != null) CmbBuildWindowMode.SelectedItem = _settings.BuildDefaultWindowMode ?? "Windowed";
        if (TxtTempBuildPath != null) TxtTempBuildPath.Text = _settings.TempBuildPath ?? "";
        if (CmbBuildRuntime != null) CmbBuildRuntime.SelectedItem = _settings.DefaultBuildRuntime ?? "net8.0-windows";
        if (CmbBuildResolution != null) CmbBuildResolution.SelectedItem = _settings.DefaultBuildResolution ?? "1920x1080";
        if (TxtTileScalingBuild != null) TxtTileScalingBuild.Text = _settings.DefaultTileScalingBuild.ToString();
        if (ChkAutoOptimizeExport != null) ChkAutoOptimizeExport.IsChecked = _settings.AutoOptimizeOnExport;

        if (ChkDebugOverlay != null) ChkDebugOverlay.IsChecked = _settings.DebugShowOverlay;
        if (ChkDebugFps != null) ChkDebugFps.IsChecked = _settings.DebugShowFps;
        if (ChkDebugDrawCalls != null) ChkDebugDrawCalls.IsChecked = _settings.DebugShowDrawCalls;
        if (ChkDebugMemory != null) ChkDebugMemory.IsChecked = _settings.DebugShowMemory;
        if (ChkTestSpriteStacking != null) ChkTestSpriteStacking.IsChecked = _settings.TestRenderSpriteStacking;
        if (ChkTestDepthBlending != null) ChkTestDepthBlending.IsChecked = _settings.TestRenderDepthBlending;
        if (ChkAutoLogs != null) ChkAutoLogs.IsChecked = _settings.AutoLogsEnabled;
        if (TxtAutoLogsPath != null) TxtAutoLogsPath.Text = _settings.AutoLogsPath ?? "";
        if (TxtAssetCacheMaxMb != null) TxtAssetCacheMaxMb.Text = _settings.AssetCacheMaxMb.ToString();
        if (ChkAssetCacheEnabled != null) ChkAssetCacheEnabled.IsChecked = _settings.AssetCacheEnabled;

        if (CmbNewProjectFolderOrderPreset != null)
            CmbNewProjectFolderOrderPreset.SelectedIndex = string.Equals(_settings.NewProjectRootFolderOrderPresetId?.Trim(), "custom", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        if (TxtCustomNewProjectFolderOrder != null)
            TxtCustomNewProjectFolderOrder.Text = string.Join(Environment.NewLine, _settings.CustomNewProjectRootFolderOrder ?? new List<string>());
        if (TxtExtraNewProjectRootFolders != null)
            TxtExtraNewProjectRootFolders.Text = string.Join(Environment.NewLine, _settings.ExtraNewProjectRootFolders ?? new List<string>());
        if (CmbNewProjectExplorerTheme != null)
        {
            var tid = (_settings.NewProjectExplorerThemeId ?? "none").Trim().ToLowerInvariant();
            CmbNewProjectExplorerTheme.SelectedIndex = 0;
            for (int i = 0; i < CmbNewProjectExplorerTheme.Items.Count; i++)
            {
                if (CmbNewProjectExplorerTheme.Items[i] is ComboBoxItem cbi && cbi.Tag is string tg &&
                    string.Equals(tg, tid, StringComparison.OrdinalIgnoreCase))
                { CmbNewProjectExplorerTheme.SelectedIndex = i; break; }
            }
        }
        if (ChkDefaultNewProjectDebugMode != null) ChkDefaultNewProjectDebugMode.IsChecked = _settings.DefaultNewProjectDebugMode;

        SyncShortcutPresetCombo();
        RebuildShortcutRows();
    }

    private void ReadFromUi()
    {
        if (CmbStartupBehavior != null) _settings.StartupBehavior = StartupBehaviorFromIndex(CmbStartupBehavior.SelectedIndex);
        if (ChkHardwareAcceleration != null) _settings.HardwareAccelerationEnabled = ChkHardwareAcceleration.IsChecked != false;
        if (TxtAiPath != null) _settings.AiIntegrationPath = TxtAiPath.Text?.Trim() ?? "";

        if (CmbTheme != null) _settings.Theme = CmbTheme.SelectedItem?.ToString() ?? "Oscuro";
        var langStr = CmbLanguage?.SelectedItem?.ToString() ?? "";
        _settings.Language = langStr.Contains("(ja)") ? "ja" : langStr.Contains("(en)") ? "en" : "es";
        _settings.CoordinateUnit = CmbCoordinateUnit?.SelectedItem?.ToString() ?? "Tiles";
        _settings.ShowTipsOnStartup = ChkShowTips?.IsChecked == true;
        EngineFontPresets.ApplySelectionToSettings(CmbEngineFont, _settings);
        if (ChkAutoUpdate != null) _settings.AutoUpdateCheckEnabled = ChkAutoUpdate.IsChecked == true;
        if (CmbUpdateChannel != null) _settings.AutoUpdateChannel = CmbUpdateChannel.SelectedItem?.ToString() ?? "Stable";

        _settings.UseEngineAutoSaveSettings = ChkUseEngineAutoSave?.IsChecked == true;
        if (ChkEngineAutoSaveEnabled != null) _settings.EngineAutoSaveEnabled = ChkEngineAutoSaveEnabled.IsChecked == true;
        if (SldEngineAutoSaveInterval != null)
            _settings.EngineAutoSaveIntervalMinutes = (int)Math.Round(SldEngineAutoSaveInterval.Value);
        if (SldZoomWheelSensitivity != null) _settings.MapZoomWheelSensitivity = SldZoomWheelSensitivity.Value;
        if (SldPanKeyScale != null) _settings.MapPanKeyboardStepScale = SldPanKeyScale.Value;

        _settings.ExternalCodeEditorPath = TxtExternalCodeEditor?.Text?.Trim() ?? "";

        _settings.DefaultProjectsPath = TxtProjectsPath?.Text?.Trim() ?? "";
        _settings.SharedAssetsPath = TxtSharedAssetsPath?.Text?.Trim() ?? "";
        _settings.BuildExportPath = TxtBuildExportPath?.Text?.Trim() ?? "";
        _settings.SaveOverwriteProtection = ChkSaveOverwriteProtection?.IsChecked == true;
        if (CmbExportFormatImage != null) _settings.DefaultExportFormatImage = CmbExportFormatImage.SelectedItem?.ToString() ?? "PNG";
        if (CmbExportFormatAudio != null) _settings.DefaultExportFormatAudio = CmbExportFormatAudio.SelectedItem?.ToString() ?? "OGG";

        if (ChkCollisionMaskDefault != null) _settings.CollisionMaskVisibleByDefault = ChkCollisionMaskDefault.IsChecked == true;
        _settings.EditorZoomMin = (TxtZoomMin != null && double.TryParse(TxtZoomMin.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var zmin) && zmin > 0) ? zmin : 0.25;
        _settings.EditorZoomMax = (TxtZoomMax != null && double.TryParse(TxtZoomMax.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var zmax) && zmax > 0) ? zmax : 4.0;
        _settings.GridSnapPx = (CmbGridSnap?.SelectedIndex ?? -1) switch { 1 => 1, 2 => 2, 3 => 4, _ => 0 };
        if (ChkGridVisible != null) _settings.GridVisibleByDefault = ChkGridVisible.IsChecked == true;
        if (ChkRulersVisible != null) _settings.RulersVisibleByDefault = ChkRulersVisible.IsChecked == true;
        if (TxtDefaultSceneBgColor != null) _settings.DefaultSceneBackgroundColor = TxtDefaultSceneBgColor.Text?.Trim() ?? "#FFFFFF";
        if (ChkThumbnailPreview != null) _settings.ThumbnailPreviewEnabled = ChkThumbnailPreview.IsChecked == true;
        if (TxtDefaultPlugins != null) _settings.DefaultEnabledPlugins = (TxtDefaultPlugins.Text ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        if (ChkLightingPreview != null) _settings.LightingPreviewEnabled = ChkLightingPreview.IsChecked == true;

        _settings.DefaultTileScale = (TxtDefaultTileScale != null && int.TryParse(TxtDefaultTileScale.Text, out var ts) && ts >= 8 && ts <= 128) ? ts : 16;
        _settings.RenderMode = CmbRenderMode?.SelectedItem?.ToString() ?? "PixelPerfect";
        if (TxtDefaultProjectFps != null && int.TryParse(TxtDefaultProjectFps.Text, out var dpf) && dpf >= 1 && dpf <= 240) _settings.DefaultProjectFps = dpf;
        _settings.PhysicsCollisionMode = CmbPhysicsCollisionMode?.SelectedItem?.ToString() ?? "Simple";
        if (TxtGlobalGravity != null && double.TryParse(TxtGlobalGravity.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var g)) _settings.GlobalGravity = g;
        _settings.DefaultLightingMode = CmbLightingMode?.SelectedItem?.ToString() ?? "Desactivar";
        _settings.DefaultPaletteId = CmbDefaultPalette?.SelectedItem?.ToString() ?? "default";
        _settings.LightingIntensity = (TxtLightingIntensity != null && int.TryParse(TxtLightingIntensity.Text, out var li) && li >= 0 && li <= 100) ? li : 80;
        _settings.LightingDirectionDegrees = (TxtLightingDirection != null && int.TryParse(TxtLightingDirection.Text, out var ld) && ld >= 0 && ld <= 360) ? ld : 45;
        _settings.DitheringEnabled = ChkDithering?.IsChecked == true;
        if (ChkTestDynamicLight != null) _settings.TestRenderDynamicLight = ChkTestDynamicLight.IsChecked == true;
        _settings.ParallaxSpeed = (TxtParallaxSpeed != null && double.TryParse(TxtParallaxSpeed.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var ps) && ps >= 0 && ps <= 2) ? ps : 1.0;
        _settings.DefaultAnimationFps = (TxtDefaultAnimationFps != null && int.TryParse(TxtDefaultAnimationFps.Text, out var dafps) && dafps >= 1 && dafps <= 120) ? dafps : 12;
        _settings.AnimationInterpolation = CmbAnimationInterpolation?.SelectedItem?.ToString() ?? "Lineal";
        _settings.DefaultChunkSize = (CmbChunkSize != null && int.TryParse(CmbChunkSize.SelectedItem?.ToString(), out var cs) && (cs == 16 || cs == 32 || cs == 64)) ? cs : 16;
        _settings.DefaultTileSize = (TxtDefaultTileSize != null && int.TryParse(TxtDefaultTileSize.Text, out var tsz) && tsz >= 8 && tsz <= 128) ? tsz : 16;
        _settings.MaxTileHeight = (TxtMaxTileHeight != null && int.TryParse(TxtMaxTileHeight.Text, out var mh) && mh >= 1) ? mh : 1;
        _settings.AutoTilingByDefault = ChkAutoTiling?.IsChecked == true;

        var ids = (TxtCommonScriptIds?.Text ?? "").Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        _settings.DefaultCommonScriptIds = ids;
        _settings.ScriptEditorMode = CmbScriptEditorMode?.SelectedItem?.ToString() ?? "Nodos";
        _settings.DefaultPlaceholderAssetsPath = TxtPlaceholderAssetsPath?.Text?.Trim() ?? "";

        _settings.SplashCreatedWithFUEngine = ChkSplashCreatedWith?.IsChecked == true;
        _settings.SplashDurationMs = (TxtSplashDuration != null && int.TryParse(TxtSplashDuration.Text, out var dur) && dur > 0) ? dur : 2500;
        _settings.SplashFadeIn = ChkSplashFadeIn?.IsChecked == true;
        _settings.SplashFadeOut = ChkSplashFadeOut?.IsChecked == true;
        _settings.SplashLogoPath = TxtSplashLogoPath?.Text?.Trim() ?? "Assets/mando_logo_de_fuengine.png";
        _settings.BuildExeIconPath = TxtBuildExeIcon?.Text?.Trim() ?? "";
        _settings.BuildGameVersion = TxtBuildGameVersion?.Text?.Trim() ?? "1.0.0";
        _settings.BuildDefaultWindowMode = CmbBuildWindowMode?.SelectedItem?.ToString() ?? "Windowed";
        _settings.TempBuildPath = TxtTempBuildPath?.Text?.Trim() ?? "";
        _settings.DefaultBuildRuntime = CmbBuildRuntime?.SelectedItem?.ToString() ?? "net8.0-windows";
        _settings.DefaultBuildResolution = CmbBuildResolution?.SelectedItem?.ToString() ?? "1920x1080";
        _settings.DefaultTileScalingBuild = (TxtTileScalingBuild != null && int.TryParse(TxtTileScalingBuild.Text, out var scale) && scale >= 1) ? scale : 1;
        if (ChkAutoOptimizeExport != null) _settings.AutoOptimizeOnExport = ChkAutoOptimizeExport.IsChecked == true;

        _settings.DebugShowOverlay = ChkDebugOverlay?.IsChecked == true;
        _settings.DebugShowFps = ChkDebugFps?.IsChecked == true;
        _settings.DebugShowDrawCalls = ChkDebugDrawCalls?.IsChecked == true;
        _settings.DebugShowMemory = ChkDebugMemory?.IsChecked == true;
        _settings.TestRenderSpriteStacking = ChkTestSpriteStacking?.IsChecked == true;
        _settings.TestRenderDepthBlending = ChkTestDepthBlending?.IsChecked == true;
        _settings.AutoLogsEnabled = ChkAutoLogs?.IsChecked == true;
        _settings.AutoLogsPath = TxtAutoLogsPath?.Text?.Trim() ?? "";
        if (TxtAssetCacheMaxMb != null && int.TryParse(TxtAssetCacheMaxMb.Text, out var cacheMb)) _settings.AssetCacheMaxMb = Math.Max(0, cacheMb);
        if (ChkAssetCacheEnabled != null) _settings.AssetCacheEnabled = ChkAssetCacheEnabled.IsChecked == true;

        _settings.NewProjectRootFolderOrderPresetId = CmbNewProjectFolderOrderPreset?.SelectedIndex == 1 ? "custom" : "default";
        _settings.CustomNewProjectRootFolderOrder = (TxtCustomNewProjectFolderOrder?.Text ?? "")
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();
        _settings.ExtraNewProjectRootFolders = (TxtExtraNewProjectRootFolders?.Text ?? "")
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();
        _settings.NewProjectExplorerThemeId = (CmbNewProjectExplorerTheme?.SelectedItem as ComboBoxItem)?.Tag as string ?? "none";
        _settings.DefaultNewProjectDebugMode = ChkDefaultNewProjectDebugMode?.IsChecked != false;

        _settings.ShortcutBindings ??= new Dictionary<string, string>(StringComparer.Ordinal);
        if (CmbShortcutPreset?.SelectedItem is EditorShortcutPresets.Choice ch)
            _settings.ShortcutPreset = ch.Id;
    }

    public static void ApplyHardwareAccelerationToAllWindows(bool enabled)
    {
        var mode = enabled ? RenderMode.Default : RenderMode.SoftwareOnly;
        foreach (Window w in System.Windows.Application.Current.Windows)
        {
            try
            {
                if (System.Windows.PresentationSource.FromVisual(w) is System.Windows.Interop.HwndSource hs && hs.CompositionTarget != null)
                    hs.CompositionTarget.RenderMode = mode;
            }
            catch { /* ignore */ }
        }
    }

    public void SaveSettings()
    {
        try
        {
            ReadFromUi();
            var dir = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var options = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };
            var json = System.Text.Json.JsonSerializer.Serialize(_settings, options);
            File.WriteAllText(SettingsPath, json);

            ApplyHardwareAccelerationToAllWindows(_settings.HardwareAccelerationEnabled);

            var w = Window.GetWindow(this);
            if (w != null)
                EngineTypography.ApplyToRoot(w);
            var main = System.Windows.Application.Current?.MainWindow as System.Windows.Controls.Control;
            if (main != null && main != w)
                EngineTypography.ApplyToRoot(main);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Error al guardar: " + ex.Message, "Configuración", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnBrowseFolder(string description, System.Windows.Controls.TextBox target)
    {
        using var dlg = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = description,
            SelectedPath = target?.Text ?? ""
        };
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK && target != null)
            target.Text = dlg.SelectedPath;
    }

    private void BtnBrowseProjects_OnClick(object sender, RoutedEventArgs e) => BtnBrowseFolder("Carpeta por defecto para proyectos", TxtProjectsPath);
    private void BtnBrowseSharedAssets_OnClick(object sender, RoutedEventArgs e) => BtnBrowseFolder("Carpeta de assets compartidos", TxtSharedAssetsPath);
    private void BtnBrowseBuildExport_OnClick(object sender, RoutedEventArgs e) => BtnBrowseFolder("Carpeta de exportación de builds", TxtBuildExportPath);
    private void BtnBrowsePlaceholder_OnClick(object sender, RoutedEventArgs e) => BtnBrowseFolder("Carpeta de placeholder assets", TxtPlaceholderAssetsPath);
    private void BtnBrowseTempBuild_OnClick(object sender, RoutedEventArgs e) => BtnBrowseFolder("Carpeta de compilación temporal", TxtTempBuildPath);
    private void BtnBrowseAi_OnClick(object sender, RoutedEventArgs e) => BtnBrowseFolder("Ruta de integración con IA", TxtAiPath);
    private void BtnBrowseLogs_OnClick(object sender, RoutedEventArgs e) => BtnBrowseFolder("Carpeta de logs automáticos", TxtAutoLogsPath);

    private void BtnBrowseExternalEditor_OnClick(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Ejecutable (*.exe)|*.exe|Todos|*.*",
            Title = "Editor de código externo"
        };
        if (dlg.ShowDialog() == true && TxtExternalCodeEditor != null)
            TxtExternalCodeEditor.Text = dlg.FileName;
    }

    private void BtnBrowseBuildIcon_OnClick(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Icono (*.ico)|*.ico|Todos|*.*",
            Title = "Icono del ejecutable"
        };
        if (dlg.ShowDialog() == true && TxtBuildExeIcon != null)
            TxtBuildExeIcon.Text = dlg.FileName;
    }
}
