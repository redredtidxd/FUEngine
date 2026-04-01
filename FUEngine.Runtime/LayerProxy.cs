using System;
using FUEngine.Core;

namespace FUEngine.Runtime;

/// <summary>
/// Tabla Lua <c>layer</c> en scripts de capa del mapa. Lee/escribe el <see cref="MapLayerDescriptor"/> activo en Play.
/// </summary>
[LuaVisible]
public sealed class LayerProxy
{
    private readonly MapLayerDescriptor _descriptor;

    public LayerProxy(MapLayerDescriptor descriptor, int layerIndex)
    {
        _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        index = layerIndex;
    }

    /// <summary>Índice de la capa en <see cref="TileMap.Layers"/>.</summary>
    public int index { get; }

    public string id => _descriptor.Id;

    public string name
    {
        get => _descriptor.Name;
        set => _descriptor.Name = value ?? "";
    }

    public double offsetX
    {
        get => _descriptor.OffsetX;
        set => _descriptor.OffsetX = (float)value;
    }

    public double offsetY
    {
        get => _descriptor.OffsetY;
        set => _descriptor.OffsetY = (float)value;
    }

    public double parallaxX
    {
        get => _descriptor.ParallaxX;
        set => _descriptor.ParallaxX = (float)value;
    }

    public double parallaxY
    {
        get => _descriptor.ParallaxY;
        set => _descriptor.ParallaxY = (float)value;
    }

    /// <summary>Opacidad 0–100 (misma semántica que el inspector de capa).</summary>
    public double opacity
    {
        get => _descriptor.Opacity;
        set => _descriptor.Opacity = (int)Math.Clamp(Math.Round(value), 0, 100);
    }
}
