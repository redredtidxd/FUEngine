namespace FUEngine.Core;

/// <summary>
/// Presets de tipo de capa para el tilemap.
/// Background = suelo/base sin colisión; Solid = paredes (colisión automática);
/// Objects = objetos/muebles; Foreground = se dibuja encima del jugador.
/// </summary>
public enum LayerType
{
    /// <summary>Suelo / fondo. No tiene colisión por defecto.</summary>
    Background = 0,

    /// <summary>Paredes. Si un tile está en esta capa, PhysicsWorld lo trata como muro.</summary>
    Solid = 1,

    /// <summary>Objetos / muebles (escritorios, puertas, etc.).</summary>
    Objects = 2,

    /// <summary>Superposición (techos, bordes que se dibujan por encima del jugador).</summary>
    Foreground = 3
}
