using System;

namespace FUEngine;

/// <summary>Plantilla inicial para archivos .lua creados desde el explorador (onStart / onUpdate / onDestroy).</summary>
public static class DefaultLuaScriptTemplate
{
    private const string Template = """
-- Script: {ScriptName}
-- Generado por FUEngine
-- Variables públicas (globales sin "local"): el Inspector del objeto las detecta y puedes ajustar valores ahí.

speed = 5

function onStart()
    -- Se ejecuta una vez al iniciar el objeto
    print("{ScriptNameLua} iniciado.")
end

function onUpdate(dt)
    -- Se ejecuta cada frame. dt es el tiempo transcurrido (DeltaTime)
    -- Depuración en el viewport del tab Juego (coordenadas mundo = tiles como self / transform):
    -- Debug.drawLine(0, 0, 5, 5)
    -- Debug.drawCircle(0, 0, 2, 255, 80, 80, 200)
end

function onDestroy()
    -- Se ejecuta antes de eliminar el objeto
end

""";

    /// <param name="scriptBaseName">Nombre sin extensión .lua (p. ej. el id del archivo).</param>
    public static string Format(string scriptBaseName)
    {
        var name = string.IsNullOrWhiteSpace(scriptBaseName) ? "Script" : scriptBaseName.Trim();
        var luaLiteral = EscapeForLuaDoubleQuotedString(name);
        return Template
            .Replace("{ScriptName}", name, StringComparison.Ordinal)
            .Replace("{ScriptNameLua}", luaLiteral, StringComparison.Ordinal);
    }

    private static string EscapeForLuaDoubleQuotedString(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }
}
