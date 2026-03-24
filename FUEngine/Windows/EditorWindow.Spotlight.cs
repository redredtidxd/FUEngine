using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using FUEngine.Core;
using FUEngine.Spotlight;

namespace FUEngine;

public partial class EditorWindow
{
    public string? ProjectDirectoryForSpotlight => _project.ProjectDirectory;

    public ObjectLayer ObjectLayerForSpotlight => _objectLayer;

    public void OpenProjectFileFromSpotlight(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath)) return;
        if (fullPath.EndsWith(".seed", StringComparison.OrdinalIgnoreCase))
        {
            AddOrSelectTab("Mapa");
            InstantiateSeedFromFile(fullPath);
            EditorLog.Toast("Seed colocado en el mapa desde Spotlight.", LogLevel.Info, "Seed");
            return;
        }
        var name = Path.GetFileName(fullPath);
        ProjectExplorer_OnRequestOpenInEditor(null, new ProjectExplorerItem { FullPath = fullPath, Name = name, IsFolder = false });
    }

    public void FocusSceneObjectFromSpotlight(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId)) return;
        var inst = _objectLayer.Instances.FirstOrDefault(i => i.InstanceId == instanceId);
        if (inst == null) return;
        _selection.SetObjectSelection(inst);
        AddOrSelectTab("Mapa");
        CenterViewOnWorldTile(inst.X, inst.Y);
        DrawMap();
        RefreshInspector();
    }

    private void CenterViewOnWorldTile(double wx, double wy)
    {
        if (ScrollViewer == null || MapCanvas == null) return;
        var tileSize = _project?.TileSize ?? 16;
        double cx = (wx - _canvasMinWx) * tileSize * _zoom;
        double cy = (wy - _canvasMinWy) * tileSize * _zoom;
        double vw = ScrollViewer.ViewportWidth;
        double vh = ScrollViewer.ViewportHeight;
        double h = Math.Max(0, cx - vw / 2);
        double v = Math.Max(0, cy - vh / 2);
        ScrollViewer.ScrollToHorizontalOffset(Math.Min(h, ScrollViewer.ScrollableWidth));
        ScrollViewer.ScrollToVerticalOffset(Math.Min(v, ScrollViewer.ScrollableHeight));
    }

    public void ShowSpotlight()
    {
        if (SpotlightOverlay.Visibility == Visibility.Visible)
        {
            SpotlightOverlay.Visibility = Visibility.Collapsed;
            SyncDiscordRichPresence();
            return;
        }
        DiscordRichPresenceService.Instance.EnsureInitialized();
        var pn = string.IsNullOrWhiteSpace(_project.Nombre) ? "Proyecto sin nombre" : _project.Nombre.Trim();
        DiscordRichPresenceService.Instance.SetEditorActivity("FUEngine · Spotlight", $"Buscar · {pn}");
        SpotlightEmbedded.SetContext(this, null);
        SpotlightOverlay.Visibility = Visibility.Visible;
        SpotlightEmbedded.Open();
    }

    private void SpotlightBackdrop_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        SpotlightOverlay.Visibility = Visibility.Collapsed;
        SyncDiscordRichPresence();
        e.Handled = true;
    }

    private void SpotlightEmbedded_RequestClose(object? sender, EventArgs e)
    {
        SpotlightOverlay.Visibility = Visibility.Collapsed;
        SyncDiscordRichPresence();
    }

    public void ShowDocumentation(string? initialTopicId)
    {
        DiscordRichPresenceService.Instance.EnsureInitialized();
        var pn = string.IsNullOrWhiteSpace(_project.Nombre) ? "Proyecto sin nombre" : _project.Nombre.Trim();
        DiscordRichPresenceService.Instance.SetEditorActivity("Manual del motor", $"Ayuda integrada · {pn}");
        DocumentationEmbedded.Open(initialTopicId);
        DocumentationOverlay.Visibility = Visibility.Visible;
    }

    private void DocumentationBackdrop_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DocumentationOverlay.Visibility = Visibility.Collapsed;
        SyncDiscordRichPresence();
        e.Handled = true;
    }

    private void DocumentationEmbedded_RequestClose(object? sender, EventArgs e)
    {
        DocumentationOverlay.Visibility = Visibility.Collapsed;
        SyncDiscordRichPresence();
    }
}
