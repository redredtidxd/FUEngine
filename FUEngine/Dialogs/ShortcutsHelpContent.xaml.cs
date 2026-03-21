using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FUEngine;

public partial class ShortcutsHelpContent : System.Windows.Controls.UserControl
{
    public ShortcutsHelpContent()
    {
        InitializeComponent();
        Loaded += (_, _) => Populate();
    }

    private void Populate()
    {
        if (ShortcutsPanel == null) return;
        ShortcutsPanel.Children.Clear();
        var accent = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x58, 0xa6, 0xff));
        var muted = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x8b, 0x94, 0x9e));
        var primary = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3));
        string? lastCat = null;
        var first = true;
        foreach (var line in EditorShortcutRegistry.Lines)
        {
            if (line.Category != lastCat)
            {
                lastCat = line.Category;
                ShortcutsPanel.Children.Add(new TextBlock
                {
                    Text = line.Category,
                    Foreground = accent,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, first ? 0 : 12, 0, 4)
                });
                first = false;
            }
            ShortcutsPanel.Children.Add(new TextBlock
            {
                Text = line.Keys + " — " + line.Description,
                Foreground = muted,
                Margin = new Thickness(0, 2, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });
        }
        ShortcutsPanel.Children.Add(new TextBlock
        {
            Text = "Los atajos no se pueden reasignar desde aquí; esta lista es solo de referencia.",
            Foreground = primary,
            FontSize = 11,
            Margin = new Thickness(0, 16, 0, 0),
            TextWrapping = TextWrapping.Wrap
        });
    }
}
