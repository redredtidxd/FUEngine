using FUEngine.Core;
using FUEngine.Editor;

namespace FUEngine;

/// <summary>
/// Acción del editor que se puede deshacer/rehacer (mapa, objetos).
/// </summary>
public interface IEditorCommand
{
    void Execute();
    void Undo();
}

/// <summary>
/// Historial de comandos para undo/redo.
/// </summary>
public class EditorHistory
{
    private readonly List<IEditorCommand> _undoStack = new();
    private readonly List<IEditorCommand> _redoStack = new();
    public const int MaxSteps = 100;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    public int UndoCount => _undoStack.Count;
    public int RedoCount => _redoStack.Count;

    public event EventHandler? HistoryChanged;

    public void Push(IEditorCommand command)
    {
        command.Execute();
        _redoStack.Clear();
        _undoStack.Add(command);
        while (_undoStack.Count > MaxSteps)
            _undoStack.RemoveAt(0);
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Undo()
    {
        if (_undoStack.Count == 0) return;
        var cmd = _undoStack[^1];
        _undoStack.RemoveAt(_undoStack.Count - 1);
        cmd.Undo();
        _redoStack.Add(cmd);
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Redo()
    {
        if (_redoStack.Count == 0) return;
        var cmd = _redoStack[^1];
        _redoStack.RemoveAt(_redoStack.Count - 1);
        cmd.Execute();
        _undoStack.Add(cmd);
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// Comando: pintar o borrar un tile (guardamos estado anterior).
/// </summary>
public class PaintTileCommand : IEditorCommand
{
    private readonly TileMap _map;
    private readonly int _layerIndex;
    private readonly int _x, _y;
    private readonly TileData? _previous;
    private readonly TileData _new;

    public PaintTileCommand(TileMap map, int layerIndex, int x, int y, TileData? previous, TileData newTile)
    {
        _map = map;
        _layerIndex = layerIndex;
        _x = x;
        _y = y;
        _previous = previous?.Clone();
        _new = newTile.Clone();
    }

    public void Execute()
    {
        _map.SetTile(_layerIndex, _x, _y, _new.Clone());
    }

    public void Undo()
    {
        if (_previous != null)
            _map.SetTile(_layerIndex, _x, _y, _previous.Clone());
        else
            _map.RemoveTile(_layerIndex, _x, _y);
    }
}

/// <summary>
/// Comando: añadir una instancia de objeto.
/// </summary>
public class AddObjectCommand : IEditorCommand
{
    private readonly ObjectLayer _layer;
    private readonly ObjectInstance _instance;

    public AddObjectCommand(ObjectLayer layer, ObjectInstance instance)
    {
        _layer = layer;
        _instance = instance;
    }

    public void Execute()
    {
        _layer.AddInstance(_instance);
    }

    public void Undo()
    {
        _layer.RemoveInstance(_instance);
    }
}

/// <summary>
/// Comando: eliminar una instancia de objeto.
/// </summary>
public class RemoveObjectCommand : IEditorCommand
{
    private readonly ObjectLayer _layer;
    private readonly ObjectInstance _instance;

    public RemoveObjectCommand(ObjectLayer layer, ObjectInstance instance)
    {
        _layer = layer;
        _instance = instance;
    }

    public void Execute()
    {
        _layer.RemoveInstance(_instance);
    }

    public void Undo()
    {
        _layer.AddInstance(_instance);
    }
}

/// <summary>
/// Comando: pintar varios tiles en una sola operación (rectángulo, línea, stamp). Un solo undo/redo.
/// </summary>
public class PaintTileBatchCommand : IEditorCommand
{
    private readonly TileMap _map;
    private readonly int _layerIndex;
    private readonly List<(int x, int y, TileData? previous, TileData? @new)> _changes = new();

    public PaintTileBatchCommand(TileMap map, int layerIndex)
    {
        _map = map;
        _layerIndex = layerIndex;
    }

    public void Add(int x, int y, TileData? previous, TileData? newTile)
    {
        _changes.Add((x, y, previous?.Clone(), newTile?.Clone()));
    }

    public int Count => _changes.Count;

    public void Execute()
    {
        foreach (var (x, y, _, @new) in _changes)
        {
            if (@new == null)
                _map.RemoveTile(_layerIndex, x, y);
            else
                _map.SetTile(_layerIndex, x, y, @new.Clone());
        }
    }

    public void Undo()
    {
        foreach (var (x, y, previous, _) in _changes)
        {
            if (previous != null)
                _map.SetTile(_layerIndex, x, y, previous.Clone());
            else
                _map.RemoveTile(_layerIndex, x, y);
        }
    }
}

/// <summary>
/// Comando: borrar un tile (restaurar estado anterior).
/// </summary>
public class RemoveTileCommand : IEditorCommand
{
    private readonly TileMap _map;
    private readonly int _layerIndex;
    private readonly int _x, _y;
    private readonly TileData _previous;

    public RemoveTileCommand(TileMap map, int layerIndex, int x, int y, TileData previous)
    {
        _map = map;
        _layerIndex = layerIndex;
        _x = x;
        _y = y;
        _previous = previous.Clone();
    }

    public void Execute()
    {
        _map.RemoveTile(_layerIndex, _x, _y);
    }

    public void Undo()
    {
        _map.SetTile(_layerIndex, _x, _y, _previous.Clone());
    }
}

/// <summary>
/// Modo de transformación de una selección rectangular de tiles.
/// </summary>
public enum TileSelectionTransformMode
{
    Rotate90CW,
    Rotate180,
    FlipH,
    FlipV
}

/// <summary>
/// Comando: rotar o voltear una selección rectangular de tiles (con undo).
/// </summary>
public class TransformTileSelectionCommand : IEditorCommand
{
    private readonly TileMap _map;
    private readonly int _layerIndex;
    private readonly List<(int x, int y, TileData? data)> _before = new();
    private readonly List<(int x, int y, TileData data)> _after = new();
    private readonly HashSet<(int x, int y)> _allAffected = new();

    public TransformTileSelectionCommand(TileMap map, int layerIndex, int minTx, int minTy, int maxTx, int maxTy, TileSelectionTransformMode mode)
    {
        _map = map;
        _layerIndex = layerIndex;
        int w = maxTx - minTx + 1, h = maxTy - minTy + 1;
        for (int x = minTx; x <= maxTx; x++)
            for (int y = minTy; y <= maxTy; y++)
            {
                _map.TryGetTile(_layerIndex, x, y, out var data);
                _before.Add((x, y, data?.Clone()));
                _allAffected.Add((x, y));
            }
        foreach (var (x, y, data) in _before)
        {
            if (data == null) continue;
            var (nx, ny) = Transform(x, y, minTx, minTy, maxTx, maxTy, w, h, mode);
            _after.Add((nx, ny, data.Clone()));
            _allAffected.Add((nx, ny));
        }
    }

    private static (int nx, int ny) Transform(int x, int y, int minTx, int minTy, int maxTx, int maxTy, int w, int h, TileSelectionTransformMode mode)
    {
        int lx = x - minTx, ly = y - minTy;
        int nx, ny;
        switch (mode)
        {
            case TileSelectionTransformMode.Rotate90CW:
                nx = minTx + ly;
                ny = maxTy - lx;
                break;
            case TileSelectionTransformMode.Rotate180:
                nx = maxTx - lx;
                ny = maxTy - ly;
                break;
            case TileSelectionTransformMode.FlipH:
                nx = maxTx - lx;
                ny = y;
                break;
            case TileSelectionTransformMode.FlipV:
                nx = x;
                ny = maxTy - ly;
                break;
            default:
                nx = x; ny = y;
                break;
        }
        return (nx, ny);
    }

    public void Execute()
    {
        foreach (var (px, py) in _allAffected)
            _map.RemoveTile(_layerIndex, px, py);
        foreach (var (x, y, data) in _after)
            _map.SetTile(_layerIndex, x, y, data.Clone());
    }

    public void Undo()
    {
        foreach (var (x, y, _) in _after)
            _map.RemoveTile(_layerIndex, x, y);
        foreach (var (x, y, data) in _before)
        {
            if (data != null)
                _map.SetTile(_layerIndex, x, y, data.Clone());
        }
    }
}
