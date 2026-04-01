namespace FUEngine.Editor;

public class ScriptsDto
{
    public List<ScriptItemDto> Scripts { get; set; } = new();
}

public class ScriptItemDto
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Path { get; set; }
    public List<string> Eventos { get; set; } = new();
}
