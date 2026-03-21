namespace FUEngine.Core;

/// <summary>
/// Eventos estándar del motor para scripts (tiles, objetos, animatrónicos).
/// Usado por el editor para sugerir eventos y por el runtime para enganches.
/// </summary>
public static class KnownEvents
{
    /// <summary>Llamado una vez al cargar la instancia del script (tras el chunk del archivo), antes de <see cref="OnStart"/>.</summary>
    public const string OnAwake = "onAwake";
    /// <summary>Llamado una vez antes del primer <see cref="OnUpdate"/>.</summary>
    public const string OnStart = "onStart";
    /// <summary>Interacción del jugador (puertas, cofres, interruptores).</summary>
    public const string OnInteract = "onInteract";
    /// <summary>Colisión (jugador/objeto, enemigo, pickup).</summary>
    public const string OnCollision = "onCollision";
    /// <summary>Miedo (cercanía animatrónico, mirada).</summary>
    public const string OnFear = "onFear";
    /// <summary>Aparición o spawn (enemigo, objeto).</summary>
    public const string OnSpawn = "onSpawn";
    /// <summary>Destrucción de objeto o entidad.</summary>
    public const string OnDestroy = "onDestroy";
    /// <summary>Reparación / minijuego de reparación.</summary>
    public const string OnRepair = "onRepair";
    /// <summary>Hackeo / reprogramación (animatrónicos, paneles).</summary>
    public const string OnHack = "onHack";
    /// <summary>Actualización cada frame (IA, patrulla, triggers).</summary>
    public const string OnUpdate = "onUpdate";
    /// <summary>Actualización tras onUpdate de todos los objetos.</summary>
    public const string OnLateUpdate = "onLateUpdate";
    /// <summary>Entró en un trigger/colisión tipo trigger.</summary>
    public const string OnTriggerEnter = "onTriggerEnter";
    /// <summary>Salió de un trigger.</summary>
    public const string OnTriggerExit = "onTriggerExit";
    /// <summary>Trigger genérico (al pasar, al activar).</summary>
    public const string OnTrigger = "onTrigger";
    /// <summary>Inicio de ciclo día (día/noche, turnos).</summary>
    public const string OnDayStart = "onDayStart";
    /// <summary>Inicio de ciclo noche (día/noche, turnos).</summary>
    public const string OnNightStart = "onNightStart";
    /// <summary>Jugador se movió (triggers globales, minimapa, guardado).</summary>
    public const string OnPlayerMove = "onPlayerMove";
    /// <summary>Jugador entró en una zona (triggers por área).</summary>
    public const string OnZoneEnter = "onZoneEnter";
    /// <summary>Jugador salió de una zona (triggers por área).</summary>
    public const string OnZoneExit = "onZoneExit";
    /// <summary>Se añadió un hijo a la jerarquía.</summary>
    public const string OnChildAdded = "onChildAdded";
    /// <summary>Se quitó un hijo de la jerarquía.</summary>
    public const string OnChildRemoved = "onChildRemoved";
    /// <summary>Cambió el padre de este objeto.</summary>
    public const string OnParentChanged = "onParentChanged";

    /// <summary>Todos los IDs de eventos conocidos para el editor y módulos.</summary>
    public static readonly string[] All = new[]
    {
        OnAwake,
        OnStart,
        OnInteract,
        OnCollision,
        OnFear,
        OnSpawn,
        OnDestroy,
        OnRepair,
        OnHack,
        OnUpdate,
        OnLateUpdate,
        OnTriggerEnter,
        OnTriggerExit,
        OnTrigger,
        OnDayStart,
        OnNightStart,
        OnPlayerMove,
        OnZoneEnter,
        OnZoneExit,
        OnChildAdded,
        OnChildRemoved,
        OnParentChanged
    };

    /// <summary>Eventos globales del mundo (día/noche, jugador).</summary>
    public static readonly string[] WorldEvents = new[] { OnDayStart, OnNightStart, OnPlayerMove, OnZoneEnter, OnZoneExit };

    /// <summary>True si el nombre es un hook/evento reservado (no se muestra como variable editable en runtime).</summary>
    public static bool IsReservedScriptVariableName(string? name)
    {
        if (string.IsNullOrEmpty(name)) return true;
        if (name.StartsWith("__", StringComparison.Ordinal)) return true;
        foreach (var ev in All)
        {
            if (string.Equals(ev, name, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
