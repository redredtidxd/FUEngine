namespace FUEngine.Core;

/// <summary>Sensor por distancia (casillas): dispara onTriggerEnter/Exit hacia el objetivo con la etiqueta dada.</summary>
public sealed class ProximitySensorComponent : Component
{
    public float DetectionRangeTiles { get; set; } = 1f;
    public string TargetTag { get; set; } = "player";

    /// <summary>Estado interno (no serializar en editor).</summary>
    public bool WasInside { get; set; }
}
