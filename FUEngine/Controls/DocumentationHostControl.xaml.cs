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
        Loaded += OnLoadedOnce;
    }

    private void OnLoadedOnce(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoadedOnce;
        ManualDocView.LuaReferenceMode = false;
        LuaDocView.LuaReferenceMode = true;
    }

    public event EventHandler? RequestClose;

    private void Child_RequestClose(object? sender, System.EventArgs e) => RequestClose?.Invoke(this, e);

    /// <summary>Si <paramref name="initialTopicId"/> es un tema lua-kw-*, lua-guide-* o el índice Lua, se selecciona la segunda pestaña.</summary>
    public void Open(string? initialTopicId)
    {
        if (EngineDocumentation.IsLuaReferenceSidebarTopic(initialTopicId))
        {
            DocTabs.SelectedIndex = 1;
            LuaDocView.Open(initialTopicId);
        }
        else
        {
            DocTabs.SelectedIndex = 0;
            ManualDocView.Open(initialTopicId);
        }
    }
}
