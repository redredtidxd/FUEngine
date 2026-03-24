using System;
using FUEngine.Core;
using NLua;

namespace FUEngine.Runtime;

/// <summary>
/// Proxy de un componente expuesto a Lua. Permite llamar métodos del script (ej: Health:TakeDamage(10))
/// mediante invoke("methodName", args). Para ScriptComponent usa la ScriptInstance asociada.
/// </summary>
[LuaVisible]
public sealed class ComponentProxy
{
    private readonly Component _component;
    private readonly ScriptInstance? _scriptInstance;

    public ComponentProxy(Component component, ScriptInstance? scriptInstance = null)
    {
        _component = component ?? throw new ArgumentNullException(nameof(component));
        _scriptInstance = scriptInstance;
    }

    /// <summary>Nombre del tipo de componente (ej: "Health", "ScriptComponent").</summary>
    public string typeName => _component.GetType().Name;

    /// <summary>
    /// Invoca un método del componente. Para scripts Lua usa la instancia (ej: invoke("takeDamage", 10) → onDamage(10) en Lua).
    /// Nombres típicos: takeDamage, play, setTrigger, etc. El script puede definir funciones con ese nombre.
    /// </summary>
    public bool invoke(string methodName, params object[] args)
    {
        if (string.IsNullOrEmpty(methodName)) return false;
        if (_scriptInstance != null)
            return _scriptInstance.TryInvoke(methodName, out _, out _, args ?? Array.Empty<object>());
        // Futuro: otros tipos de componente podrían exponer métodos aquí
        return false;
    }

    /// <summary>Invoca y devuelve el primer valor de retorno (para scripts que devuelven algo).</summary>
    public object? invokeWithResult(string methodName, params object[] args)
    {
        if (string.IsNullOrEmpty(methodName)) return null;
        if (_scriptInstance != null && _scriptInstance.TryInvoke(methodName, out var result, out _, args ?? Array.Empty<object>()))
            return result;
        return null;
    }
}
