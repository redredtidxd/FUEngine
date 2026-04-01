namespace FUEngine;

/// <summary>Argumentos para crear un <c>.lua</c> desde un ejemplo de la documentación.</summary>
public sealed class CreateScriptFromExampleEventArgs : EventArgs
{
    public CreateScriptFromExampleEventArgs(string suggestedFileName, string luaBody)
    {
        SuggestedFileName = suggestedFileName ?? "ejemplo.lua";
        LuaBody = luaBody ?? "";
    }

    public string SuggestedFileName { get; }
    public string LuaBody { get; }
}
