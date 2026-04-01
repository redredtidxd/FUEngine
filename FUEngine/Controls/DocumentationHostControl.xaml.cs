using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using FUEngine.Help;

namespace FUEngine;

/// <summary>Pestañas: manual general, referencia Lua y ejemplos de scripts.</summary>
public partial class DocumentationHostControl : System.Windows.Controls.UserControl
{
    private int _suppressDocTabSelectionDepth;

    private int _lastDocTabIndex = -1;

    public DocumentationHostControl()
    {
        InitializeComponent();
        ManualDocView.LuaReferenceMode = false;
        ManualDocView.ScriptExamplesMode = false;
        LuaDocView.LuaReferenceMode = true;
        LuaDocView.ScriptExamplesMode = false;
        ExamplesDocView.LuaReferenceMode = false;
        ExamplesDocView.ScriptExamplesMode = true;
        ExamplesDocView.RequestCreateScriptFromExample += ExamplesDocView_OnRequestCreateScriptFromExample;
        DocTabs.SelectionChanged += DocTabs_OnSelectionChanged;
    }

    public event EventHandler? RequestClose;

    /// <summary>True cuando el host es el editor con proyecto; habilita «Crear script desde ejemplo».</summary>
    public bool AllowCreateScriptFromProject { get; set; }

    public event EventHandler<CreateScriptFromExampleEventArgs>? RequestCreateScriptFromExample;

    private void ExamplesDocView_OnRequestCreateScriptFromExample(object? sender, CreateScriptFromExampleEventArgs e) =>
        RequestCreateScriptFromExample?.Invoke(this, e);

    private void Child_RequestClose(object? sender, System.EventArgs e) => RequestClose?.Invoke(this, e);

    /// <summary>Actualiza el delegado de permiso de exportación en la vista de ejemplos.</summary>
    public void SyncScriptExampleExportAvailability()
    {
        ExamplesDocView.AllowExportScriptExample = () => AllowCreateScriptFromProject;
    }

    public void Open(string? initialTopicId)
    {
        SyncScriptExampleExportAvailability();
        _suppressDocTabSelectionDepth++;
        try
        {
            if (EngineDocumentation.IsScriptExamplesSidebarTopic(initialTopicId))
            {
                DocTabs.SelectedIndex = 2;
                EnsureExamplesOpened(initialTopicId);
            }
            else if (EngineDocumentation.IsLuaReferenceSidebarTopic(initialTopicId))
            {
                DocTabs.SelectedIndex = 1;
                EnsureLuaOpened(initialTopicId);
            }
            else
            {
                DocTabs.SelectedIndex = 0;
                EnsureManualOpened(initialTopicId);
            }

            _lastDocTabIndex = DocTabs.SelectedIndex;
        }
        finally
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_suppressDocTabSelectionDepth > 0)
                    _suppressDocTabSelectionDepth--;
            }), DispatcherPriority.ApplicationIdle);
        }
    }

    private void DocTabs_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || _suppressDocTabSelectionDepth > 0) return;
        var idx = DocTabs.SelectedIndex;
        if (idx < 0) return;
        if (idx == _lastDocTabIndex) return;
        _lastDocTabIndex = idx;
        if (idx == 1)
            EnsureLuaOpened(EngineDocumentation.LuaReferenceIntroTopicId);
        else if (idx == 2)
            EnsureExamplesOpened(EngineDocumentation.ScriptExamplesIntroTopicId);
        else
            EnsureManualOpened(EngineDocumentation.QuickStartTopicId);
    }

    private void EnsureManualOpened(string? topicId)
    {
        ManualDocView.Open(topicId);
    }

    private void EnsureLuaOpened(string? topicId)
    {
        LuaDocView.Open(topicId);
    }

    private void EnsureExamplesOpened(string? topicId)
    {
        ExamplesDocView.Open(topicId);
    }
}
