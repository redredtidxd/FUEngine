namespace FUEngine.Core;

/// <summary>
/// Instancia de un objeto colocado en el mapa (posición, rotación, referencia a definición).
/// </summary>
public class ObjectInstance
{
    public string InstanceId { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>
    /// Id de ObjectDefinition.
    /// </summary>
    public string DefinitionId { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    /// <summary>
    /// Rotación en grados (0-360).
    /// </summary>
    public double Rotation { get; set; }
    /// <summary>Escala en X (1 = 100%).</summary>
    public double ScaleX { get; set; } = 1.0;
    /// <summary>Escala en Y (1 = 100%).</summary>
    public double ScaleY { get; set; } = 1.0;
    /// <summary>Orden de render (mayor = delante).</summary>
    public int LayerOrder { get; set; }
    public string Nombre { get; set; } = "";
    /// <summary>
    /// Override de colisión (null = usar definición).
    /// </summary>
    public bool? ColisionOverride { get; set; }
    /// <summary>Tipo de colisión: "Solid", "Trigger", "Surface".</summary>
    public string? CollisionType { get; set; }
    public bool? InteractivoOverride { get; set; }
    public bool? DestructibleOverride { get; set; }
    public string? ScriptIdOverride { get; set; }
    /// <summary>Lista de scripts asignados (múltiples). Si vacío, se usa ScriptIdOverride.</summary>
    public List<string> ScriptIds { get; set; } = new();
    /// <summary>Propiedades públicas por script (clave-valor por ScriptId). Se guardan con el proyecto.</summary>
    public List<ScriptInstancePropertySet> ScriptProperties { get; set; } = new();
    /// <summary>Si el objeto viene de un seed del proyecto, id lógico del seed (seeds.json / .seed).</summary>
    public string? SourceSeedId { get; set; }

    /// <summary>Ruta relativa al proyecto del archivo .seed (si se colocó desde un archivo).</summary>
    public string? SourceSeedRelativePath { get; set; }

    /// <summary>Etiquetas para filtros y búsqueda.</summary>
    public List<string> Tags { get; set; } = new();
    /// <summary>Si false, el objeto no tiene representación visual en el mapa (objeto invisible con script en coordenada específica). En el editor se muestra un marcador pequeño.</summary>
    public bool Visible { get; set; } = true;

    /// <summary>Si false, el objeto no ejecuta scripts en Play (<see cref="GameObject.RuntimeActive"/>). Distinto de <see cref="Visible"/> (apariencia en mapa).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Punto de anclaje del sprite (p. ej. Center, Feet). Reservado para render; vacío = comportamiento por defecto del motor.</summary>
    public string? Pivot { get; set; }

    /// <summary>Si true, la instancia lleva una luz puntual en Play (<see cref="LightComponent"/>).</summary>
    public bool PointLightEnabled { get; set; }

    /// <summary>Radio de la luz en unidades de mundo (tiles aprox. o píxeles según el visor).</summary>
    public float PointLightRadius { get; set; } = 5f;

    public float PointLightIntensity { get; set; } = 1f;

    /// <summary>Color #RRGGBB.</summary>
    public string PointLightColorHex { get; set; } = "#ffffff";

    public string SpriteColorTintHex { get; set; } = "#ffffff";
    public bool SpriteFlipX { get; set; }
    public bool SpriteFlipY { get; set; }
    public int SpriteSortOffset { get; set; }

    public string? DefaultAnimationClipId { get; set; }
    public bool AnimationAutoPlay { get; set; } = true;
    public float AnimationSpeedMultiplier { get; set; } = 1f;

    public bool ParticleEmitterEnabled { get; set; }
    public string? ParticleTexturePath { get; set; }
    public float ParticleEmissionRate { get; set; } = 10f;
    public float ParticleLifeTime { get; set; } = 1f;
    public float ParticleGravityScale { get; set; }

    public string ColliderShape { get; set; } = "Box";
    public float ColliderBoxWidthTiles { get; set; }
    public float ColliderBoxHeightTiles { get; set; }
    public float ColliderCircleRadiusTiles { get; set; } = 0.5f;
    public float ColliderOffsetX { get; set; }
    public float ColliderOffsetY { get; set; }

    public bool RigidbodyEnabled { get; set; }
    public float RigidbodyMass { get; set; } = 1f;
    public float RigidbodyGravityScale { get; set; } = 1f;
    public float RigidbodyDrag { get; set; }
    public bool RigidbodyFreezeRotation { get; set; }

    public bool CameraTargetEnabled { get; set; }

    public bool AudioSourceEnabled { get; set; }
    public string? AudioClipId { get; set; }
    public float AudioVolume { get; set; } = 1f;
    public float AudioPitch { get; set; } = 1f;
    public bool AudioLoop { get; set; }
    public float AudioSpatialBlend { get; set; } = 1f;

    public bool ProximitySensorEnabled { get; set; }
    public float ProximityDetectionRangeTiles { get; set; } = 1f;
    public string ProximityTargetTag { get; set; } = "player";

    public bool HealthEnabled { get; set; }
    public float HealthMax { get; set; } = 100f;
    public float HealthCurrent { get; set; } = 100f;
    public bool HealthInvulnerable { get; set; }

    /// <summary>Área clicable en mundo (Play); ver <see cref="ClickInteractableComponent"/>.</summary>
    public bool ClickInteractableEnabled { get; set; }

    /// <summary>Si false, el área no recibe puntero (puerta bloqueada, etc.).</summary>
    public bool ClickInteractableInteractEnabled { get; set; } = true;

    public string ClickInteractableShape { get; set; } = "Box";
    public float ClickInteractableBoxWidthTiles { get; set; } = 1f;
    public float ClickInteractableBoxHeightTiles { get; set; } = 1f;
    public float ClickInteractableCircleRadiusTiles { get; set; } = 0.5f;
    public float ClickInteractableOffsetXTiles { get; set; }
    public float ClickInteractableOffsetYTiles { get; set; }
    public bool ClickInteractableHoverEffect { get; set; }
    /// <summary>Mouse, Touch o Both (JSON camelCase: clickInteractableInputFilter).</summary>
    public string ClickInteractableInputFilter { get; set; } = "Both";
    public float ClickInteractableMaxDistanceFromPlayerTiles { get; set; }

    /// <summary>Orden de prioridad del rayo de clic (mayor = encima).</summary>
    public int ClickInteractZPriority { get; set; }

    /// <summary>Bloquear clic si un tile con colisión corta la línea protagonista → punto.</summary>
    public bool ClickInteractableRequireLineOfSight { get; set; }

    /// <summary>Escala temporal al pulsar (1 = sin efecto; p. ej. 0,94).</summary>
    public float ClickInteractOnPressScale { get; set; } = 1f;

    /// <summary>Tinte en hover (#RRGGBB); reservado para feedback visual avanzado.</summary>
    public string? ClickInteractHoverTintHex { get; set; }

    public string? ClickInteractableScriptIdOnClick { get; set; }
    public string? ClickInteractableScriptIdOnPointerEnter { get; set; }
    public string? ClickInteractableScriptIdOnPointerExit { get; set; }

    public bool GetColision(ObjectDefinition definition)
    {
        return ColisionOverride ?? definition.Colision;
    }

    public bool GetInteractivo(ObjectDefinition definition)
    {
        return InteractivoOverride ?? definition.Interactivo;
    }

    public bool GetDestructible(ObjectDefinition definition)
    {
        return DestructibleOverride ?? definition.Destructible;
    }

    public string? GetScriptId(ObjectDefinition definition)
    {
        if (ScriptIds != null && ScriptIds.Count > 0) return ScriptIds[0];
        return ScriptIdOverride ?? definition.ScriptId;
    }

    /// <summary>Devuelve todos los scripts asignados (instancia + definición).</summary>
    public IReadOnlyList<string> GetScriptIds(ObjectDefinition? definition)
    {
        var list = new List<string>();
        if (ScriptIds != null && ScriptIds.Count > 0)
            list.AddRange(ScriptIds);
        else if (ScriptIdOverride != null)
            list.Add(ScriptIdOverride);
        else if (definition?.ScriptId != null)
            list.Add(definition.ScriptId);
        return list;
    }
}
