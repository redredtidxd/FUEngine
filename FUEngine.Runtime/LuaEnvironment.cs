// -----------------------------------------------------------------------------
// FUEngine (FUEngine.Runtime) — Condiciones: LICENSE.md en la raíz del repositorio.
// Copyright (c) Red Redtid. No es licencia MIT/Apache ni "open source" OSI.
// Productos comerciales: revenue share según LICENSE.md. Prohibido integrar
// publicidad de terceros en forks GitHub sin autorización escrita. Plugins
// gratuitos (no venta de extensiones). Build pública sin garantías ni servidores
// de validación del titular.
// -----------------------------------------------------------------------------
using System.Linq;
using System.Text;
using NLua;

namespace FUEngine.Runtime;

/// <summary>
/// Encapsula el estado Lua y la creación de entornos seguros (sandbox).
/// Solo expone: string, table, math, pairs, ipairs, next, type, tostring, tonumber y print (callback).
/// <c>require</c> y <c>package</c> no están en SafeGlobals: se inyectan por script en <see cref="LuaRequireSupport"/> (solo módulos bajo Scripts/).
/// No se expone: debug, coroutine, dofile, loadfile, os, io, etc.
/// </summary>
public sealed class LuaEnvironment
{
    private readonly Lua _lua;
    private readonly LuaTable _safeGlobals;
    private readonly Action<string>? _printCallback;
    private readonly bool _exposeDebugTraceback;
    private bool _disposed;

    /// <param name="printCallback">Si se asigna, print() en Lua redirige aquí (ej: EditorLog.Info).</param>
    /// <param name="exposeDebugTraceback">Si es true, el entorno seguro expone solo <c>debug.traceback</c> (resto del módulo debug no).</param>
    public LuaEnvironment(Action<string>? printCallback = null, bool exposeDebugTraceback = true)
    {
        _printCallback = printCallback;
        _exposeDebugTraceback = exposeDebugTraceback;
        _lua = new Lua();
        _lua.State.Encoding = Encoding.UTF8;
        _safeGlobals = CreateSafeGlobals();
        if (_exposeDebugTraceback)
            TryInjectDebugTraceback();
    }

    public Lua State => _lua;

    /// <summary>Tabla con solo string, table, math, pairs, ipairs, next, type, tostring, tonumber y print. Nada más.</summary>
    public LuaTable SafeGlobals => _safeGlobals;

    /// <summary>Crea una tabla vacía con metatable __index = SafeGlobals (entorno aislado por instancia).</summary>
    public LuaTable CreateInstanceEnvironment()
    {
        _lua["__safe"] = _safeGlobals;
        var result = _lua.DoString("return setmetatable({}, { __index = __safe })");
        if (result?.Length > 0 && result[0] is LuaTable tbl)
            return tbl;
        throw new InvalidOperationException("No se pudo crear el entorno de instancia Lua.");
    }

    /// <summary>
    /// Crea la tabla de globals seguros: solo string, table, math, pairs, ipairs, next, type, tostring, tonumber.
    /// No se expone debug, coroutine, dofile, loadfile, os, io ni package.loadlib; <c>require</c> lo añade <see cref="LuaRequireSupport"/>.
    /// print se inyecta desde C# como callback.
    /// </summary>
    private LuaTable CreateSafeGlobals()
    {
        var result = _lua.DoString(@"
            return setmetatable({}, {
                __index = function(_, k)
                    if k == 'string' then return string end
                    if k == 'table' then return table end
                    if k == 'math' then return math end
                    if k == 'pairs' then return pairs end
                    if k == 'ipairs' then return ipairs end
                    if k == 'next' then return next end
                    if k == 'type' then return type end
                    if k == 'tostring' then return tostring end
                    if k == 'tonumber' then return tonumber end
                    return nil
                end
            })
        ");
        if (result?.Length > 0 && result[0] is LuaTable tbl)
        {
            // print(...): soporta múltiples argumentos como en Lua (print("hp", hp, "speed", speed))
            tbl["print"] = (Action<object[]>)((args) =>
            {
                var parts = (args ?? Array.Empty<object>()).Select(a => a?.ToString() ?? "");
                _printCallback?.Invoke(string.Join(" ", parts));
            });
            return tbl;
        }
        throw new InvalidOperationException("No se pudo crear SafeGlobals.");
    }

    /// <summary>Copia <c>debug.traceback</c> del estado global al sandbox (sin <c>debug.getinfo</c> ni depurador completo).</summary>
    private void TryInjectDebugTraceback()
    {
        try
        {
            if (_lua["debug"] is not LuaTable globalDbg)
                return;
            var tracebackFn = globalDbg["traceback"];
            if (tracebackFn == null)
                return;
            var only = _lua.DoString("return {}");
            if (only?.Length > 0 && only[0] is LuaTable tbl)
            {
                tbl["traceback"] = tracebackFn;
                _safeGlobals["debug"] = tbl;
            }
        }
        catch
        {
            /* sin debug.traceback en sandbox */
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _lua?.Dispose();
        _disposed = true;
    }
}
