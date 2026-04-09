using System.Text.Json;
using FUEngine.Core;

namespace FUEngine.Editor;

public static class ObjectsSerialization
{
    public static void Save(ObjectLayer layer, string path)
    {
        var dto = ToDto(layer);
        var json = JsonSerializer.Serialize(dto, SerializationDefaults.Options);
        File.WriteAllText(path, json);
    }

    /// <summary>Copia la capa de objetos (útil para sandbox del tab Juego).</summary>
    public static ObjectLayer Clone(ObjectLayer layer)
    {
        if (layer == null) return new ObjectLayer();
        return FromDto(ToDto(layer));
    }

    public static ObjectLayer Load(string path)
    {
        if (!File.Exists(path))
            return new ObjectLayer();
        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"No se pudo leer el archivo de objetos: {ex.Message}", ex);
        }
        ObjectsDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<ObjectsDto>(json, SerializationDefaults.Options);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"JSON de objetos inválido (línea {ex.LineNumber}, posición {ex.BytePositionInLine}): {ex.Message}", ex);
        }
        if (dto == null)
            throw new InvalidOperationException("El archivo de objetos está vacío o mal formado.");
        return FromDto(dto);
    }

    public static ObjectsDto ToDto(ObjectLayer layer)
    {
        var definitions = layer.Definitions.Values
            .Select(d => new ObjectDefinitionDto
            {
                Id = d.Id,
                Nombre = d.Nombre,
                SpritePath = d.SpritePath,
                Colision = d.Colision,
                Interactivo = d.Interactivo,
                Destructible = d.Destructible,
                ScriptId = d.ScriptId,
                AnimacionId = d.AnimacionId,
                Width = d.Width,
                Height = d.Height,
                AnimatronicType = d.AnimatronicType,
                MovementPattern = d.MovementPattern,
                Personality = d.Personality,
                CanDetectPlayer = d.CanDetectPlayer,
                Tags = d.Tags ?? new List<string>(),
                EnableInGameDrawing = d.EnableInGameDrawing
            }).ToList();
        var instances = layer.Instances
            .Select(ToInstanceDto)
            .ToList();
        return new ObjectsDto { Definitions = definitions, Instances = instances };
    }

    public static ObjectInstanceDto ToInstanceDto(ObjectInstance i)
    {
        return new ObjectInstanceDto
            {
                InstanceId = i.InstanceId,
                SourceSeedId = i.SourceSeedId,
                SourceSeedRelativePath = i.SourceSeedRelativePath,
                DefinitionId = i.DefinitionId,
                X = i.X,
                Y = i.Y,
                Rotation = i.Rotation,
                ScaleX = i.ScaleX,
                ScaleY = i.ScaleY,
                LayerOrder = i.LayerOrder,
                Nombre = i.Nombre,
                ColisionOverride = i.ColisionOverride,
                CollisionType = i.CollisionType,
                InteractivoOverride = i.InteractivoOverride,
                DestructibleOverride = i.DestructibleOverride,
                ScriptIdOverride = i.ScriptIdOverride,
                ScriptIds = i.ScriptIds != null ? new List<string>(i.ScriptIds) : new List<string>(),
                ScriptProperties = (i.ScriptProperties ?? new List<ScriptInstancePropertySet>()).Select(sp => new ScriptInstancePropertySetDto
                {
                    ScriptId = sp.ScriptId,
                    Properties = (sp.Properties ?? new List<ScriptPropertyEntry>()).Select(p => new ScriptPropertyEntryDto { Key = p.Key, Type = p.Type ?? "string", Value = p.Value ?? "" }).ToList()
                }).ToList(),
                Tags = i.Tags != null ? new List<string>(i.Tags) : new List<string>(),
                Visible = i.Visible,
                Enabled = i.Enabled,
                Pivot = i.Pivot,
                PointLightEnabled = i.PointLightEnabled,
                PointLightRadius = i.PointLightRadius,
                PointLightIntensity = i.PointLightIntensity,
                PointLightColorHex = string.IsNullOrEmpty(i.PointLightColorHex) ? null : i.PointLightColorHex,
                SpriteColorTintHex = string.IsNullOrEmpty(i.SpriteColorTintHex) ? null : i.SpriteColorTintHex,
                SpriteFlipX = i.SpriteFlipX,
                SpriteFlipY = i.SpriteFlipY,
                SpriteSortOffset = i.SpriteSortOffset,
                DefaultAnimationClipId = i.DefaultAnimationClipId,
                AnimationAutoPlay = i.AnimationAutoPlay,
                AnimationSpeedMultiplier = i.AnimationSpeedMultiplier,
                ParticleEmitterEnabled = i.ParticleEmitterEnabled,
                ParticleTexturePath = i.ParticleTexturePath,
                ParticleEmissionRate = i.ParticleEmissionRate,
                ParticleLifeTime = i.ParticleLifeTime,
                ParticleGravityScale = i.ParticleGravityScale,
                ColliderShape = string.IsNullOrEmpty(i.ColliderShape) ? null : i.ColliderShape,
                ColliderBoxWidthTiles = i.ColliderBoxWidthTiles,
                ColliderBoxHeightTiles = i.ColliderBoxHeightTiles,
                ColliderCircleRadiusTiles = i.ColliderCircleRadiusTiles,
                ColliderOffsetX = i.ColliderOffsetX,
                ColliderOffsetY = i.ColliderOffsetY,
                RigidbodyEnabled = i.RigidbodyEnabled,
                RigidbodyMass = i.RigidbodyMass,
                RigidbodyGravityScale = i.RigidbodyGravityScale,
                RigidbodyDrag = i.RigidbodyDrag,
                RigidbodyFreezeRotation = i.RigidbodyFreezeRotation,
                CameraTargetEnabled = i.CameraTargetEnabled,
                AudioSourceEnabled = i.AudioSourceEnabled,
                AudioClipId = i.AudioClipId,
                AudioVolume = i.AudioVolume,
                AudioPitch = i.AudioPitch,
                AudioLoop = i.AudioLoop,
                AudioSpatialBlend = i.AudioSpatialBlend,
                ProximitySensorEnabled = i.ProximitySensorEnabled,
                ProximityDetectionRangeTiles = i.ProximityDetectionRangeTiles,
                ProximityTargetTag = string.IsNullOrEmpty(i.ProximityTargetTag) ? null : i.ProximityTargetTag,
                HealthEnabled = i.HealthEnabled,
                HealthMax = i.HealthMax,
                HealthCurrent = i.HealthCurrent,
                HealthInvulnerable = i.HealthInvulnerable,
                ClickInteractableEnabled = i.ClickInteractableEnabled,
                ClickInteractableInteractEnabled = i.ClickInteractableInteractEnabled,
                ClickInteractableShape = string.IsNullOrEmpty(i.ClickInteractableShape) ? null : i.ClickInteractableShape,
                ClickInteractableBoxWidthTiles = i.ClickInteractableBoxWidthTiles,
                ClickInteractableBoxHeightTiles = i.ClickInteractableBoxHeightTiles,
                ClickInteractableCircleRadiusTiles = i.ClickInteractableCircleRadiusTiles,
                ClickInteractableOffsetXTiles = i.ClickInteractableOffsetXTiles,
                ClickInteractableOffsetYTiles = i.ClickInteractableOffsetYTiles,
                ClickInteractableHoverEffect = i.ClickInteractableHoverEffect,
                ClickInteractableInputFilter = string.IsNullOrEmpty(i.ClickInteractableInputFilter) ? null : i.ClickInteractableInputFilter,
                ClickInteractableMaxDistanceFromPlayerTiles = i.ClickInteractableMaxDistanceFromPlayerTiles,
                ClickInteractZPriority = i.ClickInteractZPriority,
                ClickInteractableRequireLineOfSight = i.ClickInteractableRequireLineOfSight,
                ClickInteractOnPressScale = i.ClickInteractOnPressScale,
                ClickInteractHoverTintHex = string.IsNullOrEmpty(i.ClickInteractHoverTintHex) ? null : i.ClickInteractHoverTintHex,
                ClickInteractableScriptIdOnClick = i.ClickInteractableScriptIdOnClick,
                ClickInteractableScriptIdOnPointerEnter = i.ClickInteractableScriptIdOnPointerEnter,
                ClickInteractableScriptIdOnPointerExit = i.ClickInteractableScriptIdOnPointerExit
            };
    }

    public static ObjectLayer FromDto(ObjectsDto dto)
    {
        if (dto == null) return new ObjectLayer();
        var layer = new ObjectLayer();
        foreach (var d in dto.Definitions ?? new List<ObjectDefinitionDto>())
        {
            layer.RegisterDefinition(new ObjectDefinition
            {
                Id = d.Id,
                Nombre = d.Nombre,
                SpritePath = d.SpritePath,
                Colision = d.Colision,
                Interactivo = d.Interactivo,
                Destructible = d.Destructible,
                ScriptId = d.ScriptId,
                AnimacionId = d.AnimacionId,
                Width = d.Width,
                Height = d.Height,
                AnimatronicType = d.AnimatronicType,
                MovementPattern = d.MovementPattern,
                Personality = d.Personality,
                CanDetectPlayer = d.CanDetectPlayer,
                Tags = d.Tags ?? new List<string>(),
                EnableInGameDrawing = d.EnableInGameDrawing
            });
        }
        foreach (var i in dto.Instances ?? new List<ObjectInstanceDto>())
            layer.AddInstance(FromInstanceDto(i));
        return layer;
    }

    public static ObjectInstance FromInstanceDto(ObjectInstanceDto i)
    {
        return new ObjectInstance
            {
                InstanceId = i.InstanceId,
                SourceSeedId = i.SourceSeedId,
                SourceSeedRelativePath = i.SourceSeedRelativePath,
                DefinitionId = i.DefinitionId,
                X = i.X,
                Y = i.Y,
                Rotation = i.Rotation,
                ScaleX = i.ScaleX != 0 ? i.ScaleX : 1.0,
                ScaleY = i.ScaleY != 0 ? i.ScaleY : 1.0,
                LayerOrder = i.LayerOrder,
                Nombre = i.Nombre,
                ColisionOverride = i.ColisionOverride,
                CollisionType = i.CollisionType,
                InteractivoOverride = i.InteractivoOverride,
                DestructibleOverride = i.DestructibleOverride,
                ScriptIdOverride = i.ScriptIdOverride,
                ScriptIds = i.ScriptIds != null ? new List<string>(i.ScriptIds) : new List<string>(),
                ScriptProperties = (i.ScriptProperties ?? new List<ScriptInstancePropertySetDto>()).Select(sp => new ScriptInstancePropertySet
                {
                    ScriptId = sp.ScriptId,
                    Properties = (sp.Properties ?? new List<ScriptPropertyEntryDto>()).Select(p => new ScriptPropertyEntry { Key = p.Key, Type = p.Type ?? "string", Value = p.Value ?? "" }).ToList()
                }).ToList(),
                Tags = i.Tags != null ? new List<string>(i.Tags) : new List<string>(),
                Visible = i.Visible,
                Enabled = i.Enabled ?? true,
                Pivot = i.Pivot,
                PointLightEnabled = i.PointLightEnabled,
                PointLightRadius = i.PointLightRadius <= 0 ? 5f : i.PointLightRadius,
                PointLightIntensity = i.PointLightIntensity <= 0 ? 1f : i.PointLightIntensity,
                PointLightColorHex = string.IsNullOrWhiteSpace(i.PointLightColorHex) ? "#ffffff" : i.PointLightColorHex!,
                SpriteColorTintHex = string.IsNullOrWhiteSpace(i.SpriteColorTintHex) ? "#ffffff" : i.SpriteColorTintHex!,
                SpriteFlipX = i.SpriteFlipX,
                SpriteFlipY = i.SpriteFlipY,
                SpriteSortOffset = i.SpriteSortOffset,
                DefaultAnimationClipId = i.DefaultAnimationClipId,
                AnimationAutoPlay = i.AnimationAutoPlay,
                AnimationSpeedMultiplier = i.AnimationSpeedMultiplier <= 0 ? 1f : i.AnimationSpeedMultiplier,
                ParticleEmitterEnabled = i.ParticleEmitterEnabled,
                ParticleTexturePath = i.ParticleTexturePath,
                ParticleEmissionRate = i.ParticleEmissionRate <= 0 ? 10f : i.ParticleEmissionRate,
                ParticleLifeTime = i.ParticleLifeTime <= 0 ? 1f : i.ParticleLifeTime,
                ParticleGravityScale = i.ParticleGravityScale,
                ColliderShape = string.IsNullOrWhiteSpace(i.ColliderShape) ? "Box" : i.ColliderShape!,
                ColliderBoxWidthTiles = i.ColliderBoxWidthTiles,
                ColliderBoxHeightTiles = i.ColliderBoxHeightTiles,
                ColliderCircleRadiusTiles = i.ColliderCircleRadiusTiles <= 0 ? 0.5f : i.ColliderCircleRadiusTiles,
                ColliderOffsetX = i.ColliderOffsetX,
                ColliderOffsetY = i.ColliderOffsetY,
                RigidbodyEnabled = i.RigidbodyEnabled,
                RigidbodyMass = i.RigidbodyMass <= 0 ? 1f : i.RigidbodyMass,
                RigidbodyGravityScale = i.RigidbodyGravityScale,
                RigidbodyDrag = i.RigidbodyDrag,
                RigidbodyFreezeRotation = i.RigidbodyFreezeRotation,
                CameraTargetEnabled = i.CameraTargetEnabled,
                AudioSourceEnabled = i.AudioSourceEnabled,
                AudioClipId = i.AudioClipId,
                AudioVolume = i.AudioVolume <= 0 ? 1f : i.AudioVolume,
                AudioPitch = i.AudioPitch <= 0 ? 1f : i.AudioPitch,
                AudioLoop = i.AudioLoop,
                AudioSpatialBlend = i.AudioSpatialBlend,
                ProximitySensorEnabled = i.ProximitySensorEnabled,
                ProximityDetectionRangeTiles = i.ProximityDetectionRangeTiles <= 0 ? 1f : i.ProximityDetectionRangeTiles,
                ProximityTargetTag = string.IsNullOrWhiteSpace(i.ProximityTargetTag) ? "player" : i.ProximityTargetTag!,
                HealthEnabled = i.HealthEnabled,
                HealthMax = i.HealthMax <= 0 ? 100f : i.HealthMax,
                HealthCurrent = i.HealthEnabled
                    ? (i.HealthCurrent <= 0 ? (i.HealthMax <= 0 ? 100f : i.HealthMax) : i.HealthCurrent)
                    : Math.Max(0f, i.HealthCurrent),
                HealthInvulnerable = i.HealthInvulnerable,
                ClickInteractableEnabled = i.ClickInteractableEnabled,
                ClickInteractableInteractEnabled = i.ClickInteractableInteractEnabled ?? true,
                ClickInteractableShape = string.IsNullOrWhiteSpace(i.ClickInteractableShape) ? "Box" : i.ClickInteractableShape!,
                ClickInteractableBoxWidthTiles = i.ClickInteractableBoxWidthTiles <= 0 ? 1f : i.ClickInteractableBoxWidthTiles,
                ClickInteractableBoxHeightTiles = i.ClickInteractableBoxHeightTiles <= 0 ? 1f : i.ClickInteractableBoxHeightTiles,
                ClickInteractableCircleRadiusTiles = i.ClickInteractableCircleRadiusTiles <= 0 ? 0.5f : i.ClickInteractableCircleRadiusTiles,
                ClickInteractableOffsetXTiles = i.ClickInteractableOffsetXTiles,
                ClickInteractableOffsetYTiles = i.ClickInteractableOffsetYTiles,
                ClickInteractableHoverEffect = i.ClickInteractableHoverEffect,
                ClickInteractableInputFilter = string.IsNullOrWhiteSpace(i.ClickInteractableInputFilter) ? "Both" : i.ClickInteractableInputFilter!,
                ClickInteractableMaxDistanceFromPlayerTiles = i.ClickInteractableMaxDistanceFromPlayerTiles,
                ClickInteractZPriority = i.ClickInteractZPriority,
                ClickInteractableRequireLineOfSight = i.ClickInteractableRequireLineOfSight,
                ClickInteractOnPressScale = i.ClickInteractOnPressScale <= 0 || i.ClickInteractOnPressScale >= 1f ? 1f : i.ClickInteractOnPressScale,
                ClickInteractHoverTintHex = string.IsNullOrWhiteSpace(i.ClickInteractHoverTintHex) ? null : i.ClickInteractHoverTintHex!.Trim(),
                ClickInteractableScriptIdOnClick = i.ClickInteractableScriptIdOnClick,
                ClickInteractableScriptIdOnPointerEnter = i.ClickInteractableScriptIdOnPointerEnter,
                ClickInteractableScriptIdOnPointerExit = i.ClickInteractableScriptIdOnPointerExit
            };
    }
}
