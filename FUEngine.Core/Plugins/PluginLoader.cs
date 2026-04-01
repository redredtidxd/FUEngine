// -----------------------------------------------------------------------------
// FUEngine (FUEngine.Core) — Condiciones: LICENSE.md en la raíz del repositorio.
// Copyright (c) Red Redtid. No es licencia MIT/Apache ni "open source" OSI.
// Productos comerciales: revenue share según LICENSE.md. Prohibido integrar
// publicidad de terceros en forks GitHub sin autorización escrita. Plugins
// gratuitos (no venta de extensiones). Build pública sin garantías ni servidores
// de validación del titular.
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace FUEngine.Core;

/// <summary>Carga plugins .NET desde una carpeta usando <c>plugins-manifest.json</c> (lista blanca de ensamblados).</summary>
public static class PluginLoader
{
    private static readonly List<IPlugin> _loaded = new();

    public static IReadOnlyList<IPlugin> Loaded => _loaded;

    /// <summary>Registro opcional de mensajes (p. ej. consola del editor). No es obligatorio.</summary>
    public static Action<string>? DiagnosticLog { get; set; }

    /// <summary>
    /// Carga ensamblados listados en <c>plugins-manifest.json</c> en <paramref name="path"/>.
    /// Sin manifiesto no se carga nada (comportamiento seguro).
    /// Cada entrada puede ser una cadena <c>"MiPlugin.dll"</c> o un objeto
    /// <c>{"name":"MiPlugin.dll","version":"1.0.0"}</c> (version solo informativa en log; dependencias entre plugins no se resuelven automáticamente).
    /// </summary>
    public static void LoadFromDirectory(string? path)
    {
        UnloadAll();
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return;

        var manifestPath = Path.Combine(path, "plugins-manifest.json");
        if (!File.Exists(manifestPath))
        {
            DiagnosticLog?.Invoke("PluginLoader: no hay plugins-manifest.json en Plugins; no se carga ningún ensamblado.");
            return;
        }

        List<(string file, string? versionNote)> entries;
        try
        {
            var json = File.ReadAllText(manifestPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("assemblies", out var arr) || arr.ValueKind != JsonValueKind.Array)
            {
                DiagnosticLog?.Invoke("PluginLoader: plugins-manifest.json debe contener un array \"assemblies\".");
                return;
            }

            entries = new List<(string, string?)>();
            foreach (var el in arr.EnumerateArray())
            {
                if (el.ValueKind == JsonValueKind.String)
                {
                    var s = el.GetString();
                    if (!string.IsNullOrWhiteSpace(s))
                        entries.Add((s.Trim(), null));
                }
                else if (el.ValueKind == JsonValueKind.Object)
                {
                    string? name = null;
                    if (el.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String)
                        name = n.GetString();
                    else if (el.TryGetProperty("path", out var p) && p.ValueKind == JsonValueKind.String)
                        name = p.GetString();
                    string? ver = null;
                    if (el.TryGetProperty("version", out var v))
                    {
                        ver = v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString();
                    }
                    if (!string.IsNullOrWhiteSpace(name))
                        entries.Add((name.Trim(), ver));
                }
            }
        }
        catch (Exception ex)
        {
            DiagnosticLog?.Invoke("PluginLoader: error leyendo plugins-manifest.json: " + ex.Message);
            return;
        }

        if (entries.Count == 0)
        {
            DiagnosticLog?.Invoke("PluginLoader: plugins-manifest.json no lista ensamblados.");
            return;
        }

        foreach (var (name, versionNote) in entries)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            var file = Path.GetFileName(name.Trim().Replace('\\', '/'));
            if (string.IsNullOrEmpty(file) || file.Contains("..", StringComparison.Ordinal))
            {
                DiagnosticLog?.Invoke($"PluginLoader: nombre de ensamblado inválido: {name}");
                continue;
            }
            if (!file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                file += ".dll";
            var full = Path.GetFullPath(Path.Combine(path, file));
            if (!full.StartsWith(Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase))
            {
                DiagnosticLog?.Invoke($"PluginLoader: ruta rechazada (fuera de la carpeta): {name}");
                continue;
            }
            if (!File.Exists(full))
            {
                DiagnosticLog?.Invoke($"PluginLoader: no existe el ensamblado: {file}");
                continue;
            }

            if (!string.IsNullOrEmpty(versionNote))
                DiagnosticLog?.Invoke($"PluginLoader: cargando {file} (versión declarada en manifiesto: {versionNote}).");

            try
            {
                var asm = Assembly.LoadFrom(full);
                LoadPluginsFromAssembly(asm, full);
            }
            catch (Exception ex)
            {
                DiagnosticLog?.Invoke($"PluginLoader: error cargando {file}: {ex.Message}");
            }
        }
    }

    private static void LoadPluginsFromAssembly(Assembly asm, string pathForLog)
    {
        foreach (var t in asm.GetTypes())
        {
            if (t.IsAbstract || t.IsInterface) continue;
            if (!typeof(IPlugin).IsAssignableFrom(t)) continue;
            if (t.GetConstructor(Type.EmptyTypes) == null) continue;

            try
            {
                var inst = (IPlugin)Activator.CreateInstance(t)!;
                inst.OnLoad();
                _loaded.Add(inst);
                DiagnosticLog?.Invoke($"PluginLoader: cargado {inst.Id} ({inst.Name}) v{inst.Version} desde {Path.GetFileName(pathForLog)}.");
            }
            catch (Exception ex)
            {
                DiagnosticLog?.Invoke($"PluginLoader: error instanciando {t.FullName}: {ex.Message}");
            }
        }
    }

    public static void UnloadAll()
    {
        for (var i = _loaded.Count - 1; i >= 0; i--)
        {
            try
            {
                _loaded[i].OnUnload();
            }
            catch
            {
                /* best effort */
            }
        }
        _loaded.Clear();
    }
}
