namespace FUEngine.Core;

/// <summary>
/// Zona del mapa que ejecuta scripts al entrar o salir el jugador (triggers por área).
/// </summary>
public class TriggerZone
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Nombre { get; set; } = "";
    /// <summary>Descripción del trigger.</summary>
    public string? Descripcion { get; set; }
    /// <summary>Tipo: OnEnter, OnExit, Temporal, Persistent.</summary>
    public string TriggerType { get; set; } = "OnEnter";
    /// <summary>Índice de capa para filtros y orden.</summary>
    public int LayerId { get; set; }
    /// <summary>X en tiles (esquina superior izquierda).</summary>
    public int X { get; set; }
    /// <summary>Y en tiles (esquina superior izquierda).</summary>
    public int Y { get; set; }
    public int Width { get; set; } = 1;
    public int Height { get; set; } = 1;
    /// <summary>Script a ejecutar al entrar en la zona.</summary>
    public string? ScriptIdOnEnter { get; set; }
    /// <summary>Script a ejecutar al salir de la zona.</summary>
    public string? ScriptIdOnExit { get; set; }
    /// <summary>Script a ejecutar cada tick (acciones continuas).</summary>
    public string? ScriptIdOnTick { get; set; }
    public List<string> Tags { get; set; } = new();

    public bool Contains(int tx, int ty)
    {
        return tx >= X && tx < X + Width && ty >= Y && ty < Y + Height;
    }
}
