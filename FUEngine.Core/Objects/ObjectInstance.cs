namespace FUEngine.Core;

/// <summary>
/// Instancia de un objeto colocado en el mapa (posición, rotación, referencia a definición).
/// </summary>
public class ObjectInstance
{
    public string InstanceId { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>
    /// Id de ObjectDefinition.
    /// </summary>
    public string DefinitionId { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    /// <summary>
    /// Rotación en grados (0-360).
    /// </summary>
    public double Rotation { get; set; }
    /// <summary>Escala en X (1 = 100%).</summary>
    public double ScaleX { get; set; } = 1.0;
    /// <summary>Escala en Y (1 = 100%).</summary>
    public double ScaleY { get; set; } = 1.0;
    /// <summary>Orden de render (mayor = delante).</summary>
    public int LayerOrder { get; set; }
    public string Nombre { get; set; } = "";
    /// <summary>
    /// Override de colisión (null = usar definición).
    /// </summary>
    public bool? ColisionOverride { get; set; }
    /// <summary>Tipo de colisión: "Solid", "Trigger", "Surface".</summary>
    public string? CollisionType { get; set; }
    public bool? InteractivoOverride { get; set; }
    public bool? DestructibleOverride { get; set; }
    public string? ScriptIdOverride { get; set; }
    /// <summary>Lista de scripts asignados (múltiples). Si vacío, se usa ScriptIdOverride.</summary>
    public List<string> ScriptIds { get; set; } = new();
    /// <summary>Propiedades públicas por script (clave-valor por ScriptId). Se guardan con el proyecto.</summary>
    public List<ScriptInstancePropertySet> ScriptProperties { get; set; } = new();
    /// <summary>Etiquetas para filtros y búsqueda.</summary>
    public List<string> Tags { get; set; } = new();
    /// <summary>Si false, el objeto no tiene representación visual en el mapa (objeto invisible con script en coordenada específica). En el editor se muestra un marcador pequeño.</summary>
    public bool Visible { get; set; } = true;

    public bool GetColision(ObjectDefinition definition)
    {
        return ColisionOverride ?? definition.Colision;
    }

    public bool GetInteractivo(ObjectDefinition definition)
    {
        return InteractivoOverride ?? definition.Interactivo;
    }

    public bool GetDestructible(ObjectDefinition definition)
    {
        return DestructibleOverride ?? definition.Destructible;
    }

    public string? GetScriptId(ObjectDefinition definition)
    {
        if (ScriptIds != null && ScriptIds.Count > 0) return ScriptIds[0];
        return ScriptIdOverride ?? definition.ScriptId;
    }

    /// <summary>Devuelve todos los scripts asignados (instancia + definición).</summary>
    public IReadOnlyList<string> GetScriptIds(ObjectDefinition? definition)
    {
        var list = new List<string>();
        if (ScriptIds != null && ScriptIds.Count > 0)
            list.AddRange(ScriptIds);
        else if (ScriptIdOverride != null)
            list.Add(ScriptIdOverride);
        else if (definition?.ScriptId != null)
            list.Add(definition.ScriptId);
        return list;
    }
}
