using FUEngine.Core;

namespace FUEngine;

/// <summary>Rectangle of selected tiles (min/max inclusive).</summary>
public readonly record struct TileSelectionRect(int MinTx, int MinTy, int MaxTx, int MaxTy);

/// <summary>Tipo de selección para el Inspector (panel Tiles/Animaciones, capa, u overview).</summary>
public enum InspectorContextKind
{
    None,
    Tile,
    Animation,
    Layer
}

/// <summary>
/// Centralizes editor selection state: map objects, triggers, tile selection, explorer items, and inspector context (Tile/Animation from panels).
/// </summary>
public class SelectionManager
{
    private ObjectInstance? _selectedObject;
    private TriggerZone? _selectedTrigger;
    private ProjectExplorerItem? _selectedExplorerItem;
    private UICanvas? _selectedUICanvas;
    private UIElement? _selectedUIElement;
    private readonly List<ObjectInstance> _selectedObjects = new();

    #region Inspector context (Tile / Animation / Layer)
    private InspectorContextKind _inspectorContextKind = InspectorContextKind.None;
    private int? _inspectorContextTileId;
    private string? _inspectorContextTilesetRelPath;
    private AnimationDefinition? _inspectorContextAnimation;
    private MapLayerDescriptor? _inspectorContextLayer;

    public InspectorContextKind InspectorContextKind => _inspectorContextKind;
    public int? InspectorContextTileId => _inspectorContextKind == InspectorContextKind.Tile ? _inspectorContextTileId : null;
    /// <summary>Ruta relativa al proyecto del JSON del tileset del contexto del catálogo (Inspector de tile).</summary>
    public string? InspectorContextTilesetRelPath => _inspectorContextKind == InspectorContextKind.Tile ? _inspectorContextTilesetRelPath : null;
    public AnimationDefinition? InspectorContextAnimation => _inspectorContextKind == InspectorContextKind.Animation ? _inspectorContextAnimation : null;
    public MapLayerDescriptor? InspectorContextLayer => _inspectorContextKind == InspectorContextKind.Layer ? _inspectorContextLayer : null;

    public void SetInspectorContextTile(int? tileId, string? tilesetPathRelative = null)
    {
        _inspectorContextKind = tileId.HasValue ? InspectorContextKind.Tile : InspectorContextKind.None;
        _inspectorContextTileId = tileId;
        _inspectorContextTilesetRelPath = tileId.HasValue && !string.IsNullOrWhiteSpace(tilesetPathRelative)
            ? tilesetPathRelative.Replace('\\', '/').Trim()
            : null;
        _inspectorContextAnimation = null;
        _inspectorContextLayer = null;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetInspectorContextAnimation(AnimationDefinition? anim)
    {
        _inspectorContextKind = anim != null ? InspectorContextKind.Animation : InspectorContextKind.None;
        _inspectorContextAnimation = anim;
        _inspectorContextTileId = null;
        _inspectorContextTilesetRelPath = null;
        _inspectorContextLayer = null;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetInspectorContextLayer(MapLayerDescriptor? layer)
    {
        _inspectorContextKind = layer != null ? InspectorContextKind.Layer : InspectorContextKind.None;
        _inspectorContextLayer = layer;
        _inspectorContextTileId = null;
        _inspectorContextTilesetRelPath = null;
        _inspectorContextAnimation = null;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ClearInspectorContext()
    {
        if (_inspectorContextKind == InspectorContextKind.None) return;
        _inspectorContextKind = InspectorContextKind.None;
        _inspectorContextTileId = null;
        _inspectorContextTilesetRelPath = null;
        _inspectorContextAnimation = null;
        _inspectorContextLayer = null;
    }
    #endregion

    #region Tile selection
    private int? _tileMinTx, _tileMinTy, _tileMaxTx, _tileMaxTy;
    private bool _tileSelectionDragging;
    private (int x, int y)? _tileSelectionDragStart;
    private (int x, int y)? _tileSelectionDragEnd;

    public bool HasTileSelection => _tileMinTx.HasValue && _tileMinTy.HasValue && _tileMaxTx.HasValue && _tileMaxTy.HasValue;
    public int? TileMinTx => _tileMinTx;
    public int? TileMinTy => _tileMinTy;
    public int? TileMaxTx => _tileMaxTx;
    public int? TileMaxTy => _tileMaxTy;
    public TileSelectionRect? TileSelection => HasTileSelection
        ? new TileSelectionRect(_tileMinTx!.Value, _tileMinTy!.Value, _tileMaxTx!.Value, _tileMaxTy!.Value)
        : null;

    public bool IsTileSelectionDragging => _tileSelectionDragging;
    public (int x, int y)? TileSelectionDragStart => _tileSelectionDragStart;
    public (int x, int y)? TileSelectionDragEnd => _tileSelectionDragEnd;

    public bool IsInsideTileSelection(int tx, int ty)
    {
        if (!HasTileSelection) return true;
        return tx >= _tileMinTx!.Value && tx <= _tileMaxTx!.Value &&
               ty >= _tileMinTy!.Value && ty <= _tileMaxTy!.Value;
    }

    public void StartTileSelectionDrag(int tx, int ty)
    {
        _tileSelectionDragging = true;
        _tileSelectionDragStart = (tx, ty);
        _tileSelectionDragEnd = (tx, ty);
        _tileMinTx = _tileMinTy = _tileMaxTx = _tileMaxTy = null;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateTileSelectionDragEnd(int tx, int ty)
    {
        _tileSelectionDragEnd = (tx, ty);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void CommitTileSelectionDrag(int endTx, int endTy)
    {
        if (!_tileSelectionDragStart.HasValue)
        {
            _tileSelectionDragging = false;
            _tileSelectionDragStart = _tileSelectionDragEnd = null;
            return;
        }
        var start = _tileSelectionDragStart.Value;
        if (start.x == endTx && start.y == endTy)
            ClearTileSelection();
        else
        {
            _tileMinTx = Math.Min(start.x, endTx);
            _tileMinTy = Math.Min(start.y, endTy);
            _tileMaxTx = Math.Max(start.x, endTx);
            _tileMaxTy = Math.Max(start.y, endTy);
        }
        _tileSelectionDragging = false;
        _tileSelectionDragStart = null;
        _tileSelectionDragEnd = null;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ExpandTileSelection(int tx, int ty)
    {
        if (!HasTileSelection) return;
        _tileMinTx = Math.Min(_tileMinTx!.Value, tx);
        _tileMinTy = Math.Min(_tileMinTy!.Value, ty);
        _tileMaxTx = Math.Max(_tileMaxTx!.Value, tx);
        _tileMaxTy = Math.Max(_tileMaxTy!.Value, ty);
        _tileSelectionDragStart = null;
        _tileSelectionDragging = false;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetTileSelectionRect(int minTx, int minTy, int maxTx, int maxTy)
    {
        _tileMinTx = minTx;
        _tileMinTy = minTy;
        _tileMaxTx = maxTx;
        _tileMaxTy = maxTy;
        _tileSelectionDragging = false;
        _tileSelectionDragStart = null;
        _tileSelectionDragEnd = null;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ClearTileSelection()
    {
        _tileMinTx = _tileMinTy = _tileMaxTx = _tileMaxTy = null;
        _tileSelectionDragging = false;
        _tileSelectionDragStart = null;
        _tileSelectionDragEnd = null;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
    #endregion

    /// <summary>Primary selected object (last selected when multi-selecting).</summary>
    public ObjectInstance? SelectedObject
    {
        get => _selectedObject;
        set
        {
            if (_selectedObject == value) return;
            _selectedObject = value;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>All selected map objects (for multi-select).</summary>
    public IReadOnlyList<ObjectInstance> SelectedObjects => _selectedObjects;

    /// <summary>Selected trigger zone.</summary>
    public TriggerZone? SelectedTrigger
    {
        get => _selectedTrigger;
        set
        {
            if (_selectedTrigger == value) return;
            _selectedTrigger = value;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Selected item in project explorer (file/folder).</summary>
    public ProjectExplorerItem? SelectedExplorerItem
    {
        get => _selectedExplorerItem;
        set
        {
            if (_selectedExplorerItem == value) return;
            _selectedExplorerItem = value;
            if (value != null)
                _selectedTrigger = null;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Raised when any selection changes.</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>Canvas UI actualmente seleccionado en jerarquía/tab UI.</summary>
    public UICanvas? SelectedUICanvas => _selectedUICanvas;

    /// <summary>Elemento UI actualmente seleccionado para inspector.</summary>
    public UIElement? SelectedUIElement => _selectedUIElement;

    public bool IsObjectSelected(ObjectInstance instance)
        => _selectedObjects.Any(o => o.InstanceId == instance.InstanceId);

    public void SetObjectSelection(ObjectInstance? single)
    {
        _selectedObjects.Clear();
        if (single != null)
            _selectedObjects.Add(single);
        _selectedObject = _selectedObjects.Count > 0 ? _selectedObjects[^1] : null;
        _selectedTrigger = null;
        _selectedExplorerItem = null;
        ClearUISelection();
        ClearInspectorContext();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetObjectSelection(IEnumerable<ObjectInstance> objects)
    {
        _selectedObjects.Clear();
        foreach (var o in objects)
            _selectedObjects.Add(o);
        _selectedObject = _selectedObjects.Count > 0 ? _selectedObjects[^1] : null;
        _selectedTrigger = null;
        _selectedExplorerItem = null;
        ClearUISelection();
        ClearInspectorContext();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ToggleObjectInSelection(ObjectInstance instance)
    {
        var existing = _selectedObjects.FirstOrDefault(o => o.InstanceId == instance.InstanceId);
        if (existing != null)
            _selectedObjects.Remove(existing);
        else
            _selectedObjects.Add(instance);
        _selectedObject = _selectedObjects.Count > 0 ? _selectedObjects[^1] : null;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void AddOrReplaceObjectSelection(ObjectInstance instance, bool addToSelection)
    {
        if (addToSelection)
            ToggleObjectInSelection(instance);
        else
            SetObjectSelection(instance);
    }

    public void SetTriggerSelection(TriggerZone? trigger)
    {
        _selectedTrigger = trigger;
        _selectedObject = null;
        _selectedObjects.Clear();
        _selectedExplorerItem = null;
        ClearUISelection();
        ClearInspectorContext();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetExplorerItemSelection(ProjectExplorerItem? item)
    {
        bool changed = !ReferenceEquals(_selectedExplorerItem, item);
        _selectedExplorerItem = item;
        if (item != null && !item.IsFolder)
        {
            ClearUISelection();
            ClearInspectorContext();
        }
        if (item == null || item.IsFolder)
        {
            if (changed)
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            return;
        }
        _selectedTrigger = null;
        if (changed)
            SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ClearObjectSelection()
    {
        _selectedObjects.Clear();
        _selectedObject = null;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ClearAll()
    {
        _selectedObject = null;
        _selectedObjects.Clear();
        _selectedTrigger = null;
        _selectedExplorerItem = null;
        ClearUISelection();
        ClearInspectorContext();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveObjectFromSelection(ObjectInstance instance)
    {
        _selectedObjects.RemoveAll(o => o.InstanceId == instance.InstanceId);
        _selectedObject = _selectedObjects.Count > 0 ? _selectedObjects[^1] : null;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Selecciona canvas/elemento UI para inspector (limpia selección de objetos/triggers/explorador).</summary>
    public void SetUISelection(UICanvas? canvas, UIElement? element)
    {
        _selectedUICanvas = canvas;
        _selectedUIElement = element;
        _selectedObject = null;
        _selectedObjects.Clear();
        _selectedTrigger = null;
        _selectedExplorerItem = null;
        ClearInspectorContext();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ClearUISelection()
    {
        _selectedUICanvas = null;
        _selectedUIElement = null;
    }
}
