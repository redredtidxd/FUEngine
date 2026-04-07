using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using FUEngine.Help;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;

namespace FUEngine;

public partial class DocumentationView : System.Windows.Controls.UserControl
{
    private List<DocumentationTopic> _allTopics = new();
    private readonly CollectionViewSource _topicCollectionViewSource = new();

    private bool _topicsInitialized;
    private bool _topicGroupingInitialized;
    private DocumentationTopic? _current;

    public DocumentationView()
    {
        InitializeComponent();
        Loaded += DocumentationView_OnLoaded;
    }

    private void EnsureTopicGrouping()
    {
        if (_topicGroupingInitialized) return;
        _topicGroupingInitialized = true;
        _topicCollectionViewSource.GroupDescriptions.Add(
            new PropertyGroupDescription(nameof(DocumentationTopicListEntry.GroupTitle)));
    }

    /// <summary>Pestaña «Lua — sintaxis y librería».</summary>
    public bool LuaReferenceMode { get; set; }

    /// <summary>Pestaña «Ejemplos de scripts».</summary>
    public bool ScriptExamplesMode { get; set; }

    /// <summary>Si devuelve true, se habilita «Crear script desde este ejemplo» (p. ej. editor con proyecto).</summary>
    public Func<bool>? AllowExportScriptExample { get; set; }

    public string? InitialTopicId { get; set; }

    /// <summary>Tema mostrado actualmente en el panel de detalle (para ventana aparte).</summary>
    public string? CurrentTopicId => _current?.Id;

    public event EventHandler? RequestClose;

    public event EventHandler<CreateScriptFromExampleEventArgs>? RequestCreateScriptFromExample;

    private void DocumentationView_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (ScriptExamplesMode)
        {
            TxtMainTitle.Text = "Ejemplos de scripts (Lua)";
            if (FindName("TxtFilterLabel") is TextBlock tf)
                tf.Text = "Buscar ejemplos o etiquetas";
        }
        else if (LuaReferenceMode)
        {
            TxtMainTitle.Text = "Lua — sintaxis y librería";
            if (FindName("TxtFilterLabel") is TextBlock tf)
                tf.Text = "Buscar en el índice (reservadas y guías)";
        }
    }

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

    /// <summary>Chip con punto de color real (los emojis 🟢🟡🔴 suelen verse grises en RichText).</summary>
    private static UIElement? CreateScriptExampleDifficultyChip(string? difficulty)
    {
        var tier = difficulty switch
        {
            "Básico" => ScriptExampleDifficultyTier.Basic,
            "Intermedio" => ScriptExampleDifficultyTier.Intermediate,
            "Avanzado" => ScriptExampleDifficultyTier.Advanced,
            _ => ScriptExampleDifficultyTier.None
        };
        if (tier == ScriptExampleDifficultyTier.None) return null;

        SolidColorBrush dot, ring;
        string label;
        switch (tier)
        {
            case ScriptExampleDifficultyTier.Basic:
                dot = B("3fb950");
                ring = B("238636");
                label = "Básico";
                break;
            case ScriptExampleDifficultyTier.Intermediate:
                dot = B("d29922");
                ring = B("9e6a03");
                label = "Intermedio";
                break;
            default:
                dot = B("f85149");
                ring = B("da3633");
                label = "Avanzado";
                break;
        }

        var ellipse = new Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = dot,
            VerticalAlignment = VerticalAlignment.Center,
            SnapsToDevicePixels = true,
        };
        var title = new TextBlock
        {
            Text = "Dificultad: " + label,
            Foreground = B("e6edf3"),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0),
        };
        var row = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
        };
        row.Children.Add(ellipse);
        row.Children.Add(title);

        return new Border
        {
            Background = B("161b22"),
            BorderBrush = ring,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(999),
            Padding = new Thickness(12, 6, 14, 6),
            Margin = new Thickness(0, 0, 0, 12),
            SnapsToDevicePixels = true,
            Child = row,
        };
    }

    private static int CompareTopicTitles(DocumentationTopic a, DocumentationTopic b) =>
        string.Compare(a.Title ?? "", b.Title ?? "", StringComparison.OrdinalIgnoreCase);

    /// <summary>Orden alfabético y deja el tema intro primero si existe.</summary>
    private static void SortAndPinIntro(List<DocumentationTopic> list, string introId)
    {
        var intro = list.FirstOrDefault(x => x.Id == introId);
        if (intro != null)
        {
            list.Remove(intro);
            list.Sort(CompareTopicTitles);
            list.Insert(0, intro);
        }
        else
            list.Sort(CompareTopicTitles);
    }

    /// <summary>Solo coloca el tema intro al inicio; mantiene el orden de definición en código (Lua y ejemplos).</summary>
    private static void PinIntroPreserveOrder(List<DocumentationTopic> list, string introId)
    {
        var intro = list.FirstOrDefault(x => x.Id == introId);
        if (intro == null) return;
        list.Remove(intro);
        list.Insert(0, intro);
    }

    private void EnsureTopicsLoaded()
    {
        if (_topicsInitialized) return;
        _topicsInitialized = true;

        if (ScriptExamplesMode)
        {
            var q = EngineDocumentation.Topics.Where(t => EngineDocumentation.IsScriptExamplesSidebarTopic(t.Id)).ToList();
            PinIntroPreserveOrder(q, EngineDocumentation.ScriptExamplesIntroTopicId);
            _allTopics = q;
        }
        else if (LuaReferenceMode)
        {
            var q = EngineDocumentation.Topics.Where(t => EngineDocumentation.IsLuaReferenceSidebarTopic(t.Id)).ToList();
            PinIntroPreserveOrder(q, EngineDocumentation.LuaReferenceIntroTopicId);
            _allTopics = q;
        }
        else
        {
            _allTopics = EngineDocumentation.Topics.Where(t =>
                !EngineDocumentation.IsLuaReferenceSidebarTopic(t.Id)
                && !EngineDocumentation.IsScriptExamplesSidebarTopic(t.Id)).ToList();
        }
    }

    public void Open(string? initialTopicId)
    {
        EnsureTopicsLoaded();
        InitialTopicId = initialTopicId;
        if (!string.IsNullOrEmpty(initialTopicId))
            TxtFilter.Text = "";
        ApplyFilter();
        // Tras agrupar / ItemsSource, la selección y el detalle deben aplicarse cuando el ListBox ya tiene layout
        // (TabControl puede no medir la pestaña hasta que es visible). Evita lista sin selección y panel vacío.
        Dispatcher.BeginInvoke(new Action(() =>
        {
            TryApplyInitialSelection();
        }), DispatcherPriority.Loaded);
    }

    private void TryApplyInitialSelection()
    {
        if (TopicList.Items.Count == 0) return;
        var id = InitialTopicId;
        if (!string.IsNullOrEmpty(id))
            SelectTopicInListById(id);
        if (TopicList.SelectedItem == null)
            TopicList.SelectedIndex = 0;
        SyncDetailToCurrentSelection();
    }

    /// <summary>Fuerza el panel derecho al ítem seleccionado (SelectionChanged a veces no dispara si el ítem no cambia).</summary>
    private void SyncDetailToCurrentSelection()
    {
        if (TopicList.SelectedItem is not DocumentationTopicListEntry entry)
        {
            _current = null;
            DetailPanel?.Children.Clear();
            return;
        }

        if (_current?.Id == entry.Topic.Id && DetailPanel?.Children.Count > 0)
            return;

        _current = entry.Topic;
        try
        {
            RebuildDetail(entry.Topic);
        }
        catch (Exception ex)
        {
            DetailPanel?.Children.Clear();
            DetailPanel?.Children.Add(CreateSelectableErrorRichText("Error al mostrar el tema: " + ex.Message));
        }

        try
        {
            DetailScroll?.ScrollToHome();
        }
        catch
        {
            /* ignore */
        }
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
        foreach (var item in TopicList.Items)
        {
            if (item is DocumentationTopicListEntry e &&
                string.Equals(e.Topic.Id, topicId, StringComparison.Ordinal))
            {
                TopicList.SelectedItem = item;
                return;
            }
        }
    }

    private void TxtFilter_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        var prevId = _current?.Id;
        ApplyFilter();
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (!string.IsNullOrEmpty(prevId))
                SelectTopicInListById(prevId);
            if (TopicList.SelectedItem == null && TopicList.Items.Count > 0)
                TopicList.SelectedIndex = 0;
            SyncDetailToCurrentSelection();
        }), DispatcherPriority.Loaded);
    }

    private static bool FieldContains(string? field, string ql) =>
        !string.IsNullOrEmpty(field) && field.ToLowerInvariant().Contains(ql);

    private static bool TopicMatchesFilter(DocumentationTopic t, string ql)
    {
        if (FieldContains(t.Title, ql) || FieldContains(t.Subtitle, ql) || FieldContains(t.ExampleCategory, ql)
            || FieldContains(t.EnMotor, ql) || FieldContains(t.ParaQue, ql) || FieldContains(t.PorQueImporta, ql)
            || FieldContains(t.ExampleSearchTags, ql) || FieldContains(t.ExampleDifficulty, ql))
            return true;
        if (t.Paragraphs?.Any(p => FieldContains(p, ql)) == true) return true;
        if (t.Bullets?.Any(b => FieldContains(b, ql)) == true) return true;
        return !string.IsNullOrEmpty(t.LuaExampleCode) && t.LuaExampleCode.ToLowerInvariant().Contains(ql);
    }

    private void ApplyFilter()
    {
        EnsureTopicsLoaded();
        EnsureTopicGrouping();
        var q = (TxtFilter?.Text ?? "").Trim();
        IEnumerable<DocumentationTopic> src = _allTopics;
        if (q.Length > 0)
        {
            var ql = q.ToLowerInvariant();
            src = _allTopics.Where(t => TopicMatchesFilter(t, ql));
        }

        var visibleTopics = src.ToList();
        var entries = visibleTopics
            .Select(t => DocumentationTopicListGrouping.Create(
                t, LuaReferenceMode, ScriptExamplesMode, visibleTopics))
            .OrderBy(e => e.GroupOrder)
            .ThenBy(e => e.GroupTitle, StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => e.DisplayLabel, StringComparer.OrdinalIgnoreCase)
            .ToList();

        TopicList.SelectionChanged -= TopicList_OnSelectionChanged;
        try
        {
            _topicCollectionViewSource.Source = entries;
            TopicList.ItemsSource = _topicCollectionViewSource.View;
        }
        finally
        {
            TopicList.SelectionChanged += TopicList_OnSelectionChanged;
        }

        if (entries.Count == 0)
        {
            _current = null;
            DetailPanel?.Children.Clear();
        }
    }

    private void TopicList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TopicList.SelectedItem is not DocumentationTopicListEntry entry)
        {
            _current = null;
            DetailPanel?.Children.Clear();
            return;
        }

        var topic = entry.Topic;
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
            DetailPanel?.Children.Add(CreateSelectableErrorRichText("Error al mostrar el tema: " + ex.Message));
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

    private record struct DetailTheme(SolidColorBrush TitleAccent, SolidColorBrush SectionAccent, int TitleSize);

    private DetailTheme GetDetailTheme()
    {
        if (ScriptExamplesMode)
            return new DetailTheme(B("d2a8ff"), B("a371f7"), 22);
        if (LuaReferenceMode)
            return new DetailTheme(B("79c0ff"), B("a371f7"), 23);
        return new DetailTheme(B("58a6ff"), B("58a6ff"), 22);
    }

    /// <summary>Interpreta pares <c>**texto**</c> como negrita (convención del manual en strings C#).</summary>
    private static void AppendStarBoldInlines(
        InlineCollection inlines,
        string? text,
        System.Windows.Media.Brush defaultForeground,
        double fontSize,
        FontWeight? baseFontWeight = null,
        FontWeight? emphasisFontWeight = null)
    {
        var baseW = baseFontWeight ?? FontWeights.Normal;
        var emphW = emphasisFontWeight ?? FontWeights.Bold;

        if (string.IsNullOrEmpty(text)) return;
        var s = text;
        var i = 0;
        while (i < s.Length)
        {
            var open = s.IndexOf("**", i, StringComparison.Ordinal);
            if (open < 0)
            {
                inlines.Add(new Run(s[i..])
                {
                    Foreground = defaultForeground,
                    FontSize = fontSize,
                    FontWeight = baseW,
                });
                return;
            }

            if (open > i)
            {
                inlines.Add(new Run(s[i..open])
                {
                    Foreground = defaultForeground,
                    FontSize = fontSize,
                    FontWeight = baseW,
                });
            }

            var close = s.IndexOf("**", open + 2, StringComparison.Ordinal);
            if (close < 0)
            {
                inlines.Add(new Run(s[open..])
                {
                    Foreground = defaultForeground,
                    FontSize = fontSize,
                    FontWeight = baseW,
                });
                return;
            }

            var inner = s.Substring(open + 2, close - open - 2);
            inlines.Add(new Run(inner)
            {
                Foreground = defaultForeground,
                FontSize = fontSize,
                FontWeight = emphW,
            });
            i = close + 2;
        }
    }

    private void RebuildDetail(DocumentationTopic topic)
    {
        if (DetailPanel == null) return;
        DetailPanel.Children.Clear();

        var theme = GetDetailTheme();
        var body = B("e6edf3");
        var muted = B("8b949e");
        var motorBorder = B("ffa657");
        var motorBg = B("161b22");
        var bulletColor = B("7ee787");

        const double bodySize = 14;
        const double bodyLine = 22;

        var doc = new FlowDocument
        {
            PagePadding = new Thickness(0, 4, 0, 8),
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
            FontSize = bodySize,
            Foreground = body,
        };

        var titleP = new Paragraph
        {
            FontSize = theme.TitleSize,
            FontWeight = FontWeights.SemiBold,
            Foreground = theme.TitleAccent,
            Margin = new Thickness(0, 0, 0, 8),
        };
        AppendStarBoldInlines(titleP.Inlines, topic.Title ?? "", theme.TitleAccent, theme.TitleSize,
            baseFontWeight: FontWeights.SemiBold, emphasisFontWeight: FontWeights.Bold);
        doc.Blocks.Add(titleP);

        if (!string.IsNullOrWhiteSpace(topic.Subtitle))
        {
            var subP = new Paragraph
            {
                FontSize = 13,
                FontStyle = FontStyles.Italic,
                Foreground = muted,
                LineHeight = bodyLine,
                Margin = new Thickness(0, 0, 0, 10),
            };
            AppendStarBoldInlines(subP.Inlines, topic.Subtitle!, muted, 13);
            doc.Blocks.Add(subP);
        }

        if (ScriptExamplesMode)
        {
            var chip = CreateScriptExampleDifficultyChip(topic.ExampleDifficulty);
            if (chip != null)
                doc.Blocks.Add(new BlockUIContainer(chip));
        }

        doc.Blocks.Add(new BlockUIContainer(new Border
        {
            Height = 1,
            Background = B("30363d"),
            Margin = new Thickness(0, 2, 0, 18),
        }));

        void AddSection(string label, string? text, SolidColorBrush labelBrush)
        {
            if (string.IsNullOrEmpty(text)) return;
            doc.Blocks.Add(new Paragraph(new Run(label))
            {
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = labelBrush,
                Margin = new Thickness(0, 16, 0, 6),
            });
            var sectionBody = new Paragraph
            {
                FontSize = bodySize,
                Foreground = body,
                LineHeight = bodyLine,
                Margin = new Thickness(0, 0, 0, 6),
            };
            AppendStarBoldInlines(sectionBody.Inlines, text, body, bodySize);
            doc.Blocks.Add(sectionBody);
        }

        AddSection("Para qué", topic.ParaQue, theme.SectionAccent);
        AddSection("Por qué importa", topic.PorQueImporta, theme.SectionAccent);

        if (!string.IsNullOrWhiteSpace(topic.EnMotor))
        {
            var callout = new Paragraph
            {
                Margin = new Thickness(0, 16, 0, 8),
                Padding = new Thickness(14, 12, 14, 12),
                Background = motorBg,
                BorderBrush = motorBorder,
                BorderThickness = new Thickness(4, 0, 0, 0),
                LineHeight = bodyLine,
            };
            callout.Inlines.Add(new Run("En FUEngine")
            {
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = motorBorder,
            });
            callout.Inlines.Add(new LineBreak());
            AppendStarBoldInlines(callout.Inlines, topic.EnMotor, body, bodySize);
            doc.Blocks.Add(callout);
        }

        if (topic.Paragraphs is { Count: > 0 })
        {
            doc.Blocks.Add(new Paragraph(new Run("Contenido"))
            {
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = muted,
                Margin = new Thickness(0, 20, 0, 8),
            });
            foreach (var p in topic.Paragraphs)
            {
                if (string.IsNullOrEmpty(p)) continue;
                var contentP = new Paragraph
                {
                    FontSize = bodySize,
                    Foreground = body,
                    LineHeight = bodyLine,
                    Margin = new Thickness(0, 0, 0, 12),
                };
                AppendStarBoldInlines(contentP.Inlines, p, body, bodySize);
                doc.Blocks.Add(contentP);
            }
        }

        if (topic.Bullets is { Count: > 0 })
        {
            doc.Blocks.Add(new Paragraph(new Run("Puntos clave"))
            {
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = bulletColor,
                Margin = new Thickness(0, 16, 0, 8),
            });
            foreach (var b in topic.Bullets)
            {
                if (string.IsNullOrEmpty(b)) continue;
                var bp = new Paragraph
                {
                    Margin = new Thickness(0, 0, 0, 8),
                    LineHeight = bodyLine,
                };
                bp.Inlines.Add(new Run("• ") { Foreground = bulletColor, FontSize = bodySize });
                AppendStarBoldInlines(bp.Inlines, b, body, bodySize);
                doc.Blocks.Add(bp);
            }
        }

        DetailPanel.Children.Add(CreateReadOnlyRichTextHost(doc, body));

        if (!string.IsNullOrEmpty(topic.LuaExampleCode))
            AppendLuaExampleBlock(topic, body, muted);
    }

    private static System.Windows.Controls.RichTextBox CreateSelectableErrorRichText(string message)
    {
        var doc = new FlowDocument
        {
            PagePadding = new Thickness(0),
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
            FontSize = 13,
        };
        doc.Blocks.Add(new Paragraph(new Run(message))
        {
            Foreground = B("f85149"),
            Margin = new Thickness(0),
        });
        return CreateReadOnlyRichTextHost(doc, B("f85149"));
    }

    private static System.Windows.Controls.RichTextBox CreateReadOnlyRichTextHost(FlowDocument doc, System.Windows.Media.Brush caretBrush)
    {
        var rtb = new System.Windows.Controls.RichTextBox
        {
            IsReadOnly = true,
            BorderThickness = new Thickness(0),
            Background = System.Windows.Media.Brushes.Transparent,
            CaretBrush = caretBrush,
            Cursor = System.Windows.Input.Cursors.IBeam,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
            HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch,
            MinWidth = 0,
            Document = doc,
        };
        rtb.Loaded += RichTextDetail_OnLoadedOrSized;
        rtb.SizeChanged += RichTextDetail_OnLoadedOrSized;
        return rtb;
    }

    private static void RichTextDetail_OnLoadedOrSized(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.RichTextBox rtb || rtb.Document == null) return;
        var w = rtb.ActualWidth;
        for (var d = VisualTreeHelper.GetParent(rtb) as DependencyObject;
             d != null;
             d = VisualTreeHelper.GetParent(d))
        {
            if (d is ScrollViewer sv)
            {
                var vw = sv.ViewportWidth;
                if (!double.IsNaN(vw) && vw > 1)
                    w = Math.Max(w, vw - 24);
                break;
            }
        }

        if (w <= 0 || double.IsNaN(w)) return;
        rtb.Document.PageWidth = Math.Max(120, w - 16);
    }

    private void AppendLuaExampleBlock(DocumentationTopic topic, SolidColorBrush body, SolidColorBrush muted)
    {
        DetailPanel!.Children.Add(new TextBlock
        {
            Text = "Código",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = muted,
            Margin = new Thickness(0, 22, 0, 8),
        });

        var codeBorder = new Border
        {
            BorderBrush = B("30363d"),
            BorderThickness = new Thickness(1),
            Background = B("161b22"),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 14)
        };

        LuaHighlightingLoader.EnsureRegistered();
        var hl = HighlightingManager.Instance.GetDefinition(LuaHighlightingLoader.RegisteredName)
            ?? HighlightingManager.Instance.GetDefinition("Lua");
        var editor = CreateReadOnlyLuaEditor(hl, topic.LuaExampleCode ?? "");
        codeBorder.Child = editor;
        Dispatcher.BeginInvoke(new Action(() =>
        {
            try { editor.TextArea.TextView.Redraw(); }
            catch { /* ignore */ }
        }), DispatcherPriority.Loaded);
        DetailPanel.Children.Add(codeBorder);

        var btnRow = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };

        var code = topic.LuaExampleCode ?? "";
        var canExport = AllowExportScriptExample?.Invoke() == true;
        var btnCreate = new System.Windows.Controls.Button
        {
            Content = "Crear script desde este ejemplo",
            Padding = new Thickness(12, 6, 12, 6),
            Margin = new Thickness(0, 0, 8, 0),
            IsEnabled = canExport,
            Background = canExport ? B("1f6feb") : B("21262d"),
            Foreground = B("ffffff"),
            BorderBrush = canExport ? B("388bfd") : B("30363d"),
            Cursor = canExport ? System.Windows.Input.Cursors.Hand : System.Windows.Input.Cursors.No,
            ToolTip = canExport
                ? "Crea un .lua en Scripts/ y sincroniza scripts.json automáticamente"
                : "Abre un proyecto en el editor para usar esta acción."
        };
        btnCreate.Click += (_, _) =>
        {
            if (AllowExportScriptExample?.Invoke() != true) return;
            var name = topic.SuggestedExportFileName;
            if (string.IsNullOrWhiteSpace(name))
                name = topic.Id.Replace("script-ex-", "", StringComparison.Ordinal) + ".lua";
            RequestCreateScriptFromExample?.Invoke(this,
                new CreateScriptFromExampleEventArgs(name.Trim(), code));
        };
        btnRow.Children.Add(btnCreate);

        var btnCopy = new System.Windows.Controls.Button
        {
            Content = "Copiar al portapapeles",
            Padding = new Thickness(12, 6, 8, 6),
            Background = B("21262d"),
            Foreground = B("c9d1d9"),
            BorderBrush = B("30363d"),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        btnCopy.Click += (_, _) =>
        {
            try
            {
                System.Windows.Clipboard.SetText(code);
                EditorLog.Toast("Código copiado.", LogLevel.Info, "Ayuda");
            }
            catch (Exception ex)
            {
                EditorLog.Warning("No se pudo copiar: " + ex.Message, "Ayuda");
            }
        };
        btnRow.Children.Add(btnCopy);

        DetailPanel.Children.Add(btnRow);
    }

    private static TextEditor CreateReadOnlyLuaEditor(IHighlightingDefinition? hl, string text)
    {
        var editor = new TextEditor
        {
            IsReadOnly = true,
            ShowLineNumbers = true,
            FontFamily = new System.Windows.Media.FontFamily("Consolas,Cascadia Mono,Courier New"),
            FontSize = 12,
            SyntaxHighlighting = hl,
            Text = text,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = 420,
            MinHeight = 120,
            Background = B("0d1117"),
            Padding = new Thickness(8, 8, 8, 8),
            BorderThickness = new Thickness(0),
            LineNumbersForeground = B("8b949e")
        };
        if (hl != null)
        {
            editor.ClearValue(TextEditor.ForegroundProperty);
            editor.TextArea.ClearValue(System.Windows.Controls.Control.ForegroundProperty);
            editor.TextArea.SelectionBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(102, 88, 166, 255));
        }
        else
            editor.Foreground = B("c9d1d9");
        return editor;
    }
}
