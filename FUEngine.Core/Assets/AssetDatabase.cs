using System.Collections.Generic;

namespace FUEngine.Core;

/// <summary>Base de datos de assets: registra texturas, sonidos, scripts; evita duplicados por path/id.</summary>
public class AssetDatabase
{
    private readonly Dictionary<string, TextureAsset> _textures = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SoundAsset> _sounds = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ScriptAsset> _scripts = new(StringComparer.OrdinalIgnoreCase);

    public void RegisterTexture(TextureAsset asset) => _textures[asset.Id] = asset;
    public void RegisterSound(SoundAsset asset) => _sounds[asset.Id] = asset;
    public void RegisterScript(ScriptAsset asset) => _scripts[asset.Id] = asset;

    public bool TryGetTexture(string id, out TextureAsset? asset) => _textures.TryGetValue(id, out asset);
    public bool TryGetSound(string id, out SoundAsset? asset) => _sounds.TryGetValue(id, out asset);
    public bool TryGetScript(string id, out ScriptAsset? asset) => _scripts.TryGetValue(id, out asset);
}
