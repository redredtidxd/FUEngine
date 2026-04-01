using System.Collections.Generic;
using FUEngine.Core;
using FUEngine.Input;

namespace FUEngine;

/// <summary>Movimiento opcional WASD antes de Lua; colisión con tilemap en el mismo tick vía <see cref="PlayModeRunner"/>.</summary>
internal static class NativeProtagonistController
{
    public static GameObject? FindProtagonist(IReadOnlyList<GameObject> scene, ProjectInfo project, IReadOnlyDictionary<GameObject, ObjectInstance> goToInst)
    {
        if (!string.IsNullOrEmpty(project.ProtagonistInstanceId))
        {
            foreach (var go in scene)
            {
                if (go.PendingDestroy) continue;
                if (goToInst.TryGetValue(go, out var inst) &&
                    string.Equals(inst.InstanceId, project.ProtagonistInstanceId, StringComparison.Ordinal))
                    return go;
            }
        }
        foreach (var go in scene)
        {
            if (go.PendingDestroy) continue;
            if (string.Equals(go.Name, "Player", StringComparison.OrdinalIgnoreCase))
                return go;
        }
        return null;
    }

    public static void ApplyNativeInputBeforeLua(
        ProjectInfo project,
        GameObject? hero,
        PlayKeyboardSnapshot? keys,
        double deltaSeconds,
        PlayModeRunner runner,
        IReadOnlyList<AnimationDefinition>? runtimeAnimations = null,
        TextureAssetCache? textureCacheForNativeAnim = null)
    {
        if (hero == null || keys == null || deltaSeconds <= 0) return;

        int rdx = (keys.D || keys.Right ? 1 : 0) - (keys.A || keys.Left ? 1 : 0);
        int rdy = (keys.S || keys.Down ? 1 : 0) - (keys.W || keys.Up ? 1 : 0);
        bool moveIntent = rdx != 0 || rdy != 0;

        NativeAutoAnimationApplier.TryUpdateProtagonistClip(project, hero, moveIntent, runtimeAnimations, textureCacheForNativeAnim);

        if (!project.UseNativeInput) return;
        if (!moveIntent) return;

        double dx = rdx, dy = rdy;
        double len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1e-9) return;
        dx /= len;
        dy /= len;
        float speed = project.NativeMoveSpeedTilesPerSecond > 0 ? project.NativeMoveSpeedTilesPerSecond : 4f;
        double move = speed * deltaSeconds;
        if (project.AutoFlipSprite && hero.GetComponent<SpriteComponent>() != null)
        {
            float ax = Math.Abs(hero.Transform.ScaleX);
            if (ax < 0.001f) ax = 1.0f;
            hero.Transform.ScaleX = dx > 0 ? ax : (dx < 0 ? -ax : hero.Transform.ScaleX);
        }
        // Resolver por ejes separados permite deslizamiento natural en paredes/esquinas.
        if (dx != 0)
            runner.TryMoveDynamicAgainstTilemap(hero, dx * move, 0);
        if (dy != 0)
            runner.TryMoveDynamicAgainstTilemap(hero, 0, dy * move);
    }
}
