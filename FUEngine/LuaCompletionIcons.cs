using System;
using System.Collections.Generic;
using Media = System.Windows.Media;

namespace FUEngine;

public enum LuaCompletionIconKind
{
    Default,
    Keyword,
    Snippet,
    GlobalTable,
    EntityGlobal,
    Ads,
    Member,
    AdsMember
}

/// <summary>Iconos vectoriales en caché para el popup de completado (AvalonEdit).</summary>
public static class LuaCompletionIcons
{
    private static readonly Dictionary<LuaCompletionIconKind, Media.ImageSource?> Cache = new();

    public static Media.ImageSource? Get(LuaCompletionIconKind kind)
    {
        if (kind == LuaCompletionIconKind.Default)
            return null;
        if (Cache.TryGetValue(kind, out var cached))
            return cached;
        var img = Create(kind);
        Cache[kind] = img;
        return img;
    }

    private static Media.ImageSource? Create(LuaCompletionIconKind kind)
    {
        Media.Brush brush = kind switch
        {
            LuaCompletionIconKind.Keyword => new Media.SolidColorBrush(Media.Color.FromRgb(0x7c, 0xa8, 0xff)),
            LuaCompletionIconKind.Snippet => new Media.SolidColorBrush(Media.Color.FromRgb(0xc7, 0x9e, 0xf0)),
            LuaCompletionIconKind.GlobalTable => new Media.SolidColorBrush(Media.Color.FromRgb(0x8b, 0xc3, 0x4a)),
            LuaCompletionIconKind.EntityGlobal => new Media.SolidColorBrush(Media.Color.FromRgb(0x5c, 0x9e, 0xd6)),
            LuaCompletionIconKind.Ads => new Media.SolidColorBrush(Media.Color.FromRgb(0xf5, 0xb3, 0x2d)),
            LuaCompletionIconKind.AdsMember => new Media.SolidColorBrush(Media.Color.FromRgb(0xf5, 0xb3, 0x2d)),
            LuaCompletionIconKind.Member => new Media.SolidColorBrush(Media.Color.FromRgb(0xff, 0xd5, 0x4a)),
            _ => new Media.SolidColorBrush(Media.Color.FromRgb(0x9a, 0xa0, 0xa6))
        };

        Media.Geometry geo = kind switch
        {
            LuaCompletionIconKind.EntityGlobal => Media.Geometry.Parse("M4,2 L12,2 L14,6 L14,14 L2,14 L2,6 Z"),
            LuaCompletionIconKind.Ads => EllipseGeometry(new System.Windows.Point(8, 8), 6, 6),
            LuaCompletionIconKind.AdsMember => EllipseGeometry(new System.Windows.Point(8, 8), 5, 5),
            LuaCompletionIconKind.Member => Media.Geometry.Parse("M4,12 L8,4 L12,12 M6,9 h4"),
            _ => Media.Geometry.Parse("M4,4 h8 v8 h-8 Z")
        };

        var drawing = new Media.GeometryDrawing(brush, null, geo);
        var image = new Media.DrawingImage(drawing);
        image.Freeze();
        return image;
    }

    private static Media.Geometry EllipseGeometry(System.Windows.Point center, double rx, double ry)
    {
        var g = new Media.EllipseGeometry(center, rx, ry);
        return g;
    }
}
