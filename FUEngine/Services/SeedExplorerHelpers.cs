using System.IO;
using System.Linq;
using System.Text.Json;
using FUEngine.Core;
using FUEngine.Editor;

namespace FUEngine;

/// <summary>Resuelve script y miniatura para archivos <c>.seed</c> en el explorador / inspector rápido.</summary>
public static class SeedExplorerHelpers
{
    public static bool TryGetFirstSeed(string seedPath, out SeedDefinition? seed)
    {
        seed = null;
        try
        {
            var list = SeedSerialization.Load(seedPath);
            if (list.Count == 0) return false;
            seed = list[0];
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Ruta absoluta al .lua del primer script asociado al seed (instancia o definición).</summary>
    public static string? TryResolveScriptPath(string seedPath, string? projectDir, ObjectLayer? layer, ScriptRegistry? reg)
    {
        if (string.IsNullOrEmpty(projectDir) || string.IsNullOrEmpty(seedPath)) return null;
        if (!TryGetFirstSeed(seedPath, out var seed) || seed == null) return null;
        ObjectInstanceDto? dto = null;
        foreach (var entry in seed.Objects ?? new List<SeedObjectEntry>())
        {
            if (string.IsNullOrWhiteSpace(entry.SerializedInstanceJson)) continue;
            try
            {
                dto = JsonSerializer.Deserialize<ObjectInstanceDto>(entry.SerializedInstanceJson, SerializationDefaults.Options);
                if (dto != null) break;
            }
            catch { /* siguiente entrada */ }
        }
        string? scriptId = null;
        if (dto != null)
        {
            if (dto.ScriptIds is { Count: > 0 })
                scriptId = dto.ScriptIds[0];
            if (string.IsNullOrWhiteSpace(scriptId) && !string.IsNullOrWhiteSpace(dto.ScriptIdOverride))
                scriptId = dto.ScriptIdOverride;
            if (string.IsNullOrWhiteSpace(scriptId) && layer?.Definitions != null &&
                layer.Definitions.TryGetValue(dto.DefinitionId, out var def))
                scriptId = def.ScriptId;
        }
        if (string.IsNullOrWhiteSpace(scriptId) || reg == null) return null;
        var sd = reg.Get(scriptId);
        if (sd == null || string.IsNullOrWhiteSpace(sd.Path)) return null;
        return Path.GetFullPath(Path.Combine(projectDir, sd.Path.Replace('/', Path.DirectorySeparatorChar)));
    }

    /// <summary>PNG para vista previa: animación (primer frame) o sprite de la definición.</summary>
    public static string? TryResolveSpritePreviewPath(string seedPath, string? projectDir, ObjectLayer? layer)
    {
        if (string.IsNullOrEmpty(projectDir) || layer?.Definitions == null) return null;
        if (!TryGetFirstSeed(seedPath, out var seed) || seed == null) return null;
        ObjectInstanceDto? dto = null;
        ObjectDefinition? def = null;
        foreach (var entry in seed.Objects ?? new List<SeedObjectEntry>())
        {
            if (string.IsNullOrWhiteSpace(entry.SerializedInstanceJson)) continue;
            try
            {
                dto = JsonSerializer.Deserialize<ObjectInstanceDto>(entry.SerializedInstanceJson, SerializationDefaults.Options);
                if (dto != null && layer.Definitions.TryGetValue(dto.DefinitionId, out var d))
                {
                    def = d;
                    break;
                }
            }
            catch { /* ignore */ }
        }
        if (dto == null || def == null) return null;

        var animPath = ProjectIndexPaths.ResolveAnimacionesJson(projectDir);
        var clipId = !string.IsNullOrWhiteSpace(dto.DefaultAnimationClipId)
            ? dto.DefaultAnimationClipId!.Trim()
            : def.AnimacionId?.Trim();
        if (!string.IsNullOrWhiteSpace(clipId) && File.Exists(animPath))
        {
            try
            {
                var anims = AnimationSerialization.Load(animPath);
                var clip = anims.FirstOrDefault(a => string.Equals(a.Id, clipId, StringComparison.OrdinalIgnoreCase));
                if (clip?.Frames is { Count: > 0 } && !string.IsNullOrWhiteSpace(clip.Frames[0]))
                {
                    var p = Path.Combine(projectDir, clip.Frames[0].Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(p)) return p;
                }
            }
            catch { /* ignore */ }
        }

        if (!string.IsNullOrWhiteSpace(def.SpritePath))
        {
            var p = Path.Combine(projectDir, def.SpritePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(p)) return p;
        }

        return null;
    }
}
