using System.IO;
using System.Windows.Controls;

namespace FUEngine;

public partial class EditorWindow
{
    /// <summary>Antes de un modal: fija el texto; en <c>finally</c> llama a <see cref="SyncDiscordRichPresence"/>.</summary>
    internal void BeginModalDiscordPresence(string details, string state)
    {
        DiscordRichPresenceService.Instance.EnsureInitialized();
        DiscordRichPresenceService.Instance.SetEditorActivity(details, state);
    }

    /// <summary>Tras cerrar un modal u overlay que sustituyó el estado.</summary>
    internal void EndModalDiscordPresence() => SyncDiscordRichPresence();

    /// <summary>Actualiza el estado de Discord según pestaña, proyecto y Play embebido.</summary>
    internal void SyncDiscordRichPresence()
    {
        if (_project == null) return;
        DiscordRichPresenceService.Instance.EnsureInitialized();
        var projectName = string.IsNullOrWhiteSpace(_project.Nombre) ? "Proyecto sin nombre" : _project.Nombre.Trim();
        var tag = (MainTabs?.SelectedItem as TabItem)?.Tag as string ?? "Mapa";
        var embeddedPlay = GetEmbeddedGameTab()?.IsActiveAndRunning == true;

        if (embeddedPlay)
        {
            DiscordRichPresenceService.Instance.SetEditorActivity(
                "Probando juego",
                $"Modo sandbox · {projectName}");
            return;
        }

        if (string.Equals(tag, "Scripts", StringComparison.Ordinal))
        {
            var scriptPath = (GetTabByKind("Scripts")?.Content as ScriptsTabContent)?.GetEditorControl()?.FilePath;
            var fileHint = string.IsNullOrEmpty(scriptPath) ? "Lua" : Path.GetFileName(scriptPath);
            DiscordRichPresenceService.Instance.SetEditorActivity(
                "Programando en Lua",
                $"{fileHint} · {projectName}");
            return;
        }

        var state = TabKindToDiscordState(tag);
        DiscordRichPresenceService.Instance.SetEditorActivity(
            $"Proyecto: {projectName}",
            state);
    }

    private static string TabKindToDiscordState(string? tabKind) =>
        tabKind switch
        {
            "Mapa" => "Editando mapa",
            "Juego" => "Pestaña Juego (viewport)",
            "Consola" => "Consola y Lua",
            "Explorador" => "Explorador de archivos",
            "Tiles" => "Catálogo de tiles",
            "Animaciones" => "Animaciones",
            "Debug" => "Depuración / inspección Play",
            "Audio" => "Audio del proyecto",
            "Seeds" => "Seeds y objetos",
            "TileCreator" => "Crear tile",
            "TileEditor" => "Editar tile",
            "PaintCreator" => "Crear paint",
            "PaintEditor" => "Editar paint",
            "CollisionsEditor" => "Editor de colisiones",
            "ScriptableTile" => "Tile por script",
            "UITab" or "UI" => "Interfaz (UI)",
            _ => string.IsNullOrEmpty(tabKind) ? "Editor" : tabKind
        };
}
