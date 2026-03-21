namespace FUEngine.Core;

/// <summary>
/// Definición de un script disponible (id y nombre para el editor; runtime se enlaza después).
/// </summary>
public class ScriptDefinition
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    /// <summary>Ruta relativa al proyecto del archivo del script (ej: Scripts/main.lua).</summary>
    public string? Path { get; set; }
    /// <summary>
    /// Eventos que este script puede manejar (onInteract, onUpdate, onCollision, etc.).
    /// </summary>
    public IReadOnlyList<string> Eventos { get; set; } = Array.Empty<string>();
}
