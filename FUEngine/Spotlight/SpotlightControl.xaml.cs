using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using FUEngine.Help;
using EditorWindow = global::FUEngine.EditorWindow;
using StartupWindow = global::FUEngine.StartupWindow;

namespace FUEngine.Spotlight;

public partial class SpotlightControl : System.Windows.Controls.UserControl
{
    private EditorWindow? _editor;
    private StartupWindow? _startup;
    private readonly ObservableCollection<SpotlightItem> _items = new();
    private readonly CollectionViewSource _resultsView = new();
    private readonly DispatcherTimer _debounce = new() { Interval = TimeSpan.FromMilliseconds(180) };
    private bool _initialized;

    public event EventHandler? RequestClose;

    public SpotlightControl()
    {
        InitializeComponent();
        _debounce.Tick += (_, _) => { _debounce.Stop(); RunSearch(TxtQuery?.Text ?? ""); };
    }

    public void SetContext(EditorWindow? editor, StartupWindow? startup)
    {
        _editor = editor;
        _startup = startup;
    }

    public void Open()
    {
        UpdateIndexTotals();
        TxtQuery.Text = "";
        RunSearch("");
        Dispatcher.BeginInvoke(new Action(() =>
        {
            TxtQuery?.Focus();
            TxtQuery?.SelectAll();
        }), DispatcherPriority.Loaded);
    }

    private void SpotlightControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized) return;
        _initialized = true;
        EngineTypography.ApplyToRoot(this);
        SpotlightIndex.EnsureBuilt();
        _resultsView.Source = _items;
        _resultsView.GroupDescriptions.Clear();
        _resultsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(SpotlightItem.GroupHeader)));
        ResultList.ItemsSource = _resultsView.View;
        UpdateIndexTotals();
    }

    private void SpotlightControl_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CloseInternal();
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Enter && ResultList.SelectedItem is SpotlightItem item)
        {
            Execute(item);
            e.Handled = true;
        }
    }

    private void CloseInternal() => RequestClose?.Invoke(this, EventArgs.Empty);

    /// <summary>Abre un tema del manual alineado con la entrada Lua (hooks, API, palabras clave, capa, ads, etc.).</summary>
    private static string ResolveLuaApiManualTopicId(SpotlightItem item)
    {
        var key = (item.LuaSignature ?? item.Title ?? "").Trim();
        if (key.Length > 0)
        {
            if (key.StartsWith("layer.", StringComparison.OrdinalIgnoreCase))
                return "scripts-capa-layer";
            if (key.StartsWith("ads.", StringComparison.OrdinalIgnoreCase))
                return "ads-simulado";
            if (key.StartsWith("physics.", StringComparison.OrdinalIgnoreCase))
                return "fisica-raycast-dos-mundos";
            if (key.StartsWith("Debug.", StringComparison.OrdinalIgnoreCase))
                return "depuracion-y-consola";
            if (key.StartsWith("ui.", StringComparison.OrdinalIgnoreCase))
                return "iluminacion-audio-ui";
            if (key.StartsWith("component.", StringComparison.OrdinalIgnoreCase))
                return "jerarquia-gameobject-lua";
            if (key.StartsWith("world.", StringComparison.OrdinalIgnoreCase) &&
                key.Contains("raycast", StringComparison.OrdinalIgnoreCase))
                return "fisica-raycast-dos-mundos";
        }

        if (item.Subtitle == "Hook / evento")
            return "eventos-hooks-lua";
        if (item.Subtitle == "Resumen API (tabla global)")
            return "scripting-lua";
        if (item.Subtitle == "API Lua (reflexión)")
            return "scripting-lua";
        if (item.Subtitle == LuaSpotlightBuiltins.BuiltinSubtitle)
            return "scripting-lua";
        if (item.Subtitle == LuaLanguageKeywords.KeywordSubtitle)
            return "editor-mini-ide-lua";
        return "scripting-lua";
    }

    private void UpdateIndexTotals()
    {
        var t = SpotlightIndex.GetIndexTotals();
        TxtIndexTotals.Text =
            $"Índice: Manual {t.Documentation} · Ejemplos {t.ScriptExamples} · Lua {t.LuaTotal} (resúmenes tablas {t.LuaGlobalGuides} · API {t.LuaReflection} · hooks {t.LuaHooks} · estándar {t.LuaBuiltins} · sintaxis {t.LuaKeywords}) · Hub: {t.HubRecentProjects} proyectos recientes";
    }

    private static string BuildMatchSummary(IReadOnlyList<SpotlightItem> items)
    {
        if (items.Count == 0) return "Sin coincidencias en esta búsqueda.";
        var ext = items.Count(i => i.Category == SpotlightCategory.ExternalDoc);
        var doc = items.Count(i => i.Category == SpotlightCategory.Documentation);
        var ex = items.Count(i => i.Category == SpotlightCategory.ScriptExamples);
        var lua = items.Count(i => i.Category == SpotlightCategory.LuaApi);
        var hub = items.Count(i => i.Category == SpotlightCategory.HubProject);
        var files = items.Count(i => i.Category == SpotlightCategory.ProjectFile);
        var scene = items.Count(i => i.Category == SpotlightCategory.SceneObject);
        var parts = new List<string>();
        if (ext > 0) parts.Add($"Notas/repo {ext}");
        if (doc > 0) parts.Add($"Manual {doc}");
        if (ex > 0) parts.Add($"Ejemplos {ex}");
        if (lua > 0) parts.Add($"Lua {lua}");
        if (hub > 0) parts.Add($"Hub {hub}");
        if (files > 0) parts.Add($"Archivos {files}");
        if (scene > 0) parts.Add($"Escena {scene}");
        return $"{items.Count} coincidencias · " + string.Join(" · ", parts);
    }

    private void TxtQuery_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        _debounce.Stop();
        _debounce.Start();
    }

    private void RunSearch(string q)
    {
        _items.Clear();
        var query = q ?? "";

        foreach (var x in SpotlightIndex.FilterDocs(query))
            _items.Add(x);
        foreach (var x in SpotlightIndex.FilterScriptExamples(query))
            _items.Add(x);
        foreach (var x in SpotlightIndex.FilterLua(query))
            _items.Add(x);

        var ch = SpotlightIndex.MatchChangelogFile(query);
        if (ch != null) _items.Insert(0, ch);

        if (_editor != null)
        {
            var proj = _editor.ProjectDirectoryForSpotlight;
            if (!string.IsNullOrEmpty(proj))
            {
                foreach (var o in SpotlightIndex.SearchSceneObjects(_editor.ObjectLayerForSpotlight, query).Take(15))
                    _items.Add(o);
            }

            if (!string.IsNullOrWhiteSpace(query) && query.Length >= 2 && !string.IsNullOrEmpty(proj))
            {
                foreach (var f in SpotlightIndex.SearchProjectFiles(proj, query).Take(25))
                    _items.Add(f);
            }
        }
        else if (_startup != null && query.Length >= 1)
        {
            foreach (var h in SpotlightIndex.SearchHubProjects(query))
                _items.Add(h);
        }

        TxtMatchTotals.Text = BuildMatchSummary(_items.ToList());
        if (_items.Count > 0 && ResultList.SelectedIndex < 0)
            ResultList.SelectedIndex = 0;
        UpdateDetail();
    }

    private void ResultList_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateDetail();

    private void UpdateDetail()
    {
        if (ResultList.SelectedItem is not SpotlightItem item)
        {
            TxtDetailTitle.Text = "";
            TxtDetailBody.Text = "";
            TxtDetailExample.Text = "";
            LblExample.Visibility = Visibility.Collapsed;
            TxtDetailExample.Visibility = Visibility.Collapsed;
            return;
        }
        TxtDetailTitle.Text = item.Title;
        TxtDetailBody.Text = string.IsNullOrWhiteSpace(item.LuaDetail) ? item.Subtitle : item.LuaDetail!;
        if (!string.IsNullOrEmpty(item.LuaExample))
        {
            TxtDetailExample.Text = item.LuaExample;
            LblExample.Visibility = Visibility.Visible;
            TxtDetailExample.Visibility = Visibility.Visible;
        }
        else
        {
            TxtDetailExample.Text = "";
            LblExample.Visibility = Visibility.Collapsed;
            TxtDetailExample.Visibility = Visibility.Collapsed;
        }
    }

    private void ResultList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ResultList.SelectedItem is SpotlightItem item)
            Execute(item);
    }

    private void Execute(SpotlightItem item)
    {
        try
        {
            var host = Window.GetWindow(this);
            switch (item.Category)
            {
                case SpotlightCategory.Documentation:
                case SpotlightCategory.ScriptExamples:
                    if (!string.IsNullOrEmpty(item.DocumentationTopicId))
                    {
                        if (host is EditorWindow ed)
                            ed.ShowDocumentation(item.DocumentationTopicId);
                        else if (host is StartupWindow su)
                            su.ShowDocumentation(item.DocumentationTopicId);
                        else
                            new global::FUEngine.DocumentationWindow { Owner = host, InitialTopicId = item.DocumentationTopicId }.Show();
                    }
                    break;
                case SpotlightCategory.LuaApi:
                {
                    var topicId = !string.IsNullOrEmpty(item.DocumentationTopicId)
                        ? item.DocumentationTopicId
                        : ResolveLuaApiManualTopicId(item);
                    if (!string.IsNullOrEmpty(topicId))
                    {
                        if (host is EditorWindow edLua)
                            edLua.ShowDocumentation(topicId);
                        else if (host is StartupWindow suLua)
                            suLua.ShowDocumentation(topicId);
                        else
                            new global::FUEngine.DocumentationWindow { Owner = host, InitialTopicId = topicId }.Show();
                    }
                    break;
                }
                case SpotlightCategory.ProjectFile:
                    if (!string.IsNullOrEmpty(item.FilePath) && _editor != null)
                        _editor.OpenProjectFileFromSpotlight(item.FilePath);
                    break;
                case SpotlightCategory.SceneObject:
                    if (!string.IsNullOrEmpty(item.ObjectInstanceId) && _editor != null)
                        _editor.FocusSceneObjectFromSpotlight(item.ObjectInstanceId);
                    break;
                case SpotlightCategory.HubProject:
                    if (!string.IsNullOrEmpty(item.HubProjectPath) && _startup != null)
                        _startup.OpenRecentProjectFromPath(item.HubProjectPath);
                    break;
                case SpotlightCategory.ExternalDoc:
                    if (!string.IsNullOrEmpty(item.ExternalMarkdownPath) && File.Exists(item.ExternalMarkdownPath))
                        Process.Start(new ProcessStartInfo(item.ExternalMarkdownPath) { UseShellExecute = true });
                    break;
            }
        }
        catch { /* ignore */ }
        CloseInternal();
    }
}
