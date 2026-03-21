namespace FUEngine.Core;

/// <summary>Materiales conocidos para tiles (física, destructible, reacciones).</summary>
public static class TileMaterial
{
    public const string None = "";
    public const string Sand = "sand";
    public const string Stone = "stone";
    public const string Wood = "wood";
    public const string Water = "water";
    public const string Metal = "metal";
    public const string Dirt = "dirt";

    public static readonly string[] All = { None, Sand, Stone, Wood, Water, Metal, Dirt };
}
