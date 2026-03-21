using System;
using System.Collections.Generic;
using FUEngine.Core;
using NLua;

namespace FUEngine.Runtime;

/// <summary>
/// Una instancia de script Lua asociada a una entidad. Tiene su propio entorno (tabla) para no interferir con otros scripts.
/// Al recargar el script se debe llamar Dispose() antes de quitar de la lista para evitar leaks (LuaTable/LuaFunction).
/// </summary>
public sealed class ScriptInstance : IDisposable
{
    private readonly Lua _lua;
    private LuaTable? _env;
    private readonly string _scriptPath;
    private readonly string _scriptId;
    private readonly GameObject? _gameObject;
    private bool _disposed;

    public ScriptInstance(Lua lua, LuaTable env, string scriptPath, string scriptId, GameObject? gameObject = null)
    {
        _lua = lua ?? throw new ArgumentNullException(nameof(lua));
        _env = env ?? throw new ArgumentNullException(nameof(env));
        _scriptPath = scriptPath ?? "";
        _scriptId = scriptId ?? "";
        _gameObject = gameObject;
    }

    public string ScriptPath => _scriptPath;
    public string ScriptId => _scriptId;
    public LuaTable? Environment => _env;

    /// <summary>Tras <see cref="KnownEvents.OnAwake"/> (o creación sin onAwake).</summary>
    public bool LifecycleAwakeDone { get; internal set; }
    /// <summary>Tras <see cref="KnownEvents.OnStart"/> (o primer frame sin onStart).</summary>
    public bool LifecycleStartDone { get; internal set; }
    /// <summary>Objeto asociado a esta instancia (para notificar eventos de jerarquía).</summary>
    public GameObject? GameObjectRef => _gameObject;

    /// <summary>Obtiene una variable global del script (para Inspector y motor).</summary>
    public object? Get(string name)
    {
        if (_disposed || _env == null || string.IsNullOrEmpty(name)) return null;
        try
        {
            var v = _env[name];
            return v;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Establece una variable global del script (desde Inspector o motor).</summary>
    public void Set(string name, object? value)
    {
        if (_disposed || _env == null || string.IsNullOrEmpty(name)) return;
        try
        {
            _env[name] = value;
        }
        catch
        {
            // ignorar
        }
    }

    /// <summary>Comprueba si el script define la función con el nombre dado.</summary>
    public bool HasFunction(string name)
    {
        if (_disposed || _env == null || string.IsNullOrEmpty(name)) return false;
        try
        {
            var v = _env[name];
            return v is LuaFunction;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Invoca una función del script si existe. Devuelve el primer valor de retorno o null.</summary>
    public object? Invoke(string functionName, params object[] args)
    {
        if (_disposed || _env == null || !HasFunction(functionName)) return null;
        try
        {
            var fn = _env[functionName] as LuaFunction;
            if (fn == null) return null;
            var result = fn.Call(args);
            return result?.Length > 0 ? result[0] : null;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error en script {_scriptPath} en función {functionName}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Wrapper seguro: invoca la función solo si existe; no lanza. Devuelve true si se llamó correctamente.
    /// Si falla, errorMessage contiene el mensaje para reportar a consola.
    /// </summary>
    public bool TryInvoke(string functionName, out object? result, out string? errorMessage, params object[] args)
    {
        result = null;
        errorMessage = null;
        if (_disposed || _env == null || string.IsNullOrEmpty(functionName) || !HasFunction(functionName))
            return false;
        try
        {
            var fn = _env[functionName] as LuaFunction;
            if (fn == null) return false;
            var res = fn.Call(args);
            result = res?.Length > 0 ? res[0] : null;
            return true;
        }
        catch (Exception ex)
        {
            // Incluir stack trace completo para debugging (NLua suele incluir traceback en ToString)
            errorMessage = ex.ToString();
            return false;
        }
    }

    /// <summary>Inyecta propiedades clave-valor desde el editor (ScriptProperties).</summary>
    public void InjectProperties(IEnumerable<KeyValuePair<string, object?>> properties)
    {
        if (_disposed || properties == null) return;
        foreach (var kv in properties)
            Set(kv.Key, kv.Value);
    }

    /// <summary>Snapshot de variables del script para el panel Debug (excluye funciones).</summary>
    public IReadOnlyDictionary<string, string>? GetVariableSnapshot()
    {
        if (_disposed || _env == null) return null;
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var key in _env.Keys)
            {
                if (key == null) continue;
                var keyStr = key.ToString();
                if (string.IsNullOrEmpty(keyStr) || keyStr.StartsWith("__", StringComparison.Ordinal)) continue;
                try
                {
                    var v = _env[key];
                    if (v is LuaFunction) continue;
                    dict[keyStr] = v != null ? v.ToString() ?? "nil" : "nil";
                }
                catch
                {
                    dict[keyStr] = "?";
                }
            }
        }
        catch { /* ignorar si la tabla no es iterable */ }
        return dict;
    }

    /// <summary>Marca la instancia como liberada y libera referencias Lua (LuaTable) para reducir leaks.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            (_env as IDisposable)?.Dispose();
        }
        catch
        {
            // ignorar si NLua no expone Dispose en LuaTable
        }
        _env = null;
    }
}
