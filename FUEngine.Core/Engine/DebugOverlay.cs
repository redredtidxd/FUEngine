namespace FUEngine.Core;

/// <summary>Datos para overlay de debug: FPS, memoria, chunks cargados, entidades. El editor/runtime lo muestran.</summary>
public class DebugOverlay
{
    public int Fps { get; set; }
    public long MemoryBytes { get; set; }
    public int ChunksLoaded { get; set; }
    public int EntityCount { get; set; }

    public void Update(int fps, int chunks, int entities)
    {
        Fps = fps;
        ChunksLoaded = chunks;
        EntityCount = entities;
        MemoryBytes = System.GC.GetTotalMemory(false);
    }
}
