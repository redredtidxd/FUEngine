using System.IO;
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

    private void MenuGuardarMapa_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!ConfirmOverwriteForSave(File.Exists(GetCurrentSceneMapPath()), "El archivo del mapa ya existe. ¿Sobrescribir?", this)) return;
            MapSerialization.Save(_tileMap, GetCurrentSceneMapPath());
            ProjectExplorer?.SetModified(GetCurrentSceneMapPath(), false);
            RefreshSceneUsedPaths();
            UpdateMapTabDirtyState();
            NotifySaveSuccess("Mapa guardado.", "Guardar");
        }
        catch (Exception ex) { NotifySaveError("Guardar mapa", "Guardar", ex); }
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
                "Salir del proyecto",
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
        EditorLog.Toast("Proyecto cerrado.", LogLevel.Info, "Salir");
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

    private void MenuConfiguracion_OnClick(object sender, RoutedEventArgs e)
    {
        var settings = new SettingsWindow { Owner = this };
        settings.ShowDialog();
    }

    private void MenuNuevoProyecto_OnClick(object sender, RoutedEventArgs e)
    {
        var dlg = new NewProjectDialog { Owner = this };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var projectDir = dlg.ProjectPath ?? "";
            if (!Directory.Exists(projectDir) && dlg.CreateProjectFolderIfMissing)
                Directory.CreateDirectory(projectDir);
            var project = new ProjectInfo
            {
                Nombre = dlg.ProjectName ?? "",
                Descripcion = dlg.Description ?? "",
                TileSize = dlg.TileSize,
                MapWidth = dlg.MapWidth,
                MapHeight = dlg.MapHeight,
                Infinite = dlg.Infinite,
                ChunkSize = dlg.ChunkSize,
                ProjectDirectory = projectDir,
                AutoSaveEnabled = true,
                AutoSaveIntervalMinutes = 5,
                AutoSaveMaxBackupsPerType = 10,
                AutoSaveFolder = "Autoguardados",
                AutoSaveOnClose = true
            };
            var proyectoConfig = new ProyectoConfigDto
            {
                Nombre = project.Nombre,
                Logo = "logo.png",
                Plantilla = dlg.TemplateType ?? "Blank",
                AutoguardadoActivo = true,
                IntervaloAutoguardadoMin = 5,
                MaxBackupsAutoguardado = 10,
                GuardarSoloCambios = true,
                Descripcion = project.Descripcion,
                Autor = dlg.Author,
                Version = dlg.Version ?? "0.1"
            };
            var projectPath = NewProjectStructure.Create(projectDir, project, dlg.IconPath, proyectoConfig);
            System.Windows.MessageBox.Show(this, "Proyecto creado. Abriendo...", "Nuevo proyecto", MessageBoxButton.OK, MessageBoxImage.Information);
            var loaded = ProjectSerialization.Load(projectPath);
            var newEditor = new EditorWindow(loaded);
            newEditor.Show();
            Close();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "Error al crear proyecto: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

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
            var project = ProjectSerialization.Load(dlg.FileName);
            var newEditor = new EditorWindow(project);
            newEditor.Show();
            Close();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "No se pudo abrir: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MenuConfigProyecto_OnClick(object sender, RoutedEventArgs e)
    {
        var projectDir = _project.ProjectDirectory ?? "";
        var projectPath = string.IsNullOrEmpty(projectDir) ? null : System.IO.Path.Combine(projectDir, NewProjectStructure.ProjectFileName);
        var legacyPath = string.IsNullOrEmpty(projectDir) ? null : System.IO.Path.Combine(projectDir, "proyecto.json");
        var path = projectPath != null && NewProjectStructure.UsesNewStructure(projectDir) ? projectPath : legacyPath;
        var dlg = new ProjectConfigWindow(_project, path, _objectLayer) { Owner = this };
        if (dlg.ShowDialog() != true) return;
        ConfigureAutoSave();
    }

    private void MenuPropiedadesMapa_OnClick(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBox.Show(this, $"Mapa: Chunk size {_tileMap.ChunkSize}, Tile size {_project.TileSize}px. Configuración avanzada próximamente.", "Propiedades del mapa", MessageBoxButton.OK, MessageBoxImage.Information);
    }

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

    private void MenuDocumentacion_OnClick(object sender, RoutedEventArgs e)
    {
        new DocumentationWindow { Owner = this, InitialTopicId = EngineDocumentation.QuickStartTopicId }.Show();
    }

    private void MenuDocumentacionCompleta_OnClick(object sender, RoutedEventArgs e)
    {
        new DocumentationWindow { Owner = this, InitialTopicId = EngineDocumentation.FullManualStartTopicId }.Show();
    }

    private void MenuAcercaDe_OnClick(object sender, RoutedEventArgs e)
    {
        new AboutWindow { Owner = this }.ShowDialog();
    }

    private void MenuBorrarObjeto_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selection.SelectedObjects.Count == 0) return;
        foreach (var obj in _selection.SelectedObjects.ToList())
            _history.Push(new RemoveObjectCommand(_objectLayer, obj));
        _selection.ClearObjectSelection();
        _isDragging = false;
        ProjectExplorer?.SetModified(GetCurrentSceneObjectsPath(), true);
        MapHierarchy?.SetMapStructure(System.IO.Path.GetFileNameWithoutExtension(GetCurrentSceneMapPath()), _project.LayerNames, _objectLayer, _triggerZones, _visibleLayers, GetCurrentUIRoot());
        RefreshInspector();
        DrawMap();
    }
}
