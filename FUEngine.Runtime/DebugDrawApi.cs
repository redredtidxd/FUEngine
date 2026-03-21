namespace FUEngine.Runtime;

public enum DebugDrawKind
{
    Line,
    Circle
}

/// <summary>Comando de depuración para el viewport del tab Juego (mismas unidades que <see cref="GameObject.Transform"/> X/Y).</summary>
public readonly struct DebugDrawItem
{
    public DebugDrawKind Kind { get; }
    public double X1 { get; }
    public double Y1 { get; }
    public double X2 { get; }
    public double Y2 { get; }
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }
    public byte A { get; }

    public DebugDrawItem(DebugDrawKind kind, double x1, double y1, double x2, double y2, byte r, byte g, byte b, byte a)
    {
        Kind = kind;
        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
        R = r;
        G = g;
        B = b;
        A = a;
    }
}

/// <summary>
/// API Lua: <c>Debug.drawLine(...)</c>, <c>Debug.drawCircle(...)</c>. Colores 0–255 (RGBA opcional).
/// </summary>
public sealed class DebugDrawApi
{
    private readonly List<DebugDrawItem> _buffer = new();
    private IReadOnlyList<DebugDrawItem> _snapshot = Array.Empty<DebugDrawItem>();

    public void drawLine(double x1, double y1, double x2, double y2) =>
        drawLine(x1, y1, x2, y2, 0, 255, 255, 220);

    public void drawLine(double x1, double y1, double x2, double y2, double r, double g, double b, double a)
    {
        _buffer.Add(new DebugDrawItem(DebugDrawKind.Line, x1, y1, x2, y2, ToByte(r), ToByte(g), ToByte(b), ToByte(a)));
    }

    public void drawCircle(double cx, double cy, double radius) =>
        drawCircle(cx, cy, radius, 0, 255, 255, 160);

    public void drawCircle(double cx, double cy, double radius, double r, double g, double b, double a)
    {
        if (radius < 0) radius = 0;
        _buffer.Add(new DebugDrawItem(DebugDrawKind.Circle, cx, cy, radius, 0, ToByte(r), ToByte(g), ToByte(b), ToByte(a)));
    }

    internal void FinalizeFrame()
    {
        _snapshot = _buffer.Count > 0 ? _buffer.ToArray() : Array.Empty<DebugDrawItem>();
        _buffer.Clear();
    }

    public IReadOnlyList<DebugDrawItem> GetLastFrameSnapshot() => _snapshot;

    private static byte ToByte(double v)
    {
        if (double.IsNaN(v) || double.IsInfinity(v)) return 255;
        var i = (int)Math.Round(v);
        if (i < 0) return 0;
        if (i > 255) return 255;
        return (byte)i;
    }
}
