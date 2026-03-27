using System.Collections.Generic;
using System.IO;
using FUEngine.Core;
using FUEngine.Editor;
using FUEngine.Runtime;

namespace FUEngine;

/// <summary>
/// Añade entradas a scripts.json para archivos .lua nuevos (id estable tipo Guid, path relativo al proyecto).
/// </summary>
public static class ScriptRegistryProjectWriter
{
    private static string GetScriptsJsonPath(string projectDirectory)
    {
        var root = Path.GetFullPath(projectDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return ProjectIndexPaths.ResolveScriptsJson(root);
    }

    /// <summary>
    /// Registra el .lua en scripts.json si aún no hay una entrada con la misma ruta relativa.
    /// </summary>
    /// <returns>false si no se pudo leer/escribir el JSON o la ruta no pertenece al proyecto.</returns>
    public static bool TryRegisterLuaFile(
        string projectDirectory,
        string luaAbsolutePath,
        out string scriptId,
        out string relativePathForJson,
        out string? errorMessage)
    {
        scriptId = "";
        relativePathForJson = "";
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(projectDirectory))
        {
            errorMessage = "Directorio de proyecto vacío.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(luaAbsolutePath) || !File.Exists(luaAbsolutePath))
        {
            errorMessage = "El archivo .lua no existe.";
            return false;
        }

        var root = Path.GetFullPath(projectDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var luaFull = Path.GetFullPath(luaAbsolutePath);
        var rel = Path.GetRelativePath(root, luaFull);
        if (SegmentsContainParentNav(rel))
        {
            errorMessage = "El script no está dentro del directorio del proyecto.";
            return false;
        }

        relativePathForJson = ScriptLoader.NormalizeRelativePath(rel.Replace('\\', '/'));

        var scriptsPath = GetScriptsJsonPath(projectDirectory);
        ScriptRegistry registry;
        try
        {
            registry = ScriptSerialization.Load(scriptsPath);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }

        foreach (var s in registry.GetAll())
        {
            var p = ScriptLoader.NormalizeRelativePath(s.Path);
            if (string.Equals(p, relativePathForJson, StringComparison.OrdinalIgnoreCase))
            {
                scriptId = s.Id;
                return true;
            }
        }

        scriptId = Guid.NewGuid().ToString("N");
        var nombre = Path.GetFileNameWithoutExtension(luaFull);
        registry.Register(new ScriptDefinition
        {
            Id = scriptId,
            Nombre = nombre,
            Path = relativePathForJson,
            Eventos = Array.Empty<string>()
        });

        try
        {
            ScriptSerialization.Save(registry, scriptsPath);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            scriptId = "";
            return false;
        }

        return true;
    }

    /// <summary>Elimina la entrada cuyo <see cref="ScriptDefinition.Path"/> coincide con el archivo borrado.</summary>
    public static bool TryRemoveByAbsolutePath(string projectDirectory, string deletedLuaAbsolutePath, out string? removedScriptId, out string? errorMessage)
    {
        removedScriptId = null;
        errorMessage = null;
        if (!TryGetNormalizedRelative(projectDirectory, deletedLuaAbsolutePath, out var relLua, out errorMessage))
            return false;
        if (!relLua.EndsWith(".lua", StringComparison.OrdinalIgnoreCase))
            return true;

        var scriptsPath = GetScriptsJsonPath(projectDirectory);
        ScriptRegistry registry;
        try
        {
            registry = ScriptSerialization.Load(scriptsPath);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }

        ScriptDefinition? found = null;
        foreach (var s in registry.GetAll())
        {
            var p = ScriptLoader.NormalizeRelativePath(s.Path);
            if (string.Equals(p, relLua, StringComparison.OrdinalIgnoreCase))
            {
                found = s;
                break;
            }
        }

        if (found == null) return true;

        removedScriptId = found.Id;
        registry.Unregister(found.Id);
        try
        {
            ScriptSerialization.Save(registry, scriptsPath);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }

        return true;
    }

    /// <summary>Actualiza <see cref="ScriptDefinition.Path"/> y nombre al renombrar un .lua.</summary>
    public static bool TryRenameAbsolutePaths(string projectDirectory, string oldLuaAbsolutePath, string newLuaAbsolutePath, out string? errorMessage)
    {
        errorMessage = null;
        if (!TryGetNormalizedRelative(projectDirectory, oldLuaAbsolutePath, out var oldRel, out errorMessage))
            return false;
        if (!TryGetNormalizedRelative(projectDirectory, newLuaAbsolutePath, out var newRel, out errorMessage))
            return false;

        var scriptsPath = GetScriptsJsonPath(projectDirectory);
        ScriptRegistry registry;
        try
        {
            registry = ScriptSerialization.Load(scriptsPath);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }

        ScriptDefinition? found = null;
        foreach (var s in registry.GetAll())
        {
            var p = ScriptLoader.NormalizeRelativePath(s.Path);
            if (string.Equals(p, oldRel, StringComparison.OrdinalIgnoreCase))
            {
                found = s;
                break;
            }
        }

        if (found == null) return true;

        found.Path = newRel;
        found.Nombre = Path.GetFileNameWithoutExtension(newRel.Replace('/', Path.DirectorySeparatorChar));
        try
        {
            ScriptSerialization.Save(registry, scriptsPath);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }

        return true;
    }

    /// <summary>Al renombrar una carpeta dentro del proyecto, reescribe rutas de scripts bajo esa carpeta.</summary>
    public static bool TryRenameFolderInRegistry(string projectDirectory, string oldFolderAbsolutePath, string newFolderAbsolutePath, out string? errorMessage)
    {
        errorMessage = null;
        var root = Path.GetFullPath(projectDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var oldFull = Path.GetFullPath(oldFolderAbsolutePath);
        var newFull = Path.GetFullPath(newFolderAbsolutePath);
        var oldRel = ScriptLoader.NormalizeRelativePath(Path.GetRelativePath(root, oldFull).Replace('\\', '/'));
        var newRel = ScriptLoader.NormalizeRelativePath(Path.GetRelativePath(root, newFull).Replace('\\', '/'));
        if (SegmentsContainParentNav(Path.GetRelativePath(root, oldFull)) || SegmentsContainParentNav(Path.GetRelativePath(root, newFull)))
        {
            errorMessage = "La carpeta no está dentro del proyecto.";
            return false;
        }

        var scriptsPath = GetScriptsJsonPath(projectDirectory);
        ScriptRegistry registry;
        try
        {
            registry = ScriptSerialization.Load(scriptsPath);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }

        var prefix = oldRel.TrimEnd('/');
        var replacement = newRel.TrimEnd('/');
        var changed = false;
        foreach (var s in registry.GetAll())
        {
            var p = ScriptLoader.NormalizeRelativePath(s.Path);
            if (p.Length == 0) continue;
            if (string.Equals(p, prefix, StringComparison.OrdinalIgnoreCase))
            {
                s.Path = replacement;
                changed = true;
            }
            else if (p.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase))
            {
                s.Path = replacement + p.Substring(prefix.Length);
                changed = true;
            }
        }

        if (!changed) return true;
        try
        {
            ScriptSerialization.Save(registry, scriptsPath);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }

        return true;
    }

    /// <summary>Elimina entradas de scripts cuyo path está bajo la carpeta eliminada.</summary>
    public static bool TryRemoveScriptsUnderFolder(string projectDirectory, string deletedFolderAbsolutePath, out int removedCount, out string? errorMessage)
    {
        removedCount = 0;
        errorMessage = null;
        var root = Path.GetFullPath(projectDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var folderFull = Path.GetFullPath(deletedFolderAbsolutePath);
        var rel = ScriptLoader.NormalizeRelativePath(Path.GetRelativePath(root, folderFull).Replace('\\', '/'));
        if (SegmentsContainParentNav(Path.GetRelativePath(root, folderFull)))
        {
            errorMessage = "La carpeta no está dentro del proyecto.";
            return false;
        }

        var prefix = rel.TrimEnd('/');
        var scriptsPath = GetScriptsJsonPath(projectDirectory);
        ScriptRegistry registry;
        try
        {
            registry = ScriptSerialization.Load(scriptsPath);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }

        var toRemove = new List<string>();
        foreach (var s in registry.GetAll())
        {
            var p = ScriptLoader.NormalizeRelativePath(s.Path);
            if (p.Length == 0) continue;
            if (string.Equals(p, prefix, StringComparison.OrdinalIgnoreCase) ||
                p.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase))
                toRemove.Add(s.Id);
        }

        foreach (var id in toRemove)
        {
            if (registry.Unregister(id)) removedCount++;
        }

        if (toRemove.Count == 0) return true;
        try
        {
            ScriptSerialization.Save(registry, scriptsPath);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }

        return true;
    }

    private static bool TryGetNormalizedRelative(string projectDirectory, string absolutePath, out string relativePathJson, out string? errorMessage)
    {
        relativePathJson = "";
        errorMessage = null;
        if (string.IsNullOrWhiteSpace(projectDirectory))
        {
            errorMessage = "Directorio de proyecto vacío.";
            return false;
        }

        var root = Path.GetFullPath(projectDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var full = Path.GetFullPath(absolutePath);
        var rel = Path.GetRelativePath(root, full);
        if (SegmentsContainParentNav(rel))
        {
            errorMessage = "La ruta no está dentro del proyecto.";
            return false;
        }

        relativePathJson = ScriptLoader.NormalizeRelativePath(rel.Replace('\\', '/'));
        return true;
    }

    private static bool SegmentsContainParentNav(string relativePath)
    {
        foreach (var seg in relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (seg == "..")
                return true;
        }

        return false;
    }
}

/// <summary>Argumentos de <see cref="ProjectExplorerPanel.LuaScriptRegistered"/>.</summary>
public sealed class ScriptRegisteredEventArgs : EventArgs
{
    public ScriptRegisteredEventArgs(string scriptId, string relativePath)
    {
        ScriptId = scriptId;
        RelativePath = relativePath;
    }

    public string ScriptId { get; }
    public string RelativePath { get; }
}
