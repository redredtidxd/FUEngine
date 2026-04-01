using System;
using System.Collections.Generic;
using System.IO;
using NLua;

namespace FUEngine.Runtime;

/// <summary>
/// Expone <c>require("Modulo")</c> solo para archivos bajo <c>Scripts/</c> del proyecto (sin acceso al sistema de ficheros global).
/// </summary>
public static class LuaRequireSupport
{
    /// <summary>Inyecta <c>require</c> y <c>package.loaded</c> en el entorno del script.</summary>
    public static void InjectRequire(Lua lua, LuaTable env, ScriptLoader loader, Dictionary<string, object> moduleCache)
    {
        var created = lua.DoString("local p = {}; p.loaded = {}; return p");
        if (created is not { Length: > 0 } || created[0] is not LuaTable package)
            throw new InvalidOperationException("require: no se pudo crear package.");
        var loaded = package["loaded"] as LuaTable
            ?? throw new InvalidOperationException("require: package.loaded no es una tabla.");
        env["package"] = package;

        env["require"] = new Func<string, object>(modName =>
        {
            if (string.IsNullOrWhiteSpace(modName))
                throw new ArgumentException("require: el nombre del módulo no puede estar vacío.");

            modName = modName.Trim();
            var prev = loaded[modName];
            if (prev != null)
                return prev;

            var rel = ResolveModuleRelativePath(modName);
            if (moduleCache.TryGetValue(rel, out var cached))
            {
                loaded[modName] = cached;
                return cached;
            }

            var src = loader.LoadSource(rel);
            lua["__fe_req_src"] = src;
            lua["__fe_req_path"] = rel;
            lua["__fe_req_env"] = env;

            object[]? results;
            try
            {
                results = lua.DoString(@"
                    local fn, err = load(__fe_req_src, __fe_req_path, 't', __fe_req_env)
                    if not fn then error(err or 'load failed') end
                    local r = fn()
                    if r == nil then r = true end
                    return r
                ");
            }
            finally
            {
                lua["__fe_req_src"] = null;
                lua["__fe_req_path"] = null;
                lua["__fe_req_env"] = null;
            }

            var modResult = results is { Length: > 0 } ? results[0] : true;
            moduleCache[rel] = modResult;
            loaded[modName] = modResult;
            return modResult;
        });
    }

    /// <summary>Convierte <c>nombre</c> o <c>sub/nombre</c> en ruta relativa al proyecto: <c>Scripts/.../nombre.lua</c>.</summary>
    public static string ResolveModuleRelativePath(string modName)
    {
        var s = modName.Replace('\\', '/').Trim();
        if (s.Length == 0)
            throw new ArgumentException("require: nombre inválido.");

        if (s.Contains("..", StringComparison.Ordinal))
            throw new InvalidOperationException("require: no se permite «..» en el nombre del módulo.");

        if (Path.IsPathRooted(s))
            throw new InvalidOperationException("require: no se permiten rutas absolutas.");

        foreach (var seg in s.Split('/'))
        {
            if (seg.Length == 0) continue;
            foreach (var c in seg)
            {
                if (char.IsLetterOrDigit(c) || c is '_' or '-' or '.')
                    continue;
                throw new InvalidOperationException($"require: carácter no permitido en «{seg}».");
            }
        }

        return s.StartsWith("Scripts/", StringComparison.OrdinalIgnoreCase)
            ? s.EndsWith(".lua", StringComparison.OrdinalIgnoreCase) ? s : s + ".lua"
            : "Scripts/" + s + ".lua";
    }
}
