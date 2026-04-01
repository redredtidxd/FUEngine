using FUEngine.Core;

namespace FUEngine;

/// <summary>
/// Domain service for copy/paste of map zones (tiles + objects). Produces clipboard data and undoable commands.
/// </summary>
public static class ZoneClipboardService
{
    /// <summary>Gets the copy bounds from either tile selection or zone rectangle; returns null if neither is valid.</summary>
    public static (int MinTx, int MinTy, int MaxTx, int MaxTy)? TryGetCopyBounds(
        bool hasTileSelection,
        int? tileMinTx,
        int? tileMinTy,
        int? tileMaxTx,
        int? tileMaxTy,
        int? zoneMinTx,
        int? zoneMinTy,
        int? zoneMaxTx,
        int? zoneMaxTy)
    {
        if (hasTileSelection && tileMinTx.HasValue && tileMinTy.HasValue && tileMaxTx.HasValue && tileMaxTy.HasValue)
            return (tileMinTx.Value, tileMinTy.Value, tileMaxTx.Value, tileMaxTy.Value);
        if (zoneMinTx.HasValue && zoneMinTy.HasValue && zoneMaxTx.HasValue && zoneMaxTy.HasValue)
            return (zoneMinTx.Value, zoneMinTy.Value, zoneMaxTx.Value, zoneMaxTy.Value);
        return null;
    }

    /// <summary>Builds a clipboard from the given rectangle and layer. Caller stores the result and paste origin.</summary>
    public static ZoneClipboard Copy(
        TileMap tileMap,
        ObjectLayer objectLayer,
        int minTx,
        int minTy,
        int maxTx,
        int maxTy,
        int layerIndex = 0)
    {
        var clip = new ZoneClipboard { OriginX = minTx, OriginY = minTy };
        for (int tx = minTx; tx <= maxTx; tx++)
        for (int ty = minTy; ty <= maxTy; ty++)
            if (tileMap.TryGetTile(layerIndex, tx, ty, out var data) && data != null)
                clip.Tiles.Add(new ZoneTileEntry { X = tx - minTx, Y = ty - minTy, Data = data.Clone() });
        foreach (var inst in objectLayer.Instances)
        {
            int itx = (int)Math.Floor(inst.X), ity = (int)Math.Floor(inst.Y);
            if (itx >= minTx && itx <= maxTx && ity >= minTy && ity <= maxTy)
                clip.Objects.Add(new ZoneObjectEntry
                {
                    DefinitionId = inst.DefinitionId,
                    X = inst.X - minTx,
                    Y = inst.Y - minTy,
                    Rotation = inst.Rotation,
                    Nombre = inst.Nombre
                });
        }
        return clip;
    }

    /// <summary>Builds the list of commands to apply the clipboard at (originTx, originTy) on the given layer. Caller pushes each to history.</summary>
    public static IReadOnlyList<IEditorCommand> Paste(
        ZoneClipboard clipboard,
        TileMap tileMap,
        ObjectLayer objectLayer,
        int originTx,
        int originTy,
        int layerIndex = 0)
    {
        if (clipboard == null || !clipboard.HasContent)
            return Array.Empty<IEditorCommand>();
        var commands = new List<IEditorCommand>();
        foreach (var t in clipboard.Tiles)
        {
            int tx = originTx + t.X, ty = originTy + t.Y;
            tileMap.TryGetTile(layerIndex, tx, ty, out var prev);
            commands.Add(new PaintTileCommand(tileMap, layerIndex, tx, ty, prev, t.Data.Clone()));
        }
        foreach (var o in clipboard.Objects)
        {
            var inst = new ObjectInstance
            {
                DefinitionId = o.DefinitionId,
                X = originTx + o.X,
                Y = originTy + o.Y,
                Rotation = o.Rotation,
                Nombre = o.Nombre
            };
            commands.Add(new AddObjectCommand(objectLayer, inst));
        }
        return commands;
    }
}
