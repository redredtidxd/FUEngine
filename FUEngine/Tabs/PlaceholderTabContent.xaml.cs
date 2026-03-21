using System.Windows.Controls;

namespace FUEngine;

public partial class PlaceholderTabContent : System.Windows.Controls.UserControl
{
    public PlaceholderTabContent()
    {
        InitializeComponent();
    }

    public void SetContent(string title, string description)
    {
        TxtTitle.Text = title;
        TxtDescription.Text = description;
    }
}
