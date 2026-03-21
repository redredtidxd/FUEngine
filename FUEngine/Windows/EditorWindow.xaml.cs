using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using FUEngine.Core;
using FUEngine.Dialogs;
using FUEngine.Editor;

namespace FUEngine;

/// <summary>Estado en memoria de una escena abierta: datos (mapa, objetos, UI) y layout de tabs opcionales.</summary>
internal sealed class OpenSceneState
{
    public SceneDefinition? Definition { get; set; }
    public int SceneIndex { get; set; }
    public TileMap TileMap { get; set; } = new TileMap(Chunk.DefaultSize);
    public ObjectLayer ObjectLayer { get; set; } = new ObjectLayer();
    /// <summary>Raíz UI de la escena (Canvas por escena).</summary>
    public UIRoot UIRoot { get; set; } = new UIRoot();
    public List<string> OpenOptionalTabKinds { get; set; } = new();
    public string? SelectedTabKind { get; set; }
}

public partial class EditorWindow : Window
{
    private readonly ProjectInfo _project;
    private TileMap _tileMap;
    private ObjectLayer _objectLayer = new ObjectLayer();
    private List<OpenSceneState> _openScenes = new();
    private int _currentSceneIndex;
    private List<TriggerZone> _triggerZones = new();
    private List<SeedDefinition> _seedDefinitions = new();
    private ScriptRegistry _scriptRegistry = new();
    private double _zoom = 1.0;
    private TileType _selectedTileType = TileType.Suelo;
    private bool _isDragging;
    private System.Windows.Point _dragStartPos;
    private double _dragStartX, _dragStartY;
    private readonly SelectionManager _selection = new();
    private int? _zoneMinTx, _zoneMinTy, _zoneMaxTx, _zoneMaxTy;
    private bool _zoneDragging;
    private int _zoneStartTx, _zoneStartTy;
    private ZoneClipboard? _zoneClipboard;
    private int _pasteOriginTx = 0, _pasteOriginTy = 0;
    private enum ToolMode { Pintar, Rectangulo, Linea, Relleno, Goma, Picker, Stamp, Seleccionar, Colocar, Zona, Medir, PixelEdit }
    private ToolMode _toolMode = ToolMode.Seleccionar;

    /// <summary>Active tool mode. Setter updates ToolController so no need to call UpdateCurrentTool() manually.</summary>
    private ToolMode CurrentToolMode
    {
        get => _toolMode;
        set
        {
            if (_toolMode == value) return;
            _toolMode = value;
            UpdateCurrentTool();
        }
    }
    private int _brushSize = 1;
    private int _brushRotation; // 0, 90, 180, 270
    private bool _rectDragging;
    private int _rectStartTx, _rectStartTy, _rectEndTx, _rectEndTy;
    private (int x, int y)? _lineStart;
    private readonly EditorHistory _history = new();
    private (int x, int y)? _measureStart;
    private (int x, int y)? _measureEnd;
    private readonly HashSet<int> _visibleLayers = new() { 0 };
    private int _activeLayerIndex;
    private ProjectExplorerPanel? _explorerPanel;
    private ExplorerMetadataService? _explorerMetadataService;
    private AutoSaveService? _autoSaveService;
    private AudioAssetRegistry? _audioAssetRegistry;
    private AudioSystem? _audioSystem;
    private const string TabTagKey = "TabKind";
    private const string EditorStateFileName = ".fuengine-editor.json";
    private const string EditorLayoutFileName = ".editorlayout";
    private int? _sceneDragSourceIndex;
    private System.Windows.Point _sceneDragStartPoint;

    /// <summary>Registro central de tabs dinámicos: kind → factory (recibe tabItem para los que lo necesiten).</summary>
    private Dictionary<string, Func<TabItem?, System.Windows.Controls.UserControl>> TabFactories { get; }

    /// <summary>Nombre para mostrar en menú por kind.</summary>
    private static readonly Dictionary<string, string> TabDisplayNames = new()
    {
        { "Scripts", "Scripts" },
        { "Explorador", "Explorador" },
        { "Tiles", "Tiles" },
        { "Animaciones", "Animaciones" },
        { "Seeds", "Seeds" },
        { "Consola", "Consola" },
        { "Juego", "Juego (Play embebido)" },
        { "Debug", "Debug" },
        { "Audio", "Audio" },
        { "TileCreator", "Tile Creator" },
        { "TileEditor", "Tile Editor" },
        { "PaintCreator", "Paint Creator" },
        { "PaintEditor", "Paint Editor" },
        { "CollisionsEditor", "Editor de colisiones" },
        { "ScriptableTile", "Tile por script" }
    };

    /// <summary>Icono (emoji o texto) por kind para menú y cabecera de tab.</summary>
    private static readonly Dictionary<string, string> TabIcons = new()
    {
        { "Mapa", "\uD83D\uDDFA" },
        { "Consola", "\uD83D\uDDA5" },
        { "Scripts", "\uD83D\uDCDC" },
        { "Explorador", "\uD83D\uDCC1" },
        { "Tiles", "\uD83E\uDDF1" },
        { "Animaciones", "\uD83C\uDF9E" },
        { "Seeds", "\uD83E\uDDF1" },
        { "Audio", "\uD83C\uDFA4" },
        { "Juego", "\u25B6" },
        { "Debug", "\uD83D\uDC1E" },
        { "TileCreator", "\uD83E\uDDF1" },
        { "TileEditor", "\uD83E\uDDF1" },
        { "PaintCreator", "\uD83D\uDD8C" },
        { "PaintEditor", "\uD83D\uDD8C" },
        { "CollisionsEditor", "\uD83D\uDD12" },
        { "ScriptableTile", "\uD83D\uDDA8" }
    };

    /// <summary>Categoría del menú "+" por kind (Proyecto, Contenido, Multimedia, Debug).</summary>
    private static readonly Dictionary<string, string> TabCategory = new()
    {
        { "Scripts", "Proyecto" },
        { "Explorador", "Proyecto" },
        { "Tiles", "Contenido" },
        { "Animaciones", "Contenido" },
        { "Seeds", "Contenido" },
        { "Audio", "Multimedia" },
        { "Consola", "Debug" },
        { "Juego", "Debug" },
        { "Debug", "Debug" },
        { "TileCreator", "Contenido" },
        { "TileEditor", "Contenido" },
        { "PaintCreator", "Contenido" },
        { "PaintEditor", "Contenido" },
        { "CollisionsEditor", "Contenido" },
        { "ScriptableTile", "Contenido" }
    };

    /// <summary>Orden de kinds opcionales para el menú (por categoría y luego orden fijo).</summary>
    private static readonly string[] OptionalTabKindsOrder = { "Scripts", "Explorador", "Tiles", "Animaciones", "Seeds", "TileCreator", "TileEditor", "PaintCreator", "PaintEditor", "CollisionsEditor", "ScriptableTile", "Audio", "Consola", "Juego", "Debug" };
    private bool _gridVisible = true;
    private bool _snapToGrid = true;
    private System.Windows.Media.Color _gridColor = System.Windows.Media.Color.FromRgb(0x30, 0x36, 0x3d);
    private bool _panDragging;
    private System.Windows.Point? _panStartPoint;
    private double _panStartScrollX, _panStartScrollY;
    private bool _maskColision;
    /// <summary>Unidad de coordenadas en barra de estado (Tiles / SubTiles / Pixels); se recarga al activar la ventana.</summary>
    private string _coordinateUnitDisplay = "Tiles";
    private double _zoomWheelSensitivity = 1;
    private double _panKeyStepScale = 1;
    private bool _spacePanHeld;
    /// <summary>True si <see cref="Mouse.OverrideCursor"/> se puso en mano por el atajo de pan (Espacio por defecto).</summary>
    private bool _handPanShortcutCursorActive;
    private bool _maskScripts;
    private MapSnapshot? _mapSnapshot;
    private int _canvasMinWx;
    private int _canvasMinWy;
    private bool _showTileCoordinates = true;
    private bool _showVisibleArea = true;
    private bool _showStreamingGizmos;
    private bool _showColliders;
    private readonly MapRenderer _mapRenderer = new();
    private TriggerZoneInspectorPanel? _cachedTriggerPanel;
    private ObjectInspectorPanel? _cachedObjectPanel;
    private UIElementInspectorPanel? _cachedUIElementPanel;
    private MultiObjectInspectorPanel? _cachedMultiPanel;
    private DefaultInspectorPanel? _cachedOverviewPanel;
    private MapPropertiesInspectorPanel? _cachedMapPropertiesPanel;
    private TileInspectorPanel? _cachedTileInspectorPanel;
    private LayerInspectorPanel? _cachedLayerInspectorPanel;
    private AnimationInspectorPanel? _cachedAnimationInspectorPanel;
    private QuickPropertiesPanel? _cachedQuickPanel;
    private ToolController? _toolController;
    private IMapEditorToolContext? _toolContext;
    private PaintTool? _paintTool;
    /// <summary>
    /// Si se asigna (p. ej. al iniciar modo Play), se invoca al guardar un .lua con la ruta relativa para hot reload.
    /// El motor debe: 1) Llamar a LuaScriptRuntime.ReloadScript(relPath). 2) Suscribirse a runtime.ScriptReloaded y
    /// para cada path recargado, recrear instancias con CreateInstance(...) en cada objeto que tenga ese script y asignar
    /// el nuevo ScriptInstance a ScriptComponent.ScriptInstanceHandle.
    /// </summary>
    public Action<string>? OnLuaScriptSavedForReload { get; set; }

    /// <summary>Asigna el runtime de Lua para que sus errores se redirijan a la consola del editor. Llamar al iniciar modo Play.</summary>
    public void SetLuaScriptRuntimeForConsole(FUEngine.Runtime.LuaScriptRuntime? runtime)
    {
        if (_luaScriptRuntimeForConsole != null)
        {
            _luaScriptRuntimeForConsole.ScriptError = null;
            _luaScriptRuntimeForConsole.PrintOutput = null;
        }
        _luaScriptRuntimeForConsole = runtime;
        if (runtime != null)
        {
            runtime.ScriptError = (path, line, msg) => EditorLog.Error(line > 0 ? $"{path}:{line} {msg}" : $"{path} {msg}", "Lua", path, line > 0 ? line : null);
            runtime.PrintOutput = msg => EditorLog.Info(msg ?? "", "Lua");
        }
    }
    private FUEngine.Runtime.LuaScriptRuntime? _luaScriptRuntimeForConsole;
    private PlayModeRunner? _playModeRunner;
    private DispatcherTimer? _toastTimer;
    private DispatcherTimer? _mapAnimationTimer;
    private readonly Queue<(string Message, LogLevel Level)> _toastQueue = new();
    private const int MaxToastQueue = 10;

    public EditorWindow(ProjectInfo project)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        if (string.IsNullOrWhiteSpace(_project.ProjectDirectory))
            throw new InvalidOperationException("El proyecto no tiene carpeta asignada.");
        if (_project.LayerNames == null || _project.LayerNames.Count == 0)
            _project.LayerNames = new List<string> { "Suelo" };
        _tileMap = new TileMap(Chunk.DefaultSize);
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            if (EngineSettings.Load().HardwareAccelerationEnabled) return;
            if (System.Windows.PresentationSource.FromVisual(this) is System.Windows.Interop.HwndSource hs && hs.CompositionTarget != null)
                hs.CompositionTarget.RenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
        };
        EngineTypography.ApplyToRoot(this);
        ApplyEngineCollisionMaskDefault();
        Title = $"Editor - {_project.Nombre}";
        LoadProjectData();
        _explorerMetadataService = new ExplorerMetadataService();
        _explorerMetadataService.Initialize(_project.ProjectDirectory ?? "");
        _audioAssetRegistry = new AudioAssetRegistry();
        _audioAssetRegistry.Initialize(_project.ProjectDirectory ?? "");
        _audioSystem = new AudioSystem(new EditorAudioBackend(), _audioAssetRegistry);
        EnsureDefaultObjectDefinition();
        CmbObjectDef.ItemsSource = _objectLayer?.Definitions?.Values?.ToList() ?? new List<ObjectDefinition>();
        if (CmbObjectDef != null && CmbObjectDef.Items.Count > 0) CmbObjectDef.SelectedIndex = 0;
        SyncLayerComboFromTileMap();
        if (LayersPanel != null)
        {
            LayersPanel.SetTileMap(_tileMap);
            LayersPanel.ActiveLayerIndex = 0;
            LayersPanel.ActiveLayerChanged += LayersPanel_OnActiveLayerChanged;
            LayersPanel.LayerSelected += LayersPanel_OnLayerSelected;
            LayersPanel.LayerVisibilityToggled += LayersPanel_OnLayerVisibilityToggled;
            LayersPanel.LayerRemoved += (_, _) => { SyncLayerComboFromTileMap(); SyncProjectLayerNamesFromTileMap(); MapHierarchy?.SetMapStructure(System.IO.Path.GetFileNameWithoutExtension(GetCurrentSceneMapPath()), _project.LayerNames, _objectLayer, _triggerZones, _visibleLayers, GetCurrentUIRoot()); };
            LayersPanel.LayersReordered += (_, _) => { SyncLayerComboFromTileMap(); SyncProjectLayerNamesFromTileMap(); MapHierarchy?.SetMapStructure(System.IO.Path.GetFileNameWithoutExtension(GetCurrentSceneMapPath()), _project.LayerNames, _objectLayer, _triggerZones, _visibleLayers, GetCurrentUIRoot()); };
        }
        BuildVisibleLayersFromTileMap();
        Activated += (_, _) => RefreshCoordinateUnitFromSettings();
        Loaded += async (_, _) =>
        {
            RefreshCoordinateUnitFromSettings();
            _mapAnimationTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(100) };
            _mapAnimationTimer.Tick += (_, _) => DrawMap();
            _mapAnimationTimer.Start();
            MapCanvas?.Focus();
            UpdatePaletteSelection();
            ApplyZoom();
            UpdateTileSelectionToolbarVisibility();
            UpdateTransformButtonContent();
            UpdateTilePreview();
            UpdateObjectPreview();
            UpdateToolbarVisibility();
            BuildScenesStrip();
            await LoadEditorStateAsync();
            WarnIfMultipleProjectFiles(_project.ProjectDirectory ?? "");
            await LoadEditorLayoutAsync();
            ApplyCurrentSceneOptionalTabs();
            if (_openScenes.Count > 0 && !string.IsNullOrEmpty(_openScenes[_currentSceneIndex].SelectedTabKind))
            {
                var tab = GetTabByKind(_openScenes[_currentSceneIndex].SelectedTabKind!);
                if (tab != null) tab.IsSelected = true;
            }
            ConfigureAutoSave();
            UpdateCurrentTool();
        };
        DrawMap();
        RefreshInspector();
        if (MapHierarchy != null)
        {
            MapHierarchy.SetMapStructure(System.IO.Path.GetFileNameWithoutExtension(GetCurrentSceneMapPath()), _project.LayerNames, _objectLayer, _triggerZones, _visibleLayers, GetCurrentUIRoot());
            MapHierarchy.SetTriggerData(_triggerZones, () => TriggerZoneSerialization.Save(_triggerZones, _project.TriggerZonesPath));
            MapHierarchy.ObjectSelected += MapHierarchy_OnObjectSelected;
            MapHierarchy.LayerVisibilityToggled += MapHierarchy_OnLayerVisibilityToggled;
            MapHierarchy.TriggerSelected += MapHierarchy_OnTriggerSelected;
            MapHierarchy.RequestCreateObject += MapHierarchy_OnRequestCreateObject;
            MapHierarchy.RequestDuplicateObject += MapHierarchy_OnRequestDuplicateObject;
            MapHierarchy.RequestDeleteObject += MapHierarchy_OnRequestDeleteObject;
            MapHierarchy.RequestRenameObject += MapHierarchy_OnRequestRenameObject;
            MapHierarchy.RequestAddLayer += MapHierarchy_OnRequestAddLayer;
            MapHierarchy.RequestRefresh += MapHierarchy_OnRequestRefresh;
            MapHierarchy.RequestInstantiateAsset += MapHierarchy_OnRequestInstantiateAsset;
            MapHierarchy.RequestReorderLayers += MapHierarchy_OnRequestReorderLayers;
            MapHierarchy.RequestCreateUICanvas += MapHierarchy_OnRequestCreateUICanvas;
            MapHierarchy.RequestCreateUIElement += MapHierarchy_OnRequestCreateUIElement;
            MapHierarchy.RequestOpenCanvasInTab += MapHierarchy_OnRequestOpenCanvasInTab;
            MapHierarchy.UICanvasSelected += MapHierarchy_OnUICanvasSelected;
            MapHierarchy.UIElementSelected += MapHierarchy_OnUIElementSelected;
        }
        if (ProjectExplorer != null)
        {
            ProjectExplorer.SetProject(_project.ProjectDirectory ?? "", _project.Nombre ?? "Proyecto");
            ProjectExplorer.SetMetadataService(_explorerMetadataService);
            ProjectExplorer.IsCompactMode = true;
            ProjectExplorer.ApplyCompactMode();
            ProjectExplorer.SelectionChanged += ProjectExplorer_OnSelectionChanged;
            ProjectExplorer.RequestOpenInEditor += ProjectExplorer_OnRequestOpenInEditor;
            ProjectExplorer.RequestOpenInCollisionsEditor += ProjectExplorer_OnRequestOpenInCollisionsEditor;
            ProjectExplorer.RequestOpenInScriptableTile += ProjectExplorer_OnRequestOpenInScriptableTile;
            ProjectExplorer.RequestCreateTileLayer += Explorer_OnRequestCreateTileLayer;
            ProjectExplorer.RequestCreateObjectLayer += Explorer_OnRequestCreateObjectLayer;
            ProjectExplorer.RequestCreateTriggerZone += Explorer_OnRequestCreateTriggerZone;
            ProjectExplorer.RequestCreateObject += Explorer_OnRequestCreateObject;
            ProjectExplorer.LuaScriptRegistered += ProjectExplorer_OnLuaScriptRegistered;
            ProjectExplorer.ScriptsRegistryChanged += ProjectExplorer_OnScriptsRegistryChanged;
            _explorerPanel = ProjectExplorer;
        }
        InitializeFixedTabs();
        EditorLog.RequestOpenFileAtLine += OnRequestOpenFileAtLine;
        EditorLog.EntryAdded += EditorLog_EntryAdded;
        EditorLog.ToastRequested += EditorLog_ToastRequested;
        _history.HistoryChanged += (_, _) => UpdateUndoRedoMenu();
        UpdateUndoRedoMenu();
        ProjectIntegrityChecker.Run(_project, _tileMap, _objectLayer!, _scriptRegistry);
        UpdateStatusBar("X: 0  Y: 0  Chunk: (0,0)  |  Seleccionar  |  Capa: Suelo  Tamaño: 1  Rot: 0°");
        ApplyVerMenuState();
        Closing += (_, _) =>
        {
            _mapAnimationTimer?.Stop();
            _mapAnimationTimer = null;
            EditorLog.RequestOpenFileAtLine -= OnRequestOpenFileAtLine;
            EditorLog.EntryAdded -= EditorLog_EntryAdded;
            EditorLog.ToastRequested -= EditorLog_ToastRequested;
            _audioAssetRegistry?.Dispose();
            _playModeRunner?.Stop();
            _playModeRunner = null;
            if (MainTabs?.Items != null)
            {
                foreach (var item in MainTabs.Items)
                {
                    if (item is TabItem tab && tab.Content is GameTabContent g)
                        g.Dispose();
                }
            }
            if (_project.AutoSaveOnClose && HasUnsavedChanges())
                _autoSaveService?.ExecuteAutosave();
            _autoSaveService?.Stop();
            _ = SaveEditorStateAsync();
            _ = SaveEditorLayoutAsync();
            CleanupCachedPanels();
        };
        _toolContext = new EditorToolContext(this);
        _toolController = new ToolController(HandleToolMouseDown, HandleToolMouseMove, HandleToolMouseUp);
        _paintTool = new PaintTool(_toolContext);

        TabFactories = new Dictionary<string, Func<TabItem?, System.Windows.Controls.UserControl>>
        {
            { "Scripts", _ => CreateScriptsTabContent() },
            { "Explorador", ti => CreateExplorerTabContent(ti!) },
            { "Tiles", _ => new TilesTabContent() },
            { "Animaciones", _ => new AnimationsTabContent() },
            // Seeds reutiliza la misma UI que objetos; si en el futuro necesita lógica distinta (filtros, drag-drop de seeds), crear CreateSeedsTabContent().
            { "Seeds", _ => CreateObjectsTabContent() },
            { "Consola", _ => new ConsoleTabContent() },
            { "Juego", _ => CreateGameTabContent() },
            { "Debug", _ => CreateDebugTabContent() },
            { "Audio", _ => CreateAudioTabContent() },
            // Creative Suite
            { "TileCreator", _ => new TileCreatorTabContent() },
            { "TileEditor", _ => new TileEditorTabContent() },
            { "PaintCreator", _ => new PaintCreatorTabContent() },
            { "PaintEditor", _ => new PaintEditorTabContent() },
            { "CollisionsEditor", _ => new CollisionsEditorTabContent() },
            { "ScriptableTile", _ => new ScriptableTileTabContent() }
        };
    }

    private void UpdateCurrentTool()
    {
        if (_toolController == null) return;
        _toolController.CurrentTool = _toolMode == ToolMode.Pintar && _paintTool != null ? _paintTool : null;
    }

    private async Task LoadEditorStateAsync()
    {
        try
        {
            var path = System.IO.Path.Combine(_project.ProjectDirectory ?? "", EditorStateFileName);
            if (!File.Exists(path)) return;
            var json = await File.ReadAllTextAsync(path);
            var state = JsonSerializer.Deserialize<EditorWindowState>(json);
            if (state == null || MainTabs == null) return;
            var idx = state.SelectedTabIndex;
            if (idx >= 0 && idx < MainTabs.Items.Count)
                MainTabs.SelectedIndex = idx;
            if (state.WindowWidth > 0 && state.WindowHeight > 0)
            {
                ClampWindowToWorkingArea(state.WindowLeft, state.WindowTop, state.WindowWidth, state.WindowHeight);
            }
        }
        catch (Exception ex) { EditorLog.Warning($"No se pudo cargar estado del editor: {ex.Message}", "Editor"); }
    }

    /// <summary>Coloca la ventana dentro del área de trabajo (SystemParameters son DPI-aware).</summary>
    private void ClampWindowToWorkingArea(double left, double top, double width, double height)
    {
        var vLeft = SystemParameters.VirtualScreenLeft;
        var vTop = SystemParameters.VirtualScreenTop;
        var vWidth = SystemParameters.VirtualScreenWidth;
        var vHeight = SystemParameters.VirtualScreenHeight;
        const double minVisible = 100;
        var w = Math.Max(400, Math.Min(width, vWidth));
        var h = Math.Max(300, Math.Min(height, vHeight));
        var l = Math.Max(vLeft - w + minVisible, Math.Min(left, vLeft + vWidth - minVisible));
        var t = Math.Max(vTop - h + minVisible, Math.Min(top, vTop + vHeight - minVisible));
        Left = l;
        Top = t;
        Width = w;
        Height = h;
    }

    private async Task LoadEditorLayoutAsync()
    {
        try
        {
            var path = System.IO.Path.Combine(_project.ProjectDirectory ?? "", EditorLayoutFileName);
            if (!File.Exists(path))
            {
                if (MainTabs != null && !HasTabWithKind("Juego"))
                    AddOrSelectTab("Juego");
                var juegoTab = GetTabByKind("Juego");
                if (juegoTab != null)
                    juegoTab.IsSelected = true;
                return;
            }
            var json = await File.ReadAllTextAsync(path);
            var layout = JsonSerializer.Deserialize<EditorLayoutState>(json);
            if (layout?.OpenTabs == null || MainTabs == null) return;

            foreach (var kind in layout.OpenTabs)
            {
                if (string.IsNullOrEmpty(kind)) continue;
                if (kind == "Mapa" || kind == "Consola") continue;
                var kindToAdd = kind == "Objetos" ? "Seeds" : kind;
                if (!HasTabWithKind(kindToAdd))
                    AddOrSelectTab(kindToAdd);
            }

            var selectedTabKind = layout.SelectedTab == "Objetos" ? "Seeds" : layout.SelectedTab;
            if (string.IsNullOrEmpty(selectedTabKind)) selectedTabKind = "Juego";
            if (!HasTabWithKind(selectedTabKind))
                AddOrSelectTab(selectedTabKind);
            var tab = GetTabByKind(selectedTabKind);
            if (tab != null)
                tab.IsSelected = true;
        }
        catch (Exception ex) { EditorLog.Warning($"No se pudo cargar layout del editor: {ex.Message}", "Editor"); }
    }

    private async Task SaveEditorLayoutAsync()
    {
        try
        {
            var dir = _project.ProjectDirectory ?? "";
            if (string.IsNullOrEmpty(dir)) return;
            var path = System.IO.Path.Combine(dir, EditorLayoutFileName);
            var openTabs = new List<string>();
            string? selectedTab = null;
            if (MainTabs != null)
            {
                foreach (TabItem tab in MainTabs.Items)
                {
                    if (tab.Tag is string k && !string.IsNullOrEmpty(k))
                        openTabs.Add(k);
                }
                if (MainTabs.SelectedItem is TabItem sel && sel.Tag is string sk)
                    selectedTab = sk;
            }
            var layout = new EditorLayoutState { OpenTabs = openTabs, SelectedTab = selectedTab ?? "Juego" };
            var json = JsonSerializer.Serialize(layout, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(path, json);
        }
        catch (Exception ex) { EditorLog.Warning($"No se pudo guardar layout del editor: {ex.Message}", "Editor"); }
    }

    private bool HasTabWithKind(string kind)
    {
        if (MainTabs == null) return false;
        foreach (TabItem tab in MainTabs.Items)
        {
            if (tab.Tag is string k && k == kind) return true;
        }
        return false;
    }

    private async Task SaveEditorStateAsync()
    {
        try
        {
            var dir = _project.ProjectDirectory ?? "";
            if (string.IsNullOrEmpty(dir)) return;
            var path = System.IO.Path.Combine(dir, EditorStateFileName);
            var openKinds = new List<string>();
            if (MainTabs != null)
            {
                foreach (TabItem tab in MainTabs.Items)
                {
                    var kind = tab.Tag as string ?? tab.Header as string ?? "";
                    if (!string.IsNullOrEmpty(kind))
                        openKinds.Add(kind);
                }
            }
            var state = new EditorWindowState
            {
                SelectedTabIndex = MainTabs?.SelectedIndex ?? 0,
                OpenTabKinds = openKinds.Count > 0 ? openKinds : null,
                WindowLeft = Left,
                WindowTop = Top,
                WindowWidth = Width,
                WindowHeight = Height
            };
            var json = JsonSerializer.Serialize(state);
            await File.WriteAllTextAsync(path, json);
        }
        catch (Exception ex) { EditorLog.Warning($"No se pudo guardar estado del editor: {ex.Message}", "Editor"); }
    }

    private void ApplyVerMenuState()
    {
        if (MenuVerConsola != null) MenuVerConsola.IsChecked = MainTabs?.SelectedItem == TabConsola;
        if (MenuVerJuego != null) MenuVerJuego.IsChecked = (MainTabs?.SelectedItem as TabItem)?.Tag as string == "Juego";
    }
    private void UpdateUndoRedoMenu()
    {
        if (MenuDeshacer != null) MenuDeshacer.IsEnabled = _history.CanUndo;
        if (MenuRehacer != null) MenuRehacer.IsEnabled = _history.CanRedo;
        if (BtnUndo != null) BtnUndo.IsEnabled = _history.CanUndo;
        if (BtnRedo != null) BtnRedo.IsEnabled = _history.CanRedo;
    }

    private void MapHierarchy_OnObjectSelected(object? sender, ObjectInstance? instance)
    {
        _selection.SetObjectSelection(instance);
        RefreshInspector();
        DrawMap();
    }

    private void MapHierarchy_OnUICanvasSelected(object? sender, UICanvas? canvas)
    {
        if (canvas == null) return;
        _selection.SetUISelection(canvas, element: null);
        RefreshInspector();
        SyncSelectedUIElementInOpenTabs();
    }

    private void MapHierarchy_OnUIElementSelected(object? sender, FUEngine.Core.UIElement? element)
    {
        if (element == null) return;
        var canvas = FindCanvasForElement(element);
        _selection.SetUISelection(canvas, element);
        RefreshInspector();
        SyncSelectedUIElementInOpenTabs();
    }

    private void ProjectExplorer_OnSelectionChanged(object? sender, ProjectExplorerItem? item)
    {
        _selection.SelectedExplorerItem = item;
        if (item == null || item.IsFolder)
        {
            RefreshInspector();
            return;
        }
        if (InspectorPanel == null) return;
        var quick = GetOrCreateQuickPropertiesPanel();
        quick.SetItem(item, _project, _tileMap, _objectLayer, _scriptRegistry);
        InspectorPanel.Content = quick;
    }

    private void ProjectExplorer_OnRequestOpenInEditor(object? sender, ProjectExplorerItem? item)
    {
        if (item == null || item.IsFolder || string.IsNullOrEmpty(item.FullPath)) return;
        var ext = System.IO.Path.GetExtension(item.FullPath);
        var isScript = string.Equals(ext, ".lua", StringComparison.OrdinalIgnoreCase) || string.Equals(ext, ".cs", StringComparison.OrdinalIgnoreCase);
        if (isScript)
        {
            AddOrSelectTab("Scripts");
            var scriptsContent = GetTabByKind("Scripts")?.Content as ScriptsTabContent;
            scriptsContent?.OpenFile(item.FullPath);
            return;
        }
        // Creative Suite: image with Tile/Paint metadata opens in TileEditor or PaintEditor; any other image in Collisions Editor
        if (CreativeSuiteMetadata.IsImagePath(item.FullPath))
        {
            if (CreativeSuiteMetadata.IsTile(item.FullPath))
            {
                AddOrSelectTab("TileEditor");
                (GetTabByKind("TileEditor")?.Content as TileEditorTabContent)?.LoadAsset(item.FullPath);
                return;
            }
            if (CreativeSuiteMetadata.IsPaint(item.FullPath))
            {
                AddOrSelectTab("PaintEditor");
                (GetTabByKind("PaintEditor")?.Content as PaintEditorTabContent)?.LoadAsset(item.FullPath);
                return;
            }
            AddOrSelectTab("CollisionsEditor");
            (GetTabByKind("CollisionsEditor")?.Content as CollisionsEditorTabContent)?.LoadAsset(item.FullPath);
            return;
        }
        // Default: open in text editor window
        var editor = new ScriptEditorWindow { Owner = this };
        editor.OpenFile(item.FullPath);
        editor.Show();
    }

    private void ProjectExplorer_OnRequestOpenInCollisionsEditor(object? sender, ProjectExplorerItem? item)
    {
        if (item == null || item.IsFolder || string.IsNullOrEmpty(item.FullPath)) return;
        AddOrSelectTab("CollisionsEditor");
        (GetTabByKind("CollisionsEditor")?.Content as CollisionsEditorTabContent)?.LoadAsset(item.FullPath);
    }

    private void ProjectExplorer_OnRequestOpenInScriptableTile(object? sender, ProjectExplorerItem? item)
    {
        if (item == null || item.IsFolder || string.IsNullOrEmpty(item.FullPath)) return;
        AddOrSelectTab("ScriptableTile");
        (GetTabByKind("ScriptableTile")?.Content as ScriptableTileTabContent)?.LoadScript(item.FullPath);
    }

    private void Quick_RequestOpenInEditor(object? sender, ProjectExplorerItem item)
    {
        ProjectExplorer_OnRequestOpenInEditor(sender, item);
    }

    private void Quick_RequestDuplicate(object? sender, ProjectExplorerItem item)
    {
        ProjectExplorer.DuplicateItem(item);
        RefreshInspector();
    }

    private void Quick_RequestRename(object? sender, ProjectExplorerItem item)
    {
        ProjectExplorer.RenameItem(item);
        RefreshInspector();
    }

    private void Quick_RequestShowInFolder(object? sender, ProjectExplorerItem item)
    {
        ProjectExplorer.ShowInFolder(item);
    }

    private void Quick_RequestDelete(object? sender, ProjectExplorerItem item)
    {
        ProjectExplorer.DeleteItem(item);
        _selection.SelectedExplorerItem = null;
        RefreshInspector();
    }

    private void ConvertObjectToSeed(ObjectInstance inst)
    {
        if (inst == null) return;
        var defaultName = string.IsNullOrWhiteSpace(inst.Nombre) ? "Seed" : inst.Nombre.Trim() + " Seed";
        var name = MapHierarchyPanel.ShowRenameDialogPublic("Convertir a seed", defaultName);
        if (string.IsNullOrWhiteSpace(name)) return;
        var seed = new SeedDefinition
        {
            Id = Guid.NewGuid().ToString("N"),
            Nombre = name.Trim(),
            Descripcion = $"Creado desde objeto '{inst.Nombre}'.",
            Objects = new List<SeedObjectEntry>
            {
                new SeedObjectEntry
                {
                    DefinitionId = inst.DefinitionId,
                    OffsetX = 0,
                    OffsetY = 0,
                    Rotation = inst.Rotation,
                    Nombre = inst.Nombre
                }
            },
            Tags = inst.Tags != null ? new List<string>(inst.Tags) : new List<string>()
        };
        _seedDefinitions.Add(seed);
        try
        {
            SeedSerialization.Save(_seedDefinitions, _project.SeedsPath);
            EditorLog.Info($"Seed '{seed.Nombre}' guardado en seeds.json.", "Seed");
            EditorLog.Toast($"Seed \"{seed.Nombre}\" creado correctamente. Instanciable desde el explorador (seeds.json).", LogLevel.Info, "Seed");
        }
        catch (Exception ex)
        {
            _seedDefinitions.Remove(seed);
            EditorLog.Error($"Error al guardar seed: {ex.Message}", "Seed");
            System.Windows.MessageBox.Show(this, "Error al guardar seeds.json: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadProjectData()
    {
        EditorLog.Clear();
        var projectDir = _project.ProjectDirectory ?? "";
        NewProjectStructure.EnsureProjectFolders(projectDir);
        var config = ProyectoConfigSerialization.Load(projectDir);
        if (config != null)
        {
            if (!string.IsNullOrWhiteSpace(config.Nombre)) _project.Nombre = config.Nombre;
            if (!string.IsNullOrWhiteSpace(config.Descripcion)) _project.Descripcion = config.Descripcion;
            if (config.Autor != null) _project.Author = config.Autor;
            if (config.Version != null) _project.Version = config.Version;
            _project.AutoSaveEnabled = config.AutoguardadoActivo;
            _project.AutoSaveIntervalMinutes = config.IntervaloAutoguardadoMin > 0 ? config.IntervaloAutoguardadoMin : 5;
            _project.AutoSaveMaxBackupsPerType = config.MaxBackupsAutoguardado > 0 ? config.MaxBackupsAutoguardado : 10;
            if (!string.IsNullOrWhiteSpace(config.Logo))
                _project.IconPath = System.IO.Path.Combine(projectDir, config.Logo.TrimStart(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar));
            var currentPath = System.IO.Path.GetFullPath(projectDir);
            if (!string.IsNullOrEmpty(config.UltimaRuta) && !string.Equals(config.UltimaRuta, currentPath, StringComparison.OrdinalIgnoreCase))
            {
                config.UltimaRuta = currentPath;
                config.UltimaModificacion = DateTime.UtcNow.ToString("O");
                ProyectoConfigSerialization.Save(projectDir, config);
                EditorLog.Info("Ruta del proyecto actualizada en proyecto.config (proyecto movido o abierto desde otra ruta).", "Editor");
            }
        }
        EditorLog.Info($"Proyecto cargado: {_project.Nombre}", "Editor");

        if (_project.Scenes != null && _project.Scenes.Count > 0)
        {
            for (var i = 0; i < _project.Scenes.Count; i++)
            {
                var def = _project.Scenes[i];
                var mapPath = NewProjectStructure.ResolveMapPath(_project.ProjectDirectory ?? "", def.MapPathRelative);
                var objectsPath = NewProjectStructure.ResolveObjectsPath(_project.ProjectDirectory ?? "", def.ObjectsPathRelative);
                var tileMap = new TileMap(_project.ChunkSize);
                var objectLayer = new ObjectLayer();
                try
                {
                    if (File.Exists(mapPath))
                    {
                        tileMap = MapSerialization.Load(mapPath);
                        EditorLog.Info($"Mapa cargado: {def.Name}", System.IO.Path.GetFileName(mapPath));
                    }
                    else
                        EditorLog.Warning($"No se encontró {def.MapPathRelative}; se usa mapa vacío para {def.Name}.", "mapa");
                }
                catch (Exception ex) { EditorLog.Error($"Error al cargar mapa {def.Name}: {ex.Message}", "mapa"); }
                try
                {
                    if (File.Exists(objectsPath))
                    {
                        objectLayer = ObjectsSerialization.Load(objectsPath);
                        EditorLog.Info($"Objetos cargados: {def.Name}", System.IO.Path.GetFileName(objectsPath));
                    }
                    else
                        EditorLog.Warning($"No se encontró {def.ObjectsPathRelative}; se usa capa vacía para {def.Name}.", "objetos");
                }
                catch (Exception ex) { EditorLog.Error($"Error al cargar objetos {def.Name}: {ex.Message}", "objetos"); }

                var sceneState = new OpenSceneState
                {
                    Definition = def,
                    SceneIndex = i,
                    TileMap = tileMap,
                    ObjectLayer = objectLayer,
                    OpenOptionalTabKinds = def.DefaultTabKinds != null ? new List<string>(def.DefaultTabKinds) : new List<string>(),
                    SelectedTabKind = "Juego"
                };
                LoadUIRootForState(sceneState);
                _openScenes.Add(sceneState);
            }
            _currentSceneIndex = 0;
            if (_openScenes.Count > 0)
            {
                _tileMap = _openScenes[0].TileMap;
                _objectLayer = _openScenes[0].ObjectLayer;
                RefreshLayersPanelFromTileMap();
            }
        }
        else
        {
            try
            {
                if (File.Exists(_project.MapPath))
                {
                    _tileMap = MapSerialization.Load(_project.MapPath);
                    RefreshLayersPanelFromTileMap();
                    EditorLog.Info("Mapa cargado correctamente", System.IO.Path.GetFileName(_project.MapPath));
                }
                else
                    EditorLog.Warning($"No se encontró {_project.MapPathRelative}; se usa mapa vacío.", "mapa");
            }
            catch (Exception ex) { EditorLog.Error($"Error al cargar mapa: {ex.Message}", "mapa.json"); }
            try
            {
                if (File.Exists(_project.ObjectsPath))
                {
                    _objectLayer = ObjectsSerialization.Load(_project.ObjectsPath);
                    EditorLog.Info("Objetos cargados correctamente", "objetos.json");
                }
                else
                    EditorLog.Warning("No se encontró objetos.json; se usa capa vacía.", "objetos.json");
            }
            catch (Exception ex) { EditorLog.Error($"Error al cargar objetos: {ex.Message}", "objetos.json"); }
            var legacyState = new OpenSceneState
            {
                Definition = null,
                SceneIndex = 0,
                TileMap = _tileMap,
                ObjectLayer = _objectLayer,
                OpenOptionalTabKinds = new List<string>(),
                SelectedTabKind = "Juego"
            };
            LoadUIRootForState(legacyState);
            _openScenes.Add(legacyState);
            _currentSceneIndex = 0;
        }

        RefreshSceneUsedPaths();
        try
        {
            _scriptRegistry = ScriptSerialization.Load(_project.ScriptsPath);
            var n = _scriptRegistry?.GetAll()?.Count ?? 0;
            EditorLog.Info($"Scripts cargados: {n}", "scripts.json");
        }
        catch (Exception ex)
        {
            EditorLog.Error($"Error al cargar scripts: {ex.Message}", "scripts.json");
        }
        try
        {
            if (File.Exists(_project.TriggerZonesPath))
            {
                _triggerZones = TriggerZoneSerialization.Load(_project.TriggerZonesPath);
                EditorLog.Info($"Triggers cargados: {_triggerZones.Count}", "triggerZones.json");
            }
        }
        catch (Exception ex)
        {
            EditorLog.Error($"Error al cargar triggers: {ex.Message}", "triggerZones.json");
        }
        try
        {
            var seedsPath = _project.SeedsPath;
            var legacyPath = System.IO.Path.Combine(_project.ProjectDirectory ?? "", "prefabs.json");
            if (File.Exists(seedsPath))
            {
                _seedDefinitions = SeedSerialization.Load(seedsPath);
                EditorLog.Info($"Seeds cargados: {_seedDefinitions.Count}", "seeds.json");
            }
            else if (File.Exists(legacyPath))
            {
                _seedDefinitions = SeedSerialization.Load(legacyPath);
                EditorLog.Info($"Seeds migrados desde prefabs.json: {_seedDefinitions.Count}", "seeds.json");
                // Backup del archivo antiguo antes de escribir seeds.json para no perder datos
                try
                {
                    var backupPath = legacyPath + ".backup";
                    File.Copy(legacyPath, backupPath, overwrite: true);
                    EditorLog.Info($"Copia de seguridad guardada: prefabs.json.backup", "seeds.json");
                }
                catch (Exception exBackup)
                {
                    EditorLog.Warning($"No se pudo crear backup de prefabs.json: {exBackup.Message}", "seeds.json");
                }
                SeedSerialization.Save(_seedDefinitions, seedsPath);
            }
        }
        catch (Exception ex)
        {
            EditorLog.Error($"Error al cargar seeds: {ex.Message}", "seeds.json");
        }
        NotifyMissingScripts();
    }
    private void NotifyMissingScripts()
    {
        var scriptIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in _scriptRegistry?.GetAll() ?? Array.Empty<ScriptDefinition>())
            scriptIds.Add(s.Id);
        foreach (var inst in _objectLayer.Instances)
        {
            var def = _objectLayer.GetDefinition(inst.DefinitionId);
            if (def == null) continue;
            var sid = inst.GetScriptId(def);
            if (!string.IsNullOrEmpty(sid) && !scriptIds.Contains(sid))
                EditorLog.Warning($"Objeto '{inst.Nombre}' referencia script inexistente: {sid}", "objetos.json");
        }
    }

    private void RefreshSceneUsedPaths()
    {
        var paths = SceneAssetReferenceCollector.Collect(
            _project.ProjectDirectory ?? "",
            GetCurrentSceneMapPath(),
            GetCurrentSceneObjectsPath(),
            _objectLayer);
        ProjectExplorer?.SetSceneUsedPaths(paths);
    }

    /// <summary>Guarda mapa y objetos de todas las escenas abiertas.</summary>
    private void SaveAllOpenScenes()
    {
        foreach (var state in _openScenes)
        {
            string mapPath, objectsPath;
            if (state.Definition != null)
            {
                mapPath = state.Definition.GetMapPath(_project.ProjectDirectory ?? "");
                objectsPath = state.Definition.GetObjectsPath(_project.ProjectDirectory ?? "");
            }
            else
            {
                mapPath = _project.MapPath;
                objectsPath = _project.ObjectsPath;
            }
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(mapPath)!);
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(objectsPath)!);
                MapSerialization.Save(state.TileMap, mapPath);
                ObjectsSerialization.Save(state.ObjectLayer, objectsPath);
                CanvasControllerLuaTemplate.EnsureCanvasControllerScriptIfNeeded(_project.ProjectDirectory, state.ObjectLayer);
                SaveUIRootForState(state);
                ProjectExplorer?.SetModified(mapPath, false);
                ProjectExplorer?.SetModified(objectsPath, false);
            }
            catch (Exception ex) { EditorLog.Error($"Error al guardar escena {state.Definition?.Name ?? "Principal"}: {ex.Message}", "Guardar"); }
        }
        UpdateMapTabDirtyState();
    }

    private string GetCurrentSceneMapPath()
    {
        if (_openScenes.Count > 0 && _openScenes[_currentSceneIndex].Definition != null)
            return _openScenes[_currentSceneIndex].Definition!.GetMapPath(_project.ProjectDirectory ?? "");
        return _project.MapPath;
    }

    private string GetCurrentSceneObjectsPath()
    {
        if (_openScenes.Count > 0 && _openScenes[_currentSceneIndex].Definition != null)
            return _openScenes[_currentSceneIndex].Definition!.GetObjectsPath(_project.ProjectDirectory ?? "");
        return _project.ObjectsPath;
    }

    private string GetCurrentUIFolder()
    {
        if (_openScenes.Count > 0 && _openScenes[_currentSceneIndex].Definition != null)
            return _openScenes[_currentSceneIndex].Definition!.GetUIFolder(_project.ProjectDirectory ?? "");
        return System.IO.Path.Combine(_project.ProjectDirectory ?? "", "UI");
    }

    private UIRoot GetCurrentUIRoot()
    {
        if (_openScenes.Count == 0) return new UIRoot();
        return _openScenes[_currentSceneIndex].UIRoot;
    }

    private UICanvas? FindCanvasForElement(FUEngine.Core.UIElement? element)
    {
        if (element == null) return null;
        var root = GetCurrentUIRoot();
        foreach (var canvas in root.Canvases)
        {
            if (CanvasContainsElement(canvas, element))
                return canvas;
        }
        return null;
    }

    private static bool CanvasContainsElement(UICanvas canvas, FUEngine.Core.UIElement target)
    {
        foreach (var root in canvas.Children)
        {
            if (ContainsElementRecursive(root, target))
                return true;
        }
        return false;
    }

    private static bool ContainsElementRecursive(FUEngine.Core.UIElement current, FUEngine.Core.UIElement target)
    {
        if (ReferenceEquals(current, target)) return true;
        foreach (var child in current.Children)
        {
            if (ContainsElementRecursive(child, target))
                return true;
        }
        return false;
    }

    private void RefreshOpenUICanvasTabs()
    {
        if (MainTabs?.Items == null) return;
        foreach (TabItem tab in MainTabs.Items)
        {
            if (tab.Tag is not string kind || !kind.StartsWith("UI:", StringComparison.OrdinalIgnoreCase)) continue;
            if (tab.Content is not UITabContent uiTab) continue;
            uiTab.RefreshFromRoot();
            if (_selection.SelectedUIElement != null)
                uiTab.SetSelectedElement(_selection.SelectedUIElement);
        }
    }

    private void SyncSelectedUIElementInOpenTabs()
    {
        if (MainTabs?.Items == null) return;
        foreach (TabItem tab in MainTabs.Items)
        {
            if (tab.Tag is not string kind || !kind.StartsWith("UI:", StringComparison.OrdinalIgnoreCase)) continue;
            if (tab.Content is UITabContent uiTab)
                uiTab.SetSelectedElement(_selection.SelectedUIElement);
        }
    }

    private void LoadUIRootForState(OpenSceneState state)
    {
        string uiFolder;
        if (state.Definition != null)
            uiFolder = state.Definition.GetUIFolder(_project.ProjectDirectory ?? "");
        else
            uiFolder = System.IO.Path.Combine(_project.ProjectDirectory ?? "", "UI");
        state.UIRoot = new UIRoot();
        if (!Directory.Exists(uiFolder)) return;
        foreach (var file in Directory.EnumerateFiles(uiFolder, "*.json"))
        {
            try
            {
                var canvas = UICanvasSerialization.Load(file);
                if (!string.IsNullOrEmpty(canvas.Id))
                    state.UIRoot.AddCanvas(canvas);
            }
            catch (Exception ex) { EditorLog.Error($"Error al cargar UI {System.IO.Path.GetFileName(file)}: {ex.Message}", "UI"); }
        }
    }

    private void SaveUIRootForState(OpenSceneState state)
    {
        string uiFolder;
        if (state.Definition != null)
            uiFolder = state.Definition.GetUIFolder(_project.ProjectDirectory ?? "");
        else
            uiFolder = System.IO.Path.Combine(_project.ProjectDirectory ?? "", "UI");
        foreach (var canvas in state.UIRoot.Canvases)
        {
            if (string.IsNullOrEmpty(canvas.Id)) continue;
            var path = System.IO.Path.Combine(uiFolder, canvas.Id + ".json");
            try
            {
                UICanvasSerialization.Save(canvas, path);
                ProjectExplorer?.SetModified(path, false);
            }
            catch (Exception ex) { EditorLog.Error($"Error al guardar UI {canvas.Id}: {ex.Message}", "UI"); }
        }
    }

    private bool HasUnsavedChanges()
    {
        if (ProjectExplorer == null) return false;
        return ProjectExplorer.IsPathModified(GetCurrentSceneMapPath()) || ProjectExplorer.IsPathModified(GetCurrentSceneObjectsPath());
    }

    private void ConfigureAutoSave()
    {
        _autoSaveService ??= new AutoSaveService();
        var projectDir = _project.ProjectDirectory ?? "";
        var es = EngineSettings.Load();
        bool autoSaveEnabled;
        int interval;
        if (es.UseEngineAutoSaveSettings)
        {
            autoSaveEnabled = es.EngineAutoSaveEnabled;
            interval = es.EngineAutoSaveIntervalMinutes > 0 ? es.EngineAutoSaveIntervalMinutes : 5;
        }
        else
        {
            autoSaveEnabled = _project.AutoSaveEnabled;
            interval = _project.AutoSaveIntervalMinutes > 0 ? _project.AutoSaveIntervalMinutes : 5;
        }
        _autoSaveService.Configure(
            projectDir,
            autoSaveEnabled,
            interval,
            _project.AutoSaveMaxBackupsPerType > 0 ? _project.AutoSaveMaxBackupsPerType : 10,
            _project.AutoSaveFolder ?? "Autoguardados",
            HasUnsavedChanges,
            (mapPath, objectsPath) =>
            {
                MapSerialization.Save(_tileMap, mapPath);
                ObjectsSerialization.Save(_objectLayer, objectsPath);
            },
            RefreshSceneUsedPaths);
    }

    private void EnsureDefaultObjectDefinition()
    {
        if (_objectLayer.Definitions.Count > 0) return;
        _objectLayer.RegisterDefinition(new ObjectDefinition
        {
            Id = "obj_default",
            Nombre = "Objeto",
            Colision = true,
            Interactivo = false,
            Destructible = false,
            Width = 1,
            Height = 1
        });
    }

    private ObjectInstance? GetObjectAt(double canvasX, double canvasY)
    {
        var tileSize = _project.TileSize;
        foreach (var inst in _objectLayer.Instances.AsEnumerable().Reverse())
        {
            var def = _objectLayer.GetDefinition(inst.DefinitionId);
            if (def == null) continue;
            double left = ToCanvasX((int)Math.Floor(inst.X));
            double top = ToCanvasY((int)Math.Floor(inst.Y));
            double w = def.Width * tileSize;
            double h = def.Height * tileSize;
            if (canvasX >= left && canvasX < left + w && canvasY >= top && canvasY < top + h)
                return inst;
        }
        return null;
    }

    private void DrawMap()
    {
        if (MapCanvas == null) return;
        var tileSize = _project.TileSize;
        var selectedIds = new HashSet<string>(_selection.SelectedObjects.Select(o => o.InstanceId));
        var ctx = new MapRenderContext
        {
            TileMap = _tileMap,
            ObjectLayer = _objectLayer,
            TriggerZones = _triggerZones,
            Project = _project,
            VisibleLayers = _visibleLayers,
            ActiveLayerIndex = GetActiveLayerIndex(),
            GridVisible = _gridVisible,
            GridColor = _gridColor,
            ShowTileCoordinates = _showTileCoordinates,
            MaskColision = _maskColision,
            MaskScripts = _maskScripts,
            HighlightInteractives = ChkResaltarInteractivos?.IsChecked == true,
            SelectedObjectIds = selectedIds,
            SelectedTriggerId = _selection.SelectedTrigger?.Id,
            TileSelectionDragging = _selection.IsTileSelectionDragging,
            TileSelectionStart = _selection.TileSelectionDragStart,
            TileSelectionEnd = _selection.TileSelectionDragEnd,
            SelectedTileMinTx = _selection.TileMinTx,
            SelectedTileMinTy = _selection.TileMinTy,
            SelectedTileMaxTx = _selection.TileMaxTx,
            SelectedTileMaxTy = _selection.TileMaxTy,
            RectDragging = _rectDragging,
            RectStartTx = _rectStartTx,
            RectStartTy = _rectStartTy,
            RectEndTx = _rectEndTx,
            RectEndTy = _rectEndTy,
            ZoneMinTx = _zoneMinTx,
            ZoneMinTy = _zoneMinTy,
            ZoneMaxTx = _zoneMaxTx,
            ZoneMaxTy = _zoneMaxTy,
            MeasureStart = _measureStart,
            MeasureEnd = _measureEnd,
            ShowVisibleArea = _showVisibleArea,
            ShowStreamingGizmos = _showStreamingGizmos,
            ShowColliders = _showColliders,
            TotalSeconds = Environment.TickCount64 / 1000.0
        };
        _mapRenderer.Draw(MapCanvas, ctx);
        _canvasMinWx = ctx.CanvasMinWx;
        _canvasMinWy = ctx.CanvasMinWy;
        if (_measureStart.HasValue && _measureEnd.HasValue)
        {
            var m1 = _measureStart.Value;
            var m2 = _measureEnd.Value;
            var distTiles = Math.Sqrt((m2.x - m1.x) * (m2.x - m1.x) + (m2.y - m1.y) * (m2.y - m1.y));
            var distPx = distTiles * tileSize;
            if (TxtStatusBar != null) TxtStatusBar.Text = $"Medir: {distTiles:F1} tiles, {distPx:F0} px";
        }
        UpdateWelcomeOverlay();
    }

    private bool _welcomeOverlayDismissed;

    private void UpdateWelcomeOverlay()
    {
        if (CanvasWelcomeOverlay == null) return;
        if (_welcomeOverlayDismissed) { CanvasWelcomeOverlay.Visibility = Visibility.Collapsed; return; }
        if (EngineSettings.Load().WelcomeOverlayDismissedOnce) { _welcomeOverlayDismissed = true; CanvasWelcomeOverlay.Visibility = Visibility.Collapsed; return; }
        bool hasContent = false;
        foreach (var _ in _tileMap.EnumerateChunkCoords())
        {
            hasContent = true;
            break;
        }
        CanvasWelcomeOverlay.Visibility = hasContent ? Visibility.Collapsed : Visibility.Visible;
    }

    private void CanvasWelcomeOverlay_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _welcomeOverlayDismissed = true;
        var settings = EngineSettings.Load();
        settings.WelcomeOverlayDismissedOnce = true;
        EngineSettings.Save(settings);
        if (CanvasWelcomeOverlay != null) CanvasWelcomeOverlay.Visibility = Visibility.Collapsed;
        MapCanvas?.Focus();
    }

    private void UpdateStatusBar(string text)
    {
        if (TxtStatusBar != null) TxtStatusBar.Text = text;
    }

    private void EditorLog_EntryAdded(object? sender, LogEntry e)
    {
        if (TxtLastLog == null || LogBar == null) return;
        var prefix = e.Level switch { LogLevel.Error => "[Error] ", LogLevel.Warning => "[Aviso] ", _ => "" };
        TxtLastLog.Text = prefix + e.Message;
        if (e.Time != default)
            TxtLastLog.ToolTip = $"{e.Time:HH:mm:ss} [{e.Source ?? "Log"}] {e.Message}";
        LogBar.Visibility = Visibility.Visible;
        UpdateLogCounts();
    }

    private void UpdateLogCounts()
    {
        if (TxtLogCounts == null) return;
        var warnings = EditorLog.Entries.Count(entry => entry.Level == LogLevel.Warning);
        var errors = EditorLog.Entries.Count(entry => entry.Level == LogLevel.Error);
        TxtLogCounts.Text = (warnings > 0 || errors > 0) ? $"⚠ {warnings}  ❌ {errors}" : "";
    }

    private void LogBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        OpenConsoleTab();
    }

    private void EditorLog_ToastRequested(object? sender, (string Message, LogLevel Level) e)
    {
        if (ToastPanel == null || ToastText == null) return;
        if (_toastQueue.Count >= MaxToastQueue) _toastQueue.Dequeue();
        _toastQueue.Enqueue((e.Message, e.Level));
        if (_toastTimer == null || !_toastTimer.IsEnabled)
            ShowNextToast();
    }

    private void ShowNextToast()
    {
        if (ToastPanel == null || ToastText == null || _toastQueue.Count == 0)
        {
            if (ToastPanel != null) ToastPanel.Visibility = Visibility.Collapsed;
            return;
        }
        var (message, level) = _toastQueue.Dequeue();
        ToastText.Text = message;
        ToastText.Foreground = level switch
        {
            LogLevel.Error => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xf8, 0x51, 0x49)),
            LogLevel.Warning => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xd2, 0x99, 0x22)),
            _ => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3))
        };
        ToastPanel.Visibility = Visibility.Visible;
        _toastTimer?.Stop();
        if (level == LogLevel.Error)
        {
            ToastPanel.ToolTip = "Error: clic para cerrar.";
            return;
        }
        var seconds = level == LogLevel.Warning ? 5 : 3;
        _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };
        _toastTimer.Tick += (_, _) =>
        {
            _toastTimer?.Stop();
            ShowNextToast();
        };
        _toastTimer.Start();
    }

    private void ToastPanel_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _toastTimer?.Stop();
        ShowNextToast();
    }

    private void BtnOpenConsola_OnClick(object sender, RoutedEventArgs e)
    {
        OpenConsoleTab();
    }

    private static System.Windows.Media.Color GetBaseColorForTileType(TileType tipo)
    {
        return tipo switch
        {
            TileType.Suelo => System.Windows.Media.Color.FromRgb(80, 80, 80),
            TileType.Pared => System.Windows.Media.Color.FromRgb(120, 80, 60),
            TileType.Objeto => System.Windows.Media.Color.FromRgb(90, 90, 120),
            TileType.Especial => System.Windows.Media.Color.FromRgb(100, 60, 100),
            _ => System.Windows.Media.Color.FromRgb(80, 80, 80)
        };
    }

    private double ToCanvasX(int wx) => (wx - _canvasMinWx) * _project.TileSize;
    private double ToCanvasY(int wy) => (wy - _canvasMinWy) * _project.TileSize;

    private (int x, int y) GetTileAt(System.Windows.Point pos)
    {
        var tileSizePx = _project.TileSize;
        int tx = (int)Math.Floor(pos.X / tileSizePx) + _canvasMinWx;
        int ty = (int)Math.Floor(pos.Y / tileSizePx) + _canvasMinWy;
        return (tx, ty);
    }

    private static TileData CreateTileData(TileType tipo)
    {
        return new TileData
        {
            TipoTile = tipo,
            Colision = tipo == TileType.Pared,
            Interactivo = tipo == TileType.Especial
        };
    }

    private void MapCanvas_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(MapCanvas);
        var ctrl = (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) != 0;
        var shift = (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) != 0;
        _toolController?.HandleMouseDown(pos, ctrl, shift);
    }

    /// <summary>Handles left-button down for all tools that are not yet implemented as ITool (delegated from ToolController when CurrentTool is null).</summary>
    private void HandleToolMouseDown(System.Windows.Point pos, bool ctrl, bool shift)
    {
        if (_toolMode == ToolMode.PixelEdit) { HandleTilePixelEditClick(pos); return; }
        if (_toolMode == ToolMode.Medir) { HandleMeasureClick(pos); return; }
        if (_toolMode == ToolMode.Zona) { HandleZoneToolClick(pos); return; }
        if (_toolMode == ToolMode.Seleccionar) { HandleSelectToolClick(pos, ctrl); return; }
        if (_toolMode == ToolMode.Colocar) { HandlePlaceObjectClick(pos); return; }

        var (tx2, ty2) = GetTileAt(pos);
        if (_toolMode == ToolMode.Rectangulo) { HandleRectToolClick(tx2, ty2); return; }
        if (_toolMode == ToolMode.Linea) { HandleLineToolClick(tx2, ty2); return; }
        if (_toolMode == ToolMode.Relleno) { BucketFill(tx2, ty2); return; }
        if (_toolMode == ToolMode.Goma) { HandleEraserClick(tx2, ty2); return; }
        if (_toolMode == ToolMode.Picker) { HandlePickerClick(tx2, ty2); return; }
        if (_toolMode == ToolMode.Stamp) { HandleStampClick(tx2, ty2); return; }
    }

    /// <summary>Legacy move handling: object drag, rect/zone/selection drag. Called by ToolController when CurrentTool is null.</summary>
    private void HandleToolMouseMove(System.Windows.Point pos)
    {
        var tileSize = _project.TileSize;
        if (_isDragging && _selection.SelectedObject != null)
        {
            var dx = (pos.X - _dragStartPos.X) / tileSize;
            var dy = (pos.Y - _dragStartPos.Y) / tileSize;
            var nx = _dragStartX + dx;
            var ny = _dragStartY + dy;
            if (_snapToGrid) { nx = Math.Round(nx); ny = Math.Round(ny); }
            _selection.SelectedObject.X = nx;
            _selection.SelectedObject.Y = ny;
            DrawMap();
            return;
        }
        if (_rectDragging && _toolMode == ToolMode.Rectangulo)
        {
            var (tx, ty) = GetTileAt(pos);
            _rectEndTx = tx;
            _rectEndTy = ty;
            DrawMap();
            return;
        }
        if (_zoneDragging && _toolMode == ToolMode.Zona)
        {
            var (tx, ty) = GetTileAt(pos);
            _zoneMinTx = Math.Min(_zoneStartTx, tx);
            _zoneMaxTx = Math.Max(_zoneStartTx, tx);
            _zoneMinTy = Math.Min(_zoneStartTy, ty);
            _zoneMaxTy = Math.Max(_zoneStartTy, ty);
            DrawMap();
            return;
        }
        if (_selection.IsTileSelectionDragging)
        {
            var (tx, ty) = GetTileAt(pos);
            _selection.UpdateTileSelectionDragEnd(tx, ty);
            DrawMap();
        }
    }

    /// <summary>Legacy mouse-up handling: commit rect/zone/selection drag, clear object drag. Called by ToolController when CurrentTool is null.</summary>
    private void HandleToolMouseUp(System.Windows.Point pos)
    {
        if (_rectDragging && _toolMode == ToolMode.Rectangulo)
        {
            if (IsActiveLayerLocked()) { _rectDragging = false; return; }
            var (endTx, endTy) = GetTileAt(pos);
            int minTx = Math.Min(_rectStartTx, endTx), maxTx = Math.Max(_rectStartTx, endTx);
            int minTy = Math.Min(_rectStartTy, endTy), maxTy = Math.Max(_rectStartTy, endTy);
            var layerIdx = GetActiveLayerIndex();
            var batch = new PaintTileBatchCommand(_tileMap, layerIdx);
            var newTile = CreateTileData(_selectedTileType);
            for (int tx = minTx; tx <= maxTx; tx++)
                for (int ty = minTy; ty <= maxTy; ty++)
                {
                    _tileMap.TryGetTile(layerIdx, tx, ty, out var prev);
                    batch.Add(tx, ty, prev, newTile.Clone());
                }
            if (batch.Count > 0) _history.Push(batch);
            ProjectExplorer?.SetModified(GetCurrentSceneMapPath(), true);
            _rectDragging = false;
            DrawMap();
        }
        if (_zoneDragging && _toolMode == ToolMode.Zona)
        {
            var (endTx, endTy) = GetTileAt(pos);
            _zoneMinTx = Math.Min(_zoneStartTx, endTx);
            _zoneMaxTx = Math.Max(_zoneStartTx, endTx);
            _zoneMinTy = Math.Min(_zoneStartTy, endTy);
            _zoneMaxTy = Math.Max(_zoneStartTy, endTy);
            _zoneDragging = false;
            UpdateZoneMenuState();
            DrawMap();
        }
        if (_selection.IsTileSelectionDragging)
        {
            var (endTx, endTy) = GetTileAt(pos);
            _selection.CommitTileSelectionDrag(endTx, endTy);
            DrawMap();
            UpdateTileSelectionToolbarVisibility();
        }
        _isDragging = false;
    }

    private void HandleTilePixelEditClick(System.Windows.Point pos)
    {
        var (tx, ty) = GetTileAt(pos);
        var layerIdx = GetActiveLayerIndex();
        if (!_tileMap.TryGetTile(layerIdx, tx, ty, out var existing) || existing == null)
        {
            EditorLog.Toast("No hay tile en esta celda. Coloca un tile (Pincel o Rect) y luego usa Pixel.", LogLevel.Info, "Mapa");
            return;
        }
        var baseColor = GetBaseColorForTileType(existing.TipoTile);
        var clone = existing.Clone();
        var win = new PixelEditWindow(tx, ty, clone, _project.TileSize, baseColor, saved =>
        {
            _history.Push(new PaintTileCommand(_tileMap, layerIdx, tx, ty, existing, saved));
            _tileMap.SetTile(layerIdx, tx, ty, saved);
            ProjectExplorer.SetModified(GetCurrentSceneMapPath(), true);
            DrawMap();
        }, _project.ProjectDirectory);
        win.Owner = this;
        win.ShowDialog();
    }

    private void HandleMeasureClick(System.Windows.Point pos)
    {
        var (tx, ty) = GetTileAt(pos);
        if (_measureStart == null)
        {
            _measureStart = (tx, ty);
            _measureEnd = null;
            UpdateStatusBar($"Medir: origen ({tx}, {ty}) — clic en destino");
        }
        else
            _measureEnd = (tx, ty);
        DrawMap();
    }

    private void HandleZoneToolClick(System.Windows.Point pos)
    {
        var (tx, ty) = GetTileAt(pos);
        _zoneDragging = true;
        _zoneStartTx = tx;
        _zoneStartTy = ty;
        _zoneMinTx = _zoneMaxTx = tx;
        _zoneMinTy = _zoneMaxTy = ty;
        DrawMap();
    }

    private void HandleSelectToolClick(System.Windows.Point pos, bool ctrl)
    {
        var obj = GetObjectAt(pos.X, pos.Y);
        if (obj != null)
        {
            _selection.SelectedExplorerItem = null;
            _selection.AddOrReplaceObjectSelection(obj, ctrl);
            if (!ctrl)
            {
                _isDragging = true;
                _dragStartPos = pos;
                _dragStartX = obj.X;
                _dragStartY = obj.Y;
            }
            RefreshInspector();
            DrawMap();
            return;
        }
        var (stx, sty) = GetTileAt(pos);
        if (ctrl && _selection.HasTileSelection)
        {
            _selection.ExpandTileSelection(stx, sty);
        }
        else if (!ctrl)
        {
            // No limpiar SelectedExplorerItem: mantener selección rápida en Inspector al hacer clic en vacío
            _selection.ClearObjectSelection();
            _selection.StartTileSelectionDrag(stx, sty);
        }
        RefreshInspector();
        DrawMap();
        UpdateTileSelectionToolbarVisibility();
    }

    private void HandlePlaceObjectClick(System.Windows.Point pos)
    {
        var defId = (CmbObjectDef.SelectedValue as string) ?? _objectLayer.Definitions.Values.FirstOrDefault()?.Id;
        if (string.IsNullOrEmpty(defId)) return;
        var (tx, ty) = GetTileAt(pos);
        var inst = new ObjectInstance
        {
            DefinitionId = defId,
            X = tx,
            Y = ty,
            Nombre = _objectLayer.GetDefinition(defId)?.Nombre ?? "Objeto"
        };
        _history.Push(new AddObjectCommand(_objectLayer, inst));
        ProjectExplorer.SetModified(GetCurrentSceneObjectsPath(), true);
        MapHierarchy.SetMapStructure(System.IO.Path.GetFileNameWithoutExtension(GetCurrentSceneMapPath()), _project.LayerNames, _objectLayer, _triggerZones, _visibleLayers, GetCurrentUIRoot());
        DrawMap();
    }

    private void HandleRectToolClick(int tx, int ty)
    {
        _rectDragging = true;
        _rectStartTx = _rectEndTx = tx;
        _rectStartTy = _rectEndTy = ty;
        DrawMap();
    }

    private void HandleLineToolClick(int tx, int ty)
    {
        if (!_lineStart.HasValue)
        {
            _lineStart = (tx, ty);
            UpdateStatusBar($"Línea: origen ({tx}, {ty}) — clic en destino");
            DrawMap();
            return;
        }
        if (IsActiveLayerLocked()) { _lineStart = null; return; }
        var (x0, y0) = _lineStart.Value;
        var layerIdx = GetActiveLayerIndex();
        var batch = new PaintTileBatchCommand(_tileMap, layerIdx);
        var newTile = CreateTileData(_selectedTileType);
        foreach (var (px, py) in LineTiles(x0, y0, tx, ty))
        {
            _tileMap.TryGetTile(layerIdx, px, py, out var prev);
            batch.Add(px, py, prev, newTile.Clone());
        }
        _history.Push(batch);
        _lineStart = null;
        ProjectExplorer.SetModified(GetCurrentSceneMapPath(), true);
        DrawMap();
    }

    private void HandleEraserClick(int tx, int ty)
    {
        if (IsActiveLayerLocked()) return;
        var layerIdx = GetActiveLayerIndex();
        var batch = new PaintTileBatchCommand(_tileMap, layerIdx);
        var emptyTile = CreateTileData(TileType.Suelo);
        for (int dx = 0; dx < _brushSize; dx++)
            for (int dy = 0; dy < _brushSize; dy++)
            {
                int px = tx + dx, py = ty + dy;
                if (_tileMap.TryGetTile(layerIdx, px, py, out var old) && old != null)
                    batch.Add(px, py, old, emptyTile);
            }
        if (batch.Count > 0) _history.Push(batch);
        ProjectExplorer.SetModified(GetCurrentSceneMapPath(), true);
        DrawMap();
    }

    private void HandlePickerClick(int tx, int ty)
    {
        var layerIdx = GetActiveLayerIndex();
        if (_tileMap.TryGetTile(layerIdx, tx, ty, out var picked) && picked != null)
        {
            _selectedTileType = picked.TipoTile;
            if (CmbTileType != null) CmbTileType.SelectedIndex = (int)_selectedTileType;
            UpdateStatusBar($"Cuentagotas: {picked.TipoTile}");
        }
        DrawMap();
    }

    private void HandleStampClick(int tx, int ty)
    {
        if (_zoneClipboard != null && _zoneClipboard.HasContent)
        {
            _pasteOriginTx = tx;
            _pasteOriginTy = ty;
            PasteZone();
        }
        else
            UpdateStatusBar("Stamp: copia una zona (Zona → selecciona → Ctrl+C) antes de pegar.");
    }

    private static IEnumerable<(int x, int y)> LineTiles(int x0, int y0, int x1, int y1)
    {
        int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0);
        int steps = Math.Max(Math.Max(dx, dy), 1);
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            int x = (int)Math.Round(x0 + (x1 - x0) * t);
            int y = (int)Math.Round(y0 + (y1 - y0) * t);
            yield return (x, y);
        }
    }

    private void BucketFill(int startTx, int startTy)
    {
        if (IsActiveLayerLocked()) return;
        var layerIdx = GetActiveLayerIndex();
        var newTile = CreateTileData(_selectedTileType);
        var cells = TilePaintService.ComputeBucketFill(
            _tileMap,
            startTx,
            startTy,
            _selection.IsInsideTileSelection,
            maxFill: 2000,
            layerIndex: layerIdx);
        foreach (var (tx, ty) in cells)
        {
            _tileMap.TryGetTile(layerIdx, tx, ty, out var prev);
            _history.Push(new PaintTileCommand(_tileMap, layerIdx, tx, ty, prev, newTile.Clone()));
        }
        if (cells.Count > 0)
        {
            ProjectExplorer.SetModified(GetCurrentSceneMapPath(), true);
            UpdateStatusBar(cells.Count >= 2000 ? "Relleno: 2000+ tiles" : $"Relleno: {cells.Count} tiles");
        }
        DrawMap();
    }

    private void MapCanvas_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_toolMode == ToolMode.Seleccionar && _selection.SelectedObject != null)
        {
            var obj = _selection.SelectedObject;
            _selection.ClearObjectSelection();
            _isDragging = false;
            _history.Push(new RemoveObjectCommand(_objectLayer, obj));
            ProjectExplorer.SetModified(GetCurrentSceneObjectsPath(), true);
            RefreshInspector();
            DrawMap();
            return;
        }
        if (IsActiveLayerLocked()) return;
        var pos = e.GetPosition(MapCanvas);
        var (tx, ty) = GetTileAt(pos);
        var layerIdx = GetActiveLayerIndex();
        if (_tileMap.TryGetTile(layerIdx, tx, ty, out var oldTile) && oldTile != null)
        {
            _history.Push(new RemoveTileCommand(_tileMap, layerIdx, tx, ty, oldTile));
            ProjectExplorer.SetModified(GetCurrentSceneMapPath(), true);
        }
        DrawMap();
    }

    private void MapCanvas_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _toolController?.HandleMouseUp(e.GetPosition(MapCanvas));
    }

    private void MapCanvas_OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        var pos = e.GetPosition(MapCanvas);
        UpdateStatusBarFromPosition(pos);
        _toolController?.HandleMouseMove(pos);
    }

    private void ApplyEngineCollisionMaskDefault()
    {
        try
        {
            var es = EngineSettings.Load();
            _maskColision = es.CollisionMaskVisibleByDefault;
            if (ChkMaskColision != null) ChkMaskColision.IsChecked = _maskColision;
        }
        catch { /* ignore */ }
    }

    private void RefreshCoordinateUnitFromSettings()
    {
        try
        {
            var s = EngineSettings.Load();
            _coordinateUnitDisplay = s.CoordinateUnit ?? "Tiles";
            _zoomWheelSensitivity = s.MapZoomWheelSensitivity > 0 ? s.MapZoomWheelSensitivity : 1;
            _panKeyStepScale = s.MapPanKeyboardStepScale > 0 ? s.MapPanKeyboardStepScale : 1;
        }
        catch
        {
            _coordinateUnitDisplay = "Tiles";
            _zoomWheelSensitivity = 1;
            _panKeyStepScale = 1;
        }
    }

    private void UpdateStatusBarFromPosition(System.Windows.Point pos)
    {
        var (mx, my) = GetTileAt(pos);
        var tileSizePx = _project.TileSize;
        var unit = _coordinateUnitDisplay ?? "Tiles";
        string xyPart;
        if (string.Equals(unit, "Pixels", StringComparison.OrdinalIgnoreCase))
        {
            var px = (int)System.Math.Floor(_canvasMinWx * tileSizePx + pos.X);
            var py = (int)System.Math.Floor(_canvasMinWy * tileSizePx + pos.Y);
            xyPart = $"X: {px} px  Y: {py} px";
        }
        else if (string.Equals(unit, "SubTiles", StringComparison.OrdinalIgnoreCase))
        {
            var hs = tileSizePx / 2.0;
            var sx = (int)System.Math.Floor(pos.X / hs) + _canvasMinWx * 2;
            var sy = (int)System.Math.Floor(pos.Y / hs) + _canvasMinWy * 2;
            xyPart = $"X: {sx}  Y: {sy}  (subtiles)";
        }
        else
            xyPart = $"X: {mx}  Y: {my}";
        var toolName = _toolMode switch { ToolMode.Pintar => "Pincel", ToolMode.Rectangulo => "Rect", ToolMode.Linea => "Línea", ToolMode.Relleno => "Relleno", ToolMode.Goma => "Goma", ToolMode.Picker => "Cuentagotas", ToolMode.Stamp => "Stamp", ToolMode.Seleccionar => "Seleccionar", ToolMode.Colocar => "Colocar", ToolMode.Zona => "Zona", ToolMode.Medir => "Medir", ToolMode.PixelEdit => "Pixel", _ => "" };
        var tileName = _toolMode == ToolMode.Pintar ? new[] { "Suelo", "Pared", "Objeto", "Especial" }[(int)_selectedTileType] : "";
        int cx = _tileMap.ChunkSize > 0 ? (mx < 0 ? (mx + 1) / _tileMap.ChunkSize - 1 : mx / _tileMap.ChunkSize) : 0;
        int cy = _tileMap.ChunkSize > 0 ? (my < 0 ? (my + 1) / _tileMap.ChunkSize - 1 : my / _tileMap.ChunkSize) : 0;
        var layerName = CmbCapaVisible?.SelectedItem as string ?? "Suelo";
        var brushSize = CmbBrushSize?.SelectedIndex >= 0 && CmbBrushSize?.Items?.Count > 0 ? (CmbBrushSize.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "1" : "1";
        var rotLabel = CmbBrushRotation?.SelectedIndex >= 0 && CmbBrushRotation?.Items?.Count > 0 ? (CmbBrushRotation.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "0°" : "0°";
        var status = $"{xyPart}  Chunk: ({cx},{cy})  |  {toolName} {tileName}  |  Capa: {layerName}  Tamaño: {brushSize}  Rot: {rotLabel}".Trim();
        if (EngineSettings.Load().DebugShowOverlay && _tileMap.TryGetTile(mx, my, out var debugTile) && debugTile != null)
            status += $"  |  Col:{ (debugTile.Colision ? "sí" : "no") } Z:{debugTile.Height}";
        UpdateStatusBar(status);
    }

    private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Space)
        {
            _spacePanHeld = false;
            if (_handPanShortcutCursorActive)
            {
                Mouse.OverrideCursor = null;
                _handPanShortcutCursorActive = false;
            }
        }
    }

    /// <summary>Evita atajos globales (p. ej. pan con Espacio) cuando el foco está en un control que acepta texto.</summary>
    private static bool KeyboardFocusAcceptsTextInput()
    {
        var focused = Keyboard.FocusedElement;
        if (focused is TextBoxBase || focused is PasswordBox)
            return true;
        if (focused is not DependencyObject d)
            return false;
        for (var p = d; p != null; p = VisualTreeHelper.GetParent(p))
        {
            var pt = p.GetType();
            if (typeof(TextBoxBase).IsAssignableFrom(pt) || typeof(PasswordBox).IsAssignableFrom(pt))
                return true;
            var typeName = pt.FullName;
            if (typeName != null && typeName.StartsWith("ICSharpCode.AvalonEdit.", StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    private bool HandleConfigurableShortcut(string id, object sender)
    {
        var ev = new RoutedEventArgs();
        switch (id)
        {
            case EditorShortcutBindings.SaveMap:
                MenuGuardarMapa_OnClick(sender, ev);
                return true;
            case EditorShortcutBindings.SaveAll:
                MenuGuardarTodo_OnClick(sender, ev);
                return true;
            case EditorShortcutBindings.Undo:
                if (_history.CanUndo) { _history.Undo(); DrawMap(); RefreshInspector(); }
                return true;
            case EditorShortcutBindings.Redo:
                if (_history.CanRedo) { _history.Redo(); DrawMap(); RefreshInspector(); }
                return true;
            case EditorShortcutBindings.CopyZone:
                CopyZone();
                return true;
            case EditorShortcutBindings.PasteZone:
                PasteZone();
                return true;
            case EditorShortcutBindings.DeleteSelection:
                if (_selection.HasTileSelection)
                {
                    if (IsActiveLayerLocked()) return true;
                    var layerIdx = GetActiveLayerIndex();
                    var rect = _selection.TileSelection!.Value;
                    for (int tx = rect.MinTx; tx <= rect.MaxTx; tx++)
                        for (int ty = rect.MinTy; ty <= rect.MaxTy; ty++)
                        {
                            if (_tileMap.TryGetTile(layerIdx, tx, ty, out var oldTile) && oldTile != null)
                            {
                                _history.Push(new RemoveTileCommand(_tileMap, layerIdx, tx, ty, oldTile));
                                _tileMap.RemoveTile(layerIdx, tx, ty);
                            }
                        }
                    _selection.ClearTileSelection();
                    ProjectExplorer.SetModified(GetCurrentSceneMapPath(), true);
                    DrawMap();
                    UpdateTileSelectionToolbarVisibility();
                }
                else if (_selection.SelectedObjects.Count > 0)
                {
                    foreach (var obj in _selection.SelectedObjects.ToList())
                        _history.Push(new RemoveObjectCommand(_objectLayer, obj));
                    _selection.ClearObjectSelection();
                    _isDragging = false;
                    ProjectExplorer.SetModified(GetCurrentSceneObjectsPath(), true);
                    RefreshInspector();
                    DrawMap();
                }
                return true;
            case EditorShortcutBindings.RotateObject:
                if (_selection.SelectedObject == null) return true;
                _selection.SelectedObject.Rotation = (_selection.SelectedObject.Rotation + 90) % 360;
                ProjectExplorer.SetModified(GetCurrentSceneObjectsPath(), true);
                DrawMap();
                RefreshInspector();
                return true;
            case EditorShortcutBindings.Tool1:
                if (ToolPintar != null) ToolPintar.IsChecked = true;
                CurrentToolMode = ToolMode.Pintar;
                return true;
            case EditorShortcutBindings.Tool2:
                if (ToolSeleccionar != null) ToolSeleccionar.IsChecked = true;
                CurrentToolMode = ToolMode.Seleccionar;
                return true;
            case EditorShortcutBindings.Tool3:
                if (ToolColocar != null) ToolColocar.IsChecked = true;
                CurrentToolMode = ToolMode.Colocar;
                return true;
            case EditorShortcutBindings.Tool4:
                if (ToolZona != null) ToolZona.IsChecked = true;
                CurrentToolMode = ToolMode.Zona;
                return true;
            case EditorShortcutBindings.Tool5:
                if (ToolMedir != null) ToolMedir.IsChecked = true;
                CurrentToolMode = ToolMode.Medir;
                return true;
            case EditorShortcutBindings.Tool6:
                if (ToolPixelEdit != null) ToolPixelEdit.IsChecked = true;
                CurrentToolMode = ToolMode.PixelEdit;
                return true;
            case EditorShortcutBindings.Play:
                StartPlay(useMainScene: false);
                return true;
            case EditorShortcutBindings.PausePlay:
                BtnPausePlay_OnClick(sender, ev);
                return true;
            case EditorShortcutBindings.ToggleGrid:
                if (ChkGridVisible != null)
                    ChkGridVisible.IsChecked = !(ChkGridVisible.IsChecked == true);
                return true;
            case EditorShortcutBindings.GroupObjects:
                EditorLog.Toast("Agrupar objetos: en desarrollo.", LogLevel.Info, "Editor");
                return true;
            case EditorShortcutBindings.HandPan:
                _spacePanHeld = true;
                if (!_handPanShortcutCursorActive)
                {
                    _handPanShortcutCursorActive = true;
                    Mouse.OverrideCursor = System.Windows.Input.Cursors.Hand;
                }
                return true;
            default:
                return false;
        }
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (KeyboardFocusAcceptsTextInput())
            return;

        var key = e.Key;
        if (key >= System.Windows.Input.Key.NumPad1 && key <= System.Windows.Input.Key.NumPad6)
            key = System.Windows.Input.Key.D1 + (key - System.Windows.Input.Key.NumPad1);

        var settings = EngineSettings.Load();
        var id = EditorShortcutBindings.MatchActionId(settings, key, e.KeyboardDevice.Modifiers);
        if (id != null)
        {
            e.Handled = HandleConfigurableShortcut(id, sender);
            return;
        }

        if (ScrollViewer != null && (key == System.Windows.Input.Key.W || key == System.Windows.Input.Key.Up || key == System.Windows.Input.Key.A || key == System.Windows.Input.Key.Left || key == System.Windows.Input.Key.S || key == System.Windows.Input.Key.Down || key == System.Windows.Input.Key.D || key == System.Windows.Input.Key.Right))
        {
            var step = 48 * _panKeyStepScale;
            var dx = (key == System.Windows.Input.Key.A || key == System.Windows.Input.Key.Left) ? -step : (key == System.Windows.Input.Key.D || key == System.Windows.Input.Key.Right) ? step : 0;
            var dy = (key == System.Windows.Input.Key.W || key == System.Windows.Input.Key.Up) ? -step : (key == System.Windows.Input.Key.S || key == System.Windows.Input.Key.Down) ? step : 0;
            if (dx != 0 || dy != 0)
            {
                ScrollViewer.ScrollToHorizontalOffset(ScrollViewer.HorizontalOffset + dx);
                ScrollViewer.ScrollToVerticalOffset(ScrollViewer.VerticalOffset + dy);
                e.Handled = true;
            }
        }
    }

    private void EditorWindow_OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        Window_KeyDown(sender, e);
    }

    private void MapHierarchy_OnTriggerSelected(object? sender, TriggerZone? zone)
    {
        _selection.SetTriggerSelection(zone);
        RefreshInspector();
        DrawMap();
    }

    private void MapHierarchy_OnRequestCreateObject(object? sender, EventArgs e)
    {
        var defId = (_objectLayer.Definitions?.Keys?.FirstOrDefault()) ?? "";
        if (string.IsNullOrEmpty(defId)) return;
        var inst = new ObjectInstance
        {
            DefinitionId = defId,
            X = 0,
            Y = 0,
            Nombre = _objectLayer.GetDefinition(defId)?.Nombre ?? "Objeto"
        };
        _history.Push(new AddObjectCommand(_objectLayer, inst));
        ProjectExplorer.SetModified(GetCurrentSceneObjectsPath(), true);
        MapHierarchy.SetMapStructure(System.IO.Path.GetFileNameWithoutExtension(GetCurrentSceneMapPath()), _project.LayerNames, _objectLayer, _triggerZones, _visibleLayers, GetCurrentUIRoot());
        DrawMap();
        RefreshInspector();
    }

    private void MapHierarchy_OnRequestDuplicateObject(object? sender, ObjectInstance instance)
    {
        var clone = new ObjectInstance
        {
            InstanceId = Guid.NewGuid().ToString("N"),
            DefinitionId = instance.DefinitionId,
            X = instance.X + 1,
            Y = instance.Y,
            Nombre = instance.Nombre + " (copia)",
            Rotation = instance.Rotation,
            ScaleX = instance.ScaleX,
            ScaleY = instance.ScaleY,
            LayerOrder = instance.LayerOrder,
            ColisionOverride = instance.ColisionOverride,
            CollisionType = instance.CollisionType,
            InteractivoOverride = instance.InteractivoOverride,
            DestructibleOverride = instance.DestructibleOverride,
            ScriptIdOverride = instance.ScriptIdOverride,
            ScriptIds = instance.ScriptIds != null ? new List<string>(instance.ScriptIds) : new List<string>(),
            ScriptProperties = (instance.ScriptProperties ?? new List<ScriptInstancePropertySet>()).Select(sp => new ScriptInstancePropertySet
            {
                ScriptId = sp.ScriptId,
                Properties = (sp.Properties ?? new List<ScriptPropertyEntry>()).Select(p => new ScriptPropertyEntry { Key = p.Key, Type = p.Type, Value = p.Value }).ToList()
            }).ToList(),
            Tags = instance.Tags != null ? new List<string>(instance.Tags) : new List<string>()
        };
        _history.Push(new AddObjectCommand(_objectLayer, clone));
        ProjectExplorer.SetModified(GetCurrentSceneObjectsPath(), true);
        MapHierarchy.SetMapStructure(System.IO.Path.GetFileNameWithoutExtension(GetCurrentSceneMapPath()), _project.LayerNames, _objectLayer, _triggerZones, _visibleLayers, GetCurrentUIRoot());
        DrawMap();
        RefreshInspector();
    }

    private void MapHierarchy_OnRequestDeleteObject(object? sender, ObjectInstance instance)
    {
        if (System.Windows.MessageBox.Show(this, "¿Eliminar este objeto?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        _history.Push(new RemoveObjectCommand(_objectLayer, instance));
        _selection.RemoveObjectFromSelection(instance);
        ProjectExplorer.SetModified(GetCurrentSceneObjectsPath(), true);
        MapHierarchy.SetMapStructure(System.IO.Path.GetFileNameWithoutExtension(GetCurrentSceneMapPath()), _project.LayerNames, _objectLayer, _triggerZones, _visibleLayers, GetCurrentUIRoot());
        DrawMap();
        RefreshInspector();
    }

    private void MapHierarchy_OnRequestRenameObject(object? sender, ObjectInstance instance)
    {
        var name = MapHierarchyPanel.ShowRenameDialogPublic("Renombrar objeto", instance.Nombre);
        if (!string.IsNullOrWhiteSpace(name)) { instance.Nombre = name.Trim(); ProjectExplorer.SetModified(GetCurrentSceneObjectsPath(), true); MapHierarchy.SetMapStructure(System.IO.Path.GetFileNameWithoutExtension(GetCurrentSceneMapPath()), _project.LayerNames, _objectLayer, _triggerZones, _visibleLayers, GetCurrentUIRoot()); RefreshInspector(); }
    }

    private void MapHierarchy_OnRequestAddLayer(object? sender, EventArgs e)
    {
        _project.LayerNames ??= new List<string> { "Suelo" };
        var idx = _project.LayerNames.Count;
        _project.LayerNames.Add($"Nueva capa {idx}");
        var projectPath = GetProjectFilePath();
        if (projectPath == null)
        {
            EditorLog.Toast("No se pudo determinar la ruta del proyecto.", LogLevel.Error, "Proyecto");
            return;
        }
        if (File.Exists(projectPath))
            ProjectSerialization.Save(_project, projectPath);
        MapHierarchy.SetMapStructure(System.IO.Path.GetFileNameWithoutExtension(GetCurrentSceneMapPath()), _project.LayerNames, _objectLayer, _triggerZones, _visibleLayers, GetCurrentUIRoot());
    }

    private void MapHierarchy_OnRequestRefresh(object? sender, EventArgs e)
    {
        MapHierarchy.SetMapStructure(System.IO.Path.GetFileNameWithoutExtension(GetCurrentSceneMapPath()), _project.LayerNames, _objectLayer, _triggerZones, _visibleLayers, GetCurrentUIRoot());
    }

    private void MapHierarchy_OnLayerVisibilityToggled(object? sender, (int layerIndex, bool visible) e)
    {
        if (e.visible)
            _visibleLayers.Add(e.layerIndex);
        else
            _visibleLayers.Remove(e.layerIndex);
        MapHierarchy.SetMapStructure(System.IO.Path.GetFileNameWithoutExtension(GetCurrentSceneMapPath()), _project.LayerNames, _objectLayer, _triggerZones, _visibleLayers, GetCurrentUIRoot());
        DrawMap();
    }

    private void MapHierarchy_OnRequestReorderLayers(object? sender, (int fromIndex, int toIndex) e)
    {
        var list = _project.LayerNames ?? new List<string> { "Suelo" };
        if (e.fromIndex < 0 || e.fromIndex >= list.Count || e.toIndex < 0 || e.toIndex >= list.Count) return;
        var name = list[e.fromIndex];
        list.RemoveAt(e.fromIndex);
        list.Insert(e.toIndex, name);
        var projectPath = GetProjectFilePath();
        if (projectPath == null)
        {
            EditorLog.Toast("No se pudo determinar la ruta del proyecto.", LogLevel.Error, "Proyecto");
            return;
        }
        if (File.Exists(projectPath))
            ProjectSerialization.Save(_project, projectPath);
        MapHierarchy.SetMapStructure(System.IO.Path.GetFileNameWithoutExtension(GetCurrentSceneMapPath()), _project.LayerNames, _objectLayer, _triggerZones, _visibleLayers, GetCurrentUIRoot());
    }

    private void MapHierarchy_OnRequestCreateUICanvas(object? sender, EventArgs e)
    {
        var root = GetCurrentUIRoot();
        var id = "Canvas_1";
        for (int i = 1; root.GetCanvas(id) != null; i++) id = $"Canvas_{i}";
        var canvas = new UICanvas { Id = id, Name = id, ResolutionWidth = 1920, ResolutionHeight = 1080, ZIndex = root.Canvases.Count };
        root.AddCanvas(canvas);
        _selection.SetUISelection(canvas, element: null);
        ProjectExplorer?.SetModified(System.IO.Path.Combine(GetCurrentUIFolder(), id + ".json"), true);
        MapHierarchy.SetMapStructure(System.IO.Path.GetFileNameWithoutExtension(GetCurrentSceneMapPath()), _project.LayerNames, _objectLayer, _triggerZones, _visibleLayers, GetCurrentUIRoot());
        RefreshOpenUICanvasTabs();
        RefreshInspector();
        EditorLog.Toast($"Canvas \"{id}\" creado. Clic derecho → Abrir en tab UI para editarlo.", LogLevel.Info, "UI");
    }

    private void MapHierarchy_OnRequestCreateUIElement(object? sender, (UICanvas canvas, FUEngine.Core.UIElement? parent, UIElementKind kind) e)
    {
        var (canvas, parent, kind) = e;
        var prefix = kind switch { UIElementKind.Button => "btn_", UIElementKind.Text => "txt_", UIElementKind.Image => "img_", UIElementKind.Panel => "panel_", _ => "el_" };
        var id = prefix + "001";
        for (int i = 1; !UIRoot.IsIdUniqueInCanvas(canvas, id); i++) id = $"{prefix}{i:D3}";
        var element = new FUEngine.Core.UIElement
        {
            Id = id,
            Kind = kind,
            Rect = new UIRect { X = 0, Y = 0, Width = kind == UIElementKind.Text ? 120 : 100, Height = kind == UIElementKind.Text ? 24 : 32 },
            Anchors = new UIAnchors { MinX = 0, MinY = 0, MaxX = 0, MaxY = 0 },
            Text = kind == UIElementKind.Text || kind == UIElementKind.Button ? (kind == UIElementKind.Button ? "Button" : "Text") : ""
        };
        if (parent != null)
            parent.Children.Add(element);
        else
            canvas.Children.Add(element);
        _selection.SetUISelection(canvas, element);
        ProjectExplorer?.SetModified(System.IO.Path.Combine(GetCurrentUIFolder(), canvas.Id + ".json"), true);
        MapHierarchy.SetMapStructure(System.IO.Path.GetFileNameWithoutExtension(GetCurrentSceneMapPath()), _project.LayerNames, _objectLayer, _triggerZones, _visibleLayers, GetCurrentUIRoot());
        RefreshOpenUICanvasTabs();
        RefreshInspector();
    }

    private void MapHierarchy_OnRequestOpenCanvasInTab(object? sender, UICanvas? canvas)
    {
        if (canvas == null) return;
        var kind = "UI:" + canvas.Id;
        if (!HasTabWithKind(kind))
            AddOrSelectTab(kind);
        else
            SelectTabByKind(kind);
    }

    private void MapHierarchy_OnRequestInstantiateAsset(object? sender, string assetPath)
    {
        var defId = (_objectLayer.Definitions?.Keys?.FirstOrDefault()) ?? "";
        if (string.IsNullOrEmpty(defId)) defId = "default";
        var inst = new ObjectInstance
        {
            DefinitionId = defId,
            X = 0,
            Y = 0,
            Nombre = System.IO.Path.GetFileNameWithoutExtension(assetPath)
        };
        _history.Push(new AddObjectCommand(_objectLayer, inst));
        ProjectExplorer.SetModified(GetCurrentSceneObjectsPath(), true);
        MapHierarchy.SetMapStructure(System.IO.Path.GetFileNameWithoutExtension(GetCurrentSceneMapPath()), _project.LayerNames, _objectLayer, _triggerZones, _visibleLayers, GetCurrentUIRoot());
        DrawMap();
        RefreshInspector();
    }

    private void CopyZone()
    {
        var bounds = ZoneClipboardService.TryGetCopyBounds(
            _selection.HasTileSelection,
            _selection.TileMinTx, _selection.TileMinTy, _selection.TileMaxTx, _selection.TileMaxTy,
            _zoneMinTx, _zoneMinTy, _zoneMaxTx, _zoneMaxTy);
        if (!bounds.HasValue) return;
        var (minTx, minTy, maxTx, maxTy) = bounds.Value;
        _zoneClipboard = ZoneClipboardService.Copy(_tileMap, _objectLayer, minTx, minTy, maxTx, maxTy, GetActiveLayerIndex());
        _pasteOriginTx = minTx;
        _pasteOriginTy = minTy;
        UpdateZoneMenuState();
        EditorLog.Info($"Zona copiada: {_zoneClipboard.Tiles.Count} tiles, {_zoneClipboard.Objects.Count} objetos.", "Editor");
    }

    private void PasteZone()
    {
        if (_zoneClipboard == null || !_zoneClipboard.HasContent) return;
        if (IsActiveLayerLocked()) return;
        int ox = _selection.TileMinTx ?? _pasteOriginTx;
        int oy = _selection.TileMinTy ?? _pasteOriginTy;
        foreach (var cmd in ZoneClipboardService.Paste(_zoneClipboard, _tileMap, _objectLayer, ox, oy, GetActiveLayerIndex()))
            _history.Push(cmd);
        ProjectExplorer.SetModified(GetCurrentSceneMapPath(), true);
        ProjectExplorer.SetModified(GetCurrentSceneObjectsPath(), true);
        _pasteOriginTx = ox + 1;
        _pasteOriginTy = oy;
        DrawMap();
        EditorLog.Info("Zona pegada.", "Editor");
    }

    private void CmbBrushSize_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _brushSize = CmbBrushSize?.SelectedIndex switch { 0 => 1, 1 => 2, 2 => 3, _ => 1 };
        UpdateTransformButtonContent();
    }

    private void CmbBrushRotation_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _brushRotation = CmbBrushRotation?.SelectedIndex switch { 0 => 0, 1 => 90, 2 => 180, 3 => 270, _ => 0 };
        UpdateTransformButtonContent();
    }

    private void UpdateTransformButtonContent()
    {
        if (BtnTransform == null) return;
        var size = CmbBrushSize?.SelectedIndex switch { 0 => "1", 1 => "2", 2 => "3", _ => "1" };
        var rot = CmbBrushRotation?.SelectedIndex switch { 0 => "0°", 1 => "90°", 2 => "180°", 3 => "270°", _ => "0°" };
        BtnTransform.Content = $"{size} · {rot}";
    }

    private void BtnAddTab_OnClick(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu
        {
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x21, 0x26, 0x2d)),
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3)),
            BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x30, 0x36, 0x3d)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(2),
            MinWidth = 220,
            HasDropShadow = false
        };
        var brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3));
        var padding = new Thickness(10, 6, 16, 6);

        var categoryOrder = new[] { "Proyecto", "Contenido", "Multimedia", "Debug" };
        var categoryMenus = new Dictionary<string, MenuItem>();

        foreach (var cat in categoryOrder)
        {
            var catItem = new MenuItem { Header = cat, Foreground = brush, Background = System.Windows.Media.Brushes.Transparent, Padding = padding };
            categoryMenus[cat] = catItem;
            menu.Items.Add(catItem);
        }

        foreach (var kind in OptionalTabKindsOrder)
        {
            if (!TabCategory.TryGetValue(kind, out var cat) || !categoryMenus.TryGetValue(cat, out var parent))
                continue;
            var displayName = TabDisplayNames.GetValueOrDefault(kind, kind);
            var icon = TabIcons.TryGetValue(kind, out var ic) ? ic + " " : "";
            var item = new MenuItem { Header = icon + displayName, Tag = kind, Foreground = brush, Background = System.Windows.Media.Brushes.Transparent, Padding = padding };
            item.Click += (s, _) => AddOrSelectTabFromMenu((string)((MenuItem)s!).Tag!);
            parent.Items.Add(item);
        }

        var selectedTab = MainTabs?.SelectedItem as TabItem;
        var activeIsMap = selectedTab != null && (selectedTab.Tag as string == "Mapa" || (selectedTab.Header is System.Windows.Controls.StackPanel sp && sp.Children.Count > 0 && sp.Children[0] is TextBlock tb && tb.Text == "Mapa"));
        if (activeIsMap && categoryMenus.TryGetValue("Proyecto", out var proyectoMenu))
        {
            var nuevoSeeds = new MenuItem { Header = "Seeds", Tag = "Seeds", Foreground = brush, Background = System.Windows.Media.Brushes.Transparent, Padding = padding };
            nuevoSeeds.Click += (s, _) => AddOrSelectTabFromMenu("Seeds");
            proyectoMenu.Items.Add(nuevoSeeds);
        }

        if (sender is System.Windows.FrameworkElement fe)
        {
            menu.PlacementTarget = fe;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.HorizontalOffset = 0;
            menu.VerticalOffset = 2;
        }
        menu.IsOpen = true;
    }

    private void InitializeFixedTabs()
    {
        // Solo Mapa y Consola son tabs fijos; el resto se añaden con + y tienen botón x.
    }

    private void BuildScenesStrip()
    {
        if (ScenesStrip == null) return;
        ScenesStrip.Items.Clear();
        var canClose = _openScenes.Count >= 2;
        for (var i = 0; i < _openScenes.Count; i++)
        {
            var idx = i;
            var state = _openScenes[i];
            var name = state.Definition?.Name ?? "Principal";
            var panel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 8, 0) };
            var border = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x21, 0x26, 0x2d)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x30, 0x36, 0x3d)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 4, 8, 4),
                CornerRadius = new CornerRadius(4),
                Cursor = System.Windows.Input.Cursors.Hand,
                Child = panel
            };
            border.Tag = idx;
            border.PreviewMouseLeftButtonDown += ScenesStripTab_PreviewMouseLeftButtonDown;
            border.MouseLeftButtonUp += ScenesStripTab_MouseLeftButtonUp;
            border.MouseMove += ScenesStripTab_MouseMove;
            border.MouseLeave += ScenesStripTab_MouseLeave;
            border.MouseLeftButtonDown += (_, e) => { if (!_sceneDragSourceIndex.HasValue) SwitchToScene(idx); };
            var sceneId = state.Definition?.Id ?? "principal";
            border.ToolTip = $"{name} (Id: {sceneId}) — Arrastra para reordenar";
            panel.Children.Add(new TextBlock { Text = name, Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3)), FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) });
            var closeBtn = new System.Windows.Controls.Button
            {
                Content = "×",
                Width = 18,
                Height = 18,
                Padding = new Thickness(0),
                FontSize = 12,
                Foreground = System.Windows.Media.Brushes.White,
                Background = System.Windows.Media.Brushes.Transparent,
                Cursor = System.Windows.Input.Cursors.Hand,
                IsEnabled = canClose
            };
            closeBtn.Click += (_, _) => { if (_openScenes.Count >= 2) CloseScene(idx); };
            panel.Children.Add(closeBtn);
            ScenesStrip.Items.Add(border);
        }
    }

    private void ScenesStripTab_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border || border.Tag is not int idx) return;
        _sceneDragSourceIndex = idx;
        _sceneDragStartPoint = e.GetPosition(this);
    }

    private void ScenesStripTab_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_sceneDragSourceIndex.HasValue || sender is not Border border) return;
        var pos = e.GetPosition(this);
        if (Math.Abs(pos.X - _sceneDragStartPoint.X) + Math.Abs(pos.Y - _sceneDragStartPoint.Y) < 8) return;
        try { border.CaptureMouse(); } catch { /* ignore */ }
    }

    private void ScenesStripTab_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is Border b && _sceneDragSourceIndex.HasValue) try { b.ReleaseMouseCapture(); } catch { }
    }

    private void ScenesStripTab_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border) try { border.ReleaseMouseCapture(); } catch { }
        if (!_sceneDragSourceIndex.HasValue || ScenesStrip == null) { _sceneDragSourceIndex = null; return; }
        var sourceIdx = _sceneDragSourceIndex.Value;
        _sceneDragSourceIndex = null;

        var pos = e.GetPosition(ScenesStrip);
        int? dropIdx = null;
        for (var i = 0; i < ScenesStrip.Items.Count; i++)
        {
            if (ScenesStrip.Items[i] is not Border b || b.Tag is not int tag) continue;
            var bounds = new System.Windows.Rect(b.TransformToAncestor(ScenesStrip).Transform(new System.Windows.Point(0, 0)), b.RenderSize);
            if (bounds.Contains(pos)) { dropIdx = tag; break; }
        }
        if (!dropIdx.HasValue || dropIdx.Value == sourceIdx)
        {
            SwitchToScene(sourceIdx);
            return;
        }

        var state = _openScenes[sourceIdx];
        _openScenes.RemoveAt(sourceIdx);
        var insertAt = dropIdx.Value > sourceIdx ? dropIdx.Value - 1 : dropIdx.Value;
        insertAt = Math.Max(0, Math.Min(insertAt, _openScenes.Count));
        _openScenes.Insert(insertAt, state);

        if (_currentSceneIndex == sourceIdx) _currentSceneIndex = insertAt;
        else if (sourceIdx < _currentSceneIndex && insertAt >= _currentSceneIndex) _currentSceneIndex--;
        else if (sourceIdx > _currentSceneIndex && insertAt <= _currentSceneIndex) _currentSceneIndex++;

        if (_project.Scenes != null && _project.Scenes.Count == _openScenes.Count && sourceIdx < _project.Scenes.Count)
        {
            var def = _project.Scenes[sourceIdx];
            _project.Scenes.RemoveAt(sourceIdx);
            var defInsert = Math.Max(0, Math.Min(insertAt, _project.Scenes.Count));
            _project.Scenes.Insert(defInsert, def);
        }
        for (var i = 0; i < _openScenes.Count; i++)
            _openScenes[i].SceneIndex = i;
        var projectPath = GetProjectFilePath();
        if (!string.IsNullOrEmpty(projectPath))
        {
            try { ProjectSerialization.Save(_project, projectPath); }
            catch (Exception ex)
            {
                EditorLog.Toast($"Error al guardar el proyecto: {ex.Message}", LogLevel.Error, "Proyecto");
                EditorLog.Error($"Error al guardar el proyecto: {ex.Message}", "Proyecto");
            }
        }
        BuildScenesStrip();
    }

    private void SaveCurrentSceneTabState()
    {
        if (_currentSceneIndex < 0 || _currentSceneIndex >= _openScenes.Count) return;
        var state = _openScenes[_currentSceneIndex];
        state.OpenOptionalTabKinds.Clear();
        if (MainTabs?.Items != null)
        {
            foreach (TabItem tab in MainTabs.Items)
            {
                if (tab.Tag is string k && k != "Mapa" && k != "Consola")
                    state.OpenOptionalTabKinds.Add(k);
            }
        }
        state.SelectedTabKind = (MainTabs?.SelectedItem as TabItem)?.Tag as string ?? "Juego";
    }

    private void RemoveOptionalTabs()
    {
        if (MainTabs?.Items == null) return;
        var toRemove = new List<TabItem>();
        foreach (TabItem tab in MainTabs.Items)
        {
            if (tab.Tag is string k && k != "Mapa" && k != "Consola")
                toRemove.Add(tab);
        }
        foreach (var tab in toRemove)
        {
            if (tab.Content is GameTabContent g) g.Dispose();
            if (tab.Content is DebugTabContent d) d.StopRefresh();
            if (tab.Content is AudioTabContent) _audioSystem?.StopPreview();
            MainTabs.Items.Remove(tab);
        }
        if (toRemove.Any(t => (t.Tag as string) == "Explorador")) _explorerPanel = ProjectExplorer;
    }

    private void ApplyCurrentSceneOptionalTabs()
    {
        if (_openScenes.Count == 0) return;
        RemoveOptionalTabs();
        foreach (var kind in _openScenes[_currentSceneIndex].OpenOptionalTabKinds.ToList())
        {
            if (!HasTabWithKind(kind))
                AddOrSelectTab(kind);
        }
        var sel = _openScenes[_currentSceneIndex].SelectedTabKind ?? "Juego";
        var tab = GetTabByKind(sel);
        if (tab != null) tab.IsSelected = true;
    }

    private void SwitchToScene(int index)
    {
        if (index < 0 || index >= _openScenes.Count || index == _currentSceneIndex) return;
        SaveCurrentSceneTabState();
        _currentSceneIndex = index;
        var state = _openScenes[_currentSceneIndex];
        _tileMap = state.TileMap;
        _objectLayer = state.ObjectLayer;
        RefreshLayersPanelFromTileMap();
        RemoveOptionalTabs();
        foreach (var kind in state.OpenOptionalTabKinds.ToList())
        {
            if (!HasTabWithKind(kind))
                AddOrSelectTab(kind);
        }
        var sel = state.SelectedTabKind ?? "Juego";
        var tab = GetTabByKind(sel);
        if (tab != null) tab.IsSelected = true;
        MapHierarchy?.SetMapStructure(
            System.IO.Path.GetFileNameWithoutExtension(state.Definition?.MapPathRelative ?? _project.MapPathRelative),
            _project.LayerNames,
            _objectLayer,
            _triggerZones,
            _visibleLayers,
            GetCurrentUIRoot());
        DrawMap();
        RefreshInspector();
        CmbObjectDef.ItemsSource = _objectLayer?.Definitions?.Values?.ToList() ?? new List<ObjectDefinition>();
        if (CmbObjectDef != null && CmbObjectDef.Items.Count > 0) CmbObjectDef.SelectedIndex = 0;
        BuildScenesStrip();
    }

    private void CloseScene(int index)
    {
        if (_openScenes.Count <= 1) return;
        SaveCurrentSceneTabState();
        _openScenes.RemoveAt(index);
        if (_openScenes.Count == 0) { BuildScenesStrip(); return; }
        if (_currentSceneIndex >= _openScenes.Count)
            _currentSceneIndex = _openScenes.Count - 1;
        if (index < _currentSceneIndex)
            _currentSceneIndex--;
        var state = _openScenes[_currentSceneIndex];
        _tileMap = state.TileMap;
        _objectLayer = state.ObjectLayer;
        RefreshLayersPanelFromTileMap();
        RemoveOptionalTabs();
        foreach (var kind in state.OpenOptionalTabKinds.ToList())
        {
            if (!HasTabWithKind(kind))
                AddOrSelectTab(kind);
        }
        var sel = state.SelectedTabKind ?? "Juego";
        var tab = GetTabByKind(sel);
        if (tab != null) tab.IsSelected = true;
        MapHierarchy?.SetMapStructure(
            System.IO.Path.GetFileNameWithoutExtension(state.Definition?.MapPathRelative ?? _project.MapPathRelative),
            _project.LayerNames,
            _objectLayer,
            _triggerZones,
            _visibleLayers,
            GetCurrentUIRoot());
        DrawMap();
        RefreshInspector();
        BuildScenesStrip();
        _ = SaveEditorLayoutAsync();
    }

    private void MenuAbrirEscena_SubmenuOpened(object sender, RoutedEventArgs e)
    {
        PopulateOpenSceneMenu(MenuAbrirEscena);
    }

    private void MenuAbrirScene_SubmenuOpened(object sender, RoutedEventArgs e)
    {
        PopulateOpenSceneMenu(MenuAbrirScene);
    }

    private void PopulateOpenSceneMenu(MenuItem? parent)
    {
        if (parent?.Items == null) return;
        parent.Items.Clear();
        if (_project.Scenes == null || _project.Scenes.Count == 0)
        {
            parent.Items.Add(new MenuItem { Header = "(No hay escenas en el proyecto)", IsEnabled = false });
            return;
        }
        var openIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in _openScenes)
            if (s.Definition?.Id != null) openIds.Add(s.Definition.Id);
        foreach (var def in _project.Scenes)
        {
            if (openIds.Contains(def.Id ?? "")) continue;
            var item = new MenuItem { Header = def.Name ?? def.Id, Tag = def };
            item.Click += (_, _) => OpenSceneFromProject(def);
            parent.Items.Add(item);
        }
        if (parent.Items.Count == 0)
        {
            parent.Items.Add(new MenuItem { Header = "(Todas las escenas están abiertas)", IsEnabled = false });
        }
    }

    private void OpenSceneFromProject(SceneDefinition def)
    {
        if (def == null) return;
        var mapPath = NewProjectStructure.ResolveMapPath(_project.ProjectDirectory ?? "", def.MapPathRelative);
        var objectsPath = NewProjectStructure.ResolveObjectsPath(_project.ProjectDirectory ?? "", def.ObjectsPathRelative);
        var tileMap = new TileMap(_project.ChunkSize);
        var objectLayer = new ObjectLayer();
        try
        {
            if (File.Exists(mapPath)) tileMap = MapSerialization.Load(mapPath);
            else EditorLog.Warning($"No se encontró {def.MapPathRelative}; se usa mapa vacío para {def.Name}.", "mapa");
        }
        catch (Exception ex) { EditorLog.Error($"Error al cargar mapa {def.Name}: {ex.Message}", "mapa"); }
        try
        {
            if (File.Exists(objectsPath)) objectLayer = ObjectsSerialization.Load(objectsPath);
            else EditorLog.Warning($"No se encontró {def.ObjectsPathRelative}; se usa capa vacía para {def.Name}.", "objetos");
        }
        catch (Exception ex) { EditorLog.Error($"Error al cargar objetos {def.Name}: {ex.Message}", "objetos"); }

        var sceneIndex = _project.Scenes?.IndexOf(def) ?? _openScenes.Count;
        var openState = new OpenSceneState
        {
            Definition = def,
            SceneIndex = sceneIndex,
            TileMap = tileMap,
            ObjectLayer = objectLayer,
            OpenOptionalTabKinds = def.DefaultTabKinds != null ? new List<string>(def.DefaultTabKinds) : new List<string>(),
            SelectedTabKind = "Juego"
        };
        LoadUIRootForState(openState);
        _openScenes.Add(openState);
        BuildScenesStrip();
        SwitchToScene(_openScenes.Count - 1);
    }

    /// <summary>Obtiene la ruta del archivo de proyecto. Orden: Project.FUE (prioridad), proyecto.json, Project.json.
    /// Si existen varios archivos, se usa .FUE como fuente de verdad. Para proyectos nuevos se devuelve la ruta .FUE.</summary>
    private string? GetProjectFilePath()
    {
        var dir = _project.ProjectDirectory ?? "";
        if (string.IsNullOrEmpty(dir)) return null;
        var fuePath = System.IO.Path.Combine(dir, NewProjectStructure.ProjectFileName);
        var proyectoJson = System.IO.Path.Combine(dir, "proyecto.json");
        var projectJson = System.IO.Path.Combine(dir, "Project.json");
        var hasFue = File.Exists(fuePath);
        var hasProyecto = File.Exists(proyectoJson);
        var hasProject = File.Exists(projectJson);
        if (hasFue) return fuePath;
        if (hasProyecto) return proyectoJson;
        if (hasProject) return projectJson;
        return fuePath;
    }

    private void SetProjectIconPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;
        _project.IconPath = relativePath.Trim();
        var projectPath = GetProjectFilePath();
        if (!string.IsNullOrEmpty(projectPath))
        {
            try { ProjectSerialization.Save(_project, projectPath); }
            catch (Exception ex) { EditorLog.Error($"No se pudo guardar proyecto: {ex.Message}", "Icono"); }
        }
    }

    private static readonly HashSet<string> _warnedMultipleProjectPaths = new(StringComparer.OrdinalIgnoreCase);

    private static void WarnIfMultipleProjectFiles(string projectDir)
    {
        if (string.IsNullOrEmpty(projectDir)) return;
        var normalized = System.IO.Path.GetFullPath(projectDir);
        if (!_warnedMultipleProjectPaths.Add(normalized)) return;
        var fue = File.Exists(System.IO.Path.Combine(projectDir, NewProjectStructure.ProjectFileName));
        var proyecto = File.Exists(System.IO.Path.Combine(projectDir, "proyecto.json"));
        var project = File.Exists(System.IO.Path.Combine(projectDir, "Project.json"));
        var count = (fue ? 1 : 0) + (proyecto ? 1 : 0) + (project ? 1 : 0);
        if (count > 1)
            EditorLog.Toast("Hay varios archivos de proyecto (.FUE y legacy). Se usa Project.FUE como referencia.", LogLevel.Warning, "Proyecto");
    }

    private static string GenerateNewSceneId(IEnumerable<SceneDefinition> existing)
    {
        var ids = new HashSet<string>(existing.Select(s => s.Id ?? ""), StringComparer.OrdinalIgnoreCase);
        for (var i = 1; i <= 999; i++)
        {
            var id = $"scene_{i:D3}";
            if (!ids.Contains(id)) return id;
        }
        return "scene_" + Guid.NewGuid().ToString("N")[..8];
    }

    private void MenuCrearScene_OnClick(object sender, RoutedEventArgs e)
    {
        if (_project.Scenes == null) _project.Scenes = new List<SceneDefinition>();
        if (_project.Scenes.Count == 0 && _openScenes.Count > 0 && _openScenes[0].Definition == null)
        {
            _project.Scenes.Add(new SceneDefinition
            {
                Id = "scene_0",
                Name = "Principal",
                MapPathRelative = _project.MapPathRelative ?? "mapa.json",
                ObjectsPathRelative = "objetos.json"
            });
            _openScenes[0].Definition = _project.Scenes[0];
            _openScenes[0].SceneIndex = 0;
        }

        var newId = GenerateNewSceneId(_project.Scenes);
        var mapsDir = System.IO.Path.Combine(_project.ProjectDirectory ?? "", "Maps");
        var objectsDir = System.IO.Path.Combine(_project.ProjectDirectory ?? "", "Objects");
        Directory.CreateDirectory(mapsDir);
        Directory.CreateDirectory(objectsDir);
        var mapRelative = $"Maps/{newId}/{NewProjectStructure.MapFileName}";
        var objectsRelative = $"Objects/{newId}/{NewProjectStructure.ObjectsFileName}";
        var mapPath = System.IO.Path.Combine(_project.ProjectDirectory ?? "", mapRelative);
        var objectsPath = System.IO.Path.Combine(_project.ProjectDirectory ?? "", objectsRelative);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(mapPath)!);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(objectsPath)!);

        var emptyMap = new TileMap(_project.ChunkSize);
        var emptyObjects = new ObjectLayer();
        EnsureDefaultObjectDefinition();
        if (_objectLayer.Definitions.Count > 0)
        {
            foreach (var kv in _objectLayer.Definitions)
                emptyObjects.RegisterDefinition(kv.Value);
        }
        try
        {
            MapSerialization.Save(emptyMap, mapPath);
            ObjectsSerialization.Save(emptyObjects, objectsPath);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "Error al crear archivos de la escena:\n\n" + ex.Message, "Crear escena — Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var def = new SceneDefinition
        {
            Id = newId,
            Name = "Nueva escena",
            MapPathRelative = mapRelative,
            ObjectsPathRelative = objectsRelative
        };
        _project.Scenes.Add(def);
        var projectPath = GetProjectFilePath();
        if (!string.IsNullOrEmpty(projectPath))
        {
            try { ProjectSerialization.Save(_project, projectPath); }
            catch (Exception ex) { EditorLog.Error($"No se pudo guardar proyecto: {ex.Message}", "Scene"); }
        }

        var newState = new OpenSceneState
        {
            Definition = def,
            SceneIndex = _project.Scenes.Count - 1,
            TileMap = emptyMap,
            ObjectLayer = emptyObjects,
            OpenOptionalTabKinds = new List<string>(),
            SelectedTabKind = "Juego"
        };
        LoadUIRootForState(newState);
        _openScenes.Add(newState);
        BuildScenesStrip();
        SwitchToScene(_openScenes.Count - 1);
        RefreshSceneUsedPaths();
        EditorLog.Toast("Escena creada correctamente.", LogLevel.Info, "Scene");
    }

    private void MenuDuplicarScene_OnClick(object sender, RoutedEventArgs e)
    {
        if (_currentSceneIndex < 0 || _currentSceneIndex >= _openScenes.Count) return;
        var state = _openScenes[_currentSceneIndex];
        var currentDef = state.Definition;
        if (currentDef == null)
        {
            EditorLog.Toast("No se puede duplicar la escena principal legacy. Cree primero una escena nueva.", LogLevel.Warning, "Scene");
            return;
        }

        if (_project.Scenes == null) _project.Scenes = new List<SceneDefinition>();
        var newId = GenerateNewSceneId(_project.Scenes);
        var mapRelative = $"Maps/{newId}/{NewProjectStructure.MapFileName}";
        var objectsRelative = $"Objects/{newId}/{NewProjectStructure.ObjectsFileName}";
        var mapPath = NewProjectStructure.ResolveMapPath(_project.ProjectDirectory ?? "", currentDef.MapPathRelative);
        var objectsPath = NewProjectStructure.ResolveObjectsPath(_project.ProjectDirectory ?? "", currentDef.ObjectsPathRelative);
        var newMapPath = System.IO.Path.Combine(_project.ProjectDirectory ?? "", mapRelative);
        var newObjectsPath = System.IO.Path.Combine(_project.ProjectDirectory ?? "", objectsRelative);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(newMapPath)!);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(newObjectsPath)!);

        try
        {
            File.Copy(mapPath, newMapPath, overwrite: true);
            File.Copy(objectsPath, newObjectsPath, overwrite: true);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "Error al copiar archivos:\n\n" + ex.Message, "Duplicar escena — Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var newDef = new SceneDefinition
        {
            Id = newId,
            Name = (currentDef.Name ?? currentDef.Id) + " (copia)",
            MapPathRelative = mapRelative,
            ObjectsPathRelative = objectsRelative,
            DefaultTabKinds = currentDef.DefaultTabKinds != null ? new List<string>(currentDef.DefaultTabKinds) : new List<string>()
        };
        _project.Scenes.Add(newDef);

        var projectPath = GetProjectFilePath();
        if (!string.IsNullOrEmpty(projectPath))
        {
            try { ProjectSerialization.Save(_project, projectPath); }
            catch (Exception ex) { EditorLog.Error($"No se pudo guardar proyecto: {ex.Message}", "Scene"); }
        }

        TileMap dupMap;
        ObjectLayer dupLayer;
        try
        {
            dupMap = MapSerialization.Load(newMapPath);
            dupLayer = ObjectsSerialization.Load(newObjectsPath);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "Error al cargar la copia:\n\n" + ex.Message, "Duplicar escena — Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var dupState = new OpenSceneState
        {
            Definition = newDef,
            SceneIndex = _project.Scenes.Count - 1,
            TileMap = dupMap,
            ObjectLayer = dupLayer,
            UIRoot = UIRoot.Clone(GetCurrentUIRoot()),
            OpenOptionalTabKinds = newDef.DefaultTabKinds != null ? new List<string>(newDef.DefaultTabKinds) : new List<string>(),
            SelectedTabKind = "Juego"
        };
        _openScenes.Add(dupState);
        BuildScenesStrip();
        SwitchToScene(_openScenes.Count - 1);
        RefreshSceneUsedPaths();
        EditorLog.Toast("Escena duplicada correctamente.", LogLevel.Info, "Scene");
    }

    private void MenuEliminarScene_OnClick(object sender, RoutedEventArgs e)
    {
        if (_currentSceneIndex < 0 || _currentSceneIndex >= _openScenes.Count) return;
        if (_openScenes.Count <= 1)
        {
            EditorLog.Toast("No se puede eliminar la única escena abierta.", LogLevel.Warning, "Scene");
            return;
        }
        var state = _openScenes[_currentSceneIndex];
        var def = state.Definition;
        if (def == null)
        {
            EditorLog.Toast("No se puede eliminar la escena principal.", LogLevel.Warning, "Scene");
            return;
        }
        if (System.Windows.MessageBox.Show(this,
                "¿Eliminar la escena actual? Se eliminarán sus archivos de mapa y objetos.",
                "Eliminar Scene",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        var mapPath = def.GetMapPath(_project.ProjectDirectory ?? "");
        var objectsPath = def.GetObjectsPath(_project.ProjectDirectory ?? "");

        try
        {
            if (File.Exists(mapPath)) File.Delete(mapPath);
            if (File.Exists(objectsPath)) File.Delete(objectsPath);
        }
        catch (Exception ex)
        {
            EditorLog.Warning($"No se pudieron eliminar archivos: {ex.Message}", "Scene");
            EditorLog.Toast("No se pudieron eliminar los archivos. La escena no se ha eliminado. Cierra los archivos si están en uso o usa Proyecto → Limpiar archivos huérfanos.", LogLevel.Warning, "Scene");
            return;
        }

        var idxInProject = _project.Scenes?.IndexOf(def) ?? -1;
        _openScenes.RemoveAt(_currentSceneIndex);
        if (_project.Scenes != null && idxInProject >= 0)
            _project.Scenes.RemoveAt(idxInProject);
        for (var i = 0; i < _openScenes.Count; i++)
            _openScenes[i].SceneIndex = _project.Scenes != null ? _project.Scenes.IndexOf(_openScenes[i].Definition!) : 0;

        if (_currentSceneIndex >= _openScenes.Count) _currentSceneIndex = Math.Max(0, _openScenes.Count - 1);
        if (_openScenes.Count > 0)
        {
            var next = _openScenes[_currentSceneIndex];
            _tileMap = next.TileMap;
            _objectLayer = next.ObjectLayer;
            RefreshLayersPanelFromTileMap();
        }

        var projectPath = GetProjectFilePath();
        if (!string.IsNullOrEmpty(projectPath))
        {
            try { ProjectSerialization.Save(_project, projectPath); }
            catch (Exception ex) { EditorLog.Error($"No se pudo guardar proyecto: {ex.Message}", "Scene"); }
        }

        EditorLog.Toast("Escena eliminada correctamente.", LogLevel.Info, "Scene");

        RemoveOptionalTabs();
        if (_openScenes.Count > 0)
        {
            ApplyCurrentSceneOptionalTabs();
            MapHierarchy?.SetMapStructure(
                System.IO.Path.GetFileNameWithoutExtension(_openScenes[_currentSceneIndex].Definition?.MapPathRelative ?? "mapa"),
                _project.LayerNames,
                _objectLayer,
                _triggerZones,
                _visibleLayers);
        }
        DrawMap();
        RefreshInspector();
        BuildScenesStrip();
        RefreshSceneUsedPaths();
    }

    private void MenuImportarScene_OnClick(object sender, RoutedEventArgs e)
    {
        const string aviso = "Importar una escena de otro proyecto tiene riesgos:\n\n" +
            "• Solo se copian los archivos de mapa y objetos de esa escena. Los assets (imágenes, sprites, sonidos, etc.) NO se copian automáticamente.\n" +
            "• Las rutas a assets del proyecto de origen pueden quedar rotas en este proyecto.\n" +
            "• No se detectan conflictos de nombres ni se actualizan referencias internas.\n\n" +
            "Recomendación: si la escena usa recursos externos, copia manualmente las carpetas de assets necesarias y revisa las rutas después de importar.\n\n" +
            "¿Deseas continuar y seleccionar el proyecto del que importar?";
        if (System.Windows.MessageBox.Show(this, aviso, "Importar escena — Aviso importante", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK)
            return;

        var settings = EngineSettings.Load();
        var initialDir = !string.IsNullOrWhiteSpace(settings.DefaultProjectsPath) && Directory.Exists(settings.DefaultProjectsPath)
            ? settings.DefaultProjectsPath
            : (Directory.Exists(EngineSettings.GetDefaultProjectsRoot()) ? EngineSettings.GetDefaultProjectsRoot() : "");
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Proyecto FUEngine (*.FUE)|*.FUE|Legacy (Project.json, proyecto.json)|Project.json;proyecto.json|Todos|*.*",
            Title = "Seleccionar proyecto del que importar la escena",
            InitialDirectory = initialDir
        };
        if (dlg.ShowDialog() != true) return;

        ProjectInfo? sourceProject;
        try
        {
            sourceProject = ProjectSerialization.Load(dlg.FileName);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "Error al cargar el proyecto de origen:\n\n" + ex.Message, "Importar escena — Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var sourceScenes = sourceProject.Scenes;
        if (sourceScenes == null || sourceScenes.Count == 0)
        {
            var legacyMap = System.IO.Path.Combine(sourceProject.ProjectDirectory ?? "", sourceProject.MapPathRelative ?? "mapa.json");
            var legacyObjects = System.IO.Path.Combine(sourceProject.ProjectDirectory ?? "", "objetos.json");
            if (!File.Exists(legacyMap) && !File.Exists(legacyObjects))
            {
                EditorLog.Toast("El proyecto seleccionado no tiene escenas ni archivos de mapa/objetos.", LogLevel.Warning, "Importar");
                return;
            }
            sourceScenes = new List<SceneDefinition>
            {
                new SceneDefinition
                {
                    Id = "scene_0",
                    Name = "Principal",
                    MapPathRelative = sourceProject.MapPathRelative ?? "mapa.json",
                    ObjectsPathRelative = "objetos.json"
                }
            };
        }

        SceneDefinition? toImport = sourceScenes.Count == 1 ? sourceScenes[0] : null;
        if (toImport == null && sourceScenes.Count > 1)
        {
            var choice = System.Windows.MessageBox.Show(this,
                "El proyecto tiene varias escenas. Se importará la primera: " + (sourceScenes[0].Name ?? sourceScenes[0].Id) + ".",
                "Importar escena",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question);
            if (choice != MessageBoxResult.OK) return;
            toImport = sourceScenes[0];
        }

        if (toImport == null) return;

        var srcMapPath = toImport.GetMapPath(sourceProject.ProjectDirectory ?? "");
        var srcObjectsPath = toImport.GetObjectsPath(sourceProject.ProjectDirectory ?? "");
        var sourceDir = sourceProject.ProjectDirectory ?? "";

        var (foundAssets, missingAssets) = ScanSceneAssetReferences(srcMapPath, srcObjectsPath, sourceDir);
        var scanDlg = new ImportSceneAssetScanDialog(foundAssets, missingAssets, sourceDir, _project.ProjectDirectory) { Owner = this };
        scanDlg.ShowDialog();
        if (!scanDlg.UserChoseImport) return;

        if (_project.Scenes == null) _project.Scenes = new List<SceneDefinition>();
        if (_project.Scenes.Count == 0 && _openScenes.Count > 0 && _openScenes[0].Definition == null)
        {
            _project.Scenes.Add(new SceneDefinition
            {
                Id = "scene_0",
                Name = "Principal",
                MapPathRelative = _project.MapPathRelative ?? "mapa.json",
                ObjectsPathRelative = "objetos.json"
            });
            _openScenes[0].Definition = _project.Scenes[0];
            _openScenes[0].SceneIndex = 0;
        }

        var newId = GenerateNewSceneId(_project.Scenes);
        var mapRelative = $"Maps/{newId}/{NewProjectStructure.MapFileName}";
        var objectsRelative = $"Objects/{newId}/{NewProjectStructure.ObjectsFileName}";
        var destMapPath = System.IO.Path.Combine(_project.ProjectDirectory ?? "", mapRelative);
        var destObjectsPath = System.IO.Path.Combine(_project.ProjectDirectory ?? "", objectsRelative);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destMapPath)!);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destObjectsPath)!);

        try
        {
            if (File.Exists(srcMapPath)) File.Copy(srcMapPath, destMapPath, overwrite: true);
            else MapSerialization.Save(new TileMap(_project.ChunkSize), destMapPath);
            if (File.Exists(srcObjectsPath)) File.Copy(srcObjectsPath, destObjectsPath, overwrite: true);
            else ObjectsSerialization.Save(new ObjectLayer(), destObjectsPath);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "Error al copiar archivos de la escena:\n\n" + ex.Message, "Importar escena — Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var newDef = new SceneDefinition
        {
            Id = newId,
            Name = (toImport.Name ?? toImport.Id) + " (importada)",
            MapPathRelative = mapRelative,
            ObjectsPathRelative = objectsRelative,
            DefaultTabKinds = toImport.DefaultTabKinds != null ? new List<string>(toImport.DefaultTabKinds) : new List<string>()
        };
        _project.Scenes.Add(newDef);

        var projectPath = GetProjectFilePath();
        if (!string.IsNullOrEmpty(projectPath))
        {
            try { ProjectSerialization.Save(_project, projectPath); }
            catch (Exception ex) { EditorLog.Error($"No se pudo guardar proyecto: {ex.Message}", "Scene"); }
        }

        TileMap importedMap;
        ObjectLayer importedLayer;
        try
        {
            importedMap = MapSerialization.Load(destMapPath);
            importedLayer = ObjectsSerialization.Load(destObjectsPath);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "Error al cargar la escena importada:\n\n" + ex.Message + "\n\nRevisa que los archivos se hayan copiado correctamente.", "Importar escena — Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var importedState = new OpenSceneState
        {
            Definition = newDef,
            SceneIndex = _project.Scenes.Count - 1,
            TileMap = importedMap,
            ObjectLayer = importedLayer,
            OpenOptionalTabKinds = newDef.DefaultTabKinds != null ? new List<string>(newDef.DefaultTabKinds) : new List<string>(),
            SelectedTabKind = "Juego"
        };
        LoadUIRootForState(importedState);
        _openScenes.Add(importedState);
        BuildScenesStrip();
        SwitchToScene(_openScenes.Count - 1);
        RefreshSceneUsedPaths();
        EditorLog.Toast("Escena importada correctamente. Si usaba assets de otro proyecto, copia los archivos y revisa rutas.", LogLevel.Info, "Importar");
    }

    private static (List<AssetScanItem> found, List<AssetScanItem> missing) ScanSceneAssetReferences(string srcMapPath, string srcObjectsPath, string sourceDir)
    {
        var allPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            if (File.Exists(srcObjectsPath))
            {
                var layer = ObjectsSerialization.Load(srcObjectsPath);
                foreach (var def in layer.Definitions.Values)
                    if (!string.IsNullOrWhiteSpace(def.SpritePath))
                        allPaths.Add(def.SpritePath.Trim());
            }
        }
        catch { /* ignore */ }
        try
        {
            if (File.Exists(srcMapPath))
            {
                var map = MapSerialization.Load(srcMapPath);
                foreach (var (cx, cy) in map.EnumerateChunkCoords())
                {
                    var chunk = map.GetChunk(cx, cy);
                    if (chunk == null) continue;
                    foreach (var (_, _, data) in chunk.EnumerateTiles())
                        if (!string.IsNullOrWhiteSpace(data.SourceImagePath))
                            allPaths.Add(data.SourceImagePath.Trim());
                }
            }
        }
        catch { /* ignore */ }

        var found = new List<AssetScanItem>();
        var missing = new List<AssetScanItem>();
        foreach (var rel in allPaths)
        {
            var full = System.IO.Path.Combine(sourceDir, rel);
            var item = new AssetScanItem { RelativePath = rel, AssetType = GetAssetTypeFromPath(rel) };
            if (File.Exists(full))
                found.Add(item);
            else
                missing.Add(item);
        }
        return (found, missing);
    }

    private static string GetAssetTypeFromPath(string path)
    {
        var ext = (System.IO.Path.GetExtension(path) ?? "").ToLowerInvariant();
        if (new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp" }.Contains(ext)) return "Imagen";
        if (new[] { ".wav", ".ogg", ".mp3", ".flac" }.Contains(ext)) return "Sonido";
        if (new[] { ".json", ".lua" }.Contains(ext)) return "Datos";
        return "Otro";
    }

    private TabItem? GetTabByKind(string kind)
    {
        if (MainTabs?.Items == null) return null;
        foreach (TabItem tab in MainTabs.Items)
        {
            if (tab.Tag is string k && k == kind) return tab;
        }
        return null;
    }

    private void ApplyGameTabStateToScene(IReadOnlyList<RuntimeObjectState> states, IReadOnlyList<RuntimeScriptPropertySnapshot> scriptProps)
    {
        if (states == null) return;
        foreach (var s in states)
        {
            var inst = _objectLayer.Instances.FirstOrDefault(i => string.Equals(i.InstanceId, s.InstanceId, StringComparison.OrdinalIgnoreCase));
            if (inst == null) continue;
            inst.X = s.X;
            inst.Y = s.Y;
            inst.Rotation = s.Rotation;
            inst.ScaleX = s.ScaleX;
            inst.ScaleY = s.ScaleY;
        }

        if (scriptProps != null && scriptProps.Count > 0)
            MergeScriptPropertySnapshotsIntoLayer(_objectLayer, scriptProps);

        DrawMap();
        RefreshInspector();
        if (MapHierarchy != null)
            MapHierarchy.SetMapStructure(System.IO.Path.GetFileNameWithoutExtension(GetCurrentSceneMapPath()), _project.LayerNames, _objectLayer, _triggerZones, _visibleLayers, GetCurrentUIRoot());

        try
        {
            var objectsPath = GetCurrentSceneObjectsPath();
            var dir = System.IO.Path.GetDirectoryName(objectsPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            ObjectsSerialization.Save(_objectLayer, objectsPath);
            ProjectExplorer?.SetModified(objectsPath, false);
        }
        catch (Exception ex)
        {
            EditorLog.Error($"No se pudo guardar objetos.json: {ex.Message}", "Juego");
        }
    }

    private static void MergeScriptPropertySnapshotsIntoLayer(ObjectLayer layer, IReadOnlyList<RuntimeScriptPropertySnapshot> snaps)
    {
        foreach (var grp in snaps.GroupBy(s => s.InstanceId, StringComparer.OrdinalIgnoreCase))
        {
            var inst = layer.Instances.FirstOrDefault(i => string.Equals(i.InstanceId, grp.Key, StringComparison.OrdinalIgnoreCase));
            if (inst == null) continue;
            inst.ScriptProperties ??= new List<ScriptInstancePropertySet>();
            foreach (var byScript in grp.GroupBy(s => s.ScriptId, StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(byScript.Key)) continue;
                var set = inst.ScriptProperties.FirstOrDefault(sp => string.Equals(sp.ScriptId, byScript.Key, StringComparison.OrdinalIgnoreCase));
                if (set == null)
                {
                    set = new ScriptInstancePropertySet { ScriptId = byScript.Key, Properties = new List<ScriptPropertyEntry>() };
                    inst.ScriptProperties.Add(set);
                }

                set.Properties ??= new List<ScriptPropertyEntry>();
                foreach (var snap in byScript)
                {
                    if (string.IsNullOrEmpty(snap.Key)) continue;
                    var existing = set.Properties.FirstOrDefault(p => string.Equals(p.Key, snap.Key, StringComparison.OrdinalIgnoreCase));
                    if (existing == null)
                        set.Properties.Add(new ScriptPropertyEntry { Key = snap.Key, Type = snap.Type, Value = snap.Value });
                    else
                    {
                        existing.Type = snap.Type;
                        existing.Value = snap.Value;
                    }
                }
            }
        }
    }

    private GameTabContent CreateGameTabContent()
    {
        var content = new GameTabContent();
        content.SetContext(_project, () => _objectLayer, _scriptRegistry, GetCurrentUIRoot, () => _tileMap);
        content.ApplyStateToScene = ApplyGameTabStateToScene;
        return content;
    }

    private void AddOrSelectTabFromMenu(string kind)
    {
        AddOrSelectTab(kind);
    }

    private void SelectTabByKind(string kind)
    {
        var tab = GetTabByKind(kind);
        if (tab != null) tab.IsSelected = true;
    }

    private void UpdateTabHeaderDirty(TabItem? tabItem, string kind, bool isDirty)
    {
        if (tabItem == null) return;
        var baseName = TabDisplayNames.GetValueOrDefault(kind, kind);
        var prefix = TabIcons.TryGetValue(kind, out var icon) ? icon + " " : "";
        var text = prefix + baseName + (isDirty ? " *" : "");
        if (tabItem.Header is string)
            tabItem.Header = baseName + (isDirty ? " *" : "");
        else if (tabItem.Header is System.Windows.Controls.StackPanel panel && panel.Children.Count > 0 && panel.Children[0] is System.Windows.Controls.TextBlock tb)
            tb.Text = text;
    }

    private void UpdateMapTabDirtyState()
    {
        var mapModified = ProjectExplorer?.IsPathModified(GetCurrentSceneMapPath()) == true;
        var objectsModified = ProjectExplorer?.IsPathModified(GetCurrentSceneObjectsPath()) == true;
        UpdateTabHeaderDirty(TabMapa, "Mapa", mapModified || objectsModified);
    }

    private void AddOrSelectTab(string kind)
    {
        foreach (TabItem tab in MainTabs.Items)
        {
            if (tab.Tag is string k && k == kind)
            {
                tab.IsSelected = true;
                return;
            }
        }

        var tabItem = new TabItem { Tag = kind };
        var displayName = TabDisplayNames.GetValueOrDefault(kind, kind);
        var icon = TabIcons.TryGetValue(kind, out var iconStr) ? iconStr + " " : "";
        var headerPanel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
        headerPanel.Children.Add(new TextBlock { Text = icon + displayName, VerticalAlignment = VerticalAlignment.Center, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3)), Margin = new Thickness(0, 0, 8, 0) });
        var closeBtn = new System.Windows.Controls.Button { Content = "×", Width = 20, Height = 20, Padding = new Thickness(0), FontSize = 14, Foreground = System.Windows.Media.Brushes.White, Background = System.Windows.Media.Brushes.Transparent, Cursor = System.Windows.Input.Cursors.Hand };
        closeBtn.Click += (_, _) =>
        {
            if (tabItem.Content is GameTabContent toDispose)
                toDispose.Dispose();
            if (tabItem.Content is DebugTabContent debugTab)
                debugTab.StopRefresh();
            if (tabItem.Content is AudioTabContent)
                _audioSystem?.StopPreview();
            MainTabs.Items.Remove(tabItem);
            if (_openScenes.Count > 0 && _currentSceneIndex < _openScenes.Count)
                _openScenes[_currentSceneIndex].OpenOptionalTabKinds.Remove(kind);
            if (kind == "Explorador") _explorerPanel = ProjectExplorer;
            _ = SaveEditorLayoutAsync();
        };
        headerPanel.Children.Add(closeBtn);
        tabItem.Header = headerPanel;

        System.Windows.Controls.UserControl content;
        if (TabFactories.TryGetValue(kind, out var factory))
            content = factory(tabItem);
        else if (kind.StartsWith("UI:", StringComparison.OrdinalIgnoreCase))
        {
            var canvasId = kind.Length > 3 ? kind.Substring(3) : "";
            var canvas = GetCurrentUIRoot().GetCanvas(canvasId);
            var uiTab = new UITabContent();
            uiTab.SetCanvas(canvas ?? new UICanvas { Id = canvasId, Name = canvasId }, GetCurrentUIRoot);
            uiTab.ElementSelected += (_, selectedElement) =>
            {
                var selectedCanvas = canvas ?? FindCanvasForElement(selectedElement);
                _selection.SetUISelection(selectedCanvas, selectedElement);
                RefreshInspector();
                SyncSelectedUIElementInOpenTabs();
            };
            if (_selection.SelectedUIElement != null)
                uiTab.SetSelectedElement(_selection.SelectedUIElement);
            content = uiTab;
        }
        else
            content = CreatePlaceholder(kind);
        tabItem.Content = content;
        ConfigureTabAfterCreate(kind, tabItem, content);

        MainTabs.Items.Insert(MainTabs.Items.Count, tabItem);
        tabItem.IsSelected = true;
        if (_openScenes.Count > 0 && _currentSceneIndex < _openScenes.Count && kind != "Mapa" && kind != "Consola")
        {
            var list = _openScenes[_currentSceneIndex].OpenOptionalTabKinds;
            if (!list.Contains(kind)) list.Add(kind);
        }
        _ = SaveEditorLayoutAsync();
    }

    private void ConfigureTabAfterCreate(string kind, TabItem tabItem, System.Windows.Controls.UserControl content)
    {
        if (kind == "Scripts" && content is ScriptsTabContent scriptsContent)
        {
            scriptsContent.SetProjectDirectory(_project.ProjectDirectory ?? "");
            scriptsContent.ScriptSaved += OnLuaScriptSaved;
            scriptsContent.DirtyChanged += (_, isDirty) => UpdateTabHeaderDirty(tabItem, kind, isDirty);
        }
        if (kind == "Juego" && content is GameTabContent gameTab)
        {
            gameTab.ApplyStateToScene = ApplyGameTabStateToScene;
            gameTab.RequestOpenFileAtLine += (_, e) =>
            {
                AddOrSelectTab("Scripts");
                (GetTabByKind("Scripts")?.Content as ScriptsTabContent)?.OpenFileAtLine(e.FilePath, e.Line);
            };
        }
        if (kind == "Explorador")
            _explorerPanel = (content as ExplorerTabContent)?.GetExplorerPanel();
        if (kind == "Tiles" && content is TilesTabContent tilesContent)
        {
            tilesContent.SetProject(_project);
            tilesContent.TileSelected += (_, id) => { _selection.SetInspectorContextTile(id); RefreshInspector(); };
        }
        if (kind == "TileCreator" && content is TileCreatorTabContent tileCreatorContent)
        {
            tileCreatorContent.SetProjectDirectory(_project.ProjectDirectory ?? "");
            tileCreatorContent.DirtyChanged += (_, isDirty) => UpdateTabHeaderDirty(tabItem, kind, isDirty);
        }
        if (kind == "TileEditor" && content is TileEditorTabContent tileEditorContent)
        {
            tileEditorContent.SetProjectDirectory(_project.ProjectDirectory ?? "");
            tileEditorContent.DirtyChanged += (_, isDirty) => UpdateTabHeaderDirty(tabItem, kind, isDirty);
        }
        if (kind == "PaintCreator" && content is PaintCreatorTabContent paintCreatorContent)
        {
            paintCreatorContent.SetProjectDirectory(_project.ProjectDirectory ?? "");
            paintCreatorContent.DirtyChanged += (_, isDirty) => UpdateTabHeaderDirty(tabItem, kind, isDirty);
            paintCreatorContent.RequestSetProjectIcon += (_, relPath) => SetProjectIconPath(relPath);
        }
        if (kind == "PaintEditor" && content is PaintEditorTabContent paintEditorContent)
        {
            paintEditorContent.SetProjectDirectory(_project.ProjectDirectory ?? "");
            paintEditorContent.PaintSaved += (_, path) => DrawMap();
            paintEditorContent.DirtyChanged += (_, isDirty) => UpdateTabHeaderDirty(tabItem, kind, isDirty);
            paintEditorContent.RequestSetProjectIcon += (_, relPath) => SetProjectIconPath(relPath);
        }
        if (kind == "CollisionsEditor" && content is CollisionsEditorTabContent collisionsEditorContent)
        {
            collisionsEditorContent.SetProjectDirectory(_project.ProjectDirectory ?? "");
            collisionsEditorContent.DirtyChanged += (_, isDirty) => UpdateTabHeaderDirty(tabItem, kind, isDirty);
        }
        if (kind == "ScriptableTile" && content is ScriptableTileTabContent scriptableTileContent)
        {
            scriptableTileContent.SetProjectDirectory(_project.ProjectDirectory ?? "");
        }
        if (kind == "Animaciones" && content is AnimationsTabContent animContent)
        {
            animContent.AnimationSelected += (_, anim) => { _selection.SetInspectorContextAnimation(anim); RefreshInspector(); };
            try
            {
                var animPath = System.IO.Path.Combine(_project.ProjectDirectory ?? "", "animaciones.json");
                if (System.IO.File.Exists(animPath))
                    animContent.SetAnimations(AnimationSerialization.Load(animPath));
            }
            catch { /* ignore */ }
        }
        if (kind == "Debug" && content is DebugTabContent debugContent)
            debugContent.SetContext(GetCurrentDebugRunner);
        if (kind == "Audio" && content is AudioTabContent audioContent && _audioAssetRegistry != null && _audioSystem != null)
        {
            _audioSystem.StopPreview();
            audioContent.SetContext(_audioAssetRegistry, _audioSystem);
            audioContent.RequestPlayInGame += (id) => GetCurrentDebugRunner()?.PlayAudioInGame(id);
        }
    }

    private PlayModeRunner? GetCurrentDebugRunner()
    {
        if (_playModeRunner != null && _playModeRunner.IsRunning)
            return _playModeRunner;
        var selected = MainTabs?.SelectedItem as TabItem;
        if (selected?.Content is GameTabContent gameTab && gameTab.IsActiveAndRunning)
            return gameTab.GetRunner();
        return null;
    }

    private DebugTabContent CreateDebugTabContent()
    {
        return new DebugTabContent();
    }

    private AudioTabContent CreateAudioTabContent()
    {
        var content = new AudioTabContent();
        if (_audioAssetRegistry != null && _audioSystem != null)
            content.SetContext(_audioAssetRegistry, _audioSystem);
        return content;
    }

    private ExplorerTabContent CreateExplorerTabContent(TabItem tabItem)
    {
        var content = new ExplorerTabContent();
        _explorerPanel = content.GetExplorerPanel();
        _explorerPanel.SetProject(_project.ProjectDirectory, _project.Nombre);
        _explorerPanel.SetMetadataService(_explorerMetadataService);
        _explorerPanel.IsCompactMode = false;
        _explorerPanel.ApplyCompactMode();
        _explorerPanel.SelectionChanged += ProjectExplorer_OnSelectionChanged;
        _explorerPanel.RequestOpenInEditor += ProjectExplorer_OnRequestOpenInEditor;
        _explorerPanel.RequestOpenInCollisionsEditor += ProjectExplorer_OnRequestOpenInCollisionsEditor;
        _explorerPanel.RequestOpenInScriptableTile += ProjectExplorer_OnRequestOpenInScriptableTile;
        _explorerPanel.RequestCreateTileLayer += Explorer_OnRequestCreateTileLayer;
        _explorerPanel.RequestCreateObjectLayer += Explorer_OnRequestCreateObjectLayer;
        _explorerPanel.RequestCreateTriggerZone += Explorer_OnRequestCreateTriggerZone;
        _explorerPanel.RequestCreateObject += Explorer_OnRequestCreateObject;
        _explorerPanel.LuaScriptRegistered += ProjectExplorer_OnLuaScriptRegistered;
        _explorerPanel.ScriptsRegistryChanged += ProjectExplorer_OnScriptsRegistryChanged;
        return content;
    }

    private bool ReloadScriptRegistryFromDisk()
    {
        if (string.IsNullOrEmpty(_project.ProjectDirectory)) return false;
        try
        {
            _scriptRegistry = ScriptSerialization.Load(_project.ScriptsPath);
            return true;
        }
        catch (Exception ex)
        {
            EditorLog.Error($"Error al recargar scripts.json: {ex.Message}", "scripts.json");
            return false;
        }
    }

    private void RefreshScriptsTabList(string? selectScriptId = null)
    {
        if (GetTabByKind("Scripts")?.Content is not ScriptsTabContent scriptsContent) return;
        scriptsContent.SetProjectDirectory(_project.ProjectDirectory ?? "");
        scriptsContent.SetScripts(_scriptRegistry.GetAll(), selectScriptId);
    }

    private void ProjectExplorer_OnScriptsRegistryChanged(object? sender, EventArgs e)
    {
        if (!ReloadScriptRegistryFromDisk()) return;
        SyncScriptRegistryToGameTabs();
        RefreshScriptsTabList(selectScriptId: null);
        RefreshInspector();
    }

    private void ProjectExplorer_OnLuaScriptRegistered(object? sender, ScriptRegisteredEventArgs e)
    {
        if (!ReloadScriptRegistryFromDisk()) return;
        SyncScriptRegistryToGameTabs();
        AddOrSelectTab("Scripts");
        RefreshScriptsTabList(e.ScriptId);
        RefreshInspector();
        EditorLog.Info($"Lista de scripts actualizada; seleccionado id={e.ScriptId}.", "Scripts");
    }

    private void SyncScriptRegistryToGameTabs()
    {
        if (MainTabs?.Items == null) return;
        foreach (var item in MainTabs.Items)
        {
            if (item is TabItem tab && tab.Content is GameTabContent gameTab)
                gameTab.UpdateScriptRegistry(_scriptRegistry);
        }
    }

    private void Explorer_OnRequestCreateTileLayer(object? sender, EventArgs e) => MapHierarchy_OnRequestAddLayer(sender, e);
    private void Explorer_OnRequestCreateObjectLayer(object? sender, EventArgs e)
    {
        _project.LayerNames ??= new List<string> { "Suelo" };
        if (!_project.LayerNames.Any(l => string.Equals(l, "Objects", StringComparison.OrdinalIgnoreCase)))
        {
            _project.LayerNames.Add("Objects");
            var projectPath = GetProjectFilePath();
            if (projectPath == null)
                EditorLog.Toast("No se pudo determinar la ruta del proyecto.", LogLevel.Error, "Proyecto");
            else if (File.Exists(projectPath))
                ProjectSerialization.Save(_project, projectPath);
        }
        MapHierarchy.SetMapStructure(System.IO.Path.GetFileNameWithoutExtension(GetCurrentSceneMapPath()), _project.LayerNames, _objectLayer, _triggerZones, _visibleLayers, GetCurrentUIRoot());
    }
    private void Explorer_OnRequestCreateTriggerZone(object? sender, EventArgs e)
    {
        var zone = new TriggerZone { Id = Guid.NewGuid().ToString("N"), Nombre = "Nueva zona", Width = 2, Height = 2 };
        _triggerZones.Add(zone);
        TriggerZoneSerialization.Save(_triggerZones, _project.TriggerZonesPath);
        MapHierarchy.SetMapStructure(System.IO.Path.GetFileNameWithoutExtension(GetCurrentSceneMapPath()), _project.LayerNames, _objectLayer, _triggerZones, _visibleLayers, GetCurrentUIRoot());
    }
    private void Explorer_OnRequestCreateObject(object? sender, EventArgs e) => MapHierarchy_OnRequestCreateObject(sender, e);

    private ScriptsTabContent CreateScriptsTabContent()
    {
        var content = new ScriptsTabContent();
        content.SetScripts(_scriptRegistry?.GetAll());
        return content;
    }

    private ObjectsTabContent CreateObjectsTabContent()
    {
        var content = new ObjectsTabContent();
        content.SetObjects(_objectLayer?.Definitions?.Values);
        return content;
    }

    private PlaceholderTabContent CreatePlaceholder(string kind)
    {
        var content = new PlaceholderTabContent();
        var (title, desc) = kind switch
        {
            "Tiles" => ("Tiles", "Importar tileset, cortar tiles, asignar colisión y propiedades (solid, interactable, damage, water, ladder)."),
            "Animaciones" => ("Animaciones", "Crear animación, añadir frames, FPS, loop. Ej: animatrónico_idle, door_open."),
            "Escenas" => ("Escenas", "Lista de mapas o niveles. Crear, duplicar, cambiar mapa inicial."),
            "Variables globales" => ("Variables globales", "power = 100, night = 1, difficulty, doors_closed. Los scripts acceden a ellas."),
            "Eventos del juego" => ("Eventos del juego", "Inicio de noche, 6 AM, Power agotado, Jump scare, cambio de cámara (tipo FNAF)."),
            "Audio" => ("Audio", "SFX, ambiente, música, UI. Preview, asignar a eventos, volumen."),
            "UI" => ("UI / Interfaces", "HUD, menú, pantalla de cámaras, Game Over. Texto, botones, imágenes, barras."),
            _ => (kind, "Próximamente.")
        };
        content.SetContent(title, desc);
        return content;
    }

    private void MainTabs_PreviewDragOver(object sender, System.Windows.DragEventArgs e)
    {
        if (MainTabs == null || !e.Data.GetDataPresent(ObjectsTabContent.DataFormatObjectDefinitionId)) return;
        var pos = e.GetPosition(MainTabs);
        var hit = VisualTreeHelper.HitTest(MainTabs, pos);
        if (hit?.VisualHit == null) return;
        var tabItem = FindVisualAncestor<TabItem>(hit.VisualHit as System.Windows.DependencyObject);
        if (tabItem != null && tabItem != MainTabs.SelectedItem)
        {
            MainTabs.SelectedItem = tabItem;
        }
        e.Effects = System.Windows.DragDropEffects.Copy;
        e.Handled = false;
    }

    private static T? FindVisualAncestor<T>(System.Windows.DependencyObject? child) where T : System.Windows.DependencyObject
    {
        while (child != null)
        {
            if (child is T t) return t;
            child = VisualTreeHelper.GetParent(child);
        }
        return null;
    }

    private void MainTabs_OnSelectionChanged(object? sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        var selected = MainTabs?.SelectedItem as TabItem;
        if (MainTabs?.Items != null)
        {
            foreach (var item in MainTabs.Items)
            {
                if (item is TabItem t && t.Content is GameTabContent g)
                {
                    if (t == selected)
                        g.ResumeRunner();
                    else
                        g.PauseRunner();
                }
            }
        }
        if (MenuVerConsola != null)
            MenuVerConsola.IsChecked = selected == TabConsola;
        if (MenuVerJuego != null)
            MenuVerJuego.IsChecked = (selected?.Tag as string) == "Juego";
        if (selected == TabMapa && TabMapa?.Content is System.Windows.FrameworkElement)
            MapCanvas?.Focus();
        if (selected?.Content is GameTabContent gameTabContent)
            gameTabContent.Focus();
        UpdateToolbarVisibility();
        UpdateMapTabDirtyState();
        _ = SaveEditorStateAsync();
        _ = SaveEditorLayoutAsync();
    }

    private void UpdateToolbarVisibility()
    {
        var tag = (MainTabs?.SelectedItem as TabItem)?.Tag as string ?? "";
        if (ToolbarMapa != null) ToolbarMapa.Visibility = tag == "Mapa" ? Visibility.Visible : Visibility.Collapsed;
        if (ToolbarScripts != null) ToolbarScripts.Visibility = tag == "Scripts" ? Visibility.Visible : Visibility.Collapsed;
        if (ToolbarTilesAnimaciones != null)
        {
            ToolbarTilesAnimaciones.Visibility = (tag == "Tiles" || tag == "Animaciones") ? Visibility.Visible : Visibility.Collapsed;
            if (ToolbarTilesAnimacionesLabel != null) ToolbarTilesAnimacionesLabel.Text = tag == "Animaciones" ? "Animaciones" : "Tiles";
        }
        if (ToolbarMinimal != null) ToolbarMinimal.Visibility = (tag == "Explorador" || tag == "Consola" || tag == "Juego") ? Visibility.Visible : Visibility.Collapsed;
        if (ToolbarMinimalLabel != null)
            ToolbarMinimalLabel.Text = tag == "Consola" ? "Consola · Lua, editor y proyecto (filtros + doble clic → script)"
                : tag == "Juego" ? "Juego · Play embebido (sandbox con escena actual)"
                : "Explorador · Favoritos, recientes y árbol del proyecto";
    }

    private void ToolbarScriptRun_OnClick(object sender, RoutedEventArgs e)
    {
        EditorLog.Info("Ejecutar script: en desarrollo.", "Scripts");
    }

    private void ToolbarScriptSave_OnClick(object sender, RoutedEventArgs e)
    {
        var scriptsContent = GetTabByKind("Scripts")?.Content as ScriptsTabContent;
        if (scriptsContent?.GetEditorControl()?.SaveFile() == true)
            return;
        MenuGuardarMapa_OnClick(sender, e);
    }

    private void OnLuaScriptSaved(object? sender, string fullPath)
    {
        try
        {
            var dir = _project.ProjectDirectory ?? "";
            if (string.IsNullOrEmpty(dir) || !fullPath.StartsWith(dir, StringComparison.OrdinalIgnoreCase))
                return;
            var rel = System.IO.Path.GetRelativePath(dir, fullPath).Replace('\\', '/');
            var selectedTab = MainTabs?.SelectedItem as TabItem;
            var gameContent = selectedTab?.Content as GameTabContent;
            if (gameContent != null && gameContent.IsActiveAndRunning)
            {
                gameContent.OnScriptSaved(rel);
                return;
            }
            OnLuaScriptSavedForReload?.Invoke(rel);
        }
        catch { /* ignore */ }
    }

    private void OpenConsoleTab()
    {
        if (MenuVerConsola != null) MenuVerConsola.IsChecked = true;
        if (TabConsola != null && MainTabs != null)
            MainTabs.SelectedItem = TabConsola;
    }

    private void OnRequestOpenFileAtLine(object? sender, (string FilePath, int Line) e)
    {
        AddOrSelectTab("Scripts");
        (GetTabByKind("Scripts")?.Content as ScriptsTabContent)?.OpenFileAtLine(e.FilePath, e.Line);
    }

    private void UpdatePlayModeUI()
    {
        var running = _playModeRunner?.IsRunning ?? false;
        var paused = _playModeRunner?.IsPaused ?? false;
        if (BtnPlay != null) BtnPlay.Visibility = running ? Visibility.Collapsed : Visibility.Visible;
        if (BtnPlayMainScene != null) BtnPlayMainScene.Visibility = running ? Visibility.Collapsed : Visibility.Visible;
        if (BtnStopPlay != null) BtnStopPlay.Visibility = running ? Visibility.Visible : Visibility.Collapsed;
        if (BtnPausePlay != null)
        {
            BtnPausePlay.Visibility = running ? Visibility.Visible : Visibility.Collapsed;
            BtnPausePlay.Content = paused ? "▶ Reanudar" : "⏸ Pausar";
        }
        if (MenuIniciarJuego != null) MenuIniciarJuego.IsEnabled = !running;
        if (MenuIniciarJuegoMain != null) MenuIniciarJuegoMain.IsEnabled = !running;
        if (MenuDetenerJuego != null) MenuDetenerJuego.IsEnabled = running;
        if (MenuPausarJuego != null) { MenuPausarJuego.IsEnabled = running && !paused; MenuPausarJuego.Header = "Pausar"; }
        if (MenuReanudarJuego != null) { MenuReanudarJuego.IsEnabled = running && paused; MenuReanudarJuego.Header = "Reanudar"; }
        if (PlayHierarchyPanel != null)
        {
            PlayHierarchyPanel.Visibility = running ? Visibility.Visible : Visibility.Collapsed;
            PlayHierarchyPanel.SetObjectNames(running && _playModeRunner != null ? _playModeRunner.GetSceneObjectNames() : null);
        }
    }

    private void StartPlay(bool useMainScene)
    {
        if (useMainScene)
        {
            var mainPath = _project.MainSceneObjectsPath;
            if (string.IsNullOrWhiteSpace(mainPath) || !System.IO.File.Exists(mainPath))
            {
                EditorLog.Toast("Configura la escena Start en Archivo → Configuración del proyecto (ej: objetos.json).", LogLevel.Warning, "Play");
                return;
            }
        }
        CanvasControllerLuaTemplate.EnsureCanvasControllerScriptIfNeeded(_project.ProjectDirectory, _objectLayer);
        var gameWindow = new GamePlayWindow(_project, _objectLayer, _scriptRegistry, useMainScene) { Owner = this };
        gameWindow.Show();
    }

    private void BtnPlay_OnClick(object sender, RoutedEventArgs e) => StartPlay(useMainScene: false);
    private void BtnPlayMainScene_OnClick(object sender, RoutedEventArgs e) => StartPlay(useMainScene: true);

    private void BtnStopPlay_OnClick(object sender, RoutedEventArgs e)
    {
        _playModeRunner?.Stop();
        _playModeRunner = null;
        OnLuaScriptSavedForReload = null;
        SetLuaScriptRuntimeForConsole(null);
        UpdatePlayModeUI();
    }

    private void BtnPausePlay_OnClick(object sender, RoutedEventArgs e)
    {
        if (_playModeRunner == null) return;
        if (_playModeRunner.IsPaused) _playModeRunner.Resume(); else _playModeRunner.Pause();
        UpdatePlayModeUI();
    }

    private void MenuIniciarJuego_OnClick(object sender, RoutedEventArgs e) => StartPlay(useMainScene: false);
    private void MenuIniciarJuegoMain_OnClick(object sender, RoutedEventArgs e) => StartPlay(useMainScene: true);
    private void MenuDetenerJuego_OnClick(object sender, RoutedEventArgs e) => BtnStopPlay_OnClick(sender, e);
    private void MenuPausarJuego_OnClick(object sender, RoutedEventArgs e) => BtnPausePlay_OnClick(sender, e);
    private void MenuReanudarJuego_OnClick(object sender, RoutedEventArgs e) => BtnPausePlay_OnClick(sender, e);

    private void MenuAssetsLibrary_OnClick(object sender, RoutedEventArgs e)
    {
        var w = new GlobalLibraryBrowserWindow(_project) { Owner = this };
        w.ShowDialog();
    }

    private void MenuExportarParcial_OnClick(object sender, RoutedEventArgs e)
    {
        var w = new ExportPartialWindow(_project, _tileMap, _objectLayer, _scriptRegistry) { Owner = this };
        w.ShowDialog();
    }

    private void MenuVerificarIntegridad_OnClick(object sender, RoutedEventArgs e)
    {
        var ok = ProjectIntegrityChecker.Run(_project, _tileMap, _objectLayer, _scriptRegistry);
        EditorLog.Toast(ok ? "Integridad correcta." : "Se encontraron problemas. Revisa el log.", ok ? LogLevel.Info : LogLevel.Warning, "Integridad");
    }

    private void MenuLimpiarHuerfanos_OnClick(object sender, RoutedEventArgs e)
    {
        var projectDir = _project.ProjectDirectory ?? "";
        if (string.IsNullOrEmpty(projectDir) || !Directory.Exists(projectDir)) return;

        var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (_project.Scenes != null)
        {
            foreach (var s in _project.Scenes)
            {
                if (!string.IsNullOrEmpty(s.MapPathRelative))
                    referenced.Add(System.IO.Path.GetFullPath(System.IO.Path.Combine(projectDir, s.MapPathRelative)));
                if (!string.IsNullOrEmpty(s.ObjectsPathRelative))
                    referenced.Add(System.IO.Path.GetFullPath(System.IO.Path.Combine(projectDir, s.ObjectsPathRelative)));
            }
        }
        var mapsDir = System.IO.Path.Combine(projectDir, "Maps");
        var objectsDir = System.IO.Path.Combine(projectDir, "Objects");
        var orphans = new List<string>();
        if (Directory.Exists(mapsDir))
        {
            foreach (var f in Directory.EnumerateFiles(mapsDir, "*.json", SearchOption.AllDirectories))
                if (!referenced.Contains(System.IO.Path.GetFullPath(f))) orphans.Add(f);
            foreach (var f in Directory.EnumerateFiles(mapsDir, "*" + NewProjectStructure.MapFileExtension, SearchOption.AllDirectories))
                if (!referenced.Contains(System.IO.Path.GetFullPath(f))) orphans.Add(f);
        }
        if (Directory.Exists(objectsDir))
        {
            foreach (var f in Directory.EnumerateFiles(objectsDir, "*.json", SearchOption.AllDirectories))
                if (!referenced.Contains(System.IO.Path.GetFullPath(f))) orphans.Add(f);
            foreach (var f in Directory.EnumerateFiles(objectsDir, "*" + NewProjectStructure.ObjectsFileExtension, SearchOption.AllDirectories))
                if (!referenced.Contains(System.IO.Path.GetFullPath(f))) orphans.Add(f);
        }

        if (orphans.Count == 0)
        {
            EditorLog.Toast("No hay archivos huérfanos (mapas u objetos sin escena).", LogLevel.Info, "Proyecto");
            return;
        }
        var dlg = new CleanOrphansDialog(orphans) { Owner = this };
        dlg.ShowDialog();
        if (dlg.PathsToDelete.Count == 0) return;
        var deleted = 0;
        foreach (var f in dlg.PathsToDelete)
        {
            try { if (File.Exists(f)) { File.Delete(f); deleted++; } }
            catch (Exception ex) { EditorLog.Warning($"No se pudo eliminar {System.IO.Path.GetFileName(f)}: {ex.Message}", "Proyecto"); }
        }
        EditorLog.Toast(deleted + " archivo(s) huérfano(s) eliminados.", LogLevel.Info, "Proyecto");
    }

    private void MenuGuardarSnapshot_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var name = MapSnapshotService.SaveSnapshot(_project.ProjectDirectory ?? "", _tileMap, _objectLayer);
            EditorLog.Toast($"Snapshot guardado: {name}", LogLevel.Info, "Snapshot");
        }
        catch (Exception ex)
        {
            EditorLog.Error("Snapshot: " + ex.Message, "Snapshot");
            System.Windows.MessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MenuCargarSnapshot_OnClick(object sender, RoutedEventArgs e)
    {
        var list = MapSnapshotService.ListSnapshots(_project.ProjectDirectory ?? "");
        if (list.Count == 0)
        {
            EditorLog.Toast("No hay snapshots guardados.", LogLevel.Info, "Snapshot");
            return;
        }
        var w = new SnapshotPickerWindow(list) { Owner = this };
        if (w.ShowDialog() != true || string.IsNullOrEmpty(w.SelectedName)) return;
        if (!MapSnapshotService.LoadSnapshot(_project.ProjectDirectory ?? "", w.SelectedName, out var map, out var objects) || map == null || objects == null)
        {
            System.Windows.MessageBox.Show(this, "Error al cargar el snapshot.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        _tileMap = map;
        _objectLayer = objects;
        RefreshLayersPanelFromTileMap();
        _history.Clear();
        UpdateUndoRedoMenu();
        CmbObjectDef.ItemsSource = _objectLayer.Definitions.Values.ToList();
        DrawMap();
        RefreshInspector();
        EditorLog.Info("Snapshot cargado; estado actual reemplazado.", "Snapshot");
    }

    private void MenuAtajos_OnClick(object sender, RoutedEventArgs e)
    {
        new ShortcutsWindow { Owner = this }.ShowDialog();
    }

    private void PaletteTile_StartDrag(object sender, MouseButtonEventArgs e)
    {
        if (sender is not System.Windows.FrameworkElement fe || fe.Tag is not string tag) return;
        if (int.TryParse(tag, out int idx) && idx >= 0 && idx <= 3 && CmbTileType != null)
        {
            _selectedTileType = (TileType)idx;
            CmbTileType.SelectedIndex = idx;
            UpdatePaletteSelection();
        }
        var data = new System.Windows.DataObject("FUEngine.TileType", tag);
        System.Windows.DragDrop.DoDragDrop(fe, data, System.Windows.DragDropEffects.Copy);
    }

    private void MapCanvas_OnDragOver(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent("FUEngine.TileType") || e.Data.GetDataPresent(ProjectExplorerPanel.DataFormatAssetPath)
            || e.Data.GetDataPresent(ObjectsTabContent.DataFormatObjectDefinitionId))
            e.Effects = System.Windows.DragDropEffects.Copy;
        else
            e.Effects = System.Windows.DragDropEffects.None;
        e.Handled = true;
    }

    private void MapCanvas_OnDrop(object sender, System.Windows.DragEventArgs e)
    {
        var pos = e.GetPosition(MapCanvas);
        var (tx, ty) = GetTileAt(pos);

        if (e.Data.GetDataPresent(ObjectsTabContent.DataFormatObjectDefinitionId))
        {
            var defId = e.Data.GetData(ObjectsTabContent.DataFormatObjectDefinitionId) as string;
            if (!string.IsNullOrEmpty(defId) && _objectLayer?.Definitions?.ContainsKey(defId) == true)
            {
                var def = _objectLayer.Definitions[defId];
                var inst = new ObjectInstance
                {
                    DefinitionId = defId,
                    X = tx,
                    Y = ty,
                    Nombre = def?.Nombre ?? defId
                };
                _history.Push(new AddObjectCommand(_objectLayer, inst));
                ProjectExplorer?.SetModified(GetCurrentSceneObjectsPath(), true);
                MapHierarchy?.SetMapStructure(System.IO.Path.GetFileNameWithoutExtension(GetCurrentSceneMapPath()), _project.LayerNames, _objectLayer, _triggerZones, _visibleLayers);
                DrawMap();
                RefreshInspector();
                e.Handled = true;
                return;
            }
        }

        if (e.Data.GetDataPresent(ProjectExplorerPanel.DataFormatAssetPath))
        {
            var assetPath = e.Data.GetData(ProjectExplorerPanel.DataFormatAssetPath) as string;
            if (!string.IsNullOrEmpty(assetPath))
            {
                var defId = (_objectLayer?.Definitions?.Keys?.FirstOrDefault()) ?? "";
                if (string.IsNullOrEmpty(defId)) defId = "default";
                var inst = new ObjectInstance
                {
                    DefinitionId = defId,
                    X = tx,
                    Y = ty,
                    Nombre = System.IO.Path.GetFileNameWithoutExtension(assetPath)
                };
                if (_objectLayer == null) return;
                _history.Push(new AddObjectCommand(_objectLayer, inst));
                ProjectExplorer.SetModified(GetCurrentSceneObjectsPath(), true);
                MapHierarchy.SetMapStructure(System.IO.Path.GetFileNameWithoutExtension(GetCurrentSceneMapPath()), _project.LayerNames, _objectLayer, _triggerZones, _visibleLayers, GetCurrentUIRoot());
                DrawMap();
                RefreshInspector();
            }
            e.Handled = true;
            return;
        }

        if (!e.Data.GetDataPresent("FUEngine.TileType")) return;
        var tag = e.Data.GetData("FUEngine.TileType") as string;
        if (string.IsNullOrEmpty(tag) || !int.TryParse(tag, out int tileType)) return;
        if (IsActiveLayerLocked()) return;
        var layerIdx = GetActiveLayerIndex();
        _tileMap.TryGetTile(layerIdx, tx, ty, out var prev);
        var tileTypeEnum = (TileType)tileType;
        var newTile = CreateTileData(tileTypeEnum);
        _history.Push(new PaintTileCommand(_tileMap, layerIdx, tx, ty, prev, newTile));
        ProjectExplorer.SetModified(GetCurrentSceneMapPath(), true);
        DrawMap();
        e.Handled = true;
    }

    private void MenuSimular_OnClick(object sender, RoutedEventArgs e)
    {
        var sim = new SimulateWindow(_project, _tileMap, _objectLayer) { Owner = this };
        sim.ShowDialog();
    }

    private void MenuExportBuild_OnClick(object sender, RoutedEventArgs e)
    {
        var dir = _project.ProjectDirectory ?? "";
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
        {
            System.Windows.MessageBox.Show(this, "El proyecto no tiene una carpeta válida en disco.", "Exportar build",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dlg = new BuildExportWindow(_project, dir) { Owner = this };
        dlg.ShowDialog();
    }

    private void CmbTileType_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (CmbTileType?.SelectedIndex is int i and >= 0)
            _selectedTileType = (TileType)i;
        UpdatePaletteSelection();
        UpdateTilePreview();
        RefreshInspector();
    }

    private void UpdateTilePreview()
    {
        if (TilePreview == null) return;
        var colors = new[]
        {
            System.Windows.Media.Color.FromRgb(0x50, 0x50, 0x50),
            System.Windows.Media.Color.FromRgb(0x78, 0x50, 0x3c),
            System.Windows.Media.Color.FromRgb(0x5a, 0x5a, 0x78),
            System.Windows.Media.Color.FromRgb(0x64, 0x3c, 0x64)
        };
        var idx = Math.Clamp((int)_selectedTileType, 0, colors.Length - 1);
        TilePreview.Background = new SolidColorBrush(colors[idx]);
    }

    private void CmbObjectDef_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        UpdateObjectPreview();
    }

    private void UpdateObjectPreview()
    {
        if (ObjectPreviewText == null) return;
        if (CmbObjectDef?.SelectedItem is ObjectDefinition def && !string.IsNullOrEmpty(def.Nombre))
            ObjectPreviewText.Text = def.Nombre[0].ToString().ToUpperInvariant();
        else
            ObjectPreviewText.Text = "?";
    }

    private void UpdatePaletteSelection()
    {
        var accent = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x58, 0xa6, 0xff));
        var unselected = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x30, 0x36, 0x3d));
        var tiles = new[] { PaletteSuelo, PalettePared, PaletteObjeto, PaletteEspecial };
        for (int i = 0; i < tiles.Length && i < 4; i++)
        {
            if (tiles[i] == null) continue;
            tiles[i].BorderBrush = _selectedTileType == (TileType)i ? accent : unselected;
            tiles[i].BorderThickness = _selectedTileType == (TileType)i ? new Thickness(2) : new Thickness(1);
        }
    }

    private void CmbCapaVisible_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        var idx = CmbCapaVisible?.SelectedIndex ?? 0;
        if (idx < 0 || _tileMap?.Layers == null) return;
        if (idx >= _tileMap.Layers.Count) idx = Math.Max(0, _tileMap.Layers.Count - 1);
        _activeLayerIndex = idx;
        if (LayersPanel != null) LayersPanel.ActiveLayerIndex = idx;
        BuildVisibleLayersFromTileMap();
        DrawMap();
    }

    private int GetActiveLayerIndex()
    {
        if (_tileMap?.Layers == null || _tileMap.Layers.Count == 0) return 0;
        return Math.Clamp(LayersPanel?.ActiveLayerIndex ?? _activeLayerIndex, 0, _tileMap.Layers.Count - 1);
    }

    private bool IsActiveLayerLocked()
    {
        var idx = GetActiveLayerIndex();
        return _tileMap != null && idx < _tileMap.Layers.Count && _tileMap.Layers[idx].IsLocked;
    }

    private void SyncLayerComboFromTileMap()
    {
        if (CmbCapaVisible == null || _tileMap?.Layers == null) return;
        var names = _tileMap.Layers.Select(l => l.Name).ToList();
        CmbCapaVisible.ItemsSource = names;
        if (names.Count > 0)
            CmbCapaVisible.SelectedIndex = Math.Clamp(_activeLayerIndex, 0, names.Count - 1);
    }

    private void SyncProjectLayerNamesFromTileMap()
    {
        if (_tileMap?.Layers == null) return;
        _project.LayerNames = _tileMap.Layers.Select(l => l.Name).ToList();
        if (_project.LayerNames.Count == 0) _project.LayerNames.Add("Suelo");
    }

    private void BuildVisibleLayersFromTileMap()
    {
        _visibleLayers.Clear();
        if (_tileMap?.Layers == null) return;
        for (int i = 0; i < _tileMap.Layers.Count; i++)
            if (_tileMap.Layers[i].IsVisible) _visibleLayers.Add(i);
    }

    private void LayersPanel_OnActiveLayerChanged(object? sender, int index)
    {
        _activeLayerIndex = index;
        if (CmbCapaVisible != null && _tileMap?.Layers != null && index >= 0 && index < _tileMap.Layers.Count)
            CmbCapaVisible.SelectedIndex = index;
        DrawMap();
    }

    private void LayersPanel_OnLayerSelected(object? sender, MapLayerDescriptor descriptor)
    {
        _selection.SetInspectorContextLayer(descriptor);
        RefreshInspector();
    }

    private void LayersPanel_OnLayerVisibilityToggled(object? sender, (int layerIndex, bool visible) e)
    {
        BuildVisibleLayersFromTileMap();
        DrawMap();
    }

    private void RefreshLayersPanelFromTileMap()
    {
        LayersPanel?.SetTileMap(_tileMap);
        SyncLayerComboFromTileMap();
        BuildVisibleLayersFromTileMap();
        SyncProjectLayerNamesFromTileMap();
    }

    private void BtnZoom_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string tag)
        {
            _zoom = tag == "+" ? Math.Min(4, _zoom + 0.25) : Math.Max(0.25, _zoom - 0.25);
            ApplyZoom();
        }
    }

    private void ApplyZoom()
    {
        if (TxtZoom != null) TxtZoom.Text = $"{(int)(_zoom * 100)}%";
        if (MapCanvas != null) MapCanvas.LayoutTransform = new System.Windows.Media.ScaleTransform(_zoom, _zoom);
    }

    private void ScrollViewer_OnPreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (sender is not System.Windows.Controls.ScrollViewer sv) return;
        e.Handled = true;
        var step = 0.15 * _zoomWheelSensitivity;
        _zoom = e.Delta > 0 ? Math.Min(4, _zoom + step) : Math.Max(0.25, _zoom - step);
        ApplyZoom();
    }

    private void ScrollViewer_OnPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not System.Windows.Controls.ScrollViewer sv) return;
        if (_spacePanHeld && e.ChangedButton == System.Windows.Input.MouseButton.Left)
        {
            _panDragging = true;
            _panStartPoint = e.GetPosition(sv);
            _panStartScrollX = sv.HorizontalOffset;
            _panStartScrollY = sv.VerticalOffset;
            sv.CaptureMouse();
            e.Handled = true;
            return;
        }
        if (e.ChangedButton != System.Windows.Input.MouseButton.Middle) return;
        _panDragging = true;
        _panStartPoint = e.GetPosition(sv);
        _panStartScrollX = sv.HorizontalOffset;
        _panStartScrollY = sv.VerticalOffset;
        sv.CaptureMouse();
        e.Handled = true;
    }

    private void ScrollViewer_OnPreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_panDragging || !_panStartPoint.HasValue || sender is not System.Windows.Controls.ScrollViewer sv) return;
        var now = e.GetPosition(sv);
        sv.ScrollToHorizontalOffset(_panStartScrollX + (_panStartPoint.Value.X - now.X));
        sv.ScrollToVerticalOffset(_panStartScrollY + (_panStartPoint.Value.Y - now.Y));
    }

    private void ScrollViewer_OnPreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Middle && sender is System.Windows.Controls.ScrollViewer sv)
        {
            _panDragging = false;
            _panStartPoint = null;
            sv.ReleaseMouseCapture();
        }
    }

    private void ChkGridVisible_OnChanged(object sender, RoutedEventArgs e)
    {
        _gridVisible = ChkGridVisible?.IsChecked == true;
        DrawMap();
    }

    private void ChkShowCoords_OnChanged(object sender, RoutedEventArgs e)
    {
        _showTileCoordinates = ChkShowCoords?.IsChecked == true;
        DrawMap();
    }

    private void ChkShowVisibleArea_OnChanged(object sender, RoutedEventArgs e)
    {
        _showVisibleArea = ChkShowVisibleArea?.IsChecked == true;
        DrawMap();
    }

    private void ChkShowStreamingGizmos_OnChanged(object sender, RoutedEventArgs e)
    {
        _showStreamingGizmos = ChkShowStreamingGizmos?.IsChecked == true;
        DrawMap();
    }

    private void ChkShowColliders_OnChanged(object sender, RoutedEventArgs e)
    {
        _showColliders = ChkShowColliders?.IsChecked == true;
        DrawMap();
    }

    private void BtnGoToOrigin_OnClick(object sender, RoutedEventArgs e)
    {
        if (ScrollViewer == null || MapCanvas == null) return;
        _measureStart = null;
        _measureEnd = null;
        var tileSize = _project?.TileSize ?? 16;
        double cx = (0 - _canvasMinWx) * tileSize * _zoom;
        double cy = (0 - _canvasMinWy) * tileSize * _zoom;
        double vw = ScrollViewer.ViewportWidth;
        double vh = ScrollViewer.ViewportHeight;
        double h = Math.Max(0, cx - vw / 2);
        double v = Math.Max(0, cy - vh / 2);
        ScrollViewer.ScrollToHorizontalOffset(Math.Min(h, ScrollViewer.ScrollableWidth));
        ScrollViewer.ScrollToVerticalOffset(Math.Min(v, ScrollViewer.ScrollableHeight));
        DrawMap();
    }

    private void CmbGridColor_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (CmbGridColor?.SelectedItem is System.Windows.Controls.ComboBoxItem item && item.Tag is string hex && hex.StartsWith("#") && hex.Length == 7)
        {
            _gridColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
            DrawMap();
        }
    }

    private void ChkSnap_OnChanged(object sender, RoutedEventArgs e)
    {
        _snapToGrid = ChkSnap?.IsChecked == true;
    }

    private void UpdateTileSelectionToolbarVisibility()
    {
        var hasSelection = _selection.HasTileSelection;
        var vis = hasSelection ? Visibility.Visible : Visibility.Collapsed;
        if (LblSelection != null) LblSelection.Visibility = vis;
        if (BtnRotate90 != null) BtnRotate90.Visibility = vis;
        if (BtnRotate180 != null) BtnRotate180.Visibility = vis;
        if (BtnFlipH != null) BtnFlipH.Visibility = vis;
        if (BtnFlipV != null) BtnFlipV.Visibility = vis;
        if (BtnFillSelection != null) BtnFillSelection.Visibility = vis;
        if (BtnCopySelection != null) BtnCopySelection.Visibility = vis;
        if (BtnPasteSelection != null) BtnPasteSelection.Visibility = vis;
        if (BtnDuplicateZone != null) BtnDuplicateZone.Visibility = vis;
        UpdateZoneMenuState();
    }

    private void BtnTransformSelection_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_selection.HasTileSelection) return;
        if (sender is not System.Windows.Controls.Button btn || btn.Tag is not string tag) return;
        var mode = tag switch
        {
            "Rotate90" => TileSelectionTransformMode.Rotate90CW,
            "Rotate180" => TileSelectionTransformMode.Rotate180,
            "FlipH" => TileSelectionTransformMode.FlipH,
            "FlipV" => TileSelectionTransformMode.FlipV,
            _ => (TileSelectionTransformMode?)null
        };
        if (mode == null) return;
        if (!_selection.HasTileSelection) return;
        if (IsActiveLayerLocked()) return;
        var rect = _selection.TileSelection!.Value;
        _history.Push(new TransformTileSelectionCommand(_tileMap, GetActiveLayerIndex(), rect.MinTx, rect.MinTy, rect.MaxTx, rect.MaxTy, mode.Value));
        _selection.ClearTileSelection();
        ProjectExplorer.SetModified(GetCurrentSceneMapPath(), true);
        DrawMap();
        UpdateTileSelectionToolbarVisibility();
    }

    private void BtnFillSelection_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_selection.HasTileSelection) return;
        if (IsActiveLayerLocked()) return;
        var layerIdx = GetActiveLayerIndex();
        var r = _selection.TileSelection!.Value;
        int minTx = r.MinTx, minTy = r.MinTy, maxTx = r.MaxTx, maxTy = r.MaxTy;
        var newTile = CreateTileData(_selectedTileType);
        var batch = new PaintTileBatchCommand(_tileMap, layerIdx);
        for (int tx = minTx; tx <= maxTx; tx++)
            for (int ty = minTy; ty <= maxTy; ty++)
            {
                _tileMap.TryGetTile(layerIdx, tx, ty, out var prev);
                batch.Add(tx, ty, prev, newTile.Clone());
            }
        if (batch.Count > 0)
        {
            _history.Push(batch);
            ProjectExplorer.SetModified(GetCurrentSceneMapPath(), true);
        }
        DrawMap();
    }

    private void BtnCopySelection_OnClick(object sender, RoutedEventArgs e) => CopyZone();
    private void BtnPasteSelection_OnClick(object sender, RoutedEventArgs e) => PasteZone();
    private void BtnDuplicateZone_OnClick(object sender, RoutedEventArgs e) => MenuDuplicarZona_OnClick(sender, e);

    private static ObjectInstance CloneObject(ObjectInstance o)
    {
        return new ObjectInstance
        {
            DefinitionId = o.DefinitionId,
            X = o.X, Y = o.Y,
            Rotation = o.Rotation, ScaleX = o.ScaleX, ScaleY = o.ScaleY,
            LayerOrder = o.LayerOrder, Nombre = o.Nombre,
            ColisionOverride = o.ColisionOverride, CollisionType = o.CollisionType,
            InteractivoOverride = o.InteractivoOverride, DestructibleOverride = o.DestructibleOverride,
            ScriptIdOverride = o.ScriptIdOverride,
            ScriptIds = o.ScriptIds != null ? new List<string>(o.ScriptIds) : new List<string>(),
            ScriptProperties = o.ScriptProperties?.Select(sp => new ScriptInstancePropertySet { ScriptId = sp.ScriptId, Properties = sp.Properties?.Select(p => new ScriptPropertyEntry { Key = p.Key, Type = p.Type, Value = p.Value }).ToList() ?? new List<ScriptPropertyEntry>() }).ToList() ?? new List<ScriptInstancePropertySet>(),
            Tags = o.Tags != null ? new List<string>(o.Tags) : new List<string>()
        };
    }

    private void MenuSnapshot_OnClick(object sender, RoutedEventArgs e)
    {
        var snap = new MapSnapshot();
        var cs = _tileMap.ChunkSize;
        foreach (var (cx, cy) in _tileMap.EnumerateChunkCoords())
        {
            var ch = _tileMap.GetChunk(cx, cy);
            if (ch == null) continue;
            foreach (var (lx, ly, data) in ch.EnumerateTiles())
            {
                int wx = cx * cs + lx, wy = cy * cs + ly;
                snap.Tiles.Add((wx, wy, data.Clone()));
            }
        }
        foreach (var inst in _objectLayer.Instances)
            snap.Objects.Add(CloneObject(inst));
        _mapSnapshot = snap;
        UpdateZoneMenuState();
        EditorLog.Toast($"Snapshot creado: {snap.Tiles.Count} tiles, {snap.Objects.Count} objetos. Editar → Revertir a snapshot para restaurar.", LogLevel.Info, "Snapshot");
    }

    private void MenuRevertSnapshot_OnClick(object sender, RoutedEventArgs e)
    {
        if (_mapSnapshot == null) return;
        var snap = _mapSnapshot;
        var cs = _tileMap.ChunkSize;
        foreach (var (cx, cy) in _tileMap.EnumerateChunkCoords().ToList())
        {
            var ch = _tileMap.GetChunk(cx, cy);
            if (ch == null) continue;
            foreach (var (lx, ly, _) in ch.EnumerateTiles().ToList())
                _tileMap.RemoveTile(cx * cs + lx, cy * cs + ly);
        }
        foreach (var (x, y, data) in snap.Tiles)
            _tileMap.SetTile(x, y, data.Clone());
        // Vaciar instancias una a una: siempre se elimina [0] hasta que la lista quede vacía. No cambiar RemoveInstance sin revisar este bucle.
        while (_objectLayer.Instances.Count > 0)
            _objectLayer.RemoveInstance(_objectLayer.Instances[0]);
        foreach (var o in snap.Objects)
            _objectLayer.AddInstance(CloneObject(o));
        ProjectExplorer.SetModified(GetCurrentSceneMapPath(), true);
        ProjectExplorer.SetModified(GetCurrentSceneObjectsPath(), true);
        MapHierarchy.SetMapStructure(System.IO.Path.GetFileNameWithoutExtension(GetCurrentSceneMapPath()), _project.LayerNames, _objectLayer, _triggerZones, _visibleLayers, GetCurrentUIRoot());
        DrawMap();
        RefreshInspector();
        EditorLog.Toast("Mapa revertido al último snapshot.", LogLevel.Info, "Snapshot");
    }

    private void ChkMask_OnChanged(object sender, RoutedEventArgs e)
    {
        _maskColision = ChkMaskColision?.IsChecked == true;
        _maskScripts = ChkMaskScripts?.IsChecked == true;
        DrawMap();
    }

    private void ChkResaltar_OnChanged(object sender, RoutedEventArgs e)
    {
        DrawMap();
    }

    private sealed class EditorToolContext : IMapEditorToolContext
    {
        private readonly EditorWindow _w;

        public EditorToolContext(EditorWindow w) => _w = w;

        public TileMap TileMap => _w._tileMap;
        public ObjectLayer ObjectLayer => _w._objectLayer;
        public SelectionManager Selection => _w._selection;
        public EditorHistory History => _w._history;
        public ProjectInfo Project => _w._project;

        public (int tx, int ty) GetTileAt(System.Windows.Point canvasPos) => _w.GetTileAt(canvasPos);
        public ObjectInstance? GetObjectAt(double canvasX, double canvasY) => _w.GetObjectAt(canvasX, canvasY);

        public void DrawMap() => _w.DrawMap();
        public void RefreshInspector() => _w.RefreshInspector();
        public void UpdateStatusBar(string message) => _w.UpdateStatusBar(message);
        public void UpdateTileSelectionToolbarVisibility() => _w.UpdateTileSelectionToolbarVisibility();

        public TileType SelectedTileType { get => _w._selectedTileType; set => _w._selectedTileType = value; }
        public int BrushSize => _w._brushSize;
        public int BrushRotation => _w._brushRotation;
        public TileData CreateTileData(TileType tipo) => EditorWindow.CreateTileData(tipo);
        public void SetMapModified() => _w.ProjectExplorer?.SetModified(_w.GetCurrentSceneMapPath(), true);
        public void SetObjectsModified() => _w.ProjectExplorer?.SetModified(_w.GetCurrentSceneObjectsPath(), true);
        public int ActiveLayerIndex => _w.GetActiveLayerIndex();
        public bool IsActiveLayerLocked => _w.IsActiveLayerLocked();
    }
}

internal class EditorWindowState
{
    public int SelectedTabIndex { get; set; }
    public List<string>? OpenTabKinds { get; set; }
    public double WindowLeft { get; set; }
    public double WindowTop { get; set; }
    public double WindowWidth { get; set; }
    public double WindowHeight { get; set; }
}

/// <summary>Layout del editor: tabs abiertos y tab seleccionado (archivo .editorlayout).</summary>
internal class EditorLayoutState
{
    [JsonPropertyName("openTabs")]
    public List<string>? OpenTabs { get; set; }

    [JsonPropertyName("selectedTab")]
    public string? SelectedTab { get; set; }
}
