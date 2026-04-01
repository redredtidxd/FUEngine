namespace FUEngine.Core;

/// <summary>
/// Conjunto de propiedades por script asignado a una instancia de objeto.
/// Se guarda y carga con el proyecto (ScriptInstance.Properties).
/// </summary>
public class ScriptInstancePropertySet
{
    public string ScriptId { get; set; } = "";
    public List<ScriptPropertyEntry> Properties { get; set; } = new();
}
