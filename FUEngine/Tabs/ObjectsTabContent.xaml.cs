using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FUEngine.Core;

namespace FUEngine;

public partial class ObjectsTabContent : System.Windows.Controls.UserControl
{
    public const string DataFormatObjectDefinitionId = "FUEngine.ObjectDefinitionId";

    private System.Windows.Point? _dragStartPos;
    private ObjectDefinition? _dragSourceDefinition;

    public ObjectsTabContent()
    {
        InitializeComponent();
    }

    public void SetObjects(IEnumerable<ObjectDefinition>? definitions)
    {
        ObjectsList.Items.Clear();
        if (definitions != null)
            foreach (var d in definitions)
                ObjectsList.Items.Add(d);
    }

    private void ObjectsList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Inspector can show selected object when this tab is active
    }

    private void ObjectsList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var item = GetItemAt(e.GetPosition(ObjectsList));
        _dragSourceDefinition = item as ObjectDefinition;
        _dragStartPos = e.GetPosition(ObjectsList);
    }

    private void ObjectsList_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragSourceDefinition == null || _dragStartPos == null) return;
        var pos = e.GetPosition(ObjectsList);
        var delta = pos - _dragStartPos.Value;
        if (Math.Abs(delta.X) < 4 && Math.Abs(delta.Y) < 4) return;
        _dragStartPos = null;
        var data = new System.Windows.DataObject(DataFormatObjectDefinitionId, _dragSourceDefinition.Id ?? "");
        try
        {
            System.Windows.DragDrop.DoDragDrop(ObjectsList, data, System.Windows.DragDropEffects.Copy);
        }
        finally
        {
            _dragSourceDefinition = null;
        }
    }

    private object? GetItemAt(System.Windows.Point point)
    {
        var hit = VisualTreeHelper.HitTest(ObjectsList, point);
        if (hit == null) return null;
        var listItem = FindVisualAncestor<ListBoxItem>(hit.VisualHit as DependencyObject);
        return listItem?.DataContext ?? listItem?.Content;
    }

    private static T? FindVisualAncestor<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child != null)
        {
            if (child is T t) return t;
            child = VisualTreeHelper.GetParent(child);
        }
        return null;
    }
}
