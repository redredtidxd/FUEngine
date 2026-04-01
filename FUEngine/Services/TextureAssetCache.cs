using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;

namespace FUEngine;

/// <summary>Carga y cachea BitmapImage desde rutas relativas al proyecto (viewport Play, editor).</summary>
public sealed class TextureAssetCache
{
    private readonly string _projectRoot;
    private readonly Dictionary<string, BitmapImage> _cache = new(StringComparer.OrdinalIgnoreCase);

    public TextureAssetCache(string? projectRoot)
    {
        _projectRoot = string.IsNullOrWhiteSpace(projectRoot)
            ? ""
            : Path.GetFullPath(projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }

    public static string NormalizeRelativePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";
        return path.Trim().Replace('\\', '/');
    }

    public BitmapImage? GetOrLoad(string? relativePath)
    {
        var norm = NormalizeRelativePath(relativePath);
        if (string.IsNullOrEmpty(norm)) return null;
        if (_cache.TryGetValue(norm, out var cached)) return cached;
        if (string.IsNullOrEmpty(_projectRoot)) return null;

        var combined = Path.GetFullPath(Path.Combine(_projectRoot, norm.Replace('/', Path.DirectorySeparatorChar)));
        if (!combined.StartsWith(_projectRoot, StringComparison.OrdinalIgnoreCase)) return null;
        if (!File.Exists(combined)) return null;

        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(combined);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bmp.EndInit();
            bmp.Freeze();
            _cache[norm] = bmp;
            return bmp;
        }
        catch
        {
            return null;
        }
    }

    public void Clear() => _cache.Clear();

    /// <summary>Quita una ruta de la caché (p. ej. tras reescalar un PNG en el explorador). Acepta ruta absoluta dentro del proyecto.</summary>
    public void InvalidateAbsolutePath(string? absolutePath)
    {
        if (string.IsNullOrWhiteSpace(absolutePath) || string.IsNullOrEmpty(_projectRoot)) return;
        string full;
        try { full = Path.GetFullPath(absolutePath); } catch { return; }
        if (!full.StartsWith(_projectRoot, StringComparison.OrdinalIgnoreCase)) return;
        var rel = full.Substring(_projectRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        rel = NormalizeRelativePath(rel);
        if (string.IsNullOrEmpty(rel)) return;
        _cache.Remove(rel);
    }

    /// <summary>Quita una ruta relativa al proyecto (formato con <c>/</c>).</summary>
    public void InvalidateRelativePath(string? relativePath)
    {
        var norm = NormalizeRelativePath(relativePath);
        if (string.IsNullOrEmpty(norm)) return;
        _cache.Remove(norm);
    }
}
