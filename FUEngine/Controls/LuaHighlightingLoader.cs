using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace FUEngine;

/// <summary>Carga <c>Lua.xshd</c> (embebido o disco), lo registra en <see cref="HighlightingManager"/> como <c>LuaFUE</c> y comparte la definición con el editor.</summary>
public static class LuaHighlightingLoader
{
    /// <summary>Nombre único en <see cref="HighlightingManager"/> (no chocar con el Lua integrado de AvalonEdit).</summary>
    public const string RegisteredName = "LuaFUE";

    private static readonly object RegisterLock = new();
    private static bool _registeredOk;
    private static bool _registerAttempted;
    private static bool _warnedMissing;
    private static bool _loggedSuccess;

    /// <summary>Idempotente: registra el esquema una sola vez al arrancar o al primer uso.</summary>
    public static void EnsureRegistered()
    {
        lock (RegisterLock)
        {
            if (_registeredOk || _registerAttempted) return;
            _registerAttempted = true;

            var def = LoadDefinitionFromSources(out string? resourceUsed);
            if (def == null)
            {
                WarnMissingOnce("No se encontró Lua.xshd (embebido ni en Resources/).");
                return;
            }

            try
            {
                HighlightingManager.Instance.RegisterHighlighting(RegisteredName, new[] { ".lua", ".script" }, def);
                _registeredOk = true;
#if DEBUG
                Debug.WriteLine($"[LuaHighlightingLoader] Registrado '{RegisteredName}' (recurso: {resourceUsed ?? "disco"}).");
#endif
                if (!_loggedSuccess)
                {
                    _loggedSuccess = true;
                    try
                    {
                        EditorLog.Info(
                            $"Resaltado Lua: esquema '{RegisteredName}' registrado" + (resourceUsed != null ? $" ({resourceUsed})." : "."),
                            "Lua");
                    }
                    catch { /* sin IEditorLog en tests */ }
                }
            }
            catch (Exception ex)
            {
                WarnMissingOnce("No se pudo registrar Lua en HighlightingManager: " + ex.Message);
            }
        }
    }

    /// <summary>Definición registrada como <see cref="RegisteredName"/>; <c>null</c> si no hubo Lua.xshd.</summary>
    public static IHighlightingDefinition? LoadDefinition()
    {
        EnsureRegistered();
        return HighlightingManager.Instance.GetDefinition(RegisteredName);
    }

    private static IHighlightingDefinition? LoadDefinitionFromSources(out string? resourceUsed)
    {
        resourceUsed = null;
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            var expected = asm.GetName().Name + ".Resources.Lua.xshd";
            Stream? stream = asm.GetManifestResourceStream(expected);
            if (stream != null)
                resourceUsed = expected;
            if (stream == null)
            {
                var match = asm.GetManifestResourceNames()
                    .FirstOrDefault(n =>
                        n.Contains("Lua", StringComparison.OrdinalIgnoreCase)
                        && n.EndsWith(".xshd", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(match))
                {
                    stream = asm.GetManifestResourceStream(match);
                    if (stream != null)
                        resourceUsed = match;
                }
            }

            if (stream == null)
            {
                var dir = Path.GetDirectoryName(asm.Location);
                if (!string.IsNullOrEmpty(dir))
                {
                    var disk = Path.Combine(dir, "Resources", "Lua.xshd");
                    if (File.Exists(disk)) stream = File.OpenRead(disk);
                }
            }

            if (stream == null)
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var disk2 = Path.Combine(baseDir ?? "", "Resources", "Lua.xshd");
                if (File.Exists(disk2)) stream = File.OpenRead(disk2);
            }

            if (stream == null)
            {
#if DEBUG
                Debug.WriteLine("[LuaHighlightingLoader] GetManifestResourceStream(null). Recursos: "
                    + string.Join(", ", asm.GetManifestResourceNames()));
#endif
                return null;
            }

            using (stream)
            using (var reader = XmlReader.Create(stream))
                return HighlightingLoader.Load(reader, HighlightingManager.Instance);
        }
        catch (Exception ex)
        {
            WarnMissingOnce("Error al cargar Lua.xshd: " + ex.Message);
            return null;
        }
    }

    private static void WarnMissingOnce(string message)
    {
        if (_warnedMissing) return;
        _warnedMissing = true;
        try { EditorLog.Warning(message, "Lua"); }
        catch { /* ignore */ }
#if DEBUG
        Debug.WriteLine("[LuaHighlightingLoader] " + message);
#endif
    }
}
