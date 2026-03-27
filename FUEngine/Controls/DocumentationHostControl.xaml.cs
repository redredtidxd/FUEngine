using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using FUEngine.Help;

namespace FUEngine;

/// <summary>Pestañas: manual general del motor y referencia Lua (palabras reservadas + guías de librería).</summary>
public partial class DocumentationHostControl : System.Windows.Controls.UserControl
{
    /// <summary>
    /// Cuenta entradas a <see cref="Open"/>; <see cref="DocTabs_OnSelectionChanged"/> se ignora mientras sea &gt; 0.
    /// WPF puede entregar <see cref="TabControl.SelectionChanged"/> de forma diferida: un simple bool en <c>finally</c> se limpia
    /// antes de ese evento y provoca un segundo <c>DocumentationView.Open</c> + <c>Items.Clear</c> en el ListBox durante el clic (reentrada, cierre del proceso).
    /// </summary>
    private int _suppressDocTabSelectionDepth;

    /// <summary>Última pestaña aplicada por <see cref="Open"/> o por el usuario; evita SelectionChanged duplicados del TabControl.</summary>
    private int _lastDocTabIndex = -1;

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
        _suppressDocTabSelectionDepth++;
        try
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
