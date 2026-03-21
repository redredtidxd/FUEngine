using System.IO;
using FUEngine.Core;

namespace FUEngine;

/// <summary>
/// Ensures CanvasController.lua exists when any object has EnableInGameDrawing.
/// Template allows the player to paint on that object in PlayMode (0 to Width, 0 to Height).
/// </summary>
public static class CanvasControllerLuaTemplate
{
    public const string ScriptFileName = "CanvasController.lua";

    private const string TemplateContent = @"-- CanvasController.lua (generado por FUEngine)
-- Permite al jugador pintar sobre objetos con ""Habilitar dibujo en juego"" en el Inspector.
-- Respeta los límites de la textura (0 a Ancho, 0 a Alto).
-- Puedes editar este script para personalizar el comportamiento.

function onLoad()
  -- Objetos con EnableInGameDrawing pueden recibir eventos de pintura aquí.
  -- Ejemplo: registrar área de dibujo (API depende del runtime).
end

function onPaint(x, y, r, g, b, a)
  -- Llamado cuando el jugador pinta en (x, y) con color (r, g, b, a).
  -- x, y en coordenadas de textura (0 .. Ancho, 0 .. Alto).
end
";

    /// <summary>
    /// If any definition in the layer has EnableInGameDrawing, ensures Assets/Scripts/CanvasController.lua exists.
    /// Does not overwrite if the file already exists (user may have edited it).
    /// </summary>
    public static void EnsureCanvasControllerScriptIfNeeded(string? projectDirectory, ObjectLayer? layer)
    {
        if (string.IsNullOrWhiteSpace(projectDirectory) || layer == null) return;
        var hasAny = false;
        foreach (var def in layer.Definitions.Values)
        {
            if (def.EnableInGameDrawing) { hasAny = true; break; }
        }
        if (!hasAny) return;

        var scriptsDir = Path.Combine(projectDirectory, "Assets", "Scripts");
        var path = Path.Combine(scriptsDir, ScriptFileName);
        if (File.Exists(path)) return;

        try
        {
            if (!Directory.Exists(scriptsDir))
                Directory.CreateDirectory(scriptsDir);
            File.WriteAllText(path, TemplateContent);
        }
        catch
        {
            /* ignore */
        }
    }
}
