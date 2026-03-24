namespace FUEngine.Core;

public sealed class HealthComponent : Component
{
    public float MaxHealth { get; set; } = 100f;
    public float CurrentHealth { get; set; } = 100f;
    public bool IsInvulnerable { get; set; }
}
