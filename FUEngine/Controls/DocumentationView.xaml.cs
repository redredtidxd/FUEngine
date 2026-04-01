using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using FUEngine.Help;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;

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
        Loaded += DocumentationView_OnLoaded;
    }

    /// <summary>Pestaña «Lua — sintaxis y librería».</summary>
    public bool LuaReferenceMode { get; set; }

    /// <summary>Pestaña «Ejemplos de scripts».</summary>
    public bool ScriptExamplesMode { get; set; }

    /// <summary>Si devuelve true, se habilita «Crear script desde este ejemplo» (p. ej. editor con proyecto).</summary>
    public Func<bool>? AllowExportScriptExample { get; set; }

    public string? InitialTopicId { get; set; }

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
        var q = (TxtFilter?.Text ?? "").Trim();
        IEnumerable<DocumentationTopic> src = _allTopics;
        if (q.Length > 0)
        {
            var ql = q.ToLowerInvariant();
            src = _allTopics.Where(t => TopicMatchesFilter(t, ql));
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
        if (ScriptExamplesMode && !string.IsNullOrEmpty(t.ExampleDifficulty))
        {
            var badge = t.ExampleDifficulty switch
            {
                "Básico" => "🟢 ",
                "Intermedio" => "🟡 ",
                "Avanzado" => "🔴 ",
                _ => ""
            };
            title = badge + title;
        }
        if (ScriptExamplesMode && !string.IsNullOrEmpty(t.ExampleCategory))
            title = t.ExampleCategory + " · " + title;
        var sameTitle = _visibleTopics.Count(x =>
            string.Equals(x.Title ?? "", t.Title ?? "", StringComparison.Ordinal)
            && string.Equals(x.ExampleCategory ?? "", t.ExampleCategory ?? "", StringComparison.Ordinal)) > 1;
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

    private record struct DetailTheme(SolidColorBrush TitleAccent, SolidColorBrush SectionAccent, int TitleSize);

    private DetailTheme GetDetailTheme()
    {
        if (ScriptExamplesMode)
            return new DetailTheme(B("d2a8ff"), B("a371f7"), 22);
        if (LuaReferenceMode)
            return new DetailTheme(B("79c0ff"), B("a371f7"), 23);
        return new DetailTheme(B("58a6ff"), B("58a6ff"), 22);
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

        DetailPanel.Children.Add(new TextBlock
        {
            Text = topic.Title ?? "",
            FontSize = theme.TitleSize,
            FontWeight = FontWeights.SemiBold,
            Foreground = theme.TitleAccent,
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

        if (ScriptExamplesMode && !string.IsNullOrWhiteSpace(topic.ExampleDifficulty))
        {
            var d = topic.ExampleDifficulty!;
            var badge = d switch
            {
                "Básico" => "🟢 Básico",
                "Intermedio" => "🟡 Intermedio",
                "Avanzado" => "🔴 Avanzado",
                _ => d
            };
            DetailPanel.Children.Add(new TextBlock
            {
                Text = badge,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = B("7ee787"),
                Margin = new Thickness(0, 0, 0, 10)
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

        AddSection("Para qué", topic.ParaQue, theme.SectionAccent);
        AddSection("Por qué importa", topic.PorQueImporta, theme.SectionAccent);

        if (!string.IsNullOrWhiteSpace(topic.EnMotor))
            AppendMotorCallout(topic.EnMotor, body, motorBorder, motorBg);

        AppendParagraphs(topic.Paragraphs, body, muted);
        AppendBullets(topic.Bullets, body);

        if (!string.IsNullOrEmpty(topic.LuaExampleCode))
            AppendLuaExampleBlock(topic, body, muted);
    }

    private void AppendMotorCallout(string enMotor, SolidColorBrush body, SolidColorBrush motorBorder, SolidColorBrush motorBg)
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
            Text = enMotor,
            FontSize = 13,
            Foreground = body,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 20
        });
        callout.Child = inner;
        DetailPanel!.Children.Add(callout);
    }

    private void AppendParagraphs(IReadOnlyList<string>? paragraphs, SolidColorBrush body, SolidColorBrush muted)
    {
        if (paragraphs is not { Count: > 0 }) return;

        DetailPanel!.Children.Add(new TextBlock
        {
            Text = "Contenido",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = muted,
            Margin = new Thickness(0, 18, 0, 8)
        });
        foreach (var p in paragraphs)
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

    private void AppendBullets(IReadOnlyList<string>? bullets, SolidColorBrush body)
    {
        if (bullets is not { Count: > 0 }) return;

        var bulletColor = B("7ee787");
        DetailPanel!.Children.Add(new TextBlock
        {
            Text = "Puntos clave",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = bulletColor,
            Margin = new Thickness(0, 14, 0, 8)
        });
        foreach (var b in bullets)
        {
            if (string.IsNullOrEmpty(b)) continue;
            var row = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
            row.Children.Add(new TextBlock
            {
                Text = "• ",
                Foreground = bulletColor,
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

    private void AppendLuaExampleBlock(DocumentationTopic topic, SolidColorBrush body, SolidColorBrush muted)
    {
        DetailPanel!.Children.Add(new TextBlock
        {
            Text = "Código",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = muted,
            Margin = new Thickness(0, 20, 0, 8)
        });

        var codeBorder = new Border
        {
            BorderBrush = B("30363d"),
            BorderThickness = new Thickness(1),
            Background = B("161b22"),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 12)
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
                ? "Crea un .lua en Scripts/ del proyecto y lo registra en scripts.json"
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
