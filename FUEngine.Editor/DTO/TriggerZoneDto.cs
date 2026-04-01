namespace FUEngine.Editor;

public class TriggerZonesDto
{
    public List<TriggerZoneItemDto> Zones { get; set; } = new();
}

public class TriggerZoneItemDto
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public string TriggerType { get; set; } = "OnEnter";
    public int LayerId { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; } = 1;
    public int Height { get; set; } = 1;
    public string? ScriptIdOnEnter { get; set; }
    public string? ScriptIdOnExit { get; set; }
    public string? ScriptIdOnTick { get; set; }
    public List<string> Tags { get; set; } = new();
}
