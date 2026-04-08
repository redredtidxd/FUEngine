using System;
using System.Collections.Generic;
using System.Linq;
using FUEngine.Core;

namespace FUEngine.Runtime;

/// <summary>
/// Proxy de GameObject expuesto a Lua como "self" o devuelto por world.find(), etc.
/// API tipo Unity: getComponent (→ ComponentProxy), find, setParent, getChildren, addComponent, removeComponent, instantiate.
/// </summary>
[LuaVisible]
public sealed class SelfProxy
{
    private readonly GameObject _gameObject;
    private readonly string _instanceId;
    private readonly Func<GameObject, string?, string?, SelfProxy>? _createProxyFor;
    private readonly WorldApi? _worldApi;
    private readonly Func<GameObject, string, bool>? _playAnimationClip;
    private readonly Action<GameObject>? _stopAnimation;
    private InspectorPropertiesProxy? _inspectorProperties;

    public SelfProxy(GameObject gameObject, string? instanceId = null,
        Func<GameObject, string?, string?, SelfProxy>? createProxyFor = null, WorldApi? worldApi = null,
        Func<GameObject, string, bool>? playAnimationClip = null, Action<GameObject>? stopAnimation = null)
    {
        _gameObject = gameObject ?? throw new ArgumentNullException(nameof(gameObject));
        _instanceId = instanceId ?? gameObject.Name;
        _createProxyFor = createProxyFor;
        _worldApi = worldApi;
        _playAnimationClip = playAnimationClip;
        _stopAnimation = stopAnimation;
    }

    public string id => _instanceId;
    public string name { get => _gameObject.Name; set => _gameObject.Name = value ?? ""; }
    /// <summary>Primera etiqueta de <see cref="GameObject.Tags"/> (compatibilidad); usar <see cref="tags"/> / <see cref="hasTag"/>.</summary>
    public string tag => _gameObject.Tags?.Count > 0 ? _gameObject.Tags[0] : "";

    /// <summary>Copia de etiquetas para Lua (tabla/lista según NLua).</summary>
    public string[] tags => _gameObject.Tags is { Count: > 0 } list ? list.ToArray() : Array.Empty<string>();

    public bool hasTag(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || _gameObject.Tags == null) return false;
        foreach (var t in _gameObject.Tags)
        {
            if (string.Equals(t, name, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
    public double x { get => _gameObject.Transform.X; set => _gameObject.Transform.X = (float)value; }
    public double y { get => _gameObject.Transform.Y; set => _gameObject.Transform.Y = (float)value; }
    public double rotation { get => _gameObject.Transform.RotationDegrees; set => _gameObject.Transform.RotationDegrees = (float)value; }
    public double scale { get => (_gameObject.Transform.ScaleX + _gameObject.Transform.ScaleY) / 2.0; set { _gameObject.Transform.ScaleX = (float)value; _gameObject.Transform.ScaleY = (float)value; } }
    public bool visible { get; set; } = true;
    public bool active
    {
        get => !_gameObject.PendingDestroy && _gameObject.RuntimeActive;
        set => _gameObject.RuntimeActive = value;
    }

    /// <summary>Capa de dibujado (mayor = delante). Igual que LayerOrder del editor.</summary>
    public int renderOrder { get => _gameObject.RenderOrder; set => _gameObject.RenderOrder = value; }

    /// <summary>Proxy tipo tabla para propiedades de Inspector (Lua: <c>self.properties["x"] = 10</c>, tinte, etc.).</summary>
    public InspectorPropertiesProxy properties => _inspectorProperties ??= new InspectorPropertiesProxy(_gameObject);

    public void destroy()
    {
        if (_worldApi != null)
            _worldApi.destroy(this);
        else
        {
            _gameObject.PendingDestroy = true;
            _gameObject.RuntimeActive = false;
        }
    }

    public void move(double x, double y)
    {
        _gameObject.Transform.X = (float)x;
        _gameObject.Transform.Y = (float)y;
    }

    public void rotate(double angle)
    {
        _gameObject.Transform.RotationDegrees = (float)angle;
    }

    public void playAnimation(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        if (_playAnimationClip != null && _playAnimationClip(_gameObject, name.Trim()))
            return;
    }

    public void stopAnimation()
    {
        _stopAnimation?.Invoke(_gameObject);
    }

    /// <summary>Ruta de textura del sprite (relativa al proyecto). Crea <see cref="SpriteComponent"/> si no existe.</summary>
    public void setSpriteTexture(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;
        var s = EnsureSpriteComponent();
        s.TexturePath = relativePath.Trim().Replace('\\', '/');
        s.FrameRegions.Clear();
        s.CurrentFrameIndex = 0;
        s.AnimationTimeAccum = 0;
    }

    /// <summary>Añade un recorte de sprite sheet (píxeles en la textura). Requiere <see cref="SpriteComponent"/>.</summary>
    public void addSpriteFrame(int x, int y, int width, int height)
    {
        var s = _gameObject.GetComponent<SpriteComponent>();
        if (s == null || width <= 0 || height <= 0) return;
        s.FrameRegions.Add(new SpriteFrameRegion { X = x, Y = y, Width = width, Height = height });
    }

    public void clearSpriteFrames()
    {
        var s = _gameObject.GetComponent<SpriteComponent>();
        if (s == null) return;
        s.FrameRegions.Clear();
        s.CurrentFrameIndex = 0;
        s.AnimationTimeAccum = 0;
    }

    /// <summary>Índice del frame actual (0-based). Con lista vacía siempre 0.</summary>
    public int spriteFrame
    {
        get => _gameObject.GetComponent<SpriteComponent>()?.CurrentFrameIndex ?? 0;
        set
        {
            var s = _gameObject.GetComponent<SpriteComponent>();
            if (s == null) return;
            int n = s.FrameRegions.Count;
            if (n <= 0)
            {
                s.CurrentFrameIndex = 0;
                return;
            }
            int m = value % n;
            if (m < 0) m += n;
            s.CurrentFrameIndex = m;
        }
    }

    /// <summary>Velocidad de animación automática en frames por segundo (0 = solo control manual con spriteFrame).</summary>
    public void setSpriteAnimationFps(double framesPerSecond)
    {
        var s = _gameObject.GetComponent<SpriteComponent>();
        if (s == null) return;
        s.AnimationFramesPerSecond = (float)Math.Max(0, framesPerSecond);
        if (s.AnimationFramesPerSecond <= 0)
            s.AnimationTimeAccum = 0;
    }

    /// <summary>Orden fino dentro de la misma capa <see cref="GameObject.RenderOrder"/>.</summary>
    public void setSpriteSortOffset(int offset)
    {
        var s = _gameObject.GetComponent<SpriteComponent>();
        if (s != null) s.SortOffset = offset;
    }

    /// <summary>Tamaño de dibujado en casillas (ancho, alto).</summary>
    public void setSpriteDisplaySize(double widthTiles, double heightTiles)
    {
        var s = EnsureSpriteComponent();
        s.DisplayWidthTiles = (float)Math.Max(0.01, widthTiles);
        s.DisplayHeightTiles = (float)Math.Max(0.01, heightTiles);
    }

    private SpriteComponent EnsureSpriteComponent()
    {
        var s = _gameObject.GetComponent<SpriteComponent>();
        if (s != null) return s;
        s = new SpriteComponent();
        _gameObject.AddComponent(s);
        return s;
    }

    /// <summary>Tinte multiplicador RGB del sprite (0–1+). Lua: <c>self:setSpriteTint(1,0.5,0.2)</c>.</summary>
    public void setSpriteTint(double r, double g, double b)
    {
        var s = EnsureSpriteComponent();
        s.ColorTintR = (float)r;
        s.ColorTintG = (float)g;
        s.ColorTintB = (float)b;
    }

    /// <summary>Devuelve un ComponentProxy para invocar métodos desde Lua (ej: getComponent("Health"):invoke("takeDamage", 10)).</summary>
    public object? getComponent(string typeName)
    {
        var c = _gameObject.GetComponent(typeName);
        if (c == null) return null;
        var scriptInstance = (c as ScriptComponent)?.ScriptInstanceHandle as ScriptInstance;
        return new ComponentProxy(c, scriptInstance);
    }

    /// <summary>Añade un componente por nombre de tipo. Requiere factory en el motor; si no hay, retorna false.</summary>
    public bool addComponent(string typeName)
    {
        // El motor puede inyectar un factory vía IComponentFactory; por ahora stub
        _ = typeName;
        return false;
    }

    /// <summary>Quita un componente por nombre de tipo.</summary>
    public bool removeComponent(string typeName)
    {
        return _gameObject.RemoveComponent(typeName ?? "");
    }

    /// <summary>Busca un hijo por nombre (primer nivel).</summary>
    public SelfProxy? find(string name)
    {
        var child = _gameObject.Find(name ?? "");
        return child == null ? null : CreateProxy(child);
    }

    /// <summary>Busca por ruta jerárquica (ej: "Enemy/Weapon").</summary>
    public SelfProxy? findInHierarchy(string path)
    {
        var obj = _gameObject.FindInHierarchy(path ?? "");
        return obj == null ? null : CreateProxy(obj);
    }

    /// <summary>Establece el padre. Pasa otro proxy o null para dejar como raíz.</summary>
    public void setParent(SelfProxy? parent)
    {
        _gameObject.SetParent(parent?._gameObject);
    }

    /// <summary>Devuelve el padre o null si es raíz.</summary>
    public SelfProxy? getParent()
    {
        var p = _gameObject.Parent;
        return p == null ? null : CreateProxy(p);
    }

    /// <summary>Lista de hijos como proxies.</summary>
    public object? getChildren()
    {
        var list = new List<SelfProxy>();
        foreach (var child in _gameObject.Children)
            list.Add(CreateProxy(child));
        return list;
    }

    /// <summary>Instancia un seed (opcional <paramref name="variant"/> → id <c>prefab_variant</c>).</summary>
    public SelfProxy? instantiate(string prefabName, double x, double y, double rotation = 0, string? variant = null)
    {
        var result = _worldApi?.instantiate(prefabName ?? "", x, y, rotation, variant);
        return result as SelfProxy;
    }

    private SelfProxy CreateProxy(GameObject go)
    {
        if (_createProxyFor != null)
            return _createProxyFor(go, go.Name, null);
        return new SelfProxy(go, go.Name, null, _worldApi, _playAnimationClip, _stopAnimation);
    }

    internal GameObject GameObject => _gameObject;
}
