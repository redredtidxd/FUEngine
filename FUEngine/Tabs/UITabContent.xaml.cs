using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Brushes = System.Windows.Media.Brushes;
using FUEngine.Core;

namespace FUEngine;

/// <summary>Tab de edición de un Canvas UI. Muestra lista de elementos y superficie de diseño en coordenadas de pantalla.</summary>
public partial class UITabContent : System.Windows.Controls.UserControl
{
    private UICanvas? _canvas;
    private Func<UIRoot>? _getUIRoot;
    private FUEngine.Core.UIElement? _selectedElement;
    private readonly ObservableCollection<UIElementListItem> _elementsFlat = new();

    public UITabContent()
    {
        InitializeComponent();
        ElementsList.ItemsSource = _elementsFlat;
    }

    public void SetCanvas(UICanvas canvas, Func<UIRoot> getUIRoot)
    {
        _canvas = canvas;
        _getUIRoot = getUIRoot;
        _selectedElement = null;
        TxtCanvasName.Text = string.IsNullOrWhiteSpace(canvas.Name) ? canvas.Id : canvas.Name;
        TxtResolution.Text = $"{canvas.ResolutionWidth}×{canvas.ResolutionHeight}";
        RefreshElementsList();
        RedrawDesignSurface();
    }

    /// <summary>Sincroniza selección externa (jerarquía/inspector) con la lista y el preview de diseño.</summary>
    public void SetSelectedElement(FUEngine.Core.UIElement? element)
    {
        _selectedElement = element;
        SyncListSelectionFromSelectedElement();
        RedrawDesignSurface();
    }

    public void RefreshFromRoot()
    {
        if (_canvas == null) return;
        RefreshElementsList();
        SyncListSelectionFromSelectedElement();
        RedrawDesignSurface();
    }

    private void RefreshElementsList()
    {
        _elementsFlat.Clear();
        if (_canvas == null) return;
        foreach (var e in _canvas.Children)
            FlattenElement(e, 0);
    }

    private void SyncListSelectionFromSelectedElement()
    {
        if (ElementsList == null) return;
        if (_selectedElement == null)
        {
            ElementsList.SelectedItem = null;
            return;
        }
        var item = _elementsFlat.FirstOrDefault(i => ReferenceEquals(i.Element, _selectedElement));
        ElementsList.SelectedItem = item;
    }

    private void FlattenElement(FUEngine.Core.UIElement e, int depth)
    {
        _elementsFlat.Add(new UIElementListItem { Display = (new string(' ', depth * 2)) + (string.IsNullOrEmpty(e.Id) ? e.Kind.ToString() : e.Id), Element = e, Depth = depth });
        foreach (var child in e.Children)
            FlattenElement(child, depth + 1);
    }

    private void RedrawDesignSurface()
    {
        DesignSurface.Children.Clear();
        if (_canvas == null) return;
        var w = _canvas.ResolutionWidth;
        var h = _canvas.ResolutionHeight;
        if (w <= 0 || h <= 0) return;
        var actualW = DesignSurface.ActualWidth;
        var actualH = DesignSurface.ActualHeight;
        if (actualW <= 0 || actualH <= 0) return;
        var scaleX = actualW / w;
        var scaleY = actualH / h;
        var scale = Math.Min(scaleX, scaleY);
        var offsetX = (actualW - w * scale) / 2;
        var offsetY = (actualH - h * scale) / 2;
        var rootRect = new UIRect { X = 0, Y = 0, Width = w, Height = h };
        var frame = new System.Windows.Shapes.Rectangle
        {
            Width = w * scale,
            Height = h * scale,
            Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x30, 0x36, 0x3d)),
            StrokeThickness = 1,
            Fill = Brushes.Transparent
        };
        Canvas.SetLeft(frame, offsetX);
        Canvas.SetTop(frame, offsetY);
        DesignSurface.Children.Add(frame);
        foreach (var e in _canvas.Children)
            DrawElement(e, rootRect, scale, offsetX, offsetY);
    }

    private static UIRect ResolveRect(FUEngine.Core.UIElement e, UIRect parent)
    {
        var anchorX = parent.X + parent.Width * e.Anchors.MinX;
        var anchorY = parent.Y + parent.Height * e.Anchors.MinY;
        var anchorW = parent.Width * (e.Anchors.MaxX - e.Anchors.MinX);
        var anchorH = parent.Height * (e.Anchors.MaxY - e.Anchors.MinY);
        var width = e.Rect.Width + (Math.Abs(anchorW) > 0.0001 ? anchorW : 0);
        var height = e.Rect.Height + (Math.Abs(anchorH) > 0.0001 ? anchorH : 0);
        return new UIRect
        {
            X = anchorX + e.Rect.X,
            Y = anchorY + e.Rect.Y,
            Width = Math.Max(0, width),
            Height = Math.Max(0, height)
        };
    }

    private void DrawElement(FUEngine.Core.UIElement e, UIRect parentRect, double scale, double offsetX, double offsetY)
    {
        var resolved = ResolveRect(e, parentRect);
        var x = resolved.X * scale + offsetX;
        var y = resolved.Y * scale + offsetY;
        var w = Math.Max(3, resolved.Width * scale);
        var h = Math.Max(3, resolved.Height * scale);
        var isSelected = _selectedElement != null && ReferenceEquals(_selectedElement, e);
        var stroke = isSelected
            ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xf7, 0x81, 0x66))
            : new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x58, 0x6e, 0x96));
        var fill = e.Kind switch
        {
            UIElementKind.Button => new SolidColorBrush(System.Windows.Media.Color.FromArgb(80, 0x2e, 0xa0, 0x43)),
            UIElementKind.Text => new SolidColorBrush(System.Windows.Media.Color.FromArgb(45, 0x58, 0xa6, 0xff)),
            UIElementKind.Image => new SolidColorBrush(System.Windows.Media.Color.FromArgb(55, 0xd2, 0x99, 0x22)),
            _ => new SolidColorBrush(System.Windows.Media.Color.FromArgb(45, 0x8b, 0x94, 0x9e))
        };
        var rect = new System.Windows.Shapes.Rectangle
        {
            Width = w,
            Height = h,
            Fill = fill,
            Stroke = stroke,
            StrokeThickness = isSelected ? 2 : 1
        };
        Canvas.SetLeft(rect, x);
        Canvas.SetTop(rect, y);
        DesignSurface.Children.Add(rect);

        var label = string.IsNullOrWhiteSpace(e.Id) ? e.Kind.ToString() : e.Id;
        var text = new TextBlock
        {
            Text = label,
            Foreground = Brushes.White,
            FontSize = 10,
            Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(130, 0x0d, 0x11, 0x17)),
            Padding = new Thickness(3, 1, 3, 1)
        };
        Canvas.SetLeft(text, x + 2);
        Canvas.SetTop(text, Math.Max(0, y - 14));
        DesignSurface.Children.Add(text);
        foreach (var child in e.Children)
            DrawElement(child, resolved, scale, offsetX, offsetY);
    }

    private void DesignSurface_OnSizeChanged(object sender, SizeChangedEventArgs e) => RedrawDesignSurface();
    private void DesignSurface_OnLoaded(object sender, RoutedEventArgs e) => RedrawDesignSurface();

    private void ElementsList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedElement = (ElementsList.SelectedItem as UIElementListItem)?.Element;
        RedrawDesignSurface();
        ElementSelected?.Invoke(this, _selectedElement);
    }

    public event EventHandler<FUEngine.Core.UIElement?>? ElementSelected;
}

public class UIElementListItem
{
    public string Display { get; set; } = "";
    public FUEngine.Core.UIElement? Element { get; set; }
    public int Depth { get; set; }
}
