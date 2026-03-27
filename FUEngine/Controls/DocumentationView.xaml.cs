using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using FUEngine.Help;

namespace FUEngine;

public partial class DocumentationView : System.Windows.Controls.UserControl
{
    private List<DocumentationTopic> _allTopics = new();
    private List<DocumentationTopic> _visibleTopics = new();

    private bool _topicsInitialized;
    private DocumentationTopic? _current;

    public DocumentationView()
    {
        InitializeComponent();
    }

    public bool LuaReferenceMode { get; set; }

    public string? InitialTopicId { get; set; }

    public event EventHandler? RequestClose;

    private static System.Windows.Media.Color HexColor(string hex)
    {
        hex = hex.TrimStart('#');
        return System.Windows.Media.Color.FromRgb(
            Convert.ToByte(hex[..2], 16),
            Convert.ToByte(hex.Substring(2, 2), 16),
            Convert.ToByte(hex.Substring(4, 2), 16));
    }

    private static SolidColorBrush B(string hex)
    {
        var br = new SolidColorBrush(HexColor(hex));
        if (br.CanFreeze) br.Freeze();
        return br;
    }

    private void EnsureTopicsLoaded()
    {
        if (_topicsInitialized) return;
        _topicsInitialized = true;
        var q = EngineDocumentation.Topics.Where(t => LuaReferenceMode
            ? EngineDocumentation.IsLuaReferenceSidebarTopic(t.Id)
            : !EngineDocumentation.IsLuaReferenceSidebarTopic(t.Id)).ToList();
        if (LuaReferenceMode)
        {
            var introId = EngineDocumentation.LuaReferenceIntroTopicId;
            var intro = q.FirstOrDefault(x => x.Id == introId);
            if (intro != null)
            {
                q.Remove(intro);
                q.Sort((a, b) => string.Compare(a.Title ?? "", b.Title ?? "", StringComparison.OrdinalIgnoreCase));
                q.Insert(0, intro);
            }
            else
                q.Sort((a, b) => string.Compare(a.Title ?? "", b.Title ?? "", StringComparison.OrdinalIgnoreCase));
        }
        _allTopics = q;
    }

    public void Open(string? initialTopicId)
    {
        EnsureTopicsLoaded();
        InitialTopicId = initialTopicId;
        if (!string.IsNullOrEmpty(initialTopicId))
            TxtFilter.Text = "";
        ApplyFilter();
        var id = InitialTopicId;
        if (!string.IsNullOrEmpty(id))
        {
            var t = _allTopics.FirstOrDefault(x => x.Id == id);
            if (t != null)
            {
                SelectTopicInListById(t.Id);
                return;
            }
        }
        if (TopicList.Items.Count > 0)
            TopicList.SelectedIndex = 0;
    }

    private void BtnClose_OnClick(object sender, RoutedEventArgs e) => RequestClose?.Invoke(this, EventArgs.Empty);

    protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
            return;
        }
        base.OnPreviewKeyDown(e);
    }

    private void SelectTopicInListById(string topicId)
    {
        for (var i = 0; i < _visibleTopics.Count; i++)
        {
            if (string.Equals(_visibleTopics[i].Id, topicId, StringComparison.Ordinal))
            {
                TopicList.SelectedIndex = i;
                return;
            }
        }
    }

    private void TxtFilter_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        var prevId = _current?.Id;
        ApplyFilter();
        if (!string.IsNullOrEmpty(prevId))
        {
            SelectTopicInListById(prevId);
            return;
        }
        if (TopicList.Items.Count > 0 && TopicList.SelectedIndex < 0)
            TopicList.SelectedIndex = 0;
    }

    private void ApplyFilter()
    {
        EnsureTopicsLoaded();
        var q = (TxtFilter?.Text ?? "").Trim();
        IEnumerable<DocumentationTopic> src = _allTopics;
        if (q.Length > 0)
        {
            var ql = q.ToLowerInvariant();
            src = _allTopics.Where(t =>
                (t.Title?.ToLowerInvariant().Contains(ql) ?? false)
                || (t.Subtitle?.ToLowerInvariant().Contains(ql) ?? false)
                || (t.EnMotor?.ToLowerInvariant().Contains(ql) ?? false)
                || (t.ParaQue?.ToLowerInvariant().Contains(ql) ?? false)
                || (t.PorQueImporta?.ToLowerInvariant().Contains(ql) ?? false)
                || (t.Paragraphs?.Any(p => !string.IsNullOrEmpty(p) && p.ToLowerInvariant().Contains(ql)) ?? false)
                || (t.Bullets?.Any(b => !string.IsNullOrEmpty(b) && b.ToLowerInvariant().Contains(ql)) ?? false));
        }

        _visibleTopics = src.ToList();

        TopicList.SelectionChanged -= TopicList_OnSelectionChanged;
        try
        {
            TopicList.Items.Clear();
            foreach (var t in _visibleTopics)
                TopicList.Items.Add(FormatTopicListLabel(t));
        }
        finally
        {
            TopicList.SelectionChanged += TopicList_OnSelectionChanged;
        }
    }

    private string FormatTopicListLabel(DocumentationTopic t)
    {
        var title = t.Title ?? "";
        var sameTitle = _visibleTopics.Count(x => string.Equals(x.Title ?? "", title, StringComparison.Ordinal)) > 1;
        return sameTitle ? $"{title}  ({t.Id})" : title;
    }

    private void TopicList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var idx = TopicList.SelectedIndex;
        if (idx < 0 || idx >= _visibleTopics.Count)
        {
            _current = null;
            DetailPanel?.Children.Clear();
            return;
        }

        var topic = _visibleTopics[idx];
        _current = topic;

        try
        {
            RebuildDetail(topic);
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    EditorLog.Error("Documentación: error al componer el tema. " + msg, "Ayuda");
                }
                catch { /* ignore */ }
            }), DispatcherPriority.Background);
            DetailPanel?.Children.Clear();
            DetailPanel?.Children.Add(new TextBlock
            {
                Text = "Error al mostrar el tema: " + ex.Message,
                Foreground = B("f85149"),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13
            });
        }

        Dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                DetailScroll?.ScrollToHome();
            }
            catch { /* ignore */ }
        }), DispatcherPriority.ApplicationIdle);
    }

    private void RebuildDetail(DocumentationTopic topic)
    {
        if (DetailPanel == null) return;
        DetailPanel.Children.Clear();

        var titleAccent = LuaReferenceMode ? B("79c0ff") : B("58a6ff");
        var sectionAccent = LuaReferenceMode ? B("a371f7") : B("58a6ff");
        var body = B("e6edf3");
        var muted = B("8b949e");
        var motorBorder = B("ffa657");
        var motorBg = B("161b22");

        DetailPanel.Children.Add(new TextBlock
        {
            Text = topic.Title ?? "",
            FontSize = LuaReferenceMode ? 23 : 22,
            FontWeight = FontWeights.SemiBold,
            Foreground = titleAccent,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 6)
        });

        if (!string.IsNullOrWhiteSpace(topic.Subtitle))
        {
            DetailPanel.Children.Add(new TextBlock
            {
                Text = topic.Subtitle,
                FontSize = 14,
                FontStyle = FontStyles.Italic,
                Foreground = muted,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 14)
            });
        }

        DetailPanel.Children.Add(new Border
        {
            Height = 1,
            Background = B("30363d"),
            Margin = new Thickness(0, 0, 0, 16)
        });

        void AddSection(string label, string? text, SolidColorBrush labelBrush)
        {
            if (string.IsNullOrEmpty(text)) return;
            DetailPanel.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = labelBrush,
                Margin = new Thickness(0, 14, 0, 6)
            });
            DetailPanel.Children.Add(new TextBlock
            {
                Text = text,
                FontSize = 13,
                Foreground = body,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20,
                Margin = new Thickness(0, 0, 0, 4)
            });
        }

        AddSection("Para qué", topic.ParaQue, sectionAccent);
        AddSection("Por qué importa", topic.PorQueImporta, sectionAccent);

        if (!string.IsNullOrWhiteSpace(topic.EnMotor))
        {
            var callout = new Border
            {
                BorderThickness = new Thickness(4, 0, 0, 0),
                BorderBrush = motorBorder,
                Background = motorBg,
                CornerRadius = new CornerRadius(0, 4, 4, 0),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 16, 0, 8)
            };
            var inner = new StackPanel();
            inner.Children.Add(new TextBlock
            {
                Text = "En FUEngine",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = motorBorder,
                Margin = new Thickness(0, 0, 0, 6)
            });
            inner.Children.Add(new TextBlock
            {
                Text = topic.EnMotor,
                FontSize = 13,
                Foreground = body,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20
            });
            callout.Child = inner;
            DetailPanel.Children.Add(callout);
        }

        if (topic.Paragraphs is { Count: > 0 })
        {
            DetailPanel.Children.Add(new TextBlock
            {
                Text = "Contenido",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = muted,
                Margin = new Thickness(0, 18, 0, 8)
            });
            foreach (var p in topic.Paragraphs)
            {
                if (string.IsNullOrEmpty(p)) continue;
                DetailPanel.Children.Add(new TextBlock
                {
                    Text = p,
                    FontSize = 13,
                    Foreground = body,
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 20,
                    Margin = new Thickness(0, 0, 0, 10)
                });
            }
        }

        if (topic.Bullets is { Count: > 0 } bullets)
        {
            DetailPanel.Children.Add(new TextBlock
            {
                Text = "Puntos clave",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = B("7ee787"),
                Margin = new Thickness(0, 14, 0, 8)
            });
            foreach (var b in bullets)
            {
                if (string.IsNullOrEmpty(b)) continue;
                var row = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
                row.Children.Add(new TextBlock
                {
                    Text = "• ",
                    Foreground = B("7ee787"),
                    FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 0, 6, 0)
                });
                row.Children.Add(new TextBlock
                {
                    Text = b,
                    FontSize = 13,
                    Foreground = body,
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 20
                });
                DetailPanel.Children.Add(row);
            }
        }
    }
}
