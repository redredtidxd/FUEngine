using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace FUEngine;

public partial class ScriptEditorControl : System.Windows.Controls.UserControl
{
    private string _filePath = "";
    private string _originalContent = "";
    private bool _modified;
    private CompletionWindow? _completionWindow;

    private static readonly (string Trigger, string Template)[] LuaSnippets =
    {
        ("update", "function onUpdate(dt)\n    \nend"),
        ("awake", "function onAwake()\n    \nend"),
        ("start", "function onStart()\n    \nend"),
        ("onInteract", "function onInteract(player)\n    \nend"),
        ("onCollision", "function onCollision(other)\n    \nend"),
        ("onTriggerEnter", "function onTriggerEnter(other)\n    \nend"),
        ("onTriggerExit", "function onTriggerExit(other)\n    \nend"),
        ("onDestroy", "function onDestroy()\n    \nend"),
        ("dbggrid", "function onUpdate(dt)\n    Debug.drawGrid(\n        self.x or 0,\n        self.y or 0,\n        8, 8, 1,\n        0, 255, 255, 140)\nend"),
    };

    public ScriptEditorControl()
    {
        InitializeComponent();
        AvalonEditor.TextArea.TextEntered += AvalonEditor_OnTextEntered;
        AvalonEditor.TextArea.PreviewKeyDown += TextArea_PreviewKeyDown;
        Loaded += ScriptEditorControl_OnLoaded;
    }

    private void ScriptEditorControl_OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= ScriptEditorControl_OnLoaded;
        RefreshSyntaxVisual();
    }

    /// <summary>Tras montar la pestaña, un segundo repintado evita texto plano heredado del Window (App.xaml Foreground).</summary>
    private void RefreshSyntaxVisual()
    {
        if (!IsLuaFile() || AvalonEditor.SyntaxHighlighting == null) return;
        try
        {
            AvalonEditor.TextArea.TextView.Redraw();
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try { AvalonEditor.TextArea.TextView.Redraw(); }
                catch { /* ignore */ }
            }), DispatcherPriority.Loaded);
        }
        catch { /* ignore */ }
    }

    private void TextArea_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _completionWindow != null)
        {
            CloseCompletionWindowIfAny();
            e.Handled = true;
        }
    }

    private void CloseCompletionWindowIfAny()
    {
        try { _completionWindow?.Close(); }
        catch { /* ignore */ }
        _completionWindow = null;
    }

    private bool IsLuaFile()
    {
        var ext = Path.GetExtension(_filePath);
        return ext.Equals(".lua", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".script", StringComparison.OrdinalIgnoreCase);
    }

    public string FilePath => _filePath;
    public bool IsModified => _modified;

    /// <summary>Se dispara cuando cambia el estado de modificación (para actualizar título de pestaña).</summary>
    public event EventHandler? ModifiedChanged;
    /// <summary>Se dispara al guardar un archivo (para hot reload en runtime). Parámetro: ruta completa del archivo.</summary>
    public event EventHandler<string>? ScriptSaved;

    public void LoadFile(string? filePath)
    {
        _filePath = filePath ?? "";
        TxtFilePath.Text = string.IsNullOrEmpty(_filePath) ? "(ningún archivo)" : _filePath;
        _originalContent = "";
        SetModified(false);

        var ext = Path.GetExtension(_filePath).ToLowerInvariant();
        IHighlightingDefinition? hl = null;
        if (ext == ".json" || ext == ".scene")
        {
            hl = HighlightingManager.Instance.GetDefinition("JSON")
                ?? HighlightingManager.Instance.GetDefinition("JavaScript");
        }
        else if (ext == ".lua" || ext == ".script")
        {
            // Preferir nuestro Lua.xshd embebido (colores tema oscuro); el definido en AvalonEdit puede diferir.
            hl = LoadLuaHighlighting() ?? HighlightingManager.Instance.GetDefinition("Lua");
        }

        AvalonEditor.SyntaxHighlighting = hl;
        if (hl == null && (ext == ".lua" || ext == ".script"))
            AvalonEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Lua");
        // ClearValue deja el Foreground heredado del Window (App.xaml) y el buffer parece un solo color claro.
        if (hl != null)
            AvalonEditor.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xc9, 0xd1, 0xd9));
        else
            AvalonEditor.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3));

        if (!string.IsNullOrEmpty(_filePath) && File.Exists(_filePath))
        {
            try
            {
                _originalContent = File.ReadAllText(_filePath, Encoding.UTF8);
                AvalonEditor.Text = _originalContent;
            }
            catch (Exception ex)
            {
                AvalonEditor.Text = "";
                TxtStatus.Text = "Error al cargar: " + ex.Message;
                AvalonEditor.TextArea.TextView.Redraw();
                return;
            }
        }
        else
            AvalonEditor.Text = "";

        AvalonEditor.TextArea.TextView.Redraw();
        Dispatcher.BeginInvoke(new Action(RefreshSyntaxVisual), DispatcherPriority.Loaded);
        TxtStatus.Text = "Listo";
    }

    /// <summary>Coloca el cursor en la línea dada (1-based) y hace scroll para mostrarla.</summary>
    public void GoToLine(int lineNumber)
    {
        if (lineNumber < 1) return;
        var doc = AvalonEditor.Document;
        if (doc == null || lineNumber > doc.LineCount) return;
        var line = doc.GetLineByNumber(lineNumber);
        AvalonEditor.CaretOffset = line.Offset;
        AvalonEditor.ScrollToLine(lineNumber);
    }

    public bool SaveFile()
    {
        if (string.IsNullOrEmpty(_filePath))
        {
            TxtStatus.Text = "No hay archivo abierto.";
            return false;
        }
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(_filePath, AvalonEditor.Text, Encoding.UTF8);
            _originalContent = AvalonEditor.Text;
            SetModified(false);
            TxtStatus.Text = "Guardado.";
            if (_filePath.EndsWith(".lua", StringComparison.OrdinalIgnoreCase)
                || _filePath.EndsWith(".script", StringComparison.OrdinalIgnoreCase))
                ScriptSaved?.Invoke(this, _filePath);
            return true;
        }
        catch (Exception ex)
        {
            TxtStatus.Text = "Error: " + ex.Message;
            System.Windows.MessageBox.Show("Error al guardar: " + ex.Message, "Guardar", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private void AvalonEditor_OnTextChanged(object? sender, EventArgs e)
    {
        SetModified(AvalonEditor.Text != _originalContent);
    }

    private void SetModified(bool value)
    {
        if (_modified == value) return;
        _modified = value;
        TxtModified.Visibility = _modified ? Visibility.Visible : Visibility.Collapsed;
        ModifiedChanged?.Invoke(this, EventArgs.Empty);
    }

    private void AvalonEditor_OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;
            SaveFile();
            return;
        }
        if (e.Key == Key.Tab && IsLuaFile())
        {
            var area = AvalonEditor.TextArea;
            var doc = area.Document;
            var line = doc.GetLineByOffset(area.Caret.Offset);
            var lineText = doc.GetText(line.Offset, line.Length).TrimEnd();
            var caretInLine = area.Caret.Offset - line.Offset;
            var beforeCaret = lineText[..Math.Min(caretInLine, lineText.Length)];
            foreach (var (trigger, template) in LuaSnippets)
            {
                if (!beforeCaret.TrimEnd().EndsWith(trigger, StringComparison.Ordinal))
                    continue;
                var startOffset = area.Caret.Offset - trigger.Length;
                if (startOffset < 0 || doc.GetText(startOffset, trigger.Length) != trigger)
                    continue;
                doc.Replace(startOffset, trigger.Length, template);
                var insertLine = doc.GetLineByOffset(startOffset);
                var lineStart = insertLine.Offset;
                var indent = startOffset - lineStart;
                var newCaret = startOffset + template.IndexOf("\n    ", StringComparison.Ordinal) + 5;
                if (newCaret <= startOffset) newCaret = startOffset + template.IndexOf('\n') + 1 + indent;
                area.Caret.Offset = Math.Min(newCaret, doc.TextLength);
                e.Handled = true;
                return;
            }
        }
        if (e.Key == Key.Space && Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (IsLuaFile())
            {
                var doc = AvalonEditor.Document;
                var offset = AvalonEditor.CaretOffset;
                var line = doc.GetLineByOffset(offset);
                var textBefore = doc.GetText(line.Offset, offset - line.Offset).TrimEnd();
                if (textBefore.EndsWith(".", StringComparison.Ordinal))
                    ShowMemberCompletionAfterDot();
                else
                    TryShowIdentifierCompletion();
            }
            e.Handled = true;
        }
    }

    private void AvalonEditor_OnTextEntered(object? sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        if (!IsLuaFile() || string.IsNullOrEmpty(e.Text)) return;
        if (e.Text == ".")
        {
            ShowMemberCompletionAfterDot();
            return;
        }
        if (e.Text.Length == 1 && (char.IsLetterOrDigit(e.Text[0]) || e.Text[0] == '_'))
            TryShowIdentifierCompletion();
    }

    /// <summary>Tras escribir "tabla." muestra miembros (world, self, ads, etc.).</summary>
    private void ShowMemberCompletionAfterDot()
    {
        var area = AvalonEditor.TextArea;
        var doc = area.Document;
        var offset = area.Caret.Offset;
        var line = doc.GetLineByOffset(offset);
        var lineStart = line.Offset;
        var textBefore = doc.GetText(lineStart, offset - lineStart);
        var trimmed = textBefore.TrimEnd();
        var completionList = new List<LuaCompletionItem>();
        var members = LuaEditorCompletionCatalog.GetMembersAfterDot(trimmed);
        if (members != null)
        {
            var memberIcon = trimmed.EndsWith("ads.", StringComparison.OrdinalIgnoreCase)
                ? LuaCompletionIconKind.AdsMember
                : LuaCompletionIconKind.Member;
            foreach (var s in members)
                completionList.Add(new LuaCompletionItem(s, s, $"API {trimmed}", memberIcon));
        }
        else
        {
            foreach (var (trigger, template) in LuaSnippets)
            {
                var insert = template.StartsWith("function ", StringComparison.Ordinal)
                    ? (template.Split('\n').FirstOrDefault()?.TrimEnd() ?? trigger)
                    : trigger;
                completionList.Add(new LuaCompletionItem(trigger, insert, "Snippet", LuaCompletionIconKind.Snippet));
            }
        }
        if (completionList.Count == 0) return;
        CloseCompletionWindowIfAny();
        _completionWindow = new CompletionWindow(area);
        foreach (var item in completionList)
            _completionWindow.CompletionList.CompletionData.Add(item);
        var win = _completionWindow;
        win.Closed += (_, _) => { _completionWindow = null; };
        win.Show();
    }

    private static int FindIdentifierStart(TextDocument doc, int offset)
    {
        var i = offset - 1;
        while (i >= 0)
        {
            var c = doc.GetCharAt(i);
            if (char.IsLetterOrDigit(c) || c == '_')
                i--;
            else
                break;
        }
        return i + 1;
    }

    /// <summary>Mientras escribes un identificador: palabras clave, globales (world, ads…), snippets.</summary>
    private void TryShowIdentifierCompletion()
    {
        var doc = AvalonEditor.Document;
        var area = AvalonEditor.TextArea;
        var offset = area.Caret.Offset;
        var line = doc.GetLineByOffset(offset);
        var col = offset - line.Offset;
        var lineText = doc.GetText(line.Offset, line.Length);
        var beforeCaret = col <= 0 ? "" : lineText[..Math.Min(col, lineText.Length)];
        if (beforeCaret.TrimStart().StartsWith("--", StringComparison.Ordinal))
            return;

        var wordStart = FindIdentifierStart(doc, offset);
        var prefix = doc.GetText(wordStart, offset - wordStart);
        if (prefix.Length < 1) return;

        var entries = LuaEditorCompletionCatalog.FilterWordPrefix(prefix, LuaSnippets).Take(48).ToList();
        if (entries.Count == 0) return;

        CloseCompletionWindowIfAny();
        _completionWindow = new CompletionWindow(area);
        foreach (var entry in entries)
            _completionWindow.CompletionList.CompletionData.Add(new LuaCompletionItem(entry.Text, entry.InsertText, entry.Description, entry.IconKind));
        _completionWindow.Closed += (_, _) => { _completionWindow = null; };
        _completionWindow.Show();
    }

    private void BtnSave_OnClick(object sender, RoutedEventArgs e) => SaveFile();

    private sealed class LuaCompletionItem : ICompletionData
    {
        private readonly string _text;
        private readonly string _content;
        private readonly string? _description;
        private readonly System.Windows.Media.ImageSource? _image;

        public LuaCompletionItem(string text, string content, string? description = null, LuaCompletionIconKind iconKind = LuaCompletionIconKind.Default)
        {
            _text = text;
            _content = content;
            _description = description;
            _image = LuaCompletionIcons.Get(iconKind);
        }

        public System.Windows.Media.ImageSource? Image => _image;
        public string Text => _text;
        public object? Content => _content;
        public object? Description => _description ?? _text;
        public double Priority => 0;

        public void Complete(TextArea textArea, ISegment segment, EventArgs e)
        {
            textArea.Document.Replace(segment, _content ?? _text);
        }
    }

    private static IHighlightingDefinition? LoadLuaHighlighting()
    {
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            var name = asm.GetName().Name + ".Resources.Lua.xshd";
            Stream? stream = asm.GetManifestResourceStream(name);
            if (stream == null)
            {
                var dir = Path.GetDirectoryName(asm.Location);
                if (!string.IsNullOrEmpty(dir))
                {
                    var disk = Path.Combine(dir, "Resources", "Lua.xshd");
                    if (File.Exists(disk)) stream = File.OpenRead(disk);
                }
            }
            if (stream == null)
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var disk2 = Path.Combine(baseDir ?? "", "Resources", "Lua.xshd");
                if (File.Exists(disk2)) stream = File.OpenRead(disk2);
            }
            if (stream == null) return null;
            using (stream)
            using (var reader = XmlReader.Create(stream))
                return HighlightingLoader.Load(reader, HighlightingManager.Instance);
        }
        catch
        {
            return null;
        }
    }
}
