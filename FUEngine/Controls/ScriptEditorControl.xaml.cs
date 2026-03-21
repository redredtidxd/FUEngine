using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
    };

    public ScriptEditorControl()
    {
        InitializeComponent();
        AvalonEditor.TextArea.TextEntered += AvalonEditor_OnTextEntered;
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
                return;
            }
        }
        else
            AvalonEditor.Text = "";
        var ext = Path.GetExtension(_filePath).ToLowerInvariant();
        if (ext == ".json")
            AvalonEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("JavaScript");
        else if (ext == ".lua")
            AvalonEditor.SyntaxHighlighting = LoadLuaHighlighting();
        else
            AvalonEditor.SyntaxHighlighting = null;
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
            if (_filePath.EndsWith(".lua", StringComparison.OrdinalIgnoreCase))
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
        if (e.Key == Key.Tab && Path.GetExtension(_filePath).Equals(".lua", StringComparison.OrdinalIgnoreCase))
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
            ShowLuaCompletion();
            e.Handled = true;
        }
    }

    private void AvalonEditor_OnTextEntered(object? sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        if (e.Text == "." && Path.GetExtension(_filePath).Equals(".lua", StringComparison.OrdinalIgnoreCase))
            ShowLuaCompletion();
    }

    private void ShowLuaCompletion()
    {
        var area = AvalonEditor.TextArea;
        var doc = area.Document;
        var offset = area.Caret.Offset;
        var line = doc.GetLineByOffset(offset);
        var lineStart = line.Offset;
        var textBefore = doc.GetText(lineStart, offset - lineStart);
        var completionList = new List<LuaCompletionItem>();
        if (textBefore.TrimEnd().EndsWith("self.", StringComparison.Ordinal))
        {
            foreach (var s in new[] { "id", "name", "tag", "x", "y", "rotation", "scale", "visible", "active", "destroy", "move", "rotate", "playAnimation", "stopAnimation", "getComponent" })
                completionList.Add(new LuaCompletionItem(s, s));
        }
        else if (textBefore.TrimEnd().EndsWith("world.", StringComparison.Ordinal))
        {
            foreach (var s in new[] { "spawn", "destroy", "findObject", "findByTag", "getPlayer", "getObjects" })
                completionList.Add(new LuaCompletionItem(s, s));
        }
        else if (textBefore.TrimEnd().EndsWith("input.", StringComparison.Ordinal))
        {
            foreach (var s in new[] { "isKeyDown", "isKeyPressed", "isMouseDown", "mouseX", "mouseY" })
                completionList.Add(new LuaCompletionItem(s, s));
        }
        else if (textBefore.TrimEnd().EndsWith("time.", StringComparison.Ordinal))
        {
            foreach (var s in new[] { "delta", "time", "frame", "scale" })
                completionList.Add(new LuaCompletionItem(s, s));
        }
        else
        {
            foreach (var (trigger, template) in LuaSnippets)
                completionList.Add(new LuaCompletionItem(trigger, template != null && template.StartsWith("function ") ? (template.Split('\n').FirstOrDefault()?.TrimEnd() ?? trigger) : trigger));
        }
        if (completionList.Count == 0) return;
        var window = new CompletionWindow(area);
        foreach (var item in completionList)
            window.CompletionList.CompletionData.Add(item);
        window.Show();
        window.Closed += (_, _) => window.CompletionList.CompletionData.Clear();
    }

    private void BtnSave_OnClick(object sender, RoutedEventArgs e) => SaveFile();

    private sealed class LuaCompletionItem : ICompletionData
    {
        private readonly string _text;
        private readonly string _content;

        public LuaCompletionItem(string text, string content)
        {
            _text = text;
            _content = content;
        }

        public System.Windows.Media.ImageSource? Image => null;
        public string Text => _text;
        public object? Content => _content;
        public object? Description => null;
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
            using var stream = asm.GetManifestResourceStream(name);
            if (stream == null) return null;
            using var reader = XmlReader.Create(stream);
            return HighlightingLoader.Load(reader, HighlightingManager.Instance);
        }
        catch
        {
            return null;
        }
    }
}
