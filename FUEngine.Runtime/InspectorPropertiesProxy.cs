using System;
using FUEngine.Core;

namespace FUEngine.Runtime;

/// <summary>
/// Proxy tipo tabla para Lua: <c>self.properties["x"]</c> o <c>self.properties.x</c> según el host.
/// Refleja propiedades habituales del Inspector (transform + tinte de sprite).
/// </summary>
[LuaVisible]
public sealed class InspectorPropertiesProxy
{
    private readonly GameObject _go;

    internal InspectorPropertiesProxy(GameObject go) => _go = go;

    public object? this[string key]
    {
        get => Get(key);
        set => Set(key, value);
    }

    private object? Get(string? key)
    {
        var k = Normalize(key);
        var s = _go.GetComponent<SpriteComponent>();
        return k switch
        {
            "x" => _go.Transform.X,
            "y" => _go.Transform.Y,
            "rotation" => _go.Transform.RotationDegrees,
            "scale" or "scalex" => _go.Transform.ScaleX,
            "scaley" => _go.Transform.ScaleY,
            "renderorder" => _go.RenderOrder,
            "tintr" or "color.r" => s?.ColorTintR ?? 1f,
            "tintg" or "color.g" => s?.ColorTintG ?? 1f,
            "tintb" or "color.b" => s?.ColorTintB ?? 1f,
            "visible" => _go.RuntimeActive,
            _ => null
        };
    }

    private void Set(string? key, object? value)
    {
        var k = Normalize(key);
        var s = _go.GetComponent<SpriteComponent>();
        switch (k)
        {
            case "x":
                _go.Transform.X = ToFloat(value, _go.Transform.X);
                break;
            case "y":
                _go.Transform.Y = ToFloat(value, _go.Transform.Y);
                break;
            case "rotation":
                _go.Transform.RotationDegrees = ToFloat(value, _go.Transform.RotationDegrees);
                break;
            case "scale":
            {
                var sc = ToFloat(value, (_go.Transform.ScaleX + _go.Transform.ScaleY) / 2f);
                _go.Transform.ScaleX = _go.Transform.ScaleY = sc;
                break;
            }
            case "scalex":
                _go.Transform.ScaleX = ToFloat(value, _go.Transform.ScaleX);
                break;
            case "scaley":
                _go.Transform.ScaleY = ToFloat(value, _go.Transform.ScaleY);
                break;
            case "renderorder":
                _go.RenderOrder = (int)ToDouble(value, _go.RenderOrder);
                break;
            case "tintr":
            case "color.r":
                if (s != null) s.ColorTintR = ToFloat(value, s.ColorTintR);
                break;
            case "tintg":
            case "color.g":
                if (s != null) s.ColorTintG = ToFloat(value, s.ColorTintG);
                break;
            case "tintb":
            case "color.b":
                if (s != null) s.ColorTintB = ToFloat(value, s.ColorTintB);
                break;
            case "visible":
                _go.RuntimeActive = ToBool(value, _go.RuntimeActive);
                break;
        }
    }

    private static string Normalize(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return "";
        return key.Trim().ToLowerInvariant();
    }

    private static float ToFloat(object? value, float fallback)
    {
        if (value == null) return fallback;
        try { return Convert.ToSingle(value); } catch { return fallback; }
    }

    private static double ToDouble(object? value, double fallback)
    {
        if (value == null) return fallback;
        try { return Convert.ToDouble(value); } catch { return fallback; }
    }

    private static bool ToBool(object? value, bool fallback)
    {
        if (value == null) return fallback;
        try { return Convert.ToBoolean(value); } catch { return fallback; }
    }
}
