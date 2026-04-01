using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace FUEngine.Dialogs;

public partial class AddComponentPickerWindow : Window
{
    public sealed class ComponentPickItem
    {
        public string Category { get; init; } = "";
        public string Id { get; init; } = "";
        public string Title { get; init; } = "";
        public string Description { get; init; } = "";
        public bool Enabled { get; init; } = true;
    }

    private readonly List<ComponentPickItem> _all;
    public string? SelectedId { get; private set; }

    public AddComponentPickerWindow(IEnumerable<ComponentPickItem> items)
    {
        InitializeComponent();
        _all = items?.ToList() ?? new List<ComponentPickItem>();
        Filter("");
    }

    private void Filter(string q)
    {
        q = (q ?? "").Trim();
        var src = string.IsNullOrEmpty(q)
            ? _all
            : _all.Where(i => i.Title.Contains(q, System.StringComparison.OrdinalIgnoreCase)
                              || i.Id.Contains(q, System.StringComparison.OrdinalIgnoreCase)
                              || i.Category.Contains(q, System.StringComparison.OrdinalIgnoreCase)).ToList();
        LstItems.Items.Clear();
        foreach (var i in src.OrderBy(x => x.Category).ThenBy(x => x.Title))
        {
            var label = i.Enabled ? $"[{i.Category}] {i.Title}" : $"[{i.Category}] {i.Title} (próximamente)";
            LstItems.Items.Add(new System.Windows.Controls.ListBoxItem
            {
                Content = label + (string.IsNullOrWhiteSpace(i.Description) ? "" : " — " + i.Description),
                Tag = i,
                IsEnabled = i.Enabled
            });
        }
    }

    private void TxtSearch_OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        Filter(TxtSearch.Text);
    }

    private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BtnAdd_OnClick(object sender, RoutedEventArgs e)
    {
        TryPickAndClose();
    }

    private void LstItems_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        TryPickAndClose();
    }

    private void TryPickAndClose()
    {
        if (LstItems.SelectedItem is not System.Windows.Controls.ListBoxItem li || li.Tag is not ComponentPickItem pick)
        {
            System.Windows.MessageBox.Show(this, "Seleccione una entrada de la lista.", "Añadir componente", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!pick.Enabled) return;
        SelectedId = pick.Id;
        DialogResult = true;
        Close();
    }
}
