using System;

namespace FUEngine;

/// <summary>Plantilla inicial para archivos .lua creados desde el explorador (hooks + @prop para el Inspector).</summary>
public static class DefaultLuaScriptTemplate
{
    private const string Template = """
-- ==========================================
-- Script generado por FUEngine
-- ==========================================
-- Variables @prop: aparecen en el Inspector del objeto (Variables de script).
-- ---@ (EmmyLua): opcional; útil para LuaLS / revisión (no afecta al runtime).

-- @prop speed: float = 5.0
-- @prop isActive: bool = true

function onStart()
    -- Se ejecuta una vez cuando el objeto está activo en la escena
    print("{ScriptNameLua} iniciado (objeto: " .. tostring(self and self.name or "?") .. ")")
end

---@param dt number
function onUpdate(dt)
    -- Se ejecuta cada frame; dt = delta en segundos
    if not isActive then return end
    -- log.info("tick")  -- opcional: niveles info / warn / error en consola
    -- Ejemplo: depuración en el tab Juego
    -- Debug.drawLine(0, 0, 5, 5, 255, 255, 255, 180)
end

function onDestroy()
    -- Limpieza al destruir el objeto
end

""";

    /// <param name="scriptBaseName">Nombre sin extensión .lua (p. ej. el id del archivo).</param>
    public static string Format(string scriptBaseName)
    {
        var name = string.IsNullOrWhiteSpace(scriptBaseName) ? "Script" : scriptBaseName.Trim();
        var luaLiteral = EscapeForLuaDoubleQuotedString(name);
        return Template
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
