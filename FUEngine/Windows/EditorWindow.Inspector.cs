using System;
using System.IO;
using System.Linq;
using System.Windows;
using FUEngine.Core;
using FUEngine.Editor;

namespace FUEngine;

/// <summary>Inspector panel logic: refresh, cached panels, cleanup.</summary>
public partial class EditorWindow
{
    private void RefreshInspector()
    {
        if (MenuBorrarObjeto != null)
            MenuBorrarObjeto.IsEnabled = _selection.SelectedObjects.Count > 0;
        UpdateZoneMenuState();
        RefreshToolbarMapHint();
        if (InspectorPanel == null) return;
        if (_selection.SelectedTrigger != null)
        {
            var trig = GetOrCreateTriggerPanel();
            trig.SetAvailableScripts(_scriptRegistry?.GetAll()?.Select(s => (s.Id, s.Nombre, s.Path)) ?? Enumerable.Empty<(string, string, string?)>());
            trig.SetTarget(_selection.SelectedTrigger);
            InspectorPanel.Content = trig;
            return;
        }
        if (_selection.SelectedExplorerItem != null && !_selection.SelectedExplorerItem.IsFolder)
        {
            if (ProjectManifestPaths.IsActiveProjectManifestFile(_selection.SelectedExplorerItem.FullPath, _project.ProjectDirectory))
            {
                var manifest = GetOrCreateProjectManifestPanel();
                manifest.LoadFromProject(_project, GetProjectFilePath());
                InspectorPanel.Content = manifest;
                return;
            }
            var quick = GetOrCreateQuickPropertiesPanel();
            quick.SetItem(_selection.SelectedExplorerItem, _project, _tileMap, _objectLayer, _scriptRegistry);
            InspectorPanel.Content = quick;
            return;
        }
        if (_selection.SelectedUICanvas != null || _selection.SelectedUIElement != null)
        {
            var uiPanel = GetOrCreateUIElementPanel();
            uiPanel.SetTarget(_selection.SelectedUICanvas, _selection.SelectedUIElement, GetCurrentUIRoot());
            InspectorPanel.Content = uiPanel;
            return;
        }
        if (_selection.SelectedObjects.Count > 1)
        {
            var multi = GetOrCreateMultiObjectPanel();
            multi.SetTarget(_selection.SelectedObjects.ToList(), _objectLayer);
            InspectorPanel.Content = multi;
            return;
        }
        if (_selection.SelectedObject == null && _selection.SelectedObjects.Count == 0)
        {
            if (_selection.InspectorContextKind == InspectorContextKind.Layer && _selection.InspectorContextLayer != null)
            {
                var layerPanel = GetOrCreateLayerInspectorPanel();
                layerPanel.SetProjectDirectory(_project.ProjectDirectory);
                layerPanel.SetDescriptor(_selection.InspectorContextLayer);
                InspectorPanel.Content = layerPanel;
                return;
            }
            if (_selection.InspectorContextKind == InspectorContextKind.Animation && _selection.InspectorContextAnimation != null)
            {
                var animPanel = GetOrCreateAnimationInspectorPanel();
                animPanel.SetAnimation(_selection.InspectorContextAnimation);
                InspectorPanel.Content = animPanel;
                return;
            }
            if (_selection.InspectorContextKind == InspectorContextKind.Tile && _selection.InspectorContextTileId != null)
            {
                var tilePanel = GetOrCreateTileInspectorPanel();
                tilePanel.SetTile(_project, _tileMap, GetActiveLayerIndex(), _selection.InspectorContextTileId, _selection.InspectorContextTilesetRelPath);
                InspectorPanel.Content = tilePanel;
                return;
            }
            var overview = GetOrCreateOverviewPanel();
            int tilesCount = 0;
            for (int li = 0; li < _tileMap.Layers.Count; li++)
                foreach (var (cx, cy) in _tileMap.EnumerateChunkCoords(li))
                {
                    var ch = _tileMap.GetChunk(li, cx, cy);
                    if (ch != null) tilesCount += ch.EnumerateTiles().Count();
                }
            int triggersCount = 0;
            try { if (File.Exists(_project.TriggerZonesPath)) triggersCount = TriggerZoneSerialization.Load(_project.TriggerZonesPath).Count; } catch (System.Exception ex) { EditorLog.Warning($"No se pudo contar triggers: {ex.Message}", "Editor"); }
            var toolName = _toolMode switch { ToolMode.Pintar => "Pincel", ToolMode.Rectangulo => "Rectángulo relleno", ToolMode.Relleno => "Relleno", ToolMode.Goma => "Goma", ToolMode.Stamp => "Sello", ToolMode.Seleccionar => "Seleccionar", ToolMode.Colocar => "Colocar objeto", ToolMode.Zona => "Zona", ToolMode.Medir => "Medir", ToolMode.PixelEdit => "Pixel", _ => "Pincel" };
            string toolDetail;
            if (_toolMode == ToolMode.Pintar)
            {
                if (!ActiveLayerUsesTilesetCatalog())
                    toolDetail = "Asigna un tileset (.json / .fuetileset) a la capa activa para pintar desde el atlas.";
                else
                    toolDetail = _selectedCatalogTileId is int cid ? $"Catálogo: tile #{cid}" : "Catálogo: elige un tile en la pestaña «Tiles» (abajo).";
            }
            else
                toolDetail = _toolMode == ToolMode.Colocar && CmbObjectDef?.SelectedItem is ObjectDefinition od ? $"Objeto: {od.Nombre}" : "";
            var layerName = GetActiveLayerDisplayName();
            overview.SetData(_project, _tileMap, _objectLayer, _scriptRegistry, toolName, toolDetail, layerName, 0, tilesCount, _objectLayer.Instances.Count, triggersCount, _scriptRegistry?.GetAll()?.Count ?? 0, 0, CmbObjectDef?.SelectedItem as ObjectDefinition);
            InspectorPanel.Content = overview;
            return;
        }
        var objPanel = GetOrCreateObjectPanel();
        var scripts = _scriptRegistry?.GetAll()?.Select(s => (s.Id, s.Nombre, s.Path)) ?? Enumerable.Empty<(string, string, string?)>();
        objPanel.SetProjectDirectory(_project.ProjectDirectory);
        objPanel.SetTileSize(_project.TileSize > 0 ? _project.TileSize : 32);
        objPanel.SetAvailableScripts(scripts);
        var playLive = GetCurrentDebugRunner() is { IsRunning: true };
        objPanel.LiveVariablesProvider = playLive
            ? (instId, sid) =>
            {
                var r = GetCurrentDebugRunner();
                return r != null && r.TryGetLiveScriptVariables(instId, sid, out var d) ? d : null;
            }
            : null;
        objPanel.LiveVariableWriter = playLive
            ? (instId, sid, key, ty, val) =>
            {
                _ = GetCurrentDebugRunner()?.TrySetLiveScriptVariable(instId, sid, key, ty, val);
            }
            : null;
        objPanel.SetTarget(_selection.SelectedObject, _objectLayer);
        InspectorPanel.Content = objPanel;
    }

    private TriggerZoneInspectorPanel GetOrCreateTriggerPanel()
    {
        if (_cachedTriggerPanel != null) return _cachedTriggerPanel;
        _cachedTriggerPanel = new TriggerZoneInspectorPanel();
        _cachedTriggerPanel.PropertyChanged += (_, _) => DrawMap();
        _cachedTriggerPanel.RequestDuplicate += (_, z) =>
        {
            var clone = new TriggerZone { Id = Guid.NewGuid().ToString("N"), Nombre = z.Nombre + " (copia)", Descripcion = z.Descripcion, TriggerType = z.TriggerType, LayerId = z.LayerId, X = z.X + 2, Y = z.Y, Width = z.Width, Height = z.Height, ScriptIdOnEnter = z.ScriptIdOnEnter, ScriptIdOnExit = z.ScriptIdOnExit, ScriptIdOnTick = z.ScriptIdOnTick, Tags = new List<string>(z.Tags ?? new List<string>()) };
            _triggerZones.Add(clone);
            TriggerZoneSerialization.Save(_triggerZones, _project.TriggerZonesPath);
            RefreshMapHierarchy();
            _selection.SetTriggerSelection(clone);
            RefreshInspector();
        };
        _cachedTriggerPanel.RequestDelete += (_, z) =>
        {
            if (System.Windows.MessageBox.Show(this, "¿Eliminar esta zona trigger?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            _triggerZones.Remove(z);
            _selection.SetTriggerSelection(null);
            TriggerZoneSerialization.Save(_triggerZones, _project.TriggerZonesPath);
            RefreshMapHierarchy();
            RefreshInspector();
            DrawMap();
        };
        return _cachedTriggerPanel;
    }

    private void CachedObjectPanel_ProjectProtagonistMetadataChanged(object? sender, EventArgs e)
    {
        var pp = GetProjectFilePath();
        if (!string.IsNullOrEmpty(pp))
            ProjectExplorer?.SetModified(pp, true);
        DrawMap();
    }

    private void CachedObjectPanel_PropertyChanged(object? sender, EventArgs e)
    {
        ProjectExplorer?.SetModified(GetCurrentSceneObjectsPath(), true);
        RefreshMapHierarchy();
        DrawMap();
    }
    private void CachedObjectPanel_RequestConvertToSeed(object? sender, ObjectInstance e) => ConvertObjectToSeed(e);
    private void CachedObjectPanel_RequestApplyToSeed(object? sender, ObjectInstance e) => ApplyInstanceToSourceSeed(e);
    private void CachedUIElementPanel_PropertyChanged(object? sender, EventArgs e)
    {
        var canvas = _selection.SelectedUICanvas ?? FindCanvasForElement(_selection.SelectedUIElement);
        if (canvas != null)
            ProjectExplorer?.SetModified(System.IO.Path.Combine(GetCurrentUIFolder(), canvas.Id + ".json"), true);
        RefreshMapHierarchy();
        RefreshOpenUICanvasTabs();
        DrawMap();
    }

    private UIElementInspectorPanel GetOrCreateUIElementPanel()
    {
        if (_cachedUIElementPanel != null) return _cachedUIElementPanel;
        _cachedUIElementPanel = new UIElementInspectorPanel();
        _cachedUIElementPanel.PropertyChanged += CachedUIElementPanel_PropertyChanged;
        return _cachedUIElementPanel;
    }

    private ObjectInspectorPanel GetOrCreateObjectPanel()
    {
        if (_cachedObjectPanel != null) return _cachedObjectPanel;
        _cachedObjectPanel = new ObjectInspectorPanel { Margin = new Thickness(0) };
        _cachedObjectPanel.PropertyChanged += CachedObjectPanel_PropertyChanged;
        _cachedObjectPanel.RequestDuplicate += MapHierarchy_OnRequestDuplicateObject;
        _cachedObjectPanel.RequestDelete += MapHierarchy_OnRequestDeleteObject;
        _cachedObjectPanel.RequestRename += MapHierarchy_OnRequestRenameObject;
        _cachedObjectPanel.RequestConvertToSeed += CachedObjectPanel_RequestConvertToSeed;
        _cachedObjectPanel.RequestApplyToSeed += CachedObjectPanel_RequestApplyToSeed;
        return _cachedObjectPanel;
    }

    private MultiObjectInspectorPanel GetOrCreateMultiObjectPanel()
    {
        if (_cachedMultiPanel != null) return _cachedMultiPanel;
        _cachedMultiPanel = new MultiObjectInspectorPanel();
        _cachedMultiPanel.PropertyChanged += (_, _) => DrawMap();
        _cachedMultiPanel.RequestClearSelection += (_, _) => { _selection.ClearObjectSelection(); RefreshInspector(); DrawMap(); };
        return _cachedMultiPanel;
    }

    private DefaultInspectorPanel GetOrCreateOverviewPanel()
    {
        if (_cachedOverviewPanel != null) return _cachedOverviewPanel;
        _cachedOverviewPanel = new DefaultInspectorPanel();
        _cachedOverviewPanel.CreateObjectClicked += (_, _) => System.Windows.MessageBox.Show(this, "Crear objeto: diálogo en desarrollo. Añade definiciones en objetos.json.", "Crear objeto", MessageBoxButton.OK);
        _cachedOverviewPanel.AddTriggerClicked += (_, _) =>
        {
            var zone = new TriggerZone { Id = Guid.NewGuid().ToString("N"), Nombre = "Nueva zona", Width = 2, Height = 2 };
            _triggerZones.Add(zone);
            TriggerZoneSerialization.Save(_triggerZones, _project.TriggerZonesPath);
            RefreshMapHierarchy();
            _selection.SetTriggerSelection(zone);
            RefreshInspector();
            DrawMap();
        };
        _cachedOverviewPanel.OpenMapConfigClicked += (_, _) =>
        {
            var panel = GetOrCreateMapPropertiesPanel();
            panel.SetProject(_project);
            InspectorPanel.Content = panel;
        };
        _cachedOverviewPanel.CenterCameraClicked += (_, _) => { try { ScrollViewer?.ScrollToHorizontalOffset(0); ScrollViewer?.ScrollToVerticalOffset(0); } catch (System.Exception ex) { EditorLog.Warning($"Centrar cámara: {ex.Message}", "Editor"); } };
        return _cachedOverviewPanel;
    }

    private MapPropertiesInspectorPanel GetOrCreateMapPropertiesPanel()
    {
        if (_cachedMapPropertiesPanel != null) return _cachedMapPropertiesPanel;
        _cachedMapPropertiesPanel = new MapPropertiesInspectorPanel();
        _cachedMapPropertiesPanel.RequestOpenProjectConfig += (_, _) => MenuEditorProyectoAvanzado_OnClick(this, new RoutedEventArgs());
        return _cachedMapPropertiesPanel;
    }

    private LayerInspectorPanel GetOrCreateLayerInspectorPanel()
    {
        if (_cachedLayerInspectorPanel != null) return _cachedLayerInspectorPanel;
        _cachedLayerInspectorPanel = new LayerInspectorPanel();
        _cachedLayerInspectorPanel.PropertyChanged += (_, _) =>
        {
            ProjectExplorer?.SetModified(GetCurrentSceneMapPath(), true);
            DrawMap();
        };
        _cachedLayerInspectorPanel.LayerComponentRequested += (_, desc) =>
        {
            _selection.SetInspectorContextLayer(desc);
            RefreshInspector();
        };
        return _cachedLayerInspectorPanel;
    }

    private TileInspectorPanel GetOrCreateTileInspectorPanel()
    {
        if (_cachedTileInspectorPanel != null) return _cachedTileInspectorPanel;
        _cachedTileInspectorPanel = new TileInspectorPanel();
        return _cachedTileInspectorPanel;
    }

    private AnimationInspectorPanel GetOrCreateAnimationInspectorPanel()
    {
        if (_cachedAnimationInspectorPanel != null) return _cachedAnimationInspectorPanel;
        _cachedAnimationInspectorPanel = new AnimationInspectorPanel();
        return _cachedAnimationInspectorPanel;
    }

    /// <summary>Releases references to cached inspector panels and unsubscribes events to avoid leaks. Call on Closing.</summary>
    private void CleanupCachedPanels()
    {
        if (InspectorPanel != null) InspectorPanel.Content = null;
        if (_cachedObjectPanel != null)
        {
            _cachedObjectPanel.PropertyChanged -= CachedObjectPanel_PropertyChanged;
            _cachedObjectPanel.RequestDuplicate -= MapHierarchy_OnRequestDuplicateObject;
            _cachedObjectPanel.RequestDelete -= MapHierarchy_OnRequestDeleteObject;
            _cachedObjectPanel.RequestRename -= MapHierarchy_OnRequestRenameObject;
            _cachedObjectPanel.RequestConvertToSeed -= CachedObjectPanel_RequestConvertToSeed;
        }
        if (_cachedUIElementPanel != null)
            _cachedUIElementPanel.PropertyChanged -= CachedUIElementPanel_PropertyChanged;
        _cachedTriggerPanel = null;
        _cachedObjectPanel = null;
        _cachedUIElementPanel = null;
        _cachedMultiPanel = null;
        _cachedOverviewPanel = null;
        _cachedMapPropertiesPanel = null;
        _cachedTileInspectorPanel = null;
        _cachedAnimationInspectorPanel = null;
        _cachedLayerInspectorPanel = null;
        _cachedQuickPanel = null;
        _cachedProjectManifestPanel = null;
    }

    private ProjectManifestPanel GetOrCreateProjectManifestPanel()
    {
        if (_cachedProjectManifestPanel != null) return _cachedProjectManifestPanel;
        _cachedProjectManifestPanel = new ProjectManifestPanel();
        _cachedProjectManifestPanel.RequestSaveAfterApply += ProjectManifest_OnSaveAfterApply;
        _cachedProjectManifestPanel.RequestExportBuild += (_, _) => MenuExportBuild_OnClick(this, new RoutedEventArgs());
        _cachedProjectManifestPanel.RequestIntegrityCheck += (_, _) => MenuVerificarIntegridad_OnClick(this, new RoutedEventArgs());
        _cachedProjectManifestPanel.RequestOpenProjectFolder += ProjectManifest_OnOpenProjectFolder;
        _cachedProjectManifestPanel.RequestAdvancedConfig += (_, _) => MenuEditorProyectoAvanzado_OnClick(this, new RoutedEventArgs());
        return _cachedProjectManifestPanel;
    }

    private void ProjectManifest_OnSaveAfterApply(object? sender, EventArgs e)
    {
        if (_cachedProjectManifestPanel == null) return;
        if (!_cachedProjectManifestPanel.TryApplyToProject(_project)) return;
        TrySaveProjectInfo();
        ClampViewportCenterForCurrentMap();
        ApplyEditorVisualColorsFromProject();
        DrawMap();
        var path = GetProjectFilePath();
        if (!string.IsNullOrEmpty(path))
            ProjectExplorer?.SetModified(path, true);
        ConfigureAutoSave();
        SyncDiscordRichPresence();
    }

    private void ProjectManifest_OnOpenProjectFolder(object? sender, EventArgs e)
    {
        var dir = _project.ProjectDirectory;
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return;
        try { System.Diagnostics.Process.Start("explorer.exe", dir); }
        catch (Exception ex) { EditorLog.Warning($"No se pudo abrir la carpeta: {ex.Message}", "Proyecto"); }
    }

    private QuickPropertiesPanel GetOrCreateQuickPropertiesPanel()
    {
        if (_cachedQuickPanel != null) return _cachedQuickPanel;
        _cachedQuickPanel = new QuickPropertiesPanel();
        _cachedQuickPanel.RequestOpenInEditor += Quick_RequestOpenInEditor;
        _cachedQuickPanel.RequestOpenScriptPath += Quick_RequestOpenScriptPath;
        _cachedQuickPanel.RequestDuplicate += Quick_RequestDuplicate;
        _cachedQuickPanel.RequestRename += Quick_RequestRename;
        _cachedQuickPanel.RequestShowInFolder += Quick_RequestShowInFolder;
        _cachedQuickPanel.RequestDelete += Quick_RequestDelete;
        return _cachedQuickPanel;
    }
}
