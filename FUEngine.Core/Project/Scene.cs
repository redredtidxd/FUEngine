using System.Collections.Generic;

namespace FUEngine.Core;

/// <summary>
/// Escena = mundo completo. No es un solo tilemap.
/// Scene
///   ├ TilemapLayers (varias capas: fondo, paredes, detalles)
///   ├ Objects (ObjectLayer)
///   ├ Lights (LightLayer)
///   └ Triggers (TriggerLayer)
/// </summary>
public class Scene
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";

    /// <summary>Capas de tilemap (cada una usa un Tileset y guarda tile IDs).</summary>
    public List<TilemapLayer> TilemapLayers { get; set; } = new();

    /// <summary>Objetos colocados en la escena (puertas, cámaras, animatrónicos, etc.).</summary>
    public List<SceneObject> Objects { get; set; } = new();

    /// <summary>Luces de la escena.</summary>
    public List<LightSource> Lights { get; set; } = new();

    /// <summary>Zonas trigger (entrar/salir, jumpscare, etc.).</summary>
    public List<TriggerZone> Triggers { get; set; } = new();
}
