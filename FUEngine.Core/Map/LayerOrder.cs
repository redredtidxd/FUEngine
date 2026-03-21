namespace FUEngine.Core;

/// <summary>
/// Orden de capas recomendado para 2D: fondo → delante.
/// Usar como SortOrder en TilemapLayer (0 = Background, 4 = Foreground).
/// </summary>
public static class LayerOrder
{
    public const int Background = 0;
    public const int Ground = 1;
    public const int Walls = 2;
    public const int Decoration = 3;
    public const int Foreground = 4;
}
