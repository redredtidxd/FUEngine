namespace FUEngine.Core;

/// <summary>Componente que ejecuta un script (ScriptId) con eventos onUpdate, onCollision, etc.</summary>
public class ScriptComponent : Component
{
    public string? ScriptId { get; set; }
    /// <summary>Ruta relativa al proyecto del archivo .lua (ej: Scripts/player.lua).</summary>
    public string? ScriptPath { get; set; }
    /// <summary>Referencia opcional a la instancia del script en runtime (Lua). No serializar.</summary>
    public object? ScriptInstanceHandle { get; set; }
    /// <summary>Si false, el script no recibe eventos.</summary>
    public bool Enabled { get; set; } = true;

    public override void Update(float deltaTime) { }
}
