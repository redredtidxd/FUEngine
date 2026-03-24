namespace FUEngine.Core;

/// <summary>Datos de emisor de partículas (persistencia; el visor puede ampliarse).</summary>
public sealed class ParticleEmitterComponent : Component
{
    public string? ParticleTexturePath { get; set; }
    public float EmissionRate { get; set; } = 10f;
    public float LifeTime { get; set; } = 1f;
    public float GravityScale { get; set; }
}
