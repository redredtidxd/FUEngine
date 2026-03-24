using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FUEngine.Core;

namespace FUEngine;

public partial class ScriptsTabContent : System.Windows.Controls.UserControl
{
    private string _projectDirectory = "";
    private List<ScriptDefinition>? _scripts;
    private readonly Dictionary<string, OpenScriptTab> _openTabs = new(StringComparer.OrdinalIgnoreCase);

    public ScriptsTabContent()
    {
        InitializeComponent();
    }

    public void SetProjectDirectory(string projectDirectory)
    {
        _projectDirectory = projectDirectory ?? "";
    }

    public void SetScripts(IEnumerable<ScriptDefinition>? scripts, string? selectScriptId = null)
    {
        _scripts = scripts?.ToList();
        ScriptsList.Items.Clear();

        static string NormRel(string rel) => rel.Replace('\\', '/').TrimStart('/').Replace('/', Path.DirectorySeparatorChar);

        static string GetScriptRelativePath(ScriptDefinition s)
        {
            if (!string.IsNullOrWhiteSpace(s.Path))
                return NormRel(s.Path.Trim());
            return NormRel(Path.Combine("Scripts", s.Id + ".lua"));
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rows = new List<ScriptListItem>();

        if (_scripts != null)
        {
            foreach (var s in _scripts)
            {
                var rel = GetScriptRelativePath(s);
                seen.Add(rel);
                var fileName = Path.GetFileName(rel);
                rows.Add(new ScriptListItem { Id = s.Id, Display = $"{s.Nombre} · {fileName}", OpenRelativePath = null });
            }
        }

        if (!string.IsNullOrEmpty(_projectDirectory))
        {
            var rootJson = Path.Combine(_projectDirectory, "scripts.json");
            if (File.Exists(rootJson) && !seen.Contains("scripts.json"))
            {
                rows.Insert(0, new ScriptListItem
                {
                    Id = "__file_scripts_json__",
                    Display = "scripts.json · registro",
                    OpenRelativePath = "scripts.json"
                });
                seen.Add("scripts.json");
            }

            var scriptsDir = Path.Combine(_projectDirectory, "Scripts");
            if (Directory.Exists(scriptsDir))
            {
                foreach (var full in Directory.EnumerateFiles(scriptsDir, "*.json", SearchOption.TopDirectoryOnly).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                {
                    var rel = NormRel(Path.GetRelativePath(_projectDirectory, full));
                    if (seen.Contains(rel)) continue;
                    seen.Add(rel);
                    var name = Path.GetFileName(full);
                    rows.Add(new ScriptListItem
                    {
                        Id = "__json_" + rel,
                        Display = $"{name} · JSON",
                        OpenRelativePath = rel
                    });
                }
                foreach (var full in Directory.EnumerateFiles(scriptsDir, "*.lua", SearchOption.TopDirectoryOnly).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                {
                    var rel = NormRel(Path.GetRelativePath(_projectDirectory, full));
                    if (seen.Contains(rel)) continue;
                    seen.Add(rel);
                    var name = Path.GetFileName(full);
                    rows.Add(new ScriptListItem
                    {
                        Id = "__lua_" + rel,
                        Display = $"{name} · Lua",
                        OpenRelativePath = rel
                    });
                }
                foreach (var full in Directory.EnumerateFiles(scriptsDir, "*.script", SearchOption.TopDirectoryOnly).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                {
                    var rel = NormRel(Path.GetRelativePath(_projectDirectory, full));
                    if (seen.Contains(rel)) continue;
                    seen.Add(rel);
                    var name = Path.GetFileName(full);
                    rows.Add(new ScriptListItem
                    {
                        Id = "__script_" + rel,
                        Display = $"{name} · script",
                        OpenRelativePath = rel
                    });
                }
            }
        }

        foreach (var r in rows)
            ScriptsList.Items.Add(r);

        ScriptsList.DisplayMemberPath = "Display";

        if (!string.IsNullOrEmpty(selectScriptId))
        {
            for (var i = 0; i < ScriptsList.Items.Count; i++)
            {
                if (ScriptsList.Items[i] is ScriptListItem li &&
                    string.Equals(li.Id, selectScriptId, StringComparison.OrdinalIgnoreCase))
                {
                    ScriptsList.SelectedIndex = i;
                    return;
                }
            }
        }

        if (ScriptsList.Items.Count > 0)
            ScriptsList.SelectedIndex = 0;
    }

    /// <summary>Devuelve el editor de la pestaña activa (para Run/Guardar desde la toolbar).</summary>
    public ScriptEditorControl? GetEditorControl() => ScriptsTabs.SelectedContent as ScriptEditorControl;

    /// <summary>Abre un archivo por ruta relativa al proyecto y coloca el cursor en la línea (1-based). Usado desde consola al hacer clic en un error.</summary>
    public void OpenFileAtLine(string relativePath, int line)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;
        var fullPath = Path.Combine(_projectDirectory, relativePath.Trim().Replace('/', Path.DirectorySeparatorChar));
        var displayName = Path.GetFileName(fullPath);
        OpenOrSelectTab(fullPath, displayName);
        GetEditorControl()?.GoToLine(line);
    }

    /// <summary>Abre un archivo por ruta absoluta en el editor integrado (desde explorador de proyecto).</summary>
    public void OpenFile(string fullPath, int line = 1)
    {
        if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath)) return;
        var displayName = Path.GetFileName(fullPath);
        OpenOrSelectTab(fullPath, displayName);
        if (line > 0) GetEditorControl()?.GoToLine(line);
    }

    /// <summary>Se dispara cuando se guarda un script (ruta completa). Para hot reload del runtime.</summary>
    public event EventHandler<string>? ScriptSaved;

    /// <summary>Se dispara cuando cambia el estado de modificación de cualquier script abierto (para asterisco en la pestaña).</summary>
    public event EventHandler<bool>? DirtyChanged;

    private bool HasAnyModified()
    {
        foreach (var openTab in _openTabs.Values)
            if (openTab.Editor.IsModified) return true;
        return false;
    }

    private void ScriptsList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        OpenSelectedScript();
    }

    private void ScriptsList_OnMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        OpenSelectedScript();
    }

    private void ScriptsList_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (e.OriginalSource is not System.Windows.DependencyObject dep) return;
        var lbi = FindParentListBoxItem(dep);
        if (lbi?.DataContext is ScriptListItem item)
            ScriptsList.SelectedItem = item;
    }

    private static ListBoxItem? FindParentListBoxItem(System.Windows.DependencyObject? dep)
    {
        while (dep != null)
        {
            if (dep is ListBoxItem lbi) return lbi;
            dep = VisualTreeHelper.GetParent(dep);
        }
        return null;
    }

    private void OpenSelectedScript()
    {
        if (ScriptsList.SelectedItem is not ScriptListItem item) return;
        if (!string.IsNullOrEmpty(item.OpenRelativePath))
        {
            var pathJson = Path.Combine(_projectDirectory, item.OpenRelativePath);
            if (!File.Exists(pathJson)) return;
            var tabName = Path.GetFileName(item.OpenRelativePath);
            OpenOrSelectTab(pathJson, tabName);
            return;
        }
        if (_scripts == null) return;
        var script = _scripts.FirstOrDefault(s => s.Id == item.Id);
        if (script == null) return;
        var relPath = !string.IsNullOrWhiteSpace(script.Path) ? script.Path : Path.Combine("Scripts", script.Id + ".lua");
        var pathLua = Path.Combine(_projectDirectory, relPath);
        OpenOrSelectTab(pathLua, Path.GetFileName(relPath));
    }

    private void ScriptsContext_Open(object sender, RoutedEventArgs e)
    {
        OpenSelectedScript();
    }

    private void ScriptsContext_OpenInExplorer(object sender, RoutedEventArgs e)
    {
        var fullPath = TryGetSelectedScriptFullPath();
        if (fullPath != null && File.Exists(fullPath))
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{fullPath}\"");
            }
            catch { /* ignore */ }
        }
    }

    private void ScriptsContext_OpenInExternalEditor(object sender, RoutedEventArgs e)
    {
        var fullPath = TryGetSelectedScriptFullPath();
        if (fullPath == null || !File.Exists(fullPath)) return;

        var settings = EngineSettings.Load();
        var editorExePath = settings.ExternalCodeEditorPath?.Trim() ?? "";
        var hasValidExe = !string.IsNullOrWhiteSpace(editorExePath) && File.Exists(editorExePath);

        if (!hasValidExe)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "notepad.exe",
                    Arguments = $"\"{fullPath}\"",
                    UseShellExecute = true
                });
            }
            catch { /* ignore */ }
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = editorExePath,
                Arguments = $"\"{fullPath}\"",
                UseShellExecute = true
            });
        }
        catch
        {
            // Fallback si el editor configurado falla al arrancar o no acepta la línea de comandos.
            ScriptsContext_OpenInExplorer(sender, e);
        }
    }

    private string? TryGetSelectedScriptFullPath()
    {
        if (ScriptsList.SelectedItem is not ScriptListItem item) return null;
        if (!string.IsNullOrEmpty(item.OpenRelativePath))
            return Path.Combine(_projectDirectory, item.OpenRelativePath);
        if (_scripts == null) return null;
        var script = _scripts.FirstOrDefault(s => s.Id == item.Id);
        if (script == null) return null;
        var relPath = !string.IsNullOrWhiteSpace(script.Path) ? script.Path : Path.Combine("Scripts", script.Id + ".lua");
        return Path.Combine(_projectDirectory, relPath);
    }

    private void OpenOrSelectTab(string fullPath, string displayName)
    {
        if (_openTabs.TryGetValue(fullPath, out var existing))
        {
            ScriptsTabs.SelectedItem = existing.TabItem;
            return;
        }
        var editor = new ScriptEditorControl();
        editor.LoadFile(fullPath);
        editor.ModifiedChanged += (_, _) => UpdateTabHeader(editor);
        editor.ScriptSaved += (_, path) => ScriptSaved?.Invoke(this, path);

        var headerPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
        var headerText = new TextBlock
        {
            Text = displayName,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3)),
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 12,
            Margin = new Thickness(0, 0, 8, 0)
        };
        var closeBtn = new System.Windows.Controls.Button
        {
            Content = "✕",
            Width = 18,
            Height = 18,
            Padding = new Thickness(0),
            Background = System.Windows.Media.Brushes.Transparent,
            Foreground = System.Windows.Media.Brushes.Gray,
            BorderThickness = new Thickness(0),
            FontSize = 10,
            Cursor = System.Windows.Input.Cursors.Hand,
            ToolTip = "Cerrar"
        };
        headerPanel.Children.Add(headerText);
        headerPanel.Children.Add(closeBtn);

        var tabItem = new TabItem
        {
            Header = headerPanel,
            Content = editor,
            Tag = fullPath
        };
        var openTab = new OpenScriptTab(tabItem, editor, headerText, fullPath, displayName);
        _openTabs[fullPath] = openTab;

        closeBtn.Click += (_, _) => CloseTab(fullPath);
        ScriptsTabs.Items.Add(tabItem);
        ScriptsTabs.SelectedItem = tabItem;
        UpdateTabHeader(editor);
    }

    private void UpdateTabHeader(ScriptEditorControl editor)
    {
        if (!_openTabs.TryGetValue(editor.FilePath, out var openTab)) return;
        openTab.HeaderText.Text = openTab.DisplayName + (editor.IsModified ? " ●" : "");
        DirtyChanged?.Invoke(this, HasAnyModified());
    }

    private void CloseTab(string fullPath)
    {
        if (!_openTabs.Remove(fullPath, out var openTab)) return;
        ScriptsTabs.Items.Remove(openTab.TabItem);
    }

    private sealed class OpenScriptTab
    {
        public TabItem TabItem { get; }
        public ScriptEditorControl Editor { get; }
        public TextBlock HeaderText { get; }
        public string FullPath { get; }
        public string DisplayName { get; }

        public OpenScriptTab(TabItem tabItem, ScriptEditorControl editor, TextBlock headerText, string fullPath, string displayName)
        {
            TabItem = tabItem;
            Editor = editor;
            HeaderText = headerText;
            FullPath = fullPath;
            DisplayName = displayName;
        }
    }

    private sealed class ScriptListItem
    {
        public string Id { get; set; } = "";
        public string Display { get; set; } = "";
        /// <summary>Ruta relativa al proyecto para abrir sin pasar por scripts.json (p. ej. scripts.json o JSON sueltos en Scripts/).</summary>
        public string? OpenRelativePath { get; set; }
    }
}
