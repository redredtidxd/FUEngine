using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FUEngine;

/// <summary>
/// Persistencia y CRUD de metadata del explorador: favoritos, recientes, pinned, metadata por asset, colecciones virtuales.
/// </summary>
public class ExplorerMetadataService
{
    public const string ExplorerStateFileName = ".fuengine-explorer.json";
    private const int MaxRecentCount = 30;
    private const int MaxPinnedCount = 20;

    private string _projectDirectory = "";
    private ExplorerStateDto _state = new();
    private readonly object _lock = new();

    public string ProjectDirectory => _projectDirectory;

    public void Initialize(string projectDirectory)
    {
        _projectDirectory = projectDirectory ?? "";
        Load();
    }

    public IReadOnlyList<string> GetFavorites()
    {
        lock (_lock) return _state.Favorites?.ToList() ?? new List<string>();
    }

    public void AddFavorite(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        var normalized = NormalizePath(path);
        lock (_lock)
        {
            _state.Favorites ??= new List<string>();
            if (!_state.Favorites.Any(p => string.Equals(p, normalized, StringComparison.OrdinalIgnoreCase)))
            {
                _state.Favorites.Add(normalized);
                Save();
            }
        }
    }

    public void RemoveFavorite(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        var normalized = NormalizePath(path);
        lock (_lock)
        {
            _state.Favorites?.RemoveAll(p => string.Equals(p, normalized, StringComparison.OrdinalIgnoreCase));
            Save();
        }
    }

    public bool IsFavorite(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        var normalized = NormalizePath(path);
        lock (_lock)
            return _state.Favorites?.Any(p => string.Equals(p, normalized, StringComparison.OrdinalIgnoreCase)) ?? false;
    }

    public IReadOnlyList<string> GetRecent()
    {
        lock (_lock) return _state.RecentPaths?.ToList() ?? new List<string>();
    }

    public void RecordRecent(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        var normalized = NormalizePath(path);
        lock (_lock)
        {
            _state.RecentPaths ??= new List<string>();
            _state.RecentPaths.RemoveAll(p => string.Equals(p, normalized, StringComparison.OrdinalIgnoreCase));
            _state.RecentPaths.Insert(0, normalized);
            while (_state.RecentPaths.Count > MaxRecentCount)
                _state.RecentPaths.RemoveAt(_state.RecentPaths.Count - 1);
            Save();
        }
    }

    public IReadOnlyList<string> GetPinned()
    {
        lock (_lock) return _state.PinnedPaths?.ToList() ?? new List<string>();
    }

    public void AddPinned(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        var normalized = NormalizePath(path);
        lock (_lock)
        {
            _state.PinnedPaths ??= new List<string>();
            if (!_state.PinnedPaths.Any(p => string.Equals(p, normalized, StringComparison.OrdinalIgnoreCase)))
            {
                _state.PinnedPaths.Add(normalized);
                while (_state.PinnedPaths.Count > MaxPinnedCount)
                    _state.PinnedPaths.RemoveAt(0);
                Save();
            }
        }
    }

    public void RemovePinned(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        var normalized = NormalizePath(path);
        lock (_lock)
        {
            _state.PinnedPaths?.RemoveAll(p => string.Equals(p, normalized, StringComparison.OrdinalIgnoreCase));
            Save();
        }
    }

    public bool IsPinned(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        var normalized = NormalizePath(path);
        lock (_lock)
            return _state.PinnedPaths?.Any(p => string.Equals(p, normalized, StringComparison.OrdinalIgnoreCase)) ?? false;
    }

    public AssetMetaDto? GetAssetMeta(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        var normalized = NormalizePath(path);
        lock (_lock)
        {
            if (_state.AssetMeta == null || !_state.AssetMeta.TryGetValue(normalized, out var meta))
                return null;
            return meta;
        }
    }

    public void SetAssetMeta(string path, AssetMetaDto meta)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        var normalized = NormalizePath(path);
        lock (_lock)
        {
            _state.AssetMeta ??= new Dictionary<string, AssetMetaDto>();
            _state.AssetMeta[normalized] = meta;
            Save();
        }
    }

    public IReadOnlyDictionary<string, List<string>> GetVirtualCollections()
    {
        lock (_lock)
        {
            if (_state.VirtualCollections == null) return new Dictionary<string, List<string>>();
            return _state.VirtualCollections.ToDictionary(k => k.Key, v => v.Value?.ToList() ?? new List<string>());
        }
    }

    public void SetVirtualCollection(string name, List<string> paths)
    {
        lock (_lock)
        {
            _state.VirtualCollections ??= new Dictionary<string, List<string>>();
            _state.VirtualCollections[name] = paths ?? new List<string>();
            Save();
        }
    }

    public void RemoveVirtualCollection(string name)
    {
        lock (_lock)
        {
            _state.VirtualCollections?.Remove(name);
            Save();
        }
    }

    public IReadOnlyList<string> GetExpandedFolderPaths()
    {
        lock (_lock) return _state.ExpandedFolderPaths?.ToList() ?? new List<string>();
    }

    public void SetExpandedFolderPaths(List<string> paths)
    {
        lock (_lock)
        {
            _state.ExpandedFolderPaths = paths?.ToList() ?? new List<string>();
            Save();
        }
    }

    private string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return path;
        try
        {
            var full = Path.GetFullPath(path);
            if (!string.IsNullOrEmpty(_projectDirectory) && full.StartsWith(_projectDirectory, StringComparison.OrdinalIgnoreCase))
                return full.Length == _projectDirectory.Length ? full : full.Substring(_projectDirectory.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return full;
        }
        catch { return path; }
    }

    private string GetStateFilePath() => Path.Combine(_projectDirectory, ExplorerStateFileName);

    public void Load()
    {
        var file = GetStateFilePath();
        if (string.IsNullOrEmpty(_projectDirectory) || !File.Exists(file))
        {
            _state = new ExplorerStateDto();
            return;
        }
        try
        {
            var json = File.ReadAllText(file);
            var loaded = JsonSerializer.Deserialize<ExplorerStateDto>(json);
            _state = loaded ?? new ExplorerStateDto();
        }
        catch
        {
            _state = new ExplorerStateDto();
        }
    }

    public void Save()
    {
        var file = GetStateFilePath();
        if (string.IsNullOrEmpty(_projectDirectory)) return;
        if (!File.Exists(file) && IsExplorerStateEmpty(_state)) return;
        try
        {
            var json = JsonSerializer.Serialize(_state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(file, json);
        }
        catch { /* ignore */ }
    }

    private static bool IsExplorerStateEmpty(ExplorerStateDto s)
    {
        if (s.Favorites?.Count > 0) return false;
        if (s.RecentPaths?.Count > 0) return false;
        if (s.PinnedPaths?.Count > 0) return false;
        if (s.AssetMeta?.Count > 0) return false;
        if (s.VirtualCollections?.Count > 0) return false;
        if (s.ExpandedFolderPaths?.Count > 0) return false;
        return true;
    }

    public Task SaveAsync()
    {
        return Task.Run(() => Save());
    }
}

/// <summary>DTO para .fuengine-explorer.json</summary>
public class ExplorerStateDto
{
    public List<string>? Favorites { get; set; }
    public List<string>? RecentPaths { get; set; }
    public List<string>? PinnedPaths { get; set; }
    public Dictionary<string, AssetMetaDto>? AssetMeta { get; set; }
    public Dictionary<string, List<string>>? VirtualCollections { get; set; }
    public List<string>? ExpandedFolderPaths { get; set; }
}

/// <summary>Metadata por asset (tags, color, rating, etc.)</summary>
public class AssetMetaDto
{
    public List<string>? Tags { get; set; }
    public string? Color { get; set; }
    public int? Rating { get; set; }
    public Dictionary<string, string>? CustomMetadata { get; set; }
    public bool IsLocked { get; set; }
}
