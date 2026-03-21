using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FUEngine.Core;
using FUEngine.Editor;

namespace FUEngine;

public sealed class DropItem
{
    public string Display { get; set; } = "";
    public object? Value { get; set; }
}

/// <summary>Escena seleccionable como "Start": guarda mapa + objetos por separado.</summary>
public sealed class MainSceneChoice
{
    public string Display { get; set; } = "";
    public string MapPathRelative { get; set; } = "mapa.json";
    public string ObjectsPathRelative { get; set; } = "objetos.json";
}

/// <summary>Entrada del combo de protagonista en configuración del proyecto.</summary>
public sealed class ProtagonistPickerItem
{
    public string? InstanceId { get; init; }
    public string Label { get; init; } = "";
}

public partial class ProjectConfigWindow : Window
{
    private readonly ProjectInfo _project;
    private readonly string? _projectFilePath;
    private readonly ObjectLayer? _protagonistPickerLayer;

    public ProjectConfigWindow(ProjectInfo project, string? projectFilePath = null, ObjectLayer? protagonistPickerLayer = null)
    {
        InitializeComponent();
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _projectFilePath = projectFilePath;
        _protagonistPickerLayer = protagonistPickerLayer;
        LoadFromProject();
    }

    private void LoadFromProject()
    {
        TxtNombre.Text = _project.Nombre ?? "";
        TxtDescripcion.Text = _project.Descripcion ?? "";
        PopulateLogoCombo();
        CmbLogo.Text = _project.IconPath ?? "";
        TxtAuthor.Text = _project.Author ?? "";
        TxtCopyright.Text = _project.Copyright ?? "";
        TxtVersion.Text = _project.Version ?? "0.0.1";
        var mainSceneItems = new List<MainSceneChoice>();
        if (_project.Scenes != null && _project.Scenes.Count > 0)
        {
            foreach (var s in _project.Scenes)
                mainSceneItems.Add(new MainSceneChoice
                {
                    Display = (s.Name ?? s.Id ?? "Escena") + " — mapa: " + (s.MapPathRelative ?? "mapa.json") + ", objetos: " + (s.ObjectsPathRelative ?? "objetos.json"),
                    MapPathRelative = s.MapPathRelative ?? "mapa.json",
                    ObjectsPathRelative = s.ObjectsPathRelative ?? "objetos.json"
                });
        }
        if (mainSceneItems.Count == 0)
            mainSceneItems.Add(new MainSceneChoice { Display = "Principal (mapa.json + objetos.json)", MapPathRelative = "mapa.json", ObjectsPathRelative = "objetos.json" });
        CmbMainScene.ItemsSource = mainSceneItems;
        var match = mainSceneItems.FirstOrDefault(i =>
            string.Equals(i.MapPathRelative, _project.MainMapPath?.Trim(), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(i.ObjectsPathRelative, _project.MainObjectsPath?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match == null)
            match = mainSceneItems.FirstOrDefault(i => string.Equals(i.ObjectsPathRelative, _project.MainObjectsPath?.Trim(), StringComparison.OrdinalIgnoreCase));
        CmbMainScene.SelectedItem = match ?? mainSceneItems.FirstOrDefault();

        ChkAutoSaveEnabled.IsChecked = _project.AutoSaveEnabled;
        TxtIntervalMinutes.Text = _project.AutoSaveIntervalMinutes > 0 ? _project.AutoSaveIntervalMinutes.ToString() : "5";
        TxtMaxBackups.Text = _project.AutoSaveMaxBackupsPerType > 0 ? _project.AutoSaveMaxBackupsPerType.ToString() : "10";
        TxtAutoSaveFolder.Text = _project.AutoSaveFolder ?? "Autoguardados";
        ChkAutoSaveOnClose.IsChecked = _project.AutoSaveOnClose;
        if (ChkGuardarSoloCambios != null) ChkGuardarSoloCambios.IsChecked = _project.AutoSaveOnlyWhenDirty;
        UpdateAutosaveEnabledState();
        UpdateAutoSaveResolvedPath();

        FillDropdowns();
        TxtTileSizeDisplay!.Text = _project.TileSize switch { 8 => "8×8 px", 32 => "32×32 px", 64 => "64×64 px", 128 => "128×128 px", _ => "16×16 px" };
        var resStr = _project.GameResolutionWidth <= 0 || _project.GameResolutionHeight <= 0 ? "Auto" : $"{_project.GameResolutionWidth}×{_project.GameResolutionHeight}";
        TxtResolutionDisplay!.Text = ResolutionPresets.FirstOrDefault(r => string.Equals(r.value, resStr, StringComparison.OrdinalIgnoreCase)).display ?? resStr;
        var fpsPreset = FpsPresets.FirstOrDefault(f => f.fps == _project.Fps);
        TxtFpsDisplay!.Text = fpsPreset.label ?? _project.Fps.ToString();
        TxtFontDisplay!.Text = _project.GameFontFamily ?? "Default";
        if (TxtAnimationSpeed != null) TxtAnimationSpeed.Text = _project.AnimationSpeedMultiplier.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (TxtGameFontSize != null) TxtGameFontSize.Text = _project.GameFontSize.ToString();
        TxtInputDisplay!.Text = _project.DefaultInputScheme switch { "Joystick" => "Joystick", "Both" => "Both", _ => "Keyboard" };
        TxtChunkSizeDisplay!.Text = _project.ChunkSize switch { 8 => "8×8", 16 => "16×16", 64 => "64×64", _ => "32×32 (recomendado)" };
        TxtChunkRadiusDisplay!.Text = _project.ChunkLoadRadius.ToString();
        TxtExportImageDisplay!.Text = _project.ExportFormatImage == "WebP" ? "WebP" : "PNG";
        TxtExportAudioDisplay!.Text = _project.ExportFormatAudio switch { "WAV" => "WAV", "MP3" => "MP3", _ => "OGG" };
        SetDropdownSelectionsFromProject();

        if (ChkDebugMode != null) ChkDebugMode.IsChecked = _project.DebugMode;
        if (TxtAssetsRoot != null) TxtAssetsRoot.Text = _project.AssetsRootFolder ?? "Assets";
        if (TxtProjectGridSnap != null) TxtProjectGridSnap.Text = _project.ProjectGridSnapPx.ToString();
        if (TxtDefaultSceneBg != null) TxtDefaultSceneBg.Text = _project.DefaultFirstSceneBackgroundColor ?? "#1a1a2e";
        if (TxtNamingObjects != null) TxtNamingObjects.Text = _project.NamingRuleObjects ?? "libre";
        if (TxtNamingSeeds != null) TxtNamingSeeds.Text = _project.NamingRuleSeeds ?? "libre";
        if (TxtCameraW != null) TxtCameraW.Text = _project.CameraSizeWidth.ToString();
        if (TxtCameraH != null) TxtCameraH.Text = _project.CameraSizeHeight.ToString();
        if (TxtCameraLimits != null) TxtCameraLimits.Text = _project.CameraLimits ?? "";
        if (TxtCameraEffects != null) TxtCameraEffects.Text = _project.CameraEffects ?? "";
        if (TxtProjectPlugins != null) TxtProjectPlugins.Text = _project.ProjectEnabledPlugins != null ? string.Join(", ", _project.ProjectEnabledPlugins) : "";
        if (TxtDefaultAnimFps != null) TxtDefaultAnimFps.Text = _project.DefaultAnimationFps.ToString();
        if (ChkDefaultCollision != null) ChkDefaultCollision.IsChecked = _project.DefaultCollisionEnabled;
        if (ChkPhysicsEnabled != null) ChkPhysicsEnabled.IsChecked = _project.PhysicsEnabled;
        if (TxtPhysicsGravity != null) TxtPhysicsGravity.Text = _project.PhysicsGravity.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (TxtBootstrapScript != null) TxtBootstrapScript.Text = _project.BootstrapScriptId ?? "";

        if (ChkChunkUnloadFar != null) ChkChunkUnloadFar.IsChecked = _project.ChunkUnloadFar;
        if (ChkChunkSaveByChunk != null) ChkChunkSaveByChunk.IsChecked = _project.ChunkSaveByChunk;
        if (ChkChunkEntitySleep != null) ChkChunkEntitySleep.IsChecked = _project.ChunkEntitySleep;
        if (ChkChunkStreaming != null) ChkChunkStreaming.IsChecked = _project.ChunkStreaming;
        if (ChkShowChunkBounds != null) ChkShowChunkBounds.IsChecked = _project.ShowChunkBounds;

        RefreshProtagonistCombo();
        if (ChkUseNativeInput != null) ChkUseNativeInput.IsChecked = _project.UseNativeInput;
        if (ChkUseNativeCameraFollow != null) ChkUseNativeCameraFollow.IsChecked = _project.UseNativeCameraFollow;
        if (ChkAutoFlipSprite != null) ChkAutoFlipSprite.IsChecked = _project.AutoFlipSprite;
        if (ChkUseNativeAutoAnimation != null) ChkUseNativeAutoAnimation.IsChecked = _project.UseNativeAutoAnimation;
        if (TxtNativeCameraSmoothing != null) TxtNativeCameraSmoothing.Text = _project.NativeCameraSmoothing.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (TxtNativeMoveSpeed != null) TxtNativeMoveSpeed.Text = _project.NativeMoveSpeedTilesPerSecond.ToString(System.Globalization.CultureInfo.InvariantCulture);

        if (TxtAudioManifest != null) TxtAudioManifest.Text = string.IsNullOrWhiteSpace(_project.AudioManifestPath) ? "audio.json" : _project.AudioManifestPath;
        if (SldMasterVolume != null) SldMasterVolume.Value = Math.Clamp(_project.MasterVolume, 0, 1);
        if (SldMusicVolume != null) SldMusicVolume.Value = Math.Clamp(_project.MusicVolume, 0, 1);
        if (SldSfxVolume != null) SldSfxVolume.Value = Math.Clamp(_project.SfxVolume, 0, 1);
        RefreshAudioVolumeLabels();
    }

    private void RefreshAudioVolumeLabels()
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        if (TxtAudioMasterVal != null && SldMasterVolume != null) TxtAudioMasterVal.Text = SldMasterVolume.Value.ToString("0.00", inv);
        if (TxtAudioMusicVal != null && SldMusicVolume != null) TxtAudioMusicVal.Text = SldMusicVolume.Value.ToString("0.00", inv);
        if (TxtAudioSfxVal != null && SldSfxVolume != null) TxtAudioSfxVal.Text = SldSfxVolume.Value.ToString("0.00", inv);
    }

    private void AudioVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        RefreshAudioVolumeLabels();
    }

    private void BtnClearProtagonist_OnClick(object sender, RoutedEventArgs e)
    {
        _project.ProtagonistInstanceId = null;
        RefreshProtagonistCombo();
    }

    private void RefreshProtagonistCombo()
    {
        if (CmbProtagonistInstance == null) return;
        var items = new List<ProtagonistPickerItem>
        {
            new() { InstanceId = null, Label = "(ninguno)" }
        };
        var savedId = _project.ProtagonistInstanceId;
        if (_protagonistPickerLayer?.Instances is { Count: > 0 } instList)
        {
            foreach (var inst in instList.OrderBy(i => i.Nombre ?? "").ThenBy(i => i.InstanceId ?? ""))
                items.Add(new ProtagonistPickerItem { InstanceId = inst.InstanceId, Label = FormatProtagonistLabel(inst, _protagonistPickerLayer) });
        }
        if (!string.IsNullOrEmpty(savedId) && items.All(i => i.InstanceId == null || !string.Equals(i.InstanceId, savedId, StringComparison.Ordinal)))
            items.Add(new ProtagonistPickerItem { InstanceId = savedId, Label = $"⚠ {savedId} (no en escena actual)" });
        CmbProtagonistInstance.ItemsSource = items;
        var sel = items.FirstOrDefault(i => string.IsNullOrEmpty(savedId)
            ? i.InstanceId == null
            : (i.InstanceId != null && string.Equals(i.InstanceId, savedId, StringComparison.Ordinal)));
        CmbProtagonistInstance.SelectedItem = sel ?? items[0];
    }

    private static string FormatProtagonistLabel(ObjectInstance inst, ObjectLayer layer)
    {
        var def = layer.GetDefinition(inst.DefinitionId);
        var name = !string.IsNullOrWhiteSpace(inst.Nombre) ? inst.Nombre.Trim() : (def?.Nombre ?? inst.DefinitionId ?? "?");
        return $"{name}  ·  {inst.InstanceId}";
    }

    private void ChkAutoSaveEnabled_Changed(object sender, RoutedEventArgs e)
    {
        UpdateAutosaveEnabledState();
    }

    private void UpdateAutosaveEnabledState()
    {
        bool enabled = ChkAutoSaveEnabled.IsChecked == true;
        TxtIntervalMinutes.IsEnabled = enabled;
        TxtMaxBackups.IsEnabled = enabled;
        TxtAutoSaveFolder.IsEnabled = enabled;
        BtnBrowseAutoSaveFolder.IsEnabled = enabled;
        ChkAutoSaveOnClose.IsEnabled = enabled;
        if (ChkGuardarSoloCambios != null) ChkGuardarSoloCambios.IsEnabled = enabled;
    }

    private void BtnBrowseAutoSaveFolder_OnClick(object sender, RoutedEventArgs e)
    {
        using var dlg = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Seleccionar carpeta de autoguardados",
            UseDescriptionForTitle = true
        };
        if (!string.IsNullOrEmpty(_project.ProjectDirectory) && Directory.Exists(_project.ProjectDirectory))
            dlg.InitialDirectory = _project.ProjectDirectory;
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK && !string.IsNullOrEmpty(dlg.SelectedPath))
        {
            var dir = dlg.SelectedPath;
            if (!string.IsNullOrEmpty(_project.ProjectDirectory) && dir.StartsWith(_project.ProjectDirectory, StringComparison.OrdinalIgnoreCase))
            {
                var rel = dir.Substring(_project.ProjectDirectory.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                TxtAutoSaveFolder.Text = string.IsNullOrEmpty(rel) ? "Autoguardados" : rel;
            }
            else
                TxtAutoSaveFolder.Text = dir;
            UpdateAutoSaveResolvedPath();
        }
    }

    private void TxtAutoSaveFolder_OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        UpdateAutoSaveResolvedPath();
    }

    private void UpdateAutoSaveResolvedPath()
    {
        if (TxtAutoSaveResolvedPath == null || TxtAutoSaveInsideProject == null) return;
        var folder = TxtAutoSaveFolder?.Text?.Trim() ?? "Autoguardados";
        var projectDir = _project.ProjectDirectory ?? "";
        string resolvedPath;
        if (string.IsNullOrEmpty(folder))
        {
            resolvedPath = Path.Combine(projectDir, "Autoguardados");
        }
        else if (Path.IsPathRooted(folder))
        {
            resolvedPath = folder;
        }
        else
        {
            resolvedPath = Path.Combine(projectDir, folder);
        }
        try { resolvedPath = Path.GetFullPath(resolvedPath); } catch { /* leave as is */ }
        var projectDirFull = string.IsNullOrEmpty(projectDir) ? "" : Path.GetFullPath(projectDir);
        var isInside = !string.IsNullOrEmpty(projectDirFull) && resolvedPath.StartsWith(projectDirFull, StringComparison.OrdinalIgnoreCase);
        TxtAutoSaveResolvedPath.Text = "Ruta absoluta: " + resolvedPath;
        TxtAutoSaveInsideProject.Text = isInside ? "Dentro del proyecto" : "Fuera del proyecto";
    }

    private void BtnCancel_OnClick(object sender, RoutedEventArgs e) => DialogResult = false;

    private void BtnOk_OnClick(object sender, RoutedEventArgs e)
    {
        var selTile = ListTileSize?.SelectedItem as DropItem;
        var newTileSize = selTile?.Value as int? ?? 16;
        if (newTileSize != _project.TileSize)
        {
            var result = System.Windows.MessageBox.Show(this,
                "Cambiar el tamaño de tiles puede afectar a capas y posiciones existentes. ¿Deseas continuar?",
                "Advertencia: tamaño de tiles",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
                return;
        }
        if (!TryApply(out var error))
        {
            System.Windows.MessageBox.Show(this, error, "Configuración del proyecto", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var projectPath = _projectFilePath ?? (string.IsNullOrEmpty(_project.ProjectDirectory) ? null : Path.Combine(_project.ProjectDirectory, NewProjectStructure.ProjectFileName));
        if (string.IsNullOrEmpty(projectPath))
        {
            System.Windows.MessageBox.Show(this, "No se puede guardar: el proyecto no está asociado a un archivo en disco.", "Guardar", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            ProjectSerialization.Save(_project, projectPath);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "No se pudo guardar el proyecto: " + ex.Message, "Guardar", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }

    private static readonly (string value, string display)[] ResolutionPresets =
    {
        ("320×180", "320×180"), ("480×270", "480×270"), ("512×288", "512×288"),
        ("640×360", "640×360"), ("800×450", "800×450"), ("960×540", "960×540"), ("1024×576", "1024×576"),
        ("1280×720", "1280×720 → HD"), ("1366×768", "1366×768"), ("1600×900", "1600×900"), ("1920×1080", "1920×1080 → Full HD"), ("2560×1440", "2560×1440 → QHD"), ("3840×2160", "3840×2160 → 4K"),
        ("1280×800", "1280×800 → 16:10"), ("1440×900", "1440×900 → 16:10"), ("1680×1050", "1680×1050 → 16:10"),
        ("1024×768", "1024×768 → 4:3"), ("1280×960", "1280×960 → 4:3"), ("1600×1200", "1600×1200 → 4:3"),
        ("3440×1440", "3440×1440 → ultrawide"), ("5120×2880", "5120×2880 → 5K"), ("7680×4320", "7680×4320 → 8K"),
        ("Auto", "Auto")
    };

    private static readonly (int fps, string label)[] FpsPresets =
    {
        (15, "15 fps — testing / low performance"),
        (24, "24 fps — estilo cine / animación tradicional"),
        (30, "30 fps — estándar bajo / móvil"),
        (60, "60 fps — estándar común / Full HD"),
        (75, "75 fps — monitores 75Hz"),
        (90, "90 fps — VR / gaming"),
        (120, "120 fps — monitores 120Hz"),
        (144, "144 fps — gaming high-end"),
        (165, "165 fps — monitores gaming high refresh"),
        (240, "240 fps — ultra gaming")
    };

    private static readonly string[] FontPresets =
    {
        "Default", "Auto",
        "Press Start 2P", "PixelOperator", "Minecraft",
        "Arial", "Verdana", "Segoe UI", "Roboto", "Open Sans",
        "Arcade Classic", "VT323", "Pixeled"
    };

    private void PopulateLogoCombo()
    {
        if (CmbLogo == null || string.IsNullOrEmpty(_project.ProjectDirectory) || !Directory.Exists(_project.ProjectDirectory)) return;
        var projectDir = _project.ProjectDirectory;
        var logoSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var f in Directory.EnumerateFiles(projectDir, "*.ico", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(projectDir, f);
                if (!rel.StartsWith("..", StringComparison.Ordinal)) logoSet.Add(rel);
            }
            foreach (var f in Directory.EnumerateFiles(projectDir, "*.png", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(projectDir, f);
                if (rel.StartsWith("..", StringComparison.Ordinal)) continue;
                if (CreativeSuiteMetadata.IsPaint(f)) logoSet.Add(rel);
            }
        }
        catch { /* ignore */ }
        var logoPaths = logoSet.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
        CmbLogo.ItemsSource = logoPaths;
    }

    private void FillDropdowns()
    {
        ListTileSize.ItemsSource = new[] { ("8×8 px", 8), ("16×16 px", 16), ("32×32 px", 32), ("64×64 px", 64), ("128×128 px", 128) }.Select(x => new DropItem { Display = x.Item1, Value = x.Item2 }).ToList();
        ListResolution.ItemsSource = ResolutionPresets.Select(r => new DropItem { Display = r.display, Value = r.value }).ToList();
        ListFps.ItemsSource = FpsPresets.Select(f => new DropItem { Display = f.label, Value = f.fps }).ToList();
        ListFont.ItemsSource = FontPresets.Select(f => new DropItem { Display = f, Value = f }).ToList();
        ListInput.ItemsSource = new[] { new DropItem { Display = "Keyboard", Value = "Keyboard" }, new DropItem { Display = "Joystick", Value = "Joystick" }, new DropItem { Display = "Both", Value = "Both" } };
        ListChunkSize.ItemsSource = new[] { new DropItem { Display = "8×8", Value = 8 }, new DropItem { Display = "16×16", Value = 16 }, new DropItem { Display = "32×32 (recomendado)", Value = 32 }, new DropItem { Display = "64×64", Value = 64 } };
        ListChunkRadius.ItemsSource = new[] { 1, 2, 3, 4 }.Select(i => new DropItem { Display = i.ToString(), Value = i }).ToList();
        ListExportImage.ItemsSource = new[] { new DropItem { Display = "PNG", Value = "PNG" }, new DropItem { Display = "WebP", Value = "WebP" } };
        ListExportAudio.ItemsSource = new[] { new DropItem { Display = "OGG", Value = "OGG" }, new DropItem { Display = "WAV", Value = "WAV" }, new DropItem { Display = "MP3", Value = "MP3" } };
    }

    private void SetDropdownSelectionsFromProject()
    {
        SelectByValue(ListTileSize, _project.TileSize);
        SelectResolutionByValue(_project.GameResolutionWidth <= 0 ? "Auto" : $"{_project.GameResolutionWidth}×{_project.GameResolutionHeight}");
        SelectByValue(ListFps, _project.Fps);
        SelectByValue(ListFont, _project.GameFontFamily ?? "Default");
        SelectByValue(ListInput, _project.DefaultInputScheme ?? "Keyboard");
        SelectByValue(ListChunkSize, _project.ChunkSize);
        SelectByValue(ListChunkRadius, _project.ChunkLoadRadius);
        SelectByValue(ListExportImage, _project.ExportFormatImage ?? "PNG");
        SelectByValue(ListExportAudio, _project.ExportFormatAudio ?? "OGG");
    }

    private static void SelectByValue(System.Windows.Controls.ListBox list, object? value)
    {
        if (list?.ItemsSource is System.Collections.IEnumerable en)
            foreach (DropItem item in en)
                if (Equals(item.Value, value)) { list.SelectedItem = item; return; }
    }

    private void SelectResolutionByValue(string value)
    {
        if (ListResolution?.ItemsSource is System.Collections.IEnumerable en)
            foreach (DropItem item in en)
                if (item.Value is string s && string.Equals(s, value, StringComparison.OrdinalIgnoreCase)) { ListResolution.SelectedItem = item; return; }
    }

    private void TileSizeDrop_Open(object sender, System.Windows.Input.MouseButtonEventArgs e) { PopupTileSize.IsOpen = true; }
    private void ResolutionDrop_Open(object sender, System.Windows.Input.MouseButtonEventArgs e) { PopupResolution.IsOpen = true; }
    private void FpsDrop_Open(object sender, System.Windows.Input.MouseButtonEventArgs e) { PopupFps.IsOpen = true; }
    private void FontDrop_Open(object sender, System.Windows.Input.MouseButtonEventArgs e) { PopupFont.IsOpen = true; }
    private void InputDrop_Open(object sender, System.Windows.Input.MouseButtonEventArgs e) { PopupInput.IsOpen = true; }
    private void ChunkSizeDrop_Open(object sender, System.Windows.Input.MouseButtonEventArgs e) { PopupChunkSize.IsOpen = true; }
    private void ChunkRadiusDrop_Open(object sender, System.Windows.Input.MouseButtonEventArgs e) { PopupChunkRadius.IsOpen = true; }
    private void ExportImageDrop_Open(object sender, System.Windows.Input.MouseButtonEventArgs e) { PopupExportImage.IsOpen = true; }
    private void ExportAudioDrop_Open(object sender, System.Windows.Input.MouseButtonEventArgs e) { PopupExportAudio.IsOpen = true; }

    private void ListTileSize_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ListTileSize.SelectedItem is DropItem d) { TxtTileSizeDisplay.Text = d.Display; PopupTileSize.IsOpen = false; }
    }
    private void ListResolution_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ListResolution.SelectedItem is DropItem d) { TxtResolutionDisplay.Text = d.Display; PopupResolution.IsOpen = false; }
    }
    private void ListFps_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ListFps.SelectedItem is DropItem d) { TxtFpsDisplay.Text = d.Display; PopupFps.IsOpen = false; }
    }
    private void ListFont_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ListFont.SelectedItem is DropItem d) { TxtFontDisplay.Text = d.Display; PopupFont.IsOpen = false; }
    }
    private void ListInput_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ListInput.SelectedItem is DropItem d) { TxtInputDisplay.Text = d.Display; PopupInput.IsOpen = false; }
    }
    private void ListChunkSize_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ListChunkSize.SelectedItem is DropItem d) { TxtChunkSizeDisplay.Text = d.Display; PopupChunkSize.IsOpen = false; }
    }
    private void ListChunkRadius_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ListChunkRadius.SelectedItem is DropItem d) { TxtChunkRadiusDisplay.Text = d.Display; PopupChunkRadius.IsOpen = false; }
    }
    private void ListExportImage_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ListExportImage.SelectedItem is DropItem d) { TxtExportImageDisplay.Text = d.Display; PopupExportImage.IsOpen = false; }
    }
    private void ListExportAudio_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ListExportAudio.SelectedItem is DropItem d) { TxtExportAudioDisplay.Text = d.Display; PopupExportAudio.IsOpen = false; }
    }

    private void BtnBrowseFont_OnClick(object sender, RoutedEventArgs e)
    {
        using var dlg = new System.Windows.Forms.OpenFileDialog
        {
            Filter = "Fuentes (*.ttf;*.otf)|*.ttf;*.otf|Todos|*.*",
            Title = "Seleccionar fuente personalizada"
        };
        if (!string.IsNullOrEmpty(_project.ProjectDirectory) && Directory.Exists(_project.ProjectDirectory))
            dlg.InitialDirectory = _project.ProjectDirectory;
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK && !string.IsNullOrEmpty(dlg.FileName))
        {
            var path = dlg.FileName;
            if (!string.IsNullOrEmpty(_project.ProjectDirectory) && path.StartsWith(_project.ProjectDirectory, StringComparison.OrdinalIgnoreCase))
            {
                var rel = path.Substring(_project.ProjectDirectory.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                TxtFontDisplay.Text = rel;
            }
            else
                TxtFontDisplay.Text = path;
        }
    }

    private bool TryApply(out string error)
    {
        error = "";

        _project.Nombre = TxtNombre.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(_project.Nombre))
        {
            error = "El nombre del proyecto no puede estar vacío.";
            return false;
        }

        _project.Descripcion = TxtDescripcion.Text?.Trim() ?? "";
        _project.IconPath = string.IsNullOrWhiteSpace(CmbLogo?.Text) ? null : CmbLogo.Text.Trim();
        if (ListTileSize.SelectedItem is DropItem tileItem && tileItem.Value is int tileSize && tileSize >= 8 && tileSize <= 128)
            _project.TileSize = tileSize;
        else
        {
            var ts = TxtTileSizeDisplay?.Text;
            if (int.TryParse(ts?.Replace("×", "").Replace(" px", "").Split(' ')[0], out var tsv) && tsv >= 8 && tsv <= 128)
                _project.TileSize = tsv;
        }
        _project.Author = string.IsNullOrWhiteSpace(TxtAuthor.Text) ? null : TxtAuthor.Text.Trim();
        _project.Copyright = string.IsNullOrWhiteSpace(TxtCopyright.Text) ? null : TxtCopyright.Text.Trim();
        _project.Version = string.IsNullOrWhiteSpace(TxtVersion.Text) ? "0.0.1" : TxtVersion.Text.Trim();
        var mainScene = CmbMainScene.SelectedItem as MainSceneChoice;
        if (mainScene != null)
        {
            _project.MainMapPath = mainScene.MapPathRelative ?? "mapa.json";
            _project.MainObjectsPath = mainScene.ObjectsPathRelative ?? "objetos.json";
        }

        _project.AutoSaveEnabled = ChkAutoSaveEnabled?.IsChecked == true;
        if (TxtIntervalMinutes != null && int.TryParse(TxtIntervalMinutes.Text, out var min) && min >= 1 && min <= 120)
            _project.AutoSaveIntervalMinutes = min;
        else if (_project.AutoSaveEnabled)
        {
            error = "Intervalo de autoguardado debe ser un número entre 1 y 120 minutos.";
            return false;
        }
        else
            _project.AutoSaveIntervalMinutes = 5;

        if (TxtMaxBackups != null && int.TryParse(TxtMaxBackups.Text, out var max) && max >= 1 && max <= 100)
            _project.AutoSaveMaxBackupsPerType = max;
        else
            _project.AutoSaveMaxBackupsPerType = 10;

        _project.AutoSaveFolder = string.IsNullOrWhiteSpace(TxtAutoSaveFolder?.Text) ? "Autoguardados" : TxtAutoSaveFolder!.Text.Trim();
        _project.AutoSaveOnClose = ChkAutoSaveOnClose?.IsChecked == true;
        _project.AutoSaveOnlyWhenDirty = ChkGuardarSoloCambios?.IsChecked == true;

        var resText = ListResolution.SelectedItem is DropItem rd && rd.Value is string rv ? rv : TxtResolutionDisplay?.Text?.Trim() ?? "";
        if (resText.Equals("Auto", StringComparison.OrdinalIgnoreCase))
        {
            _project.GameResolutionWidth = 0;
            _project.GameResolutionHeight = 0;
        }
        else
        {
            var parts = resText.Split('×', 'x', 'X');
            if (parts.Length >= 2 && int.TryParse(parts[0].Trim(), out var gw) && int.TryParse(parts[1].Trim(), out var gh) && gw > 0 && gh > 0)
            {
                _project.GameResolutionWidth = gw;
                _project.GameResolutionHeight = gh;
            }
        }
        if (ListFps.SelectedItem is DropItem fd && fd.Value is int fpsVal && fpsVal >= 1 && fpsVal <= 240)
            _project.Fps = fpsVal;
        if (TxtAnimationSpeed != null && double.TryParse(TxtAnimationSpeed.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var animSp) && animSp > 0) _project.AnimationSpeedMultiplier = animSp;
        _project.GameFontFamily = ListFont.SelectedItem is DropItem fnd && fnd.Value is string fontTag ? (string.IsNullOrWhiteSpace(fontTag) ? null : fontTag.Trim()) : (string.IsNullOrWhiteSpace(TxtFontDisplay?.Text) ? null : TxtFontDisplay.Text.Trim());
        if (TxtGameFontSize != null && int.TryParse(TxtGameFontSize.Text, out var gfs) && gfs > 0) _project.GameFontSize = gfs;
        _project.DefaultInputScheme = ListInput.SelectedItem is DropItem id && id.Value is string inputVal ? inputVal : (TxtInputDisplay?.Text ?? "Keyboard");

        if (ChkDebugMode != null) _project.DebugMode = ChkDebugMode.IsChecked == true;
        if (TxtAssetsRoot != null) _project.AssetsRootFolder = string.IsNullOrWhiteSpace(TxtAssetsRoot.Text) ? "Assets" : TxtAssetsRoot.Text.Trim();
        if (TxtProjectGridSnap != null && int.TryParse(TxtProjectGridSnap.Text, out var pgs)) _project.ProjectGridSnapPx = Math.Max(0, pgs);
        if (TxtDefaultSceneBg != null) _project.DefaultFirstSceneBackgroundColor = string.IsNullOrWhiteSpace(TxtDefaultSceneBg.Text) ? "#1a1a2e" : TxtDefaultSceneBg.Text.Trim();
        _project.ExportFormatImage = ListExportImage.SelectedItem is DropItem ei && ei.Value is string imgFmt ? imgFmt : "PNG";
        _project.ExportFormatAudio = ListExportAudio.SelectedItem is DropItem ea && ea.Value is string audFmt ? audFmt : "OGG";
        if (TxtNamingObjects != null) _project.NamingRuleObjects = string.IsNullOrWhiteSpace(TxtNamingObjects.Text) ? "libre" : TxtNamingObjects.Text.Trim();
        if (TxtNamingSeeds != null) _project.NamingRuleSeeds = string.IsNullOrWhiteSpace(TxtNamingSeeds.Text) ? "libre" : TxtNamingSeeds.Text.Trim();
        if (TxtCameraW != null && int.TryParse(TxtCameraW.Text, out var cw) && cw > 0) _project.CameraSizeWidth = cw;
        if (TxtCameraH != null && int.TryParse(TxtCameraH.Text, out var ch) && ch > 0) _project.CameraSizeHeight = ch;
        if (TxtCameraLimits != null) _project.CameraLimits = string.IsNullOrWhiteSpace(TxtCameraLimits.Text) ? null : TxtCameraLimits.Text.Trim();
        if (TxtCameraEffects != null) _project.CameraEffects = string.IsNullOrWhiteSpace(TxtCameraEffects.Text) ? null : TxtCameraEffects.Text.Trim();
        if (TxtProjectPlugins != null) _project.ProjectEnabledPlugins = (TxtProjectPlugins.Text ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        if (TxtDefaultAnimFps != null && int.TryParse(TxtDefaultAnimFps.Text, out var daf) && daf >= 1) _project.DefaultAnimationFps = daf;
        if (ChkDefaultCollision != null) _project.DefaultCollisionEnabled = ChkDefaultCollision.IsChecked == true;
        if (ChkPhysicsEnabled != null) _project.PhysicsEnabled = ChkPhysicsEnabled.IsChecked == true;
        if (TxtPhysicsGravity != null && double.TryParse(TxtPhysicsGravity.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var grav)) _project.PhysicsGravity = grav;
        if (TxtBootstrapScript != null) _project.BootstrapScriptId = string.IsNullOrWhiteSpace(TxtBootstrapScript.Text) ? null : TxtBootstrapScript.Text.Trim();

        if (ListChunkSize.SelectedItem is DropItem csd && csd.Value is int newChunkSize && newChunkSize >= 8 && newChunkSize <= 64)
            _project.ChunkSize = newChunkSize;
        if (ListChunkRadius.SelectedItem is DropItem crd && crd.Value is int radius && radius >= 1 && radius <= 4)
            _project.ChunkLoadRadius = radius;
        if (ChkChunkUnloadFar != null) _project.ChunkUnloadFar = ChkChunkUnloadFar.IsChecked == true;
        if (ChkChunkSaveByChunk != null) _project.ChunkSaveByChunk = ChkChunkSaveByChunk.IsChecked == true;
        if (ChkChunkEntitySleep != null) _project.ChunkEntitySleep = ChkChunkEntitySleep.IsChecked == true;
        if (ChkChunkStreaming != null) _project.ChunkStreaming = ChkChunkStreaming.IsChecked == true;
        if (ChkShowChunkBounds != null) _project.ShowChunkBounds = ChkShowChunkBounds.IsChecked == true;

        if (CmbProtagonistInstance?.SelectedItem is ProtagonistPickerItem pick)
            _project.ProtagonistInstanceId = string.IsNullOrEmpty(pick.InstanceId) ? null : pick.InstanceId;

        if (ChkUseNativeInput != null) _project.UseNativeInput = ChkUseNativeInput.IsChecked == true;
        if (ChkUseNativeCameraFollow != null) _project.UseNativeCameraFollow = ChkUseNativeCameraFollow.IsChecked == true;
        if (ChkAutoFlipSprite != null) _project.AutoFlipSprite = ChkAutoFlipSprite.IsChecked == true;
        if (ChkUseNativeAutoAnimation != null) _project.UseNativeAutoAnimation = ChkUseNativeAutoAnimation.IsChecked == true;
        if (TxtNativeCameraSmoothing != null && float.TryParse(TxtNativeCameraSmoothing.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var sm))
            _project.NativeCameraSmoothing = sm < 0 ? 8f : sm;
        if (TxtNativeMoveSpeed != null && float.TryParse(TxtNativeMoveSpeed.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var mv) && mv > 0)
            _project.NativeMoveSpeedTilesPerSecond = mv;

        _project.AudioManifestPath = string.IsNullOrWhiteSpace(TxtAudioManifest?.Text) ? "audio.json" : TxtAudioManifest.Text.Trim();
        _project.MasterVolume = (float)Math.Clamp(SldMasterVolume?.Value ?? 1, 0, 1);
        _project.MusicVolume = (float)Math.Clamp(SldMusicVolume?.Value ?? 0.7, 0, 1);
        _project.SfxVolume = (float)Math.Clamp(SldSfxVolume?.Value ?? 1, 0, 1);

        return true;
    }
}
