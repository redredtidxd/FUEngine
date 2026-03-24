using System.Windows;
using System.Windows.Controls;
using FUEngine.Help;

namespace FUEngine;

/// <summary>Pestañas: manual general del motor y referencia Lua (palabras reservadas + guías de librería).</summary>
public partial class DocumentationHostControl : System.Windows.Controls.UserControl
{
    public DocumentationHostControl()
    {
        InitializeComponent();
        ManualDocView.LuaReferenceMode = false;
        LuaDocView.LuaReferenceMode = true;
        DocTabs.SelectionChanged += DocTabs_OnSelectionChanged;
    }

    public event EventHandler? RequestClose;

    private void Child_RequestClose(object? sender, System.EventArgs e) => RequestClose?.Invoke(this, e);

    /// <summary>Si <paramref name="initialTopicId"/> es un tema lua-kw-*, lua-guide-* o el índice Lua, se selecciona la segunda pestaña.</summary>
    public void Open(string? initialTopicId)
    {
        if (EngineDocumentation.IsLuaReferenceSidebarTopic(initialTopicId))
        {
            DocTabs.SelectedIndex = 1;
            EnsureLuaOpened(initialTopicId);
        }
        else
        {
            DocTabs.SelectedIndex = 0;
            EnsureManualOpened(initialTopicId);
        }
    }

    private void DocTabs_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        if (DocTabs.SelectedIndex == 1)
            EnsureLuaOpened(EngineDocumentation.LuaReferenceIntroTopicId);
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
}
