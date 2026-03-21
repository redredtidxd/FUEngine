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
                tilePanel.SetTile(_selection.InspectorContextTileId);
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
            var toolName = _toolMode switch { ToolMode.Pintar => "Pincel", ToolMode.Rectangulo => "Rectángulo", ToolMode.Linea => "Línea", ToolMode.Relleno => "Relleno", ToolMode.Goma => "Goma", ToolMode.Picker => "Cuentagotas", ToolMode.Stamp => "Stamp", ToolMode.Seleccionar => "Seleccionar", ToolMode.Colocar => "Colocar objeto", ToolMode.Zona => "Zona", ToolMode.Medir => "Medir", ToolMode.PixelEdit => "Pixel", _ => "Pincel" };
            var tileNames = new[] { "Suelo", "Pared", "Objeto", "Especial" };
            int tileIdx = CmbTileType?.SelectedIndex ?? 0;
            var toolDetail = _toolMode == ToolMode.Pintar ? $"Tile: {tileNames[Math.Clamp(tileIdx, 0, 3)]}" : _toolMode == ToolMode.Colocar && CmbObjectDef?.SelectedItem is ObjectDefinition od ? $"Objeto: {od.Nombre}" : "";
            var layerName = CmbCapaVisible?.SelectedIndex >= 0 && CmbCapaVisible?.Items?.Count > 0 ? (CmbCapaVisible.SelectedItem as string) ?? "Todas" : "Todas";
            overview.SetData(_project, _tileMap, _objectLayer, _scriptRegistry, toolName, toolDetail, layerName, 0, tilesCount, _objectLayer.Instances.Count, triggersCount, _scriptRegistry?.GetAll()?.Count ?? 0, (int)_selectedTileType, CmbObjectDef?.SelectedItem as ObjectDefinition);
            InspectorPanel.Content = overview;
            return;
        }
        var objPanel = GetOrCreateObjectPanel();
        var scripts = _scriptRegistry?.GetAll()?.Select(s => (s.Id, s.Nombre, s.Path)) ?? Enumerable.Empty<(string, string, string?)>();
        objPanel.SetProjectDirectory(_project.ProjectDirectory);
        objPanel.SetAvailableScripts(scripts);
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
            MapHierarchy.SetMapStructure(System.IO.Path.GetFileNameWithoutExtension(_project.MapPath), _project.LayerNames, _objectLayer, _triggerZones, _visibleLayers, GetCurrentUIRoot());
            _selection.SetTriggerSelection(clone);
            RefreshInspector();
        };
        _cachedTriggerPanel.RequestDelete += (_, z) =>
        {
            if (System.Windows.MessageBox.Show(this, "¿Eliminar esta zona trigger?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            _triggerZones.Remove(z);
            _selection.SetTriggerSelection(null);
            TriggerZoneSerialization.Save(_triggerZones, _project.TriggerZonesPath);
            MapHierarchy.SetMapStructure(System.IO.Path.GetFileNameWithoutExtension(_project.MapPath), _project.LayerNames, _objectLayer, _triggerZones, _visibleLayers, GetCurrentUIRoot());
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
        if (MapHierarchy != null)
            MapHierarchy.SetMapStructure(System.IO.Path.GetFileNameWithoutExtension(GetCurrentSceneMapPath()), _project.LayerNames, _objectLayer, _triggerZones, _visibleLayers, GetCurrentUIRoot());
        DrawMap();
    }
    private void CachedObjectPanel_RequestConvertToSeed(object? sender, ObjectInstance e) => ConvertObjectToSeed(e);
    private void CachedUIElementPanel_PropertyChanged(object? sender, EventArgs e)
    {
        var canvas = _selection.SelectedUICanvas ?? FindCanvasForElement(_selection.SelectedUIElement);
        if (canvas != null)
            ProjectExplorer?.SetModified(System.IO.Path.Combine(GetCurrentUIFolder(), canvas.Id + ".json"), true);
        MapHierarchy.SetMapStructure(System.IO.Path.GetFileNameWithoutExtension(GetCurrentSceneMapPath()), _project.LayerNames, _objectLayer, _triggerZones, _visibleLayers, GetCurrentUIRoot());
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
        _cachedOverviewPanel.AddTriggerClicked += (_, _) => System.Windows.MessageBox.Show(this, "Añadir trigger: en desarrollo. Edita triggerZones.json o usa la pestaña correspondiente.", "Añadir trigger", MessageBoxButton.OK);
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
        _cachedMapPropertiesPanel.RequestOpenProjectConfig += (_, _) => MenuConfigProyecto_OnClick(this, new RoutedEventArgs());
        return _cachedMapPropertiesPanel;
    }

    private LayerInspectorPanel GetOrCreateLayerInspectorPanel()
    {
        if (_cachedLayerInspectorPanel != null) return _cachedLayerInspectorPanel;
        _cachedLayerInspectorPanel = new LayerInspectorPanel();
        _cachedLayerInspectorPanel.PropertyChanged += (_, _) => DrawMap();
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
    }

    private QuickPropertiesPanel GetOrCreateQuickPropertiesPanel()
    {
        if (_cachedQuickPanel != null) return _cachedQuickPanel;
        _cachedQuickPanel = new QuickPropertiesPanel();
        _cachedQuickPanel.RequestOpenInEditor += Quick_RequestOpenInEditor;
        _cachedQuickPanel.RequestDuplicate += Quick_RequestDuplicate;
        _cachedQuickPanel.RequestRename += Quick_RequestRename;
        _cachedQuickPanel.RequestShowInFolder += Quick_RequestShowInFolder;
        _cachedQuickPanel.RequestDelete += Quick_RequestDelete;
        return _cachedQuickPanel;
    }
}
