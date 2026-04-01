using System.Globalization;

namespace FUEngine.Core;

/// <summary>
/// Política de sincronización de instancias UI con su prefab (SeedId):
/// - Apply: actualiza desde prefab manteniendo overrides.
/// - Reset: limpia overrides y deja valores del prefab.
/// </summary>
public static class UIPrefabPolicy
{
    private static readonly string[] TrackedKeys =
    {
        "Kind",
        "Rect.X", "Rect.Y", "Rect.Width", "Rect.Height",
        "Anchors.MinX", "Anchors.MinY", "Anchors.MaxX", "Anchors.MaxY",
        "Text", "ImagePath", "BlocksInput"
    };

    /// <summary>Busca el prefab (elemento fuente) correspondiente al SeedId de la instancia.</summary>
    public static UIElement? FindPrefabBySeedId(UIRoot? root, UIElement? instance)
    {
        if (root == null || instance == null || string.IsNullOrWhiteSpace(instance.SeedId))
            return null;

        foreach (var canvas in root.Canvases)
        {
            var found = FindElementById(canvas.Children, instance.SeedId, instance);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>Aplica valores del prefab a la instancia. Si keepOverrides=true, se re-aplican los overrides guardados.</summary>
    public static void ApplyFromPrefab(UIElement instance, UIElement prefab, bool keepOverrides)
    {
        if (instance == null || prefab == null) return;

        var oldId = instance.Id;
        var oldSeedId = instance.SeedId;
        var overrides = keepOverrides
            ? ToCaseInsensitiveMap(instance.PropertyOverrides)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        instance.Kind = prefab.Kind;
        instance.Rect = prefab.Rect;
        instance.Anchors = prefab.Anchors;
        instance.Text = prefab.Text;
        instance.ImagePath = prefab.ImagePath;
        instance.BlocksInput = prefab.BlocksInput;

        instance.Id = oldId;
        instance.SeedId = oldSeedId;

        if (keepOverrides && overrides.Count > 0)
            ApplyOverrides(instance, overrides);

        RefreshOverridesFromPrefab(instance, prefab);
    }

    /// <summary>Limpia overrides y deja la instancia exactamente con valores del prefab (excepto Id/SeedId).</summary>
    public static void ResetOverrides(UIElement instance, UIElement prefab)
    {
        if (instance == null || prefab == null) return;
        instance.PropertyOverrides.Clear();
        ApplyFromPrefab(instance, prefab, keepOverrides: false);
    }

    /// <summary>Recalcula PropertyOverrides comparando instancia vs prefab.</summary>
    public static void RefreshOverridesFromPrefab(UIElement instance, UIElement prefab)
    {
        if (instance == null || prefab == null) return;

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in TrackedKeys)
        {
            var value = SerializeProperty(instance, key);
            var prefabValue = SerializeProperty(prefab, key);
            if (!string.Equals(value, prefabValue, StringComparison.Ordinal))
                map[key] = value;
        }

        instance.PropertyOverrides.Clear();
        foreach (var kv in map)
            instance.PropertyOverrides[kv.Key] = kv.Value;
    }

    private static void ApplyOverrides(UIElement target, IReadOnlyDictionary<string, string> overrides)
    {
        foreach (var kv in overrides)
            ApplyOverride(target, kv.Key, kv.Value);
    }

    private static void ApplyOverride(UIElement target, string key, string value)
    {
        switch (key)
        {
            case "Kind":
                if (Enum.TryParse<UIElementKind>(value, ignoreCase: true, out var kind))
                    target.Kind = kind;
                break;
            case "Rect.X":
                if (TryParseDouble(value, out var rectX))
                {
                    var r = target.Rect;
                    r.X = rectX;
                    target.Rect = r;
                }
                break;
            case "Rect.Y":
                if (TryParseDouble(value, out var rectY))
                {
                    var r = target.Rect;
                    r.Y = rectY;
                    target.Rect = r;
                }
                break;
            case "Rect.Width":
                if (TryParseDouble(value, out var rectW))
                {
                    var r = target.Rect;
                    r.Width = rectW;
                    target.Rect = r;
                }
                break;
            case "Rect.Height":
                if (TryParseDouble(value, out var rectH))
                {
                    var r = target.Rect;
                    r.Height = rectH;
                    target.Rect = r;
                }
                break;
            case "Anchors.MinX":
                if (TryParseDouble(value, out var minX))
                {
                    var a = target.Anchors;
                    a.MinX = minX;
                    target.Anchors = a;
                }
                break;
            case "Anchors.MinY":
                if (TryParseDouble(value, out var minY))
                {
                    var a = target.Anchors;
                    a.MinY = minY;
                    target.Anchors = a;
                }
                break;
            case "Anchors.MaxX":
                if (TryParseDouble(value, out var maxX))
                {
                    var a = target.Anchors;
                    a.MaxX = maxX;
                    target.Anchors = a;
                }
                break;
            case "Anchors.MaxY":
                if (TryParseDouble(value, out var maxY))
                {
                    var a = target.Anchors;
                    a.MaxY = maxY;
                    target.Anchors = a;
                }
                break;
            case "Text":
                target.Text = value ?? "";
                break;
            case "ImagePath":
                target.ImagePath = value ?? "";
                break;
            case "BlocksInput":
                if (bool.TryParse(value, out var blocks)) target.BlocksInput = blocks;
                break;
        }
    }

    private static string SerializeProperty(UIElement element, string key)
    {
        return key switch
        {
            "Kind" => element.Kind.ToString(),
            "Rect.X" => element.Rect.X.ToString(CultureInfo.InvariantCulture),
            "Rect.Y" => element.Rect.Y.ToString(CultureInfo.InvariantCulture),
            "Rect.Width" => element.Rect.Width.ToString(CultureInfo.InvariantCulture),
            "Rect.Height" => element.Rect.Height.ToString(CultureInfo.InvariantCulture),
            "Anchors.MinX" => element.Anchors.MinX.ToString(CultureInfo.InvariantCulture),
            "Anchors.MinY" => element.Anchors.MinY.ToString(CultureInfo.InvariantCulture),
            "Anchors.MaxX" => element.Anchors.MaxX.ToString(CultureInfo.InvariantCulture),
            "Anchors.MaxY" => element.Anchors.MaxY.ToString(CultureInfo.InvariantCulture),
            "Text" => element.Text ?? "",
            "ImagePath" => element.ImagePath ?? "",
            "BlocksInput" => element.BlocksInput.ToString(CultureInfo.InvariantCulture),
            _ => ""
        };
    }

    private static bool TryParseDouble(string? value, out double number) =>
        double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out number);

    private static Dictionary<string, string> ToCaseInsensitiveMap(Dictionary<string, string>? map)
    {
        var copy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (map == null) return copy;
        foreach (var kv in map)
            copy[kv.Key] = kv.Value;
        return copy;
    }

    private static UIElement? FindElementById(List<UIElement> elements, string id, UIElement instanceToSkip)
    {
        foreach (var element in elements)
        {
            if (!ReferenceEquals(element, instanceToSkip) &&
                string.Equals(element.Id, id, StringComparison.OrdinalIgnoreCase))
                return element;

            var nested = FindElementById(element.Children, id, instanceToSkip);
            if (nested != null) return nested;
        }
        return null;
    }
}
