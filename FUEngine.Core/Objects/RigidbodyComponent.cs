namespace FUEngine.Core;

/// <summary>Física simple: velocidad integrada antes del paso AABB en Play.</summary>
public sealed class RigidbodyComponent : Component
{
    public float VelocityX { get; set; }
    public float VelocityY { get; set; }

    public float Mass { get; set; } = 1f;
    public float GravityScale { get; set; } = 1f;
    public float Drag { get; set; }
    public bool FreezeRotation { get; set; }
}
