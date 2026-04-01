namespace FUEngine.Core;

/// <summary>Objeto dentro de una escena (referencia a definición, posición, datos de instancia).</summary>
public class SceneObject
{
    public string Id { get; set; } = "";
    public string DefinitionId { get; set; } = "";
    public float X { get; set; }
    public float Y { get; set; }
    public float Rotation { get; set; }
}
