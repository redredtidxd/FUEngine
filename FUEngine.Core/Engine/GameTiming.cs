namespace FUEngine.Core;

/// <summary>
/// Control de FPS y delta time para animaciones y lógica consistentes.
/// </summary>
public class GameTiming
{
    public int TargetFps { get; set; } = 60;
    public double DeltaTime { get; private set; }
    public double TotalTime { get; private set; }
    private DateTime _lastUpdate = DateTime.UtcNow;

    public void Tick()
    {
        var now = DateTime.UtcNow;
        DeltaTime = (now - _lastUpdate).TotalSeconds;
        if (TargetFps > 0)
        {
            var maxDt = 1.0 / TargetFps;
            if (DeltaTime > maxDt * 2) DeltaTime = maxDt;
        }
        _lastUpdate = now;
        TotalTime += DeltaTime;
    }

    public void Reset()
    {
        _lastUpdate = DateTime.UtcNow;
        TotalTime = 0;
        DeltaTime = 0;
    }
}
