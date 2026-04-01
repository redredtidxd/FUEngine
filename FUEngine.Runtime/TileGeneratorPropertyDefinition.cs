namespace FUEngine.Runtime;

/// <summary>Defines a script parameter discovered via property("Name", default, min, max) in Lua.</summary>
public sealed class PropertyDefinition
{
    public string Name { get; set; } = "";
    public double Default { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
}
