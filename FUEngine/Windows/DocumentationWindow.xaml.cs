using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FUEngine.Help;

namespace FUEngine;

public partial class DocumentationWindow : Window
{
    private readonly List<DocumentationTopic> _allTopics;
    private DocumentationTopic? _current;

    /// <summary>Si coincide con un <see cref="DocumentationTopic.Id"/>, ese tema se selecciona al abrir; si es null, el primero del índice.</summary>
    public string? InitialTopicId { get; set; }

    public DocumentationWindow()
    {
        InitializeComponent();
        _allTopics = EngineDocumentation.Topics.ToList();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        ApplyFilter();
        var id = InitialTopicId;
        if (!string.IsNullOrEmpty(id))
        {
            var t = _allTopics.FirstOrDefault(x => x.Id == id);
            if (t != null)
            {
                SelectTopicInList(t);
                return;
            }
        }
        if (TopicList.Items.Count > 0)
            TopicList.SelectedIndex = 0;
    }

    private void SelectTopicInList(DocumentationTopic topic)
    {
        foreach (var item in TopicList.Items)
        {
            if (item is DocumentationTopic dt && dt.Id == topic.Id)
            {
                TopicList.SelectedItem = item;
                return;
            }
        }
    }

    private void TxtFilter_OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        var prevId = _current?.Id;
        ApplyFilter();
        if (!string.IsNullOrEmpty(prevId))
        {
            var t = TopicList.Items.Cast<object>().OfType<DocumentationTopic>().FirstOrDefault(x => x.Id == prevId);
            if (t != null)
            {
                TopicList.SelectedItem = t;
                return;
            }
        }
        if (TopicList.Items.Count > 0 && TopicList.SelectedItem == null)
            TopicList.SelectedIndex = 0;
    }

    private void ApplyFilter()
    {
        var q = (TxtFilter?.Text ?? "").Trim();
        IEnumerable<DocumentationTopic> src = _allTopics;
        if (q.Length > 0)
        {
            var ql = q.ToLowerInvariant();
            src = _allTopics.Where(t =>
                t.Title.ToLowerInvariant().Contains(ql)
                || (t.ParaQue?.ToLowerInvariant().Contains(ql) ?? false)
                || (t.PorQueImporta?.ToLowerInvariant().Contains(ql) ?? false)
                || t.Paragraphs.Any(p => p.ToLowerInvariant().Contains(ql))
                || (t.Bullets?.Any(b => b.ToLowerInvariant().Contains(ql)) ?? false));
        }
        TopicList.Items.Clear();
        foreach (var t in src)
            TopicList.Items.Add(t);
    }

    private void TopicList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TopicList.SelectedItem is not DocumentationTopic topic)
        {
            DetailPanel.Children.Clear();
            _current = null;
            return;
        }
        _current = topic;
        RebuildDetail(topic);
    }

    private void RebuildDetail(DocumentationTopic topic)
    {
        DetailPanel.Children.Clear();
        var accent = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x58, 0xa6, 0xff));
        var primary = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3));
        var muted = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x8b, 0x94, 0x9e));

        DetailPanel.Children.Add(new TextBlock
        {
            Text = topic.Title,
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Foreground = primary,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12)
        });

        if (!string.IsNullOrEmpty(topic.ParaQue))
        {
            DetailPanel.Children.Add(LabelRow("Para qué", accent));
            DetailPanel.Children.Add(BodyParagraph(topic.ParaQue, primary));
        }
        if (!string.IsNullOrEmpty(topic.PorQueImporta))
        {
            DetailPanel.Children.Add(LabelRow("Por qué importa", accent));
            DetailPanel.Children.Add(BodyParagraph(topic.PorQueImporta, primary));
        }

        foreach (var p in topic.Paragraphs)
            DetailPanel.Children.Add(BodyParagraph(p, primary));

        if (topic.Bullets is { Count: > 0 } bullets)
        {
            DetailPanel.Children.Add(new TextBlock { Text = "Puntos clave", Foreground = accent, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 12, 0, 6) });
            foreach (var b in bullets)
            {
                DetailPanel.Children.Add(new TextBlock
                {
                    Text = "• " + b,
                    Foreground = muted,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 13,
                    LineHeight = 20,
                    Margin = new Thickness(0, 2, 0, 0)
                });
            }
        }

        DetailScroll.ScrollToTop();
    }

    private static TextBlock LabelRow(string text, System.Windows.Media.Brush accent) =>
        new() { Text = text, Foreground = accent, FontWeight = FontWeights.SemiBold, FontSize = 13, Margin = new Thickness(0, 8, 0, 4) };

    private static TextBlock BodyParagraph(string text, System.Windows.Media.Brush foreground) =>
        new() { Text = text, Foreground = foreground, TextWrapping = TextWrapping.Wrap, FontSize = 13, LineHeight = 20, Margin = new Thickness(0, 0, 0, 8) };
}
