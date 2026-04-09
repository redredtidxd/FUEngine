using System;
using FUEngine.Core;

namespace FUEngine.Runtime;

/// <summary>
/// Acceso corto a componentes del mismo objeto: <c>self.components["ClickInteractable"]</c> o nombre + sufijo <c>Component</c>.
/// </summary>
[LuaVisible]
public sealed class SelfComponentsProxy
{
    private readonly GameObject _gameObject;
    private readonly Func<string, object?> _getComponent;

    internal SelfComponentsProxy(GameObject gameObject, Func<string, object?> getComponent)
    {
        _gameObject = gameObject;
        _getComponent = getComponent;
    }

    public object? this[string? typeKey]
    {
        get
        {
            var name = NormalizeTypeName(typeKey);
            return string.IsNullOrEmpty(name) ? null : _getComponent(name);
        }
    }

    private static string NormalizeTypeName(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return "";
        var t = key.Trim();
        if (t.EndsWith("Component", StringComparison.OrdinalIgnoreCase))
            return t;
        return t + "Component";
    }
}
