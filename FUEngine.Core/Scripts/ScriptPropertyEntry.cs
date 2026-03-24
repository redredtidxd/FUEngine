namespace FUEngine.Core;

/// <summary>
/// Una propiedad pública de un script asignado a un objeto (estilo Unity).
/// Tipos soportados: string, int, float, bool, Vector2, Color, object (InstanceId de otro objeto, serializado como string).
/// </summary>
public class ScriptPropertyEntry
{
    public string Key { get; set; } = "";
    /// <summary>Tipo: "string", "int", "float", "bool", "Vector2", "Color", "object".</summary>
    public string Type { get; set; } = "string";
    public string Value { get; set; } = "";
}
