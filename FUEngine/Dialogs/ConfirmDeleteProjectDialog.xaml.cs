using System.Windows;
using System.Windows.Controls;

namespace FUEngine;

public partial class ConfirmDeleteProjectDialog : Window
{
    public string ProjectName { get; }
    public bool Confirmed { get; private set; }

    public ConfirmDeleteProjectDialog(string projectName)
    {
        InitializeComponent();
        ProjectName = projectName ?? "";
        TxtProjectName.Text = ProjectName;
        TxtProjectNameLabel.Text = "Proyecto a eliminar:";
        UpdateDeleteEnabled();
    }

    private void TxtConfirmName_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateDeleteEnabled();
    }

    private void UpdateDeleteEnabled()
    {
        BtnDelete.IsEnabled = string.Equals(TxtConfirmName?.Text?.Trim(), ProjectName, System.StringComparison.Ordinal);
    }

    private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
    {
        Confirmed = false;
        Close();
    }

    private void BtnDelete_OnClick(object sender, RoutedEventArgs e)
    {
        if (!BtnDelete.IsEnabled) return;
        Confirmed = true;
        Close();
    }
}
