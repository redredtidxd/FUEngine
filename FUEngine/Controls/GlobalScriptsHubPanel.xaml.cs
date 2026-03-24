using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FUEngine;

/// <summary>Lista + editor integrado (AvalonEdit) para la carpeta Scripts bajo assets compartidos, sin ventana aparte.</summary>
public partial class GlobalScriptsHubPanel : System.Windows.Controls.UserControl
{
    private string _scriptsRoot = "";
    private readonly Dictionary<string, OpenScriptTab> _openTabs = new(StringComparer.OrdinalIgnoreCase);

    public GlobalScriptsHubPanel()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshFileList();
    }

    public void RefreshFileList()
    {
        try
        {
            var st = EngineSettings.Load();
            var shared = GlobalAssetLibraryService.ResolveSharedAssetsRoot(st);
            _scriptsRoot = Path.Combine(shared, "Scripts");
            Directory.CreateDirectory(_scriptsRoot);
            if (TxtScriptsRoot != null)
                TxtScriptsRoot.Text = _scriptsRoot;

            ScriptsList.Items.Clear();
            if (!Directory.Exists(_scriptsRoot)) return;

            var items = new List<ScriptRow>();
            foreach (var full in Directory.EnumerateFiles(_scriptsRoot, "*.lua", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                var rel = Path.GetRelativePath(_scriptsRoot, full);
                items.Add(new ScriptRow { FullPath = full, Display = rel.Replace('\\', '/') });
            }

            if (items.Count == 0)
            {
                var stub = Path.Combine(_scriptsRoot, "_ejemplo_global.lua");
                if (!File.Exists(stub))
                    File.WriteAllText(stub, "-- Scripts Lua compartidos entre proyectos\n-- Mismo editor que en el proyecto: resaltado y Ctrl+Espacio\n\nlocal function noop() end\n");
                items.Add(new ScriptRow { FullPath = stub, Display = Path.GetFileName(stub) });
            }

            foreach (var it in items)
                ScriptsList.Items.Add(it);
            ScriptsList.DisplayMemberPath = nameof(ScriptRow.Display);
            if (ScriptsList.Items.Count > 0 && ScriptsList.SelectedIndex < 0)
                ScriptsList.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            if (TxtScriptsRoot != null)
                TxtScriptsRoot.Text = "Error: " + ex.Message;
        }
    }

    private void ScriptsList_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => OpenSelected();

    private void ScriptsList_OnMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => OpenSelected();

    private void OpenSelected()
    {
        if (ScriptsList.SelectedItem is not ScriptRow row || string.IsNullOrEmpty(row.FullPath)) return;
        if (!File.Exists(row.FullPath)) return;
        if (!string.Equals(Path.GetExtension(row.FullPath), ".lua", StringComparison.OrdinalIgnoreCase)) return;
        if (string.IsNullOrEmpty(_scriptsRoot)) return;
        LuaEditorCompletionCatalog.MergeDynamic(null);
        OpenOrSelectTab(row.FullPath, Path.GetFileName(row.FullPath));
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

        var headerPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
        var headerText = new TextBlock
        {
            Text = displayName,
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3)),
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

    private sealed class ScriptRow
    {
        public string FullPath { get; set; } = "";
        public string Display { get; set; } = "";
    }
}
