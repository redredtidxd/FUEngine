using System.Windows;
using FUEngine.Core;

namespace FUEngine;

/// <summary>
/// Context passed to map editor tools: map data, selection, history, and UI callbacks.
/// Implemented by the editor (or a facade) so tools stay decoupled from the window.
/// </summary>
public interface IMapEditorToolContext
{
    TileMap TileMap { get; }
    ObjectLayer ObjectLayer { get; }
    SelectionManager Selection { get; }
    EditorHistory History { get; }
    ProjectInfo Project { get; }

    (int tx, int ty) GetTileAt(System.Windows.Point canvasPos);
    ObjectInstance? GetObjectAt(double canvasX, double canvasY);

    void DrawMap();
    void RefreshInspector();
    void UpdateStatusBar(string message);
    void UpdateTileSelectionToolbarVisibility();

    TileType SelectedTileType { get; set; }
    int BrushSize { get; }
    /// <summary>Brush rotation in degrees (0, 90, 180, 270). For future use e.g. rotated brush pattern.</summary>
    int BrushRotation { get; }
    TileData CreateTileData(TileType tipo);
    void SetMapModified();
    void SetObjectsModified();

    /// <summary>Índice de la capa activa para pintar/borrar.</summary>
    int ActiveLayerIndex { get; }
    /// <summary>True si la capa activa está bloqueada (no se puede pintar).</summary>
    bool IsActiveLayerLocked { get; }
}
