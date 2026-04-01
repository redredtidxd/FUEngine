namespace FUEngine.Core;

/// <summary>Asset de script (referencia a scripts.json por id). Evita duplicados.</summary>
public class ScriptAsset
{
    public string Id { get; set; } = "";
    public string ScriptId { get; set; } = "";
}
