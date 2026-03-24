using System.Globalization;
using System.Windows.Media.Imaging;
using FUEngine.Core;
using FUEngine.Editor;

namespace FUEngine;

/// <summary>Aplica clips de <c>animaciones.json</c> al <see cref="SpriteComponent"/> del protagonista (convención <c>Idle</c> / <c>Walk</c>).</summary>
internal static class NativeAutoAnimationApplier
{
    private static readonly HashSet<string> WarnedUnusableClips = new(StringComparer.OrdinalIgnoreCase);

    public static void TryUpdateProtagonistClip(
        ProjectInfo project,
        GameObject hero,
        bool keyboardMoveIntent,
        IReadOnlyList<AnimationDefinition>? anims,
        TextureAssetCache? texCache)
    {
        if (!project.UseNativeAutoAnimation || anims == null || anims.Count == 0) return;
        var sprite = hero.GetComponent<SpriteComponent>();
        if (sprite == null) return;

        string target = keyboardMoveIntent ? "Walk" : "Idle";
        if (string.Equals(sprite.NativeAutoAnimationKey, target, StringComparison.OrdinalIgnoreCase)) return;

        var clip = FindClip(anims, target);
        if (clip == null) return;

        int tileSize = Math.Max(1, project.TileSize);
        if (!TryApplyClip(sprite, clip, project.DefaultAnimationFps, tileSize, texCache, target))
            return;

        sprite.NativeAutoAnimationKey = target;
    }

    /// <summary>Aplica un clip de <c>animaciones.json</c> por id o nombre (p. ej. desde <c>self.playAnimation</c> o instancia).</summary>
    public static bool TryApplyClipForGameObject(
        ProjectInfo project,
        GameObject go,
        string clipName,
        IReadOnlyList<AnimationDefinition>? anims,
        TextureAssetCache? texCache)
    {
        if (string.IsNullOrWhiteSpace(clipName) || anims == null || anims.Count == 0) return false;
        var sprite = go.GetComponent<SpriteComponent>();
        if (sprite == null) return false;
        var clip = FindClip(anims, clipName.Trim());
        if (clip == null) return false;
        int tileSize = Math.Max(1, project.TileSize);
        if (!TryApplyClip(sprite, clip, project.DefaultAnimationFps, tileSize, texCache, clipName.Trim()))
            return false;
        sprite.NativeAutoAnimationKey = clipName.Trim();
        return true;
    }

    private static AnimationDefinition? FindClip(IReadOnlyList<AnimationDefinition> anims, string name)
    {
        foreach (var a in anims)
        {
            if (string.Equals(a.Id, name, StringComparison.OrdinalIgnoreCase)) return a;
            if (string.Equals(a.Nombre, name, StringComparison.OrdinalIgnoreCase)) return a;
        }
        return null;
    }

    private static bool TryApplyClip(
        SpriteComponent sprite,
        AnimationDefinition clip,
        int defaultFps,
        int tileSize,
        TextureAssetCache? texCache,
        string logicalNameForLog)
    {
        if (clip.Frames == null || clip.Frames.Count == 0) return false;
        BitmapImage? bmp = texCache?.GetOrLoad(sprite.TexturePath);
        var regions = new List<SpriteFrameRegion>();
        foreach (var f in clip.Frames)
        {
            if (string.IsNullOrWhiteSpace(f)) continue;
            if (TryParseRect(f, out var rect) && rect != null)
            {
                regions.Add(rect);
                continue;
            }
            if (bmp != null && TryParseIndexStrip(f, bmp, sprite, tileSize, out var stripRect) && stripRect != null)
                regions.Add(stripRect);
        }

        if (regions.Count == 0)
        {
            var key = (clip.Id ?? "") + "|" + (clip.Nombre ?? "") + "|" + logicalNameForLog;
            if (WarnedUnusableClips.Add(key))
            {
                EditorLog.Warning(
                    $"Auto-animación nativa: el clip «{logicalNameForLog}» (id={clip.Id}) no tiene frames válidos. Usa «x,y,ancho,alto» en píxeles o índices 0,1,2… con textura cargable y tamaño de frame = DisplayTiles×TileSize.",
                    "Play");
            }
            return false;
        }

        sprite.FrameRegions.Clear();
        foreach (var r in regions)
            sprite.FrameRegions.Add(r);
        sprite.CurrentFrameIndex = 0;
        sprite.AnimationTimeAccum = 0;
        int fps = clip.Fps > 0 ? clip.Fps : (defaultFps > 0 ? defaultFps : 8);
        sprite.AnimationFramesPerSecond = fps;
        return true;
    }

    private static bool TryParseRect(string s, out SpriteFrameRegion? r)
    {
        r = null;
        var parts = s.Split(new[] { ',', ';' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4) return false;
        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x)) return false;
        if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var y)) return false;
        if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var w)) return false;
        if (!int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var h)) return false;
        if (w <= 0 || h <= 0) return false;
        r = new SpriteFrameRegion { X = x, Y = y, Width = w, Height = h };
        return true;
    }

    private static bool TryParseIndexStrip(string s, BitmapImage bmp, SpriteComponent sprite, int tileSize, out SpriteFrameRegion? r)
    {
        r = null;
        if (!int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var idx) || idx < 0)
            return false;
        int fw = (int)Math.Max(1, Math.Round(sprite.DisplayWidthTiles * tileSize));
        int fh = (int)Math.Max(1, Math.Round(sprite.DisplayHeightTiles * tileSize));
        int x = idx * fw;
        if (fw <= 0 || fh <= 0) return false;
        if (x + fw > bmp.PixelWidth || fh > bmp.PixelHeight) return false;
        r = new SpriteFrameRegion { X = x, Y = 0, Width = fw, Height = fh };
        return true;
    }
}
