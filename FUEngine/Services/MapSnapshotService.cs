using System.IO;
using FUEngine.Core;
using FUEngine.Editor;

namespace FUEngine;

/// <summary>
/// Guarda y carga snapshots temporales del mapa/objetos para testing sin sobreescribir el proyecto.
/// </summary>
public static class MapSnapshotService
{
    private static string SnapshotsDir(string projectDir) =>
        Path.Combine(projectDir ?? "", "snapshots");

    public static string SaveSnapshot(string projectDir, TileMap map, ObjectLayer objects)
    {
        var dir = SnapshotsDir(projectDir);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        var name = $"snap_{DateTime.Now:yyyyMMdd_HHmmss}";
        var mapPath = Path.Combine(dir, $"{name}_mapa.json");
        var objPath = Path.Combine(dir, $"{name}_objetos.json");
        MapSerialization.Save(map, mapPath);
        ObjectsSerialization.Save(objects, objPath);
        EditorLog.Info($"Snapshot guardado: {name}", "Snapshot");
        return name;
    }

    public static bool LoadSnapshot(string projectDir, string snapshotName, out TileMap? map, out ObjectLayer? objects)
    {
        map = null;
        objects = null;
        var dir = SnapshotsDir(projectDir);
        var mapPath = Path.Combine(dir, $"{snapshotName}_mapa.json");
        var objPath = Path.Combine(dir, $"{snapshotName}_objetos.json");
        if (!File.Exists(mapPath) || !File.Exists(objPath)) return false;
        try
        {
            map = MapSerialization.Load(mapPath);
            objects = ObjectsSerialization.Load(objPath);
            EditorLog.Info($"Snapshot cargado: {snapshotName}", "Snapshot");
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static List<string> ListSnapshots(string projectDir)
    {
        var dir = SnapshotsDir(projectDir);
        if (!Directory.Exists(dir)) return new List<string>();
        var names = new HashSet<string>();
        foreach (var f in Directory.GetFiles(dir, "*_mapa.json"))
        {
            var n = Path.GetFileNameWithoutExtension(f);
            if (n.EndsWith("_mapa")) names.Add(n[..^5]);
        }
        return names.OrderDescending().ToList();
    }
}
