using System.Windows;

namespace FUEngine;

public partial class DocumentationWindow : Window
{
    /// <summary>Si coincide con un tema del manual, se selecciona al abrir.</summary>
    public string? InitialTopicId { get; set; }

    public DocumentationWindow()
    {
        InitializeComponent();
        DocHost.SetDetachChromeVisible(false);
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        DocHost.Open(InitialTopicId);
    }

    private void DocHost_OnRequestClose(object? sender, System.EventArgs e) => Close();
}
