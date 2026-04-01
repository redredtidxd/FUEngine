namespace FUEngine.Core;

/// <summary>
/// Cuerpo AABB genérico (centro + tamaño). <strong>No</strong> participa en <see cref="PhysicsWorld.StepPlayScene"/>:
/// el bucle de Play usa solo <see cref="ColliderComponent"/> en <see cref="GameObject"/>. Reservado para extensiones o herramientas.
/// </summary>
public class CollisionBody
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; } = 1f;
    public float Height { get; set; } = 1f;
    public bool IsTrigger { get; set; }
    public string? Layer { get; set; }

    /// <summary>No se integra velocidad ni colisiona como dinámico.</summary>
    public bool IsStatic { get; set; }

    /// <summary>Aplica gravedad en <see cref="PhysicsWorld.Step"/> (eje Y positivo = abajo).</summary>
    public bool UseGravity { get; set; } = true;

    public float Vx { get; set; }
    public float Vy { get; set; }
    public float Mass { get; set; } = 1f;
}
