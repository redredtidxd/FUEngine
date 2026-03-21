namespace FUEngine.Core;

/// <summary>
/// Overlay editable a nivel píxel sobre un tile (16x16, 32x32, 64x64 o personalizado).
/// Cada píxel tiene RGBA. Se superpone sobre la imagen base del tile.
/// </summary>
public class TilePixelOverlay
{
    public int Width { get; set; }
    public int Height { get; set; }
    /// <summary>Datos RGBA por píxel, row-major: [y * Width + x] * 4 + (0=R, 1=G, 2=B, 3=A).</summary>
    public byte[] RgbaData { get; set; } = Array.Empty<byte>();

    public TilePixelOverlay() { }

    public TilePixelOverlay(int width, int height)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);
        RgbaData = new byte[Width * Height * 4];
    }

    public void EnsureSize(int width, int height)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        if (Width == width && Height == height && RgbaData.Length == width * height * 4)
            return;
        Width = width;
        Height = height;
        RgbaData = new byte[Width * Height * 4];
    }

    public (byte r, byte g, byte b, byte a) GetPixel(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height || RgbaData == null || RgbaData.Length < Width * Height * 4)
            return (0, 0, 0, 0);
        int i = (y * Width + x) * 4;
        return (RgbaData[i], RgbaData[i + 1], RgbaData[i + 2], RgbaData[i + 3]);
    }

    public void SetPixel(int x, int y, byte r, byte g, byte b, byte a)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height || RgbaData == null) return;
        int i = (y * Width + x) * 4;
        if (i + 3 >= RgbaData.Length) return;
        RgbaData[i] = r;
        RgbaData[i + 1] = g;
        RgbaData[i + 2] = b;
        RgbaData[i + 3] = a;
    }

    public void Clear(byte r = 0, byte g = 0, byte b = 0, byte a = 0)
    {
        if (RgbaData == null) return;
        for (int i = 0; i < RgbaData.Length; i += 4)
        {
            RgbaData[i] = r;
            RgbaData[i + 1] = g;
            RgbaData[i + 2] = b;
            RgbaData[i + 3] = a;
        }
    }

    public TilePixelOverlay Clone()
    {
        var c = new TilePixelOverlay(Width, Height);
        if (RgbaData != null && RgbaData.Length == c.RgbaData.Length)
            Array.Copy(RgbaData, c.RgbaData, RgbaData.Length);
        return c;
    }
}
