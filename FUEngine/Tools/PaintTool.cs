using System.Windows;
using FUEngine.Core;

namespace FUEngine;

/// <summary>
/// Paint tool: places tiles with current brush size and type. Shift+click = bucket fill. Drag = continuous paint.
/// BrushRotation is available on context for future use (e.g. rotated brush pattern); tile brush is currently axis-aligned.
/// </summary>
public sealed class PaintTool : ITool
{
    private readonly IMapEditorToolContext _ctx;
    private (int tx, int ty)? _lastPaintedPos;

    public PaintTool(IMapEditorToolContext ctx)
    {
        _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
    }

    public void OnMouseDown(System.Windows.Point canvasPos, bool ctrl, bool shift)
    {
        if (_ctx.IsActiveLayerLocked) return;
        var (tx, ty) = _ctx.GetTileAt(canvasPos);
        if (!_ctx.IsFinitePaintableTile(tx, ty)) return;
        _lastPaintedPos = (tx, ty);

        if (shift)
            BucketFill(tx, ty);
        else
            PaintAt(tx, ty);

        _ctx.DrawMap();
    }

    /// <summary>Only called by ToolController during drag (between MouseDown and MouseUp); no static Mouse dependency.</summary>
    public void OnMouseMove(System.Windows.Point canvasPos)
    {
        var (tx, ty) = _ctx.GetTileAt(canvasPos);
        if (_lastPaintedPos == (tx, ty))
            return;

        _lastPaintedPos = (tx, ty);
        PaintAt(tx, ty);
        _ctx.DrawMap();
    }

    public void OnMouseUp(System.Windows.Point canvasPos) { }

    private void PaintAt(int tx, int ty)
    {
        var layerIdx = _ctx.ActiveLayerIndex;
        var newTile = _ctx.CreateTileData(_ctx.SelectedTileType);
        if (_ctx.BrushSize <= 1)
        {
            if (!_ctx.IsFinitePaintableTile(tx, ty)) return;
            _ctx.TileMap.TryGetTile(layerIdx, tx, ty, out var prevTile);
            _ctx.History.Push(new PaintTileCommand(_ctx.TileMap, layerIdx, tx, ty, prevTile, newTile));
        }
        else
        {
            var batch = new PaintTileBatchCommand(_ctx.TileMap, layerIdx);
            for (int dx = 0; dx < _ctx.BrushSize; dx++)
            for (int dy = 0; dy < _ctx.BrushSize; dy++)
            {
                int px = tx + dx, py = ty + dy;
                if (!_ctx.IsFinitePaintableTile(px, py)) continue;
                _ctx.TileMap.TryGetTile(layerIdx, px, py, out var prev);
                batch.Add(px, py, prev, newTile.Clone());
            }
            if (batch.Count == 0) return;
            _ctx.History.Push(batch);
        }
        _ctx.SetMapModified();
    }

    private void BucketFill(int startTx, int startTy)
    {
        var layerIdx = _ctx.ActiveLayerIndex;
        var cells = TilePaintService.ComputeBucketFill(
            _ctx.TileMap,
            startTx,
            startTy,
            _ctx.Selection.IsInsideTileSelection,
            maxFill: 2000,
            layerIndex: layerIdx);

        if (cells.Count == 0)
            return;

        var newTile = _ctx.CreateTileData(_ctx.SelectedTileType);
        foreach (var (tx, ty) in cells)
        {
            _ctx.TileMap.TryGetTile(layerIdx, tx, ty, out var prev);
            _ctx.History.Push(new PaintTileCommand(_ctx.TileMap, layerIdx, tx, ty, prev, newTile.Clone()));
        }
        _ctx.SetMapModified();
        _ctx.UpdateStatusBar(cells.Count >= 2000 ? "Relleno: 2000+ tiles" : $"Relleno: {cells.Count} tiles");
    }
}
