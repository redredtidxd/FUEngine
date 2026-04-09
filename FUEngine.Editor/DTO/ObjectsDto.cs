namespace FUEngine.Editor;

/// <summary>
/// DTO para guardar/cargar definiciones de objetos e instancias.
/// </summary>
public class ObjectsDto
{
    public List<ObjectDefinitionDto> Definitions { get; set; } = new();
    public List<ObjectInstanceDto> Instances { get; set; } = new();
}

public class ObjectDefinitionDto
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? SpritePath { get; set; }
    public bool Colision { get; set; }
    public bool Interactivo { get; set; }
    public bool Destructible { get; set; }
    public string? ScriptId { get; set; }
    public string? AnimacionId { get; set; }
    public int Width { get; set; } = 1;
    public int Height { get; set; } = 1;
    public string? AnimatronicType { get; set; }
    public string? MovementPattern { get; set; }
    public string? Personality { get; set; }
    public bool CanDetectPlayer { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool EnableInGameDrawing { get; set; }
}

public class ObjectInstanceDto
{
    public string InstanceId { get; set; } = "";
    public string? SourceSeedId { get; set; }
    public string? SourceSeedRelativePath { get; set; }
    public string DefinitionId { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public double Rotation { get; set; }
    public double ScaleX { get; set; } = 1.0;
    public double ScaleY { get; set; } = 1.0;
    public int LayerOrder { get; set; }
    public string Nombre { get; set; } = "";
    public bool? ColisionOverride { get; set; }
    public string? CollisionType { get; set; }
    public bool? InteractivoOverride { get; set; }
    public bool? DestructibleOverride { get; set; }
    public string? ScriptIdOverride { get; set; }
    public List<string> ScriptIds { get; set; } = new();
    public List<ScriptInstancePropertySetDto> ScriptProperties { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public bool Visible { get; set; } = true;
    /// <summary>Null en JSON antiguo = true (activo).</summary>
    public bool? Enabled { get; set; }
    public string? Pivot { get; set; }
    public bool PointLightEnabled { get; set; }
    public float PointLightRadius { get; set; } = 5f;
    public float PointLightIntensity { get; set; } = 1f;
    public string? PointLightColorHex { get; set; }

    public string? SpriteColorTintHex { get; set; }
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

    public string? ColliderShape { get; set; }
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
    public string? ProximityTargetTag { get; set; }

    public bool HealthEnabled { get; set; }
    public float HealthMax { get; set; } = 100f;
    public float HealthCurrent { get; set; } = 100f;
    public bool HealthInvulnerable { get; set; }

    public bool ClickInteractableEnabled { get; set; }
    /// <summary>Null en JSON antiguo = true (responde al puntero).</summary>
    public bool? ClickInteractableInteractEnabled { get; set; }
    public string? ClickInteractableShape { get; set; }
    public float ClickInteractableBoxWidthTiles { get; set; } = 1f;
    public float ClickInteractableBoxHeightTiles { get; set; } = 1f;
    public float ClickInteractableCircleRadiusTiles { get; set; } = 0.5f;
    public float ClickInteractableOffsetXTiles { get; set; }
    public float ClickInteractableOffsetYTiles { get; set; }
    public bool ClickInteractableHoverEffect { get; set; }
    public string? ClickInteractableInputFilter { get; set; }
    public float ClickInteractableMaxDistanceFromPlayerTiles { get; set; }
    public int ClickInteractZPriority { get; set; }
    public bool ClickInteractableRequireLineOfSight { get; set; }
    public float ClickInteractOnPressScale { get; set; } = 1f;
    public string? ClickInteractHoverTintHex { get; set; }
    public string? ClickInteractableScriptIdOnClick { get; set; }
    public string? ClickInteractableScriptIdOnPointerEnter { get; set; }
    public string? ClickInteractableScriptIdOnPointerExit { get; set; }
}

public class ScriptInstancePropertySetDto
{
    public string ScriptId { get; set; } = "";
    public List<ScriptPropertyEntryDto> Properties { get; set; } = new();
}

public class ScriptPropertyEntryDto
{
    public string Key { get; set; } = "";
    public string Type { get; set; } = "string";
    public string Value { get; set; } = "";
}
