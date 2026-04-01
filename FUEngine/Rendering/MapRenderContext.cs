using System.Windows.Media;
using FUEngine.Core;
using FUEngine.Editor;

namespace FUEngine;

/// <summary>
/// Read-only context for map rendering: map data, view state, selection, and tool overlays.
/// </summary>
public sealed class MapRenderContext
{
    public TileMap TileMap { get; set; } = null!;
    public ObjectLayer ObjectLayer { get; set; } = null!;
    public IReadOnlyList<TriggerZone> TriggerZones { get; set; } = Array.Empty<TriggerZone>();
    public ProjectInfo Project { get; set; } = null!;

    public IReadOnlySet<int> VisibleLayers { get; set; } = null!;
    /// <summary>Índice de la capa activa (para modo edición: otras capas más transparentes).</summary>
    public int? ActiveLayerIndex { get; set; }
    public bool GridVisible { get; set; } = true;
    public System.Windows.Media.Color GridColor { get; set; }
    public bool ShowTileCoordinates { get; set; } = true;
    public bool MaskColision { get; set; }
    public bool MaskScripts { get; set; }
    public bool HighlightInteractives { get; set; } = true;

    public IReadOnlySet<string> SelectedObjectIds { get; set; } = null!;
    public string? SelectedTriggerId { get; set; }

    /// <summary>Set by renderer after computing canvas bounds.</summary>
    public int CanvasMinWx { get; set; }
    public int CanvasMinWy { get; set; }

    public bool TileSelectionDragging { get; set; }
    public (int x, int y)? TileSelectionStart { get; set; }
    public (int x, int y)? TileSelectionEnd { get; set; }
    public int? SelectedTileMinTx { get; set; }
    public int? SelectedTileMinTy { get; set; }
    public int? SelectedTileMaxTx { get; set; }
    public int? SelectedTileMaxTy { get; set; }

    public bool RectDragging { get; set; }
    public int RectStartTx { get; set; }
    public int RectStartTy { get; set; }
    public int RectEndTx { get; set; }
    public int RectEndTy { get; set; }

    public int? ZoneMinTx { get; set; }
    public int? ZoneMinTy { get; set; }
    public int? ZoneMaxTx { get; set; }
    public int? ZoneMaxTy { get; set; }

    public (int x, int y)? MeasureStart { get; set; }
    public (int x, int y)? MeasureEnd { get; set; }

    /// <summary>Si true, se dibuja el marco del área visible del juego (resolución de cámara) en el editor.</summary>
    public bool ShowVisibleArea { get; set; }

    /// <summary>Si true, se dibuja un overlay con las formas de colisión (Solid=verde, Trigger=naranja, OneWay=cyan).</summary>
    public bool ShowColliders { get; set; }

    /// <summary>Cuadrados de referencia: radio de gameplay vs radio de retención de streaming (chunks).</summary>
    public bool ShowStreamingGizmos { get; set; }

    /// <summary>Tiempo total en segundos para animación de tiles (GameTime.TotalSeconds o equivalente en editor).</summary>
    public double TotalSeconds { get; set; }

    /// <summary>Último <see cref="CanvasMinWx"/> del frame anterior (editor); ancla el scroll del ScrollViewer al mundo.</summary>
    public int PreviousCanvasMinWx { get; set; }
    /// <summary>Último <see cref="CanvasMinWy"/> del frame anterior (editor).</summary>
    public int PreviousCanvasMinWy { get; set; }
    /// <summary>Desplazamiento horizontal del ScrollViewer del mapa (px).</summary>
    public double EditorScrollHorizontalOffset { get; set; }
    /// <summary>Desplazamiento vertical del ScrollViewer del mapa (px).</summary>
    public double EditorScrollVerticalOffset { get; set; }
    /// <summary>Ancho visible del ScrollViewer (px).</summary>
    public double EditorViewportWidth { get; set; }
    /// <summary>Alto visible del ScrollViewer (px).</summary>
    public double EditorViewportHeight { get; set; }
    /// <summary>Zoom del lienzo del mapa (LayoutTransform).</summary>
    public double EditorZoom { get; set; } = 1.0;
}
