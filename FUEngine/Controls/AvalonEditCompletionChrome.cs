using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.CodeCompletion;
using WpfControl = System.Windows.Controls.Control;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;

namespace FUEngine;

/// <summary>Tema oscuro y cierre automático para el <see cref="CompletionWindow"/> de AvalonEdit (evita texto blanco sobre fondo blanco y popup vacío al filtrar).</summary>
internal static class AvalonEditCompletionChrome
{
    public static void ApplyFueDarkTheme(CompletionWindow window)
    {
        var bg = new SolidColorBrush(WpfColor.FromRgb(0x21, 0x26, 0x2d));
        var fg = new SolidColorBrush(WpfColor.FromRgb(0xe6, 0xed, 0xf3));
        var border = new SolidColorBrush(WpfColor.FromRgb(0x48, 0x54, 0x61));
        var hoverBg = new SolidColorBrush(WpfColor.FromRgb(0x30, 0x37, 0x3f));
        var selBg = new SolidColorBrush(WpfColor.FromRgb(0x38, 0x8b, 0xd6));

        window.Background = bg;
        window.Foreground = fg;
        window.BorderBrush = border;
        window.BorderThickness = new Thickness(1);

        var itemStyle = new Style(typeof(ListBoxItem));
        itemStyle.Setters.Add(new Setter(WpfControl.ForegroundProperty, fg));
        itemStyle.Setters.Add(new Setter(WpfControl.BackgroundProperty, WpfBrushes.Transparent));
        itemStyle.Setters.Add(new Setter(WpfControl.PaddingProperty, new Thickness(6, 4, 8, 4)));
        var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hoverTrigger.Setters.Add(new Setter(WpfControl.BackgroundProperty, hoverBg));
        itemStyle.Triggers.Add(hoverTrigger);
        var selTrigger = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
        selTrigger.Setters.Add(new Setter(WpfControl.BackgroundProperty, selBg));
        selTrigger.Setters.Add(new Setter(WpfControl.ForegroundProperty, WpfBrushes.White));
        itemStyle.Triggers.Add(selTrigger);
        window.Resources[typeof(ListBoxItem)] = itemStyle;

        var tbStyle = new Style(typeof(TextBlock));
        tbStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, fg));
        window.Resources[typeof(TextBlock)] = tbStyle;

        window.Loaded += (_, _) =>
        {
            try
            {
                var list = window.CompletionList;
                list.Background = bg;
                var lb = list.ListBox;
                lb.Background = bg;
                lb.Foreground = fg;
                lb.BorderThickness = new Thickness(0);
                var scroll = list.ScrollViewer;
                if (scroll != null)
                    scroll.Background = bg;
            }
            catch
            {
                /* diseñador / plantilla distinta */
            }

            WatchCloseWhenFilteredEmpty(window);
        };
    }

    private static void WatchCloseWhenFilteredEmpty(CompletionWindow window)
    {
        void Check()
        {
            try
            {
                if (!window.IsVisible) return;
                var lb = window.CompletionList?.ListBox;
                if (lb == null) return;
                if (lb.Items.Count == 0)
                    window.Close();
            }
            catch
            {
                /* ignore */
            }
        }

        try
        {
            var lb = window.CompletionList.ListBox;
            var dpdSrc = DependencyPropertyDescriptor.FromProperty(
                System.Windows.Controls.ListBox.ItemsSourceProperty,
                typeof(System.Windows.Controls.ListBox));
            dpdSrc?.AddValueChanged(lb, (_, _) => window.Dispatcher.BeginInvoke(Check, DispatcherPriority.Background));
            window.Dispatcher.BeginInvoke(Check, DispatcherPriority.Loaded);
        }
        catch
        {
            /* ignore */
        }
    }
}
