// -----------------------------------------------------------------------------
// FUEngine (FUEngine.Core) — Condiciones: LICENSE.md en la raíz del repositorio.
// Copyright (c) Red Redtid. No es licencia MIT/Apache ni "open source" OSI.
// Productos comerciales: revenue share según LICENSE.md. Prohibido integrar
// publicidad de terceros en forks GitHub sin autorización escrita. Plugins
// gratuitos (no venta de extensiones). Build pública sin garantías ni servidores
// de validación del titular.
// -----------------------------------------------------------------------------
using System.Collections.Generic;

namespace FUEngine.Core;

/// <summary>Carga plugins desde una carpeta (assemblies o scripts). Stub para futuro.</summary>
public static class PluginLoader
{
    private static readonly List<IPlugin> _loaded = new();

    public static IReadOnlyList<IPlugin> Loaded => _loaded;

    public static void LoadFromDirectory(string path) { }
    public static void UnloadAll()
    {
        _loaded.Clear();
    }
}
