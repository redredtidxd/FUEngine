using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using FUEngine.Help;
using System.Windows.Controls;
using FUEngine.Core;
using FUEngine.Editor;
namespace FUEngine;

/// <summary>Menu and toolbar handlers: file, edit, view, tools, save/undo/redo, zone.</summary>
public partial class EditorWindow
{
    private void UpdateZoneMenuState()
    {
        bool hasTileSelection = _selection.HasTileSelection;
        bool hasZoneRect = _zoneMinTx != null && _zoneMaxTx != null;
        if (MenuCopiarZona != null) MenuCopiarZona.IsEnabled = hasTileSelection || hasZoneRect;
        if (MenuPegarZona != null) MenuPegarZona.IsEnabled = _zoneClipboard != null && _zoneClipboard.HasContent;
        if (MenuDuplicarZona != null) MenuDuplicarZona.IsEnabled = hasTileSelection || hasZoneRect;
        if (MenuRevertSnapshot != null) MenuRevertSnapshot.IsEnabled = _mapSnapshot != null;
    }

    private void MenuCopiarZona_OnClick(object sender, RoutedEventArgs e) => CopyZone();
    private void MenuPegarZona_OnClick(object sender, RoutedEventArgs e) => PasteZone();

    private void MenuDuplicarZona_OnClick(object sender, RoutedEventArgs e)
    {
        var bounds = ZoneClipboardService.TryGetCopyBounds(
            _selection.HasTileSelection,
            _selection.TileMinTx, _selection.TileMinTy, _selection.TileMaxTx, _selection.TileMaxTy,
            _zoneMinTx, _zoneMinTy, _zoneMaxTx, _zoneMaxTy);
        if (!bounds.HasValue) return;
        var (_, _, maxTx, minTy) = bounds.Value;
        CopyZone();
        _pasteOriginTx = maxTx + 1;
        _pasteOriginTy = minTy;
        PasteZone();
        UpdateZoneMenuState();
    }

    private void MenuDeshacer_OnClick(object sender, RoutedEventArgs e)
    {
        if (_history.CanUndo) { _history.Undo(); DrawMap(); RefreshInspector(); }
    }

    private void MenuRehacer_OnClick(object sender, RoutedEventArgs e)
    {
        if (_history.CanRedo) { _history.Redo(); DrawMap(); RefreshInspector(); }
    }

    private void Tool_OnChecked(object sender, RoutedEventArgs e)
    {
        if (ToolPintar?.IsChecked == true) CurrentToolMode = ToolMode.Pintar;
        else if (ToolRectangulo?.IsChecked == true) CurrentToolMode = ToolMode.Rectangulo;
        else if (ToolLinea?.IsChecked == true) CurrentToolMode = ToolMode.Linea;
        else if (ToolRelleno?.IsChecked == true) CurrentToolMode = ToolMode.Relleno;
        else if (ToolGoma?.IsChecked == true) CurrentToolMode = ToolMode.Goma;
        else if (ToolPicker?.IsChecked == true) CurrentToolMode = ToolMode.Picker;
        else if (ToolStamp?.IsChecked == true) CurrentToolMode = ToolMode.Stamp;
        else if (ToolSeleccionar?.IsChecked == true) CurrentToolMode = ToolMode.Seleccionar;
        else if (ToolColocar?.IsChecked == true) CurrentToolMode = ToolMode.Colocar;
        else if (ToolZona?.IsChecked == true) CurrentToolMode = ToolMode.Zona;
        else if (ToolMedir?.IsChecked == true) CurrentToolMode = ToolMode.Medir;
        else if (ToolPixelEdit?.IsChecked == true) CurrentToolMode = ToolMode.PixelEdit;
        _measureStart = null;
        _measureEnd = null;
        _lineStart = null;
        _rectDragging = false;
        UpdateZoneMenuState();
        RefreshInspector();
    }

    private void BtnVisual_OnClick(object sender, RoutedEventArgs e)
    {
        if (PopupVisual != null) PopupVisual.IsOpen = !PopupVisual.IsOpen;
    }

    private void BtnTransform_OnClick(object sender, RoutedEventArgs e)
    {
        if (PopupTransform != null) PopupTransform.IsOpen = !PopupTransform.IsOpen;
    }

    private static bool ConfirmOverwriteForSave(bool fileOrFilesExist, string message, Window owner, string title = "Confirmar guardado")
    {
        if (!fileOrFilesExist) return true;
        var settings = EngineSettings.Load();
        if (!settings.SaveOverwriteProtection) return true;
        return System.Windows.MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }

    private void NotifySaveSuccess(string message, string logCategory)
    {
        ProjectExplorer?.RefreshTree();
        EditorLog.Info(message, logCategory);
        EditorLog.Toast(message, LogLevel.Info, logCategory);
    }

    private void NotifySaveError(string message, string logCategory, Exception ex)
    {
        EditorLog.Error($"{message}: {ex.Message}", logCategory);
        System.Windows.MessageBox.Show(this, "Error: " + ex.Message, logCategory, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    /// <summary>Compatibilidad con atajos y código que aún referencian el nombre antiguo.</summary>
    private void MenuGuardarMapa_OnClick(object sender, RoutedEventArgs e) => MenuGuardarEscena_OnClick(sender, e);

    private void MenuGuardarEscena_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var mapPath = GetCurrentSceneMapPath();
            var objectsPath = GetCurrentSceneObjectsPath();
            var anyExists = File.Exists(mapPath) || File.Exists(objectsPath);
            if (!ConfirmOverwriteForSave(anyExists, "Los archivos de la escena ya existen. ¿Sobrescribir?", this)) return;
            if (!TrySaveCurrentSceneToDisk())
            {
                System.Windows.MessageBox.Show(this, "No se pudo guardar la escena.", "Guardar escena", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            NotifySaveSuccess("Escena guardada (mapa, objetos y UI).", "Guardar");
        }
        catch (Exception ex) { NotifySaveError("Guardar escena", "Guardar", ex); }
    }

    private void MenuGuardarEscenaComo_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Mapa FUEngine (*.map)|*.map|JSON (*.json)|*.json|Todos los archivos|*.*",
                Title = "Guardar escena como…",
                FileName = System.IO.Path.GetFileName(GetCurrentSceneMapPath())
            };
            if (dlg.ShowDialog() != true) return;
            var mapPath = dlg.FileName;
            var dir = System.IO.Path.GetDirectoryName(mapPath);
            var baseName = System.IO.Path.GetFileNameWithoutExtension(mapPath);
            if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(baseName)) return;
            var objSuffix = System.IO.Path.GetExtension(GetCurrentSceneObjectsPath());
            if (string.IsNullOrEmpty(objSuffix)) objSuffix = ".objects.json";
            var objectsPath = System.IO.Path.Combine(dir, baseName + objSuffix);
            if (File.Exists(mapPath) || File.Exists(objectsPath))
            {
                if (!ConfirmOverwriteForSave(true, "Ya existen archivos con ese nombre. ¿Sobrescribir?", this, "Guardar escena como")) return;
            }
            Directory.CreateDirectory(dir);
            MapSerialization.Save(_tileMap, mapPath);
            ObjectsSerialization.Save(_objectLayer, objectsPath);
            EditorLog.Toast($"Copia de escena guardada:\n{mapPath}\n{objectsPath}\n\nNo se ha cambiado la escena activa del proyecto.", LogLevel.Info, "Guardar");
        }
        catch (Exception ex) { NotifySaveError("Guardar escena como", "Guardar", ex); }
    }

    private void MenuGuardarTodo_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var projectDir = _project.ProjectDirectory ?? "";
            var projectJsonPath = System.IO.Path.Combine(projectDir, NewProjectStructure.ProjectFileName);
            var legacyProjectPath = System.IO.Path.Combine(projectDir, "proyecto.json");
            var anyExists = File.Exists(GetCurrentSceneMapPath()) || File.Exists(GetCurrentSceneObjectsPath()) || File.Exists(projectJsonPath) || File.Exists(legacyProjectPath);
            if (!ConfirmOverwriteForSave(anyExists, "Algunos archivos del proyecto ya existen. ¿Sobrescribir?", this)) return;
            SaveAllOpenScenes();
            var saveProjectPath = NewProjectStructure.UsesNewStructure(projectDir) ? projectJsonPath : legacyProjectPath;
            ProjectSerialization.Save(_project, saveProjectPath);
            try
            {
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, () =>
                {
                    try { ProjectThumbnailService.TrySaveHubThumbnail(MapCanvas, saveProjectPath); } catch { /* no bloquear guardado */ }
                });
            }
            catch { /* ignore */ }
            RefreshSceneUsedPaths();
            UpdateMapTabDirtyState();
            NotifySaveSuccess("Proyecto guardado.", "Guardar todo");
        }
        catch (Exception ex) { NotifySaveError("Guardar todo", "Guardar todo", ex); }
    }

    private void MenuSalirProyecto_OnClick(object sender, RoutedEventArgs e)
    {
        if (HasUnsavedChanges())
        {
            var result = System.Windows.MessageBox.Show(this,
                "Hay cambios sin guardar. ¿Guardar antes de salir del proyecto?",
                "Salir al Hub",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);
            if (result == MessageBoxResult.Cancel) return;
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var projectDir = _project.ProjectDirectory ?? "";
                    var projectJsonPath = System.IO.Path.Combine(projectDir, NewProjectStructure.ProjectFileName);
                    var legacyProjectPath = System.IO.Path.Combine(projectDir, "proyecto.json");
                    var anyExists = File.Exists(GetCurrentSceneMapPath()) || File.Exists(GetCurrentSceneObjectsPath()) || File.Exists(projectJsonPath) || File.Exists(legacyProjectPath);
                    if (!ConfirmOverwriteForSave(anyExists, "Algunos archivos del proyecto ya existen. ¿Sobrescribir?", this)) return;
                    SaveAllOpenScenes();
                    var saveProjectPath = NewProjectStructure.UsesNewStructure(projectDir) ? projectJsonPath : legacyProjectPath;
                    ProjectSerialization.Save(_project, saveProjectPath);
                    RefreshSceneUsedPaths();
                }
                catch (Exception ex)
                {
                    NotifySaveError("Guardar antes de salir", "Salir", ex);
                    return;
                }
            }
        }
        EditorLog.Toast("Volviendo al Hub.", LogLevel.Info, "Salir");
        var hub = new StartupWindow();
        hub.Show();
        System.Windows.Application.Current.MainWindow = hub;
        Close();
    }

    private void MenuSalirMotor_OnClick(object sender, RoutedEventArgs e)
    {
        if (System.Windows.MessageBox.Show(this, "¿Cerrar el motor? Se cerrará el proyecto actual.", "Salir del motor", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        System.Windows.Application.Current.Shutdown();
    }

    private void MenuPreferenciasEditor_OnClick(object sender, RoutedEventArgs e) => MenuConfiguracion_OnClick(sender, e);

    private void MenuConfiguracion_OnClick(object sender, RoutedEventArgs e)
    {
        BeginModalDiscordPresence("Configuración del motor", "Preferencias, rutas y tipografía");
        try
        {
            var settings = new SettingsWindow { Owner = this };
            settings.ShowDialog();
        }
        finally
        {
            EndModalDiscordPresence();
        }
    }

    private NewProjectWizardPanel? _newProjectWizardPanel;

    private void MenuNuevoProyecto_OnClick(object sender, RoutedEventArgs e) => ShowNewProjectWizardOverlay();

    private void ShowNewProjectWizardOverlay()
    {
        DetachNewProjectWizardPanel();
        var wizard = new NewProjectWizardPanel();
        _newProjectWizardPanel = wizard;
        wizard.CreateCommitted += NewProjectWizard_OnCreateCommitted;
        wizard.CancelRequested += NewProjectWizard_OnCancelRequested;
        if (NewProjectWizardHost != null)
            NewProjectWizardHost.Content = wizard;
        if (NewProjectWizardOverlay != null)
            NewProjectWizardOverlay.Visibility = Visibility.Visible;
    }

    private void NewProjectWizard_OnCancelRequested(object? sender, EventArgs e) => HideNewProjectWizardOverlay();

    private void NewProjectWizard_OnCreateCommitted(object? sender, EventArgs e)
    {
        if (sender is not NewProjectWizardPanel w) return;
        try
        {
            var projectPath = NewProjectCreation.CreateFromWizard(w);
            System.Windows.MessageBox.Show(this, "Proyecto creado. Abriendo...", "Nuevo proyecto", MessageBoxButton.OK, MessageBoxImage.Information);
            var loaded = ProjectSerialization.Load(projectPath);
            HideNewProjectWizardOverlay();
            var newEditor = new EditorWindow(loaded);
            newEditor.Show();
            Close();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "Error al crear proyecto: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void HideNewProjectWizardOverlay()
    {
        DetachNewProjectWizardPanel();
        if (NewProjectWizardOverlay != null)
            NewProjectWizardOverlay.Visibility = Visibility.Collapsed;
        if (NewProjectWizardHost != null)
            NewProjectWizardHost.Content = null;
    }

    private void DetachNewProjectWizardPanel()
    {
        if (_newProjectWizardPanel == null) return;
        _newProjectWizardPanel.CreateCommitted -= NewProjectWizard_OnCreateCommitted;
        _newProjectWizardPanel.CancelRequested -= NewProjectWizard_OnCancelRequested;
        _newProjectWizardPanel = null;
    }

    private void NewProjectWizardBackdrop_OnMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        HideNewProjectWizardOverlay();
        e.Handled = true;
    }

    private void NewProjectWizardInner_OnMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) =>
        e.Handled = true;

    private void MenuAbrirProyecto_OnClick(object sender, RoutedEventArgs e)
    {
        var settings = EngineSettings.Load();
        var initialDir = !string.IsNullOrWhiteSpace(settings.DefaultProjectsPath) && Directory.Exists(settings.DefaultProjectsPath)
            ? settings.DefaultProjectsPath
            : (Directory.Exists(EngineSettings.GetDefaultProjectsRoot()) ? EngineSettings.GetDefaultProjectsRoot() : "");
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Proyecto FUEngine (*.FUE)|*.FUE|Legacy (Project.json, proyecto.json)|Project.json;proyecto.json|Todos|*.*",
            Title = "Abrir proyecto",
            InitialDirectory = initialDir
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            if (!ProjectFormatOpenHelper.TryPromptAndLoad(dlg.FileName, this, out var project, out var err))
            {
                if (!string.IsNullOrEmpty(err))
                    System.Windows.MessageBox.Show(this, "No se pudo abrir: " + err, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var newEditor = new EditorWindow(project!);
            newEditor.Show();
            Close();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "No se pudo abrir: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Diálogo completo de proyecto (chunks, rutas, etc.).</summary>
    private void MenuEditorProyectoAvanzado_OnClick(object sender, RoutedEventArgs e)
    {
        var projectDir = _project.ProjectDirectory ?? "";
        var projectPath = string.IsNullOrEmpty(projectDir) ? null : System.IO.Path.Combine(projectDir, NewProjectStructure.ProjectFileName);
        var legacyPath = string.IsNullOrEmpty(projectDir) ? null : System.IO.Path.Combine(projectDir, "proyecto.json");
        var path = projectPath != null && NewProjectStructure.UsesNewStructure(projectDir) ? projectPath : legacyPath;
        var dlg = new ProjectConfigWindow(_project, path, _objectLayer) { Owner = this };
        var pn = string.IsNullOrWhiteSpace(_project.Nombre) ? "Proyecto sin nombre" : _project.Nombre.Trim();
        BeginModalDiscordPresence("Configuración del proyecto", pn);
        try
        {
            if (dlg.ShowDialog() != true) return;
            ClampViewportCenterForCurrentMap();
            ApplyEditorVisualColorsFromProject();
            DrawMap();
            ConfigureAutoSave();
        }
        finally
        {
            EndModalDiscordPresence();
        }
    }

    private void MenuAjustesProyectoManifest_OnClick(object sender, RoutedEventArgs e) => FocusProjectManifestInExplorer();

    private void MenuConfigurarChunks_OnClick(object sender, RoutedEventArgs e) => MenuEditorProyectoAvanzado_OnClick(sender, e);

    private void MenuGestionarScripts_OnClick(object sender, RoutedEventArgs e) => AddOrSelectTab("Scripts");

    private void MenuImportarAsset_OnClick(object sender, RoutedEventArgs e)
    {
        var dir = _project.ProjectDirectory ?? "";
        if (string.IsNullOrEmpty(dir)) return;
        NewProjectStructure.EnsureProjectFolders(dir);
        var spritesDir = Path.Combine(dir, "Assets", "Sprites");
        var audioDir = Path.Combine(dir, "Assets", "Audio");
        try
        {
            Directory.CreateDirectory(spritesDir);
            Directory.CreateDirectory(audioDir);
        }
        catch (Exception ex)
        {
            EditorLog.Error($"Carpetas Assets: {ex.Message}", "Importar");
            return;
        }
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Importar asset al proyecto",
            Filter = "Imágenes y audio|*.png;*.bmp;*.gif;*.jpg;*.jpeg;*.webp;*.wav;*.ogg;*.mp3;*.flac|Todos los archivos|*.*",
            Multiselect = true
        };
        if (dlg.ShowDialog() != true) return;
        static bool IsAudio(string ext) => ext is ".wav" or ".ogg" or ".mp3" or ".flac";
        static bool IsImage(string ext) => ext is ".png" or ".bmp" or ".gif" or ".jpg" or ".jpeg" or ".webp";
        int ok = 0;
        foreach (var src in dlg.FileNames)
        {
            try
            {
                var ext = System.IO.Path.GetExtension(src).ToLowerInvariant();
                var destDir = IsAudio(ext) ? audioDir : IsImage(ext) ? spritesDir : spritesDir;
                var name = System.IO.Path.GetFileName(src);
                var dest = System.IO.Path.Combine(destDir, name);
                if (File.Exists(dest))
                {
                    var b = System.IO.Path.GetFileNameWithoutExtension(name);
                    var extn = System.IO.Path.GetExtension(name);
                    for (var n = 2; ; n++)
                    {
                        dest = System.IO.Path.Combine(destDir, $"{b} ({n}){extn}");
                        if (!File.Exists(dest)) break;
                    }
                }
                File.Copy(src, dest, overwrite: false);
                ok++;
            }
            catch (Exception ex)
            {
                EditorLog.Warning($"Importar {System.IO.Path.GetFileName(src)}: {ex.Message}", "Importar");
            }
        }
        ProjectExplorer?.RefreshTree();
        if (ok > 0)
            EditorLog.Toast($"{ok} archivo(s) copiado(s) a Assets/Sprites o Assets/Audio.", LogLevel.Info, "Importar");
        else
            EditorLog.Toast("No se importó ningún archivo.", LogLevel.Warning, "Importar");
    }

    private void MenuLimpiarCacheMotor_OnClick(object sender, RoutedEventArgs e)
    {
        FUEngineAppPaths.EnsureLayout();
        var v = FUEngineAppPaths.VulkanCacheDirectory;
        var lua = FUEngineAppPaths.LuaMetadataCacheDirectory;
        var cacheRoot = Path.Combine(FUEngineAppPaths.Root, "Cache");
        var msg = "Se eliminarán las carpetas de caché del motor en AppData:\n\n" +
                  $"• {v}\n• {lua}\n• (opcional) {cacheRoot}\n\n¿Continuar?";
        if (System.Windows.MessageBox.Show(this, msg, "Limpiar caché del motor", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        try
        {
            if (Directory.Exists(v)) Directory.Delete(v, recursive: true);
            if (Directory.Exists(lua)) Directory.Delete(lua, recursive: true);
            if (Directory.Exists(cacheRoot)) Directory.Delete(cacheRoot, recursive: true);
            FUEngineAppPaths.EnsureLayout();
            EditorLog.Toast("Caché del motor borrada. Reinicia el editor si algo seguía fallando.", LogLevel.Info, "Caché");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "No se pudo borrar todo:\n" + ex.Message, "Limpiar caché", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void MenuResetLayout_OnClick(object sender, RoutedEventArgs e)
    {
        var dir = _project.ProjectDirectory ?? "";
        if (!string.IsNullOrEmpty(dir))
        {
            var layoutPath = Path.Combine(dir, ".editorlayout");
            try { if (File.Exists(layoutPath)) File.Delete(layoutPath); } catch { /* ignore */ }
        }
        RemoveOptionalTabs();
        foreach (var s in _openScenes)
            s.OpenOptionalTabKinds.Clear();
        if (MenuVerJerarquia != null) MenuVerJerarquia.IsChecked = true;
        if (MenuVerInspector != null) MenuVerInspector.IsChecked = true;
        if (MenuVerConsola != null) MenuVerConsola.IsChecked = false;
        if (MenuVerJuego != null) MenuVerJuego.IsChecked = false;
        if (PanelJerarquia != null) PanelJerarquia.Visibility = Visibility.Visible;
        if (SplitterJerarquia != null) SplitterJerarquia.Visibility = Visibility.Visible;
        if (PanelInspector != null) PanelInspector.Visibility = Visibility.Visible;
        if (SplitterInspector != null) SplitterInspector.Visibility = Visibility.Visible;
        if (TabMapa != null) TabMapa.IsSelected = true;
        ApplyVerMenuState();
        await SaveEditorLayoutAsync();
        EditorLog.Toast("Disposición restaurada a valores por defecto (mapa, paneles laterales, sin pestañas extra).", LogLevel.Info, "Editor");
    }

    private void MenuVentanaTab_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem mi || mi.Tag is not string tag) return;
        switch (tag)
        {
            case "Mapa":
                if (TabMapa != null) TabMapa.IsSelected = true;
                break;
            case "Consola":
                if (TabConsola != null) TabConsola.IsSelected = true;
                break;
            case "Juego":
                AddOrSelectTab("Juego");
                break;
            case "Explorador":
                AddOrSelectTab("Explorador");
                break;
            case "Scripts":
                AddOrSelectTab("Scripts");
                break;
        }
    }

    private void MenuMapaAjustesFondo_OnClick(object sender, RoutedEventArgs e) => ShowMapSceneSettingsInInspector();

    private void MenuMapaRegenerarColisiones_OnClick(object sender, RoutedEventArgs e)
    {
        AddOrSelectTab("CollisionsEditor");
        EditorLog.Toast("Abre un sprite o tile en el Explorador y usa el editor de colisiones para regenerar máscaras. Las colisiones de mapa dependen de los tipos de tile y capas.", LogLevel.Info, "Colisiones");
    }

    private void MenuCrearNuevaSemilla_OnClick(object sender, RoutedEventArgs e)
    {
        var projectDir = _project.ProjectDirectory ?? "";
        if (string.IsNullOrEmpty(projectDir)) return;
        var seedsDir = Path.Combine(projectDir, "Seeds");
        try { Directory.CreateDirectory(seedsDir); } catch { /* ignore */ }
        var baseName = "nuevo_seed";
        var path = Path.Combine(seedsDir, baseName + ".seed");
        path = EnsureUniqueSeedFilePath(path);
        var id = MakeUniqueSeedId(Path.GetFileNameWithoutExtension(path));
        var defId = _objectLayer?.Definitions?.Keys?.FirstOrDefault() ?? "";
        var seed = new SeedDefinition
        {
            Id = id,
            Nombre = Path.GetFileNameWithoutExtension(path) ?? "Seed",
            Descripcion = "Nueva semilla creada desde el menú Semillas.",
            Objects = string.IsNullOrEmpty(defId)
                ? new List<SeedObjectEntry>()
                : new List<SeedObjectEntry> { new SeedObjectEntry { DefinitionId = defId, OffsetX = 0, OffsetY = 0 } },
            Tags = new List<string>()
        };
        try
        {
            SeedSerialization.Save(new List<SeedDefinition> { seed }, path);
            if (!_seedDefinitions.Any(s => string.Equals(s.Id, seed.Id, StringComparison.OrdinalIgnoreCase)))
                _seedDefinitions.Add(seed);
            SeedSerialization.Save(_seedDefinitions, _project.SeedsPath);
            ProjectExplorer?.RefreshTree();
            EditorLog.Toast($"Semilla creada: {Path.GetFileName(path)}", LogLevel.Info, "Seed");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, ex.Message, "Crear semilla", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MenuExploradorSemillasGlobales_OnClick(object sender, RoutedEventArgs e)
    {
        FUEngineAppPaths.EnsureLayout();
        var d = FUEngineAppPaths.GlobalTemplatesDirectory;
        try
        {
            Directory.CreateDirectory(d);
            Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = d, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, ex.Message, "GlobalTemplates", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MenuAbrirCarpetaLogs_OnClick(object sender, RoutedEventArgs e)
    {
        FUEngineAppPaths.EnsureLayout();
        var d = FUEngineAppPaths.LogsDirectory;
        try
        {
            Directory.CreateDirectory(d);
            Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = d, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, ex.Message, "Logs", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MenuReportarBug_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/redredtidxd/FUEngine/issues",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, ex.Message, "GitHub", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MenuPropiedadesMapa_OnClick(object sender, RoutedEventArgs e) => ShowMapSceneSettingsInInspector();

    private void MenuTamañoTiles_OnClick(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBox.Show(this, $"Tamaño de tile actual: {_project.TileSize} px. Se configura al crear el proyecto o en proyecto.json.", "Tamaño de tiles", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void MenuVerPanel_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi)
        {
            bool visible = mi.IsChecked;
            if (mi == MenuVerJerarquia)
            {
                PanelJerarquia.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
                SplitterJerarquia.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (mi == MenuVerInspector)
            {
                PanelInspector.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
                SplitterInspector.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (mi == MenuVerConsola)
            {
                if (visible && TabConsola != null)
                    MainTabs.SelectedItem = TabConsola;
                else if (TabMapa != null)
                    MainTabs.SelectedItem = TabMapa;
            }
            else if (mi == MenuVerJuego)
            {
                if (visible)
                    AddOrSelectTab("Juego");
                else if (TabMapa != null)
                    MainTabs.SelectedItem = TabMapa;
            }
        }
    }

    private void MenuHerramienta_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem item && item.Tag is string tag)
        {
            if (tag == "Pintar") { ToolPintar.IsChecked = true; CurrentToolMode = ToolMode.Pintar; }
            else if (tag == "Borrar") { ToolGoma.IsChecked = true; CurrentToolMode = ToolMode.Goma; }
            else if (tag == "Rectangulo") { ToolRectangulo.IsChecked = true; CurrentToolMode = ToolMode.Rectangulo; }
            else if (tag == "Linea") { ToolLinea.IsChecked = true; CurrentToolMode = ToolMode.Linea; }
            else if (tag == "Relleno") { ToolRelleno.IsChecked = true; CurrentToolMode = ToolMode.Relleno; }
            else if (tag == "Goma") { ToolGoma.IsChecked = true; CurrentToolMode = ToolMode.Goma; }
            else if (tag == "Picker") { ToolPicker.IsChecked = true; CurrentToolMode = ToolMode.Picker; }
            else if (tag == "Stamp") { ToolStamp.IsChecked = true; CurrentToolMode = ToolMode.Stamp; }
            else if (tag == "Colocar") { ToolColocar.IsChecked = true; CurrentToolMode = ToolMode.Colocar; }
            else if (tag == "Seleccionar") { ToolSeleccionar.IsChecked = true; CurrentToolMode = ToolMode.Seleccionar; }
            else if (tag == "Zona") { ToolZona.IsChecked = true; CurrentToolMode = ToolMode.Zona; }
            else if (tag == "Medir") { ToolMedir.IsChecked = true; CurrentToolMode = ToolMode.Medir; }
            else if (tag == "PixelEdit") { ToolPixelEdit.IsChecked = true; CurrentToolMode = ToolMode.PixelEdit; }
        }
    }

    private void MenuDocumentacion_OnClick(object sender, RoutedEventArgs e) =>
        ShowDocumentation(EngineDocumentation.QuickStartTopicId);

    private void MenuDocumentacionCompleta_OnClick(object sender, RoutedEventArgs e) =>
        ShowDocumentation(EngineDocumentation.FullManualStartTopicId);

    private void MenuLuaReference_OnClick(object sender, RoutedEventArgs e) =>
        ShowDocumentation(EngineDocumentation.LuaReferenceIntroTopicId);

    private void MenuAcercaDe_OnClick(object sender, RoutedEventArgs e)
    {
        BeginModalDiscordPresence("Acerca de FUEngine", "Versión y créditos");
        try
        {
            new AboutWindow { Owner = this }.ShowDialog();
        }
        finally
        {
            EndModalDiscordPresence();
        }
    }

    private void MenuBorrarObjeto_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selection.SelectedObjects.Count == 0) return;
        foreach (var obj in _selection.SelectedObjects.ToList())
            _history.Push(new RemoveObjectCommand(_objectLayer, obj));
        _selection.ClearObjectSelection();
        _isDragging = false;
        ProjectExplorer?.SetModified(GetCurrentSceneObjectsPath(), true);
        RefreshMapHierarchy();
        RefreshInspector();
        DrawMap();
    }
}
