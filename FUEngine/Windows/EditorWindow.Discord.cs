using System;
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
        if (MainTabs?.SelectedItem is not TabItem selectedTab)
        {
            DiscordRichPresenceService.Instance.SetEditorActivity($"Proyecto: {projectName}", "Editor");
            return;
        }

        var tag = selectedTab.Tag as string ?? "Mapa";
        var tabLabel = GetVisibleTabHeaderText(selectedTab);
        var embeddedPlay = GetEmbeddedGameTab()?.IsActiveAndRunning == true;
        var onGameTab = string.Equals(tag, "Juego", StringComparison.Ordinal);

        // Solo "sandbox" cuando el usuario está *viendo* la pestaña Juego con Play en marcha.
        // Si cambia a Mapa/Consola/… con Play en segundo plano, Discord refleja esa pestaña.
        if (embeddedPlay && onGameTab)
        {
            DiscordRichPresenceService.Instance.SetEditorActivity(
                "Probando juego",
                $"Modo sandbox · {projectName}");
            return;
        }

        var explorerManifest = _selection?.SelectedExplorerItem;
        if (explorerManifest != null && !explorerManifest.IsFolder &&
            !string.IsNullOrEmpty(explorerManifest.FullPath) &&
            ProjectManifestPaths.IsActiveProjectManifestFile(explorerManifest.FullPath, _project.ProjectDirectory) &&
            !string.Equals(tag, "Scripts", StringComparison.Ordinal))
        {
            DiscordRichPresenceService.Instance.SetEditorActivity("Configurando ajustes globales", $"Proyecto: {projectName}");
            return;
        }

        var explorerSeed = _selection?.SelectedExplorerItem;
        if (explorerSeed != null && !explorerSeed.IsFolder &&
            !string.IsNullOrEmpty(explorerSeed.FullPath) &&
            explorerSeed.FullPath.EndsWith(".seed", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(tag, "Scripts", StringComparison.Ordinal))
        {
            var label = Path.GetFileNameWithoutExtension(explorerSeed.FullPath);
            if (SeedExplorerHelpers.TryGetFirstSeed(explorerSeed.FullPath, out var sd) && sd != null &&
                !string.IsNullOrWhiteSpace(sd.Nombre))
                label = sd.Nombre.Trim();
            DiscordRichPresenceService.Instance.SetEditorActivity($"Modificando semilla: {label}", $"Proyecto: {projectName}");
            return;
        }

        if (string.Equals(tag, "Scripts", StringComparison.Ordinal))
        {
            var scriptPath = (GetTabByKind("Scripts")?.Content as ScriptsTabContent)?.GetEditorControl()?.FilePath;
            var fileHint = string.IsNullOrEmpty(scriptPath) ? "sin archivo" : Path.GetFileName(scriptPath);
            var state = embeddedPlay ? $"{tabLabel} · {fileHint} · Play en segundo plano" : $"{tabLabel} · {fileHint}";
            DiscordRichPresenceService.Instance.SetEditorActivity($"Proyecto: {projectName}", state);
            return;
        }

        var stateLine = embeddedPlay ? $"{tabLabel} · Play en segundo plano" : tabLabel;
        DiscordRichPresenceService.Instance.SetEditorActivity($"Proyecto: {projectName}", stateLine);
    }

    /// <summary>Texto que ve el usuario en la pestaña (Mapa, Consola, icono + nombre dinámico, UI:…).</summary>
    private string GetVisibleTabHeaderText(TabItem tab)
    {
        if (tab.Header is string s)
            return NormalizeTabHeaderForDiscord(s);
        if (tab.Header is StackPanel sp)
        {
            foreach (var child in sp.Children)
            {
                if (child is TextBlock tb)
                    return NormalizeTabHeaderForDiscord(tb.Text ?? "");
            }
        }
        if (tab.Tag is string tag)
        {
            if (tag.StartsWith("UI:", StringComparison.OrdinalIgnoreCase))
            {
                var id = tag.Length > 3 ? tag.Substring(3) : "";
                return string.IsNullOrEmpty(id) ? "UI" : $"UI · {id}";
            }
            return TabDisplayNames.GetValueOrDefault(tag, tag);
        }
        return "Editor";
    }

    private static string NormalizeTabHeaderForDiscord(string t)
    {
        t = (t ?? "").Trim();
        if (t.EndsWith(" *", StringComparison.Ordinal))
            t = t[..^2].TrimEnd();
        return string.IsNullOrEmpty(t) ? "Pestaña" : t;
    }
}
