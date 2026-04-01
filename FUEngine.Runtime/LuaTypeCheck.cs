using System;

namespace FUEngine.Runtime;

/// <summary>
/// Conversiones y comprobaciones en el borde Lua→C# para fallar con mensajes claros.
/// </summary>
public static class LuaTypeCheck
{
    public static int ToInt32(object? value, string paramName)
    {
        if (value is null)
            throw new ArgumentException($"{paramName} no puede ser nil.");
        if (value is int i) return i;
        if (value is long l) return checked((int)l);
        if (value is double d) return (int)Math.Round(d);
        if (value is float f) return (int)Math.Round(f);
        if (value is string s && int.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            return parsed;
        throw new ArgumentException($"{paramName} debe ser un número entero (recibido: {value.GetType().Name}).");
    }

    public static double ToDouble(object? value, string paramName)
    {
        if (value is null)
            throw new ArgumentException($"{paramName} no puede ser nil.");
        if (value is double d) return d;
        if (value is float f) return f;
        if (value is int i) return i;
        if (value is long l) return l;
        if (value is string s && double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            return parsed;
        throw new ArgumentException($"{paramName} debe ser un número (recibido: {value.GetType().Name}).");
    }
}
