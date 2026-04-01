using System;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace FUEngine;

/// <summary>Fondo tenue + subrayado rojo en la línea con error de sintaxis Lua (AvalonEdit).</summary>
internal sealed class LuaSyntaxErrorLineRenderer : IBackgroundRenderer
{
    private readonly TextDocument _document;
    private static readonly SolidColorBrush s_lineBg = CreateBrush(40, 255, 72, 72);
    private static readonly SolidColorBrush s_underline = CreateBrush(255, 220, 62, 62);

    private readonly struct DocSegment : ISegment
    {
        public DocSegment(int offset, int length)
        {
            Offset = offset;
            Length = length;
        }
        public int Offset { get; }
        public int Length { get; }
        public int EndOffset => Offset + Length;
    }

    private static SolidColorBrush CreateBrush(byte a, byte r, byte g, byte b)
    {
        var br = new SolidColorBrush(System.Windows.Media.Color.FromArgb(a, r, g, b));
        br.Freeze();
        return br;
    }

    public LuaSyntaxErrorLineRenderer(TextDocument document) => _document = document;

    /// <summary>1-based. Null = sin error.</summary>
    public int? ErrorLineNumber { get; set; }

    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (ErrorLineNumber is not int lineNo || lineNo < 1) return;
        if (lineNo > _document.LineCount) return;

        var docLine = _document.GetLineByNumber(lineNo);
        var segment = new DocSegment(docLine.Offset, docLine.TotalLength);
        foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment, true))
        {
            drawingContext.DrawRectangle(s_lineBg, null, rect);
            var underline = new Rect(rect.X, rect.Bottom - 2.5, Math.Max(0, rect.Width), 2.5);
            drawingContext.DrawRectangle(s_underline, null, underline);
        }
    }
}
