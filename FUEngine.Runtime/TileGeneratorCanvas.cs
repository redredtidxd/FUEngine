using System;

namespace FUEngine.Runtime;

/// <summary>
/// Canvas passed to Lua onGenerateTile(canvas, width, height).
/// Buffer is BGRA, same as WPF WriteableBitmap.
/// </summary>
public sealed class TileGeneratorCanvas
{
    private readonly byte[] _bgra;
    private readonly int _width;
    private readonly int _height;

    public TileGeneratorCanvas(int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Width and height must be positive.");
        _width = width;
        _height = height;
        _bgra = new byte[width * height * 4];
    }

    public int Width => _width;
    public int Height => _height;

    /// <summary>Returns the raw BGRA buffer (not a copy).</summary>
    public byte[] GetBuffer() => _bgra;

    /// <summary>Set one pixel. Coordinates 0..width-1, 0..height-1. R,G,B,A 0-255.</summary>
    public void SetPixel(int x, int y, int r, int g, int b, int a)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height) return;
        r = Math.Clamp(r, 0, 255);
        g = Math.Clamp(g, 0, 255);
        b = Math.Clamp(b, 0, 255);
        a = Math.Clamp(a, 0, 255);
        int i = (y * _width + x) * 4;
        _bgra[i] = (byte)b;
        _bgra[i + 1] = (byte)g;
        _bgra[i + 2] = (byte)r;
        _bgra[i + 3] = (byte)a;
    }

    /// <summary>Fill a rectangle. x,y top-left; w,h size. R,G,B,A 0-255.</summary>
    public void FillRect(int x, int y, int w, int h, int r, int g, int b, int a)
    {
        r = Math.Clamp(r, 0, 255);
        g = Math.Clamp(g, 0, 255);
        b = Math.Clamp(b, 0, 255);
        a = Math.Clamp(a, 0, 255);
        int xEnd = Math.Min(x + w, _width);
        int yEnd = Math.Min(y + h, _height);
        int xStart = Math.Max(x, 0);
        int yStart = Math.Max(y, 0);
        for (int py = yStart; py < yEnd; py++)
        {
            for (int px = xStart; px < xEnd; px++)
            {
                int i = (py * _width + px) * 4;
                _bgra[i] = (byte)b;
                _bgra[i + 1] = (byte)g;
                _bgra[i + 2] = (byte)r;
                _bgra[i + 3] = (byte)a;
            }
        }
    }

    /// <summary>Get pixel color. Returns r, g, b, a (0-255).</summary>
    public (int r, int g, int b, int a) GetPixel(int x, int y)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height)
            return (0, 0, 0, 0);
        int i = (y * _width + x) * 4;
        return (_bgra[i + 2], _bgra[i + 1], _bgra[i], _bgra[i + 3]);
    }
}
