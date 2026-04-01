using System.IO;
using FUEngine.Core;
using FUEngine.Editor;

namespace FUEngine;

/// <summary>
/// Verifica integridad del proyecto: referencias rotas (scripts, animaciones), archivos faltantes, límites.
/// </summary>
public static class ProjectIntegrityChecker
{
    public static bool Run(ProjectInfo project, TileMap tileMap, ObjectLayer objectLayer, ScriptRegistry scriptRegistry)
    {
        if (project == null || tileMap == null || objectLayer == null || scriptRegistry == null)
            return true;
        var ok = true;
        var scriptIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in scriptRegistry.GetAll())
            scriptIds.Add(s.Id);

        if (!File.Exists(project.MapPath))
        {
            EditorLog.Warning("mapa.json no encontrado.", "Integridad");
            ok = false;
        }
        if (!File.Exists(project.ObjectsPath))
        {
            EditorLog.Warning("objetos.json no encontrado.", "Integridad");
            ok = false;
        }
        if (!File.Exists(project.ScriptsPath))
            EditorLog.Warning("scripts.json no encontrado.", "Integridad");

        foreach (var inst in objectLayer.Instances)
        {
            var def = objectLayer.GetDefinition(inst.DefinitionId);
            if (def == null)
            {
                EditorLog.Error($"Instancia '{inst.Nombre}' referencia definición inexistente: {inst.DefinitionId}", "Integridad");
                ok = false;
            }
            else
            {
                var scriptId = inst.GetScriptId(def) ?? def.ScriptId;
                if (!string.IsNullOrEmpty(scriptId) && !scriptIds.Contains(scriptId))
                {
                    EditorLog.Warning($"Objeto '{inst.Nombre}' usa script inexistente: {scriptId}", "Integridad");
                    ok = false;
                }
            }
        }

        foreach (var (cx, cy) in tileMap.EnumerateChunkCoords())
        {
            var chunk = tileMap.GetChunk(cx, cy);
            if (chunk == null) continue;
            foreach (var (lx, ly, data) in chunk.EnumerateTiles())
            {
                if (data.ScriptId != null && !scriptIds.Contains(data.ScriptId))
                {
                    int wx = cx * tileMap.ChunkSize + lx, wy = cy * tileMap.ChunkSize + ly;
                    EditorLog.Warning($"Tile ({wx},{wy}) referencia script inexistente: {data.ScriptId}", "Integridad");
                    ok = false;
                }
            }
        }

        if (project.ChunkSize > 0 && tileMap.ChunkSize != project.ChunkSize)
        {
            EditorLog.Warning($"Tamaño de chunk del mapa ({tileMap.ChunkSize}) no coincide con proyecto ({project.ChunkSize}).", "Integridad");
            ok = false;
        }

        int maxChunksX = project.InitialChunksW > 0 ? project.InitialChunksW : 64;
        int maxChunksY = project.InitialChunksH > 0 ? project.InitialChunksH : 64;
        foreach (var (cx, cy) in tileMap.EnumerateChunkCoords())
        {
            if (Math.Abs(cx) > maxChunksX || Math.Abs(cy) > maxChunksY)
            {
                EditorLog.Warning($"Chunk ({cx},{cy}) fuera de rango sugerido (±{maxChunksX}, ±{maxChunksY}).", "Integridad");
            }
        }

        if (ok)
            EditorLog.Info("Verificación de integridad: sin referencias rotas.", "Integridad");
        return ok;
    }
}
