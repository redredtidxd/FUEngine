using System.Collections.Generic;
using System.Windows.Controls;

namespace FUEngine;

/// <summary>
/// Panel mínimo que muestra la lista de objetos de la escena mientras está en ejecución el modo Play.
/// </summary>
public partial class PlayHierarchyPanel : System.Windows.Controls.UserControl
{
    public PlayHierarchyPanel()
    {
        InitializeComponent();
        ObjectList.ItemsSource = _names;
    }

    private readonly System.Collections.ObjectModel.ObservableCollection<string> _names = new();

    /// <summary>Actualiza la lista de nombres de objetos (llamar al iniciar/detener Play).</summary>
    public void SetObjectNames(IEnumerable<string>? names)
    {
        _names.Clear();
        if (names != null)
        {
            foreach (var n in names)
                _names.Add(n);
        }
    }
}
