namespace FUEngine.Core;

/// <summary>
/// Definición de un tipo de objeto (asset). Soporta animatrónicos (Freddy, Bonnie, Chica, Foxy, etc.)
/// con patrones de movimiento, personalidad y eventos (onRepair, onHack, onInteract, onFear).
/// </summary>
public class ObjectDefinition
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? SpritePath { get; set; }
    public bool Colision { get; set; }
    public bool Interactivo { get; set; }
    public bool Destructible { get; set; }
    public string? ScriptId { get; set; }
    public string? AnimacionId { get; set; }
    public int Width { get; set; } = 1;
    public int Height { get; set; } = 1;

    /// <summary>Tipo de animatrónico o NPC (ej: Freddy, Bonnie, Chica, Foxy, DiamondFreddy, Endoskeleton).</summary>
    public string? AnimatronicType { get; set; }
    /// <summary>Patrón de movimiento para IA (patrulla, perseguir, aleatorio, etc.).</summary>
    public string? MovementPattern { get; set; }
    /// <summary>Personalidad o comportamiento base (agresivo, pasivo, reactivo).</summary>
    public string? Personality { get; set; }
    /// <summary>Indica si puede detectar al jugador por visibilidad/ruido (para sistema de sigilo).</summary>
    public bool CanDetectPlayer { get; set; }
    /// <summary>Etiquetas para filtros, scripts y búsqueda (ej: "enemigo", "puerta", "pickup").</summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>Si true, el motor genera o asocia CanvasController.lua para permitir que el jugador pinte sobre este objeto en PlayMode.</summary>
    public bool EnableInGameDrawing { get; set; }
}
