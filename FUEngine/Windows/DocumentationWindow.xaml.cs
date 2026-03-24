using System.Windows;

namespace FUEngine;

public partial class DocumentationWindow : Window
{
    /// <summary>Si coincide con un tema del manual, se selecciona al abrir.</summary>
    public string? InitialTopicId
    {
        get => DocView.InitialTopicId;
        set => DocView.InitialTopicId = value;
    }

    public DocumentationWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        DocView.Open(InitialTopicId);
    }

    private void DocView_OnRequestClose(object? sender, System.EventArgs e) => Close();
}
