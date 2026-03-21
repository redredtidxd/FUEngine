namespace FUEngine.Editor;

/// <summary>
/// DTO para guardar/cargar definiciones de objetos e instancias.
/// </summary>
public class ObjectsDto
{
    public List<ObjectDefinitionDto> Definitions { get; set; } = new();
    public List<ObjectInstanceDto> Instances { get; set; } = new();
}

public class ObjectDefinitionDto
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
    public string? AnimatronicType { get; set; }
    public string? MovementPattern { get; set; }
    public string? Personality { get; set; }
    public bool CanDetectPlayer { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool EnableInGameDrawing { get; set; }
}

public class ObjectInstanceDto
{
    public string InstanceId { get; set; } = "";
    public string DefinitionId { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public double Rotation { get; set; }
    public double ScaleX { get; set; } = 1.0;
    public double ScaleY { get; set; } = 1.0;
    public int LayerOrder { get; set; }
    public string Nombre { get; set; } = "";
    public bool? ColisionOverride { get; set; }
    public string? CollisionType { get; set; }
    public bool? InteractivoOverride { get; set; }
    public bool? DestructibleOverride { get; set; }
    public string? ScriptIdOverride { get; set; }
    public List<string> ScriptIds { get; set; } = new();
    public List<ScriptInstancePropertySetDto> ScriptProperties { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public bool Visible { get; set; } = true;
}

public class ScriptInstancePropertySetDto
{
    public string ScriptId { get; set; } = "";
    public List<ScriptPropertyEntryDto> Properties { get; set; } = new();
}

public class ScriptPropertyEntryDto
{
    public string Key { get; set; } = "";
    public string Type { get; set; } = "string";
    public string Value { get; set; } = "";
}
