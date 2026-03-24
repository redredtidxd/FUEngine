using System.Collections.Generic;
using System.IO;
using FUEngine.Core;
using NLua;

namespace FUEngine.Runtime;

/// <summary>
/// Registra en el entorno Lua las tablas globales: self, world, input, time, audio, physics, ui, game, ads.
/// </summary>
public static class ScriptBindings
{
    public static void SetSelf(LuaTable env, SelfProxy selfProxy)
    {
        env["self"] = selfProxy;
    }

    public static void SetWorld(LuaTable env, WorldApi api)
    {
        env["world"] = api;
    }

    public static void SetInput(LuaTable env, InputApi api)
    {
        env["input"] = api;
    }

    public static void SetTime(LuaTable env, TimeApi api)
    {
        env["time"] = api;
    }

    public static void SetAudio(LuaTable env, AudioApi api)
    {
        env["audio"] = api;
    }

    public static void SetPhysics(LuaTable env, PhysicsApi api)
    {
        env["physics"] = api;
    }

    public static void SetUi(LuaTable env, UiApi api)
    {
        env["ui"] = api;
    }

    public static void SetGame(LuaTable env, GameApi api)
    {
        env["game"] = api;
    }

    public static void SetDebug(LuaTable env, DebugDrawApi api)
    {
        env["Debug"] = api;
    }

    public static void SetAds(LuaTable env, AdsApi api)
    {
        env["ads"] = api;
    }

    /// <summary>Constantes Key.* y Mouse.* para input. Se registran en el entorno.</summary>
    public static void SetInputConstants(LuaTable env)
    {
        var key = new KeyConstants();
        var mouse = new MouseConstants();
        env["Key"] = key;
        env["Mouse"] = mouse;
    }

    /// <summary>Rellena el entorno con todas las APIs (stubs donde no haya implementación).</summary>
    public static void PopulateEnvironment(LuaTable env, SelfProxy selfProxy, WorldApi? world = null, InputApi? input = null, TimeApi? time = null, AudioApi? audio = null, UiApi? ui = null, GameApi? game = null, DebugDrawApi? debug = null, PhysicsApi? physics = null, AdsApi? ads = null)
    {
        SetSelf(env, selfProxy);
        SetWorld(env, world ?? new WorldApi());
        SetInput(env, input ?? new InputApi());
        SetTime(env, time ?? new TimeApi());
        SetAudio(env, audio ?? new AudioApi());
        SetPhysics(env, physics ?? new PhysicsApi());
        SetUi(env, ui ?? new UiApi());
        SetGame(env, game ?? new GameApi());
        SetAds(env, ads ?? new AdsApi());
        if (debug != null)
            SetDebug(env, debug);
        SetInputConstants(env);
    }

    /// <summary>Entorno para scripts de capa: <c>layer</c> + mismas APIs globales que un script de objeto (sin <c>self</c>).</summary>
    public static void PopulateLayerEnvironment(LuaTable env, LayerProxy layer, WorldApi? world = null, InputApi? input = null, TimeApi? time = null, AudioApi? audio = null, UiApi? ui = null, GameApi? game = null, DebugDrawApi? debug = null, PhysicsApi? physics = null, AdsApi? ads = null)
    {
        env["layer"] = layer;
        SetWorld(env, world ?? new WorldApi());
        SetInput(env, input ?? new InputApi());
        SetTime(env, time ?? new TimeApi());
        SetAudio(env, audio ?? new AudioApi());
        SetPhysics(env, physics ?? new PhysicsApi());
        SetUi(env, ui ?? new UiApi());
        SetGame(env, game ?? new GameApi());
        SetAds(env, ads ?? new AdsApi());
        if (debug != null)
            SetDebug(env, debug);
        SetInputConstants(env);
    }
}

/// <summary>API world expuesta a Lua: jerarquía, búsqueda, instanciación. Conecta con la escena vía IWorldContext.</summary>
[LuaVisible]
public class WorldApi
{
    private IWorldContext? _context;
    private Func<GameObject, string?, string?, SelfProxy>? _createProxy;
    private Func<double, double, double, double, double, GameObject?, object?>? _raycastImpl;
    private TileMap? _playTileMap;
    private string? _projectDirectory;
    private string? _defaultTilesetJsonRelative;
    private readonly Dictionary<string, Tileset?> _tilesetCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Conecta el world con la escena. Llamar desde el motor/editor al iniciar (ej: modo Play).</summary>
    public void SetWorldContext(IWorldContext? context, Func<GameObject, string?, string?, SelfProxy>? createProxy)
    {
        _context = context;
        _createProxy = createProxy;
    }

    /// <summary>Implementación del raycast en coordenadas de casilla (modo Play). Si es null, <see cref="raycast"/> devuelve nil.</summary>
    public void SetRaycastImpl(Func<double, double, double, double, double, GameObject?, object?>? impl) => _raycastImpl = impl;

    /// <summary>Mapa en memoria durante Play + tileset por defecto (ruta JSON relativa al proyecto).</summary>
    public void ConfigurePlayTilemap(TileMap? map, string? projectDirectory, string? defaultTilesetJsonRelative)
    {
        _playTileMap = map;
        _projectDirectory = string.IsNullOrWhiteSpace(projectDirectory) ? null : projectDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        _defaultTilesetJsonRelative = string.IsNullOrWhiteSpace(defaultTilesetJsonRelative) ? null : defaultTilesetJsonRelative.Replace('\\', '/').Trim();
        _tilesetCache.Clear();
    }

    /// <summary>Mapa activo en Play (solo lectura externa).</summary>
    public TileMap? GetPlayTileMap() => _playTileMap;

    private int ResolveLayerIndex(string? layerName)
    {
        if (_playTileMap == null || string.IsNullOrWhiteSpace(layerName)) return -1;
        for (int i = 0; i < _playTileMap.Layers.Count; i++)
        {
            if (string.Equals(_playTileMap.Layers[i].Name, layerName.Trim(), StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    private Tileset? LoadTilesetForLayer(MapLayerDescriptor layer)
    {
        if (string.IsNullOrWhiteSpace(_projectDirectory)) return null;
        var rel = !string.IsNullOrWhiteSpace(layer.TilesetAssetPath)
            ? layer.TilesetAssetPath!.Replace('\\', '/').Trim()
            : _defaultTilesetJsonRelative;
        if (string.IsNullOrWhiteSpace(rel)) return null;
        var full = Path.GetFullPath(Path.Combine(_projectDirectory, rel));
        if (_tilesetCache.TryGetValue(full, out var cached)) return cached;
        var ts = TilesetPersistence.Load(full);
        _tilesetCache[full] = ts;
        return ts;
    }

    /// <summary>ID de catálogo en la celda (0 si vacío o sin catálogo). Coordenadas en casillas enteras.</summary>
    public virtual double getTile(double tileX, double tileY, string? layerName)
    {
        if (_playTileMap == null) return 0;
        int tx = (int)Math.Floor(tileX);
        int ty = (int)Math.Floor(tileY);
        int li = ResolveLayerIndex(layerName);
        if (li < 0) return 0;
        if (!_playTileMap.TryGetTile(li, tx, ty, out var data) || data == null) return 0;
        return data.CatalogTileId > 0 ? data.CatalogTileId : 0;
    }

    /// <summary>Pone tile por ID de catálogo (0 borra la celda). Requiere tileset en la capa o <see cref="ProjectInfo.DefaultTilesetPath"/>.</summary>
    public virtual void setTile(double tileX, double tileY, string? layerName, double catalogTileId)
    {
        if (_playTileMap == null) return;
        int tx = (int)Math.Floor(tileX);
        int ty = (int)Math.Floor(tileY);
        int li = ResolveLayerIndex(layerName);
        if (li < 0) return;
        int id = (int)Math.Round(catalogTileId);
        if (id <= 0)
        {
            _playTileMap.RemoveTileFromRuntime(li, tx, ty);
            return;
        }
        var layer = _playTileMap.Layers[li];
        var tileset = LoadTilesetForLayer(layer);
        if (tileset == null) return;
        var placed = TileCatalogHelper.CreatePlacedTile(tileset, id, layer);
        _playTileMap.SetTileFromRuntime(li, tx, ty, placed);
    }

    private static string ProxyInstanceId(GameObject go) =>
        !string.IsNullOrEmpty(go.InstanceId) ? go.InstanceId! : go.Name;

    private SelfProxy? ToProxy(GameObject? go)
    {
        if (go == null || _createProxy == null) return null;
        return _createProxy(go, ProxyInstanceId(go), null);
    }

    /// <summary>Resuelve <see cref="GameObject.InstanceId"/> (objetos.json) a entidad en escena.</summary>
    public GameObject? ResolveGameObjectByInstanceId(string? instanceId)
    {
        if (_context == null || string.IsNullOrWhiteSpace(instanceId)) return null;
        return _context.GetObjectByInstanceId(instanceId.Trim());
    }

    public virtual object? findObject(string name) => ToProxy(_context?.GetObjectByName(name ?? ""));
    public virtual object? findObjectByInstanceId(string? instanceId) => ToProxy(ResolveGameObjectByInstanceId(instanceId));
    public virtual object? getObjectByName(string name) => findObject(name);
    public virtual object? findByTag(string tag)
    {
        if (_context == null || _createProxy == null) return null;
        var list = new List<SelfProxy>();
        foreach (var go in _context.GetObjectsByTag(tag ?? ""))
            list.Add(_createProxy(go, ProxyInstanceId(go), tag ?? ""));
        return list;
    }
    public virtual object? getObjectByTag(string tag) => findByTag(tag);
    public virtual object? getObjects()
    {
        if (_context == null || _createProxy == null) return null;
        var list = new List<SelfProxy>();
        foreach (var go in _context.GetAllObjects())
            list.Add(_createProxy(go, ProxyInstanceId(go), ""));
        return list;
    }
    public virtual object? getAllObjects() => getObjects();
    public virtual object? findByPath(string path) => ToProxy(_context?.FindByPath(path ?? ""));

    public virtual object? spawn(string prefab, double x, double y) => instantiate(prefab, x, y, 0);
    /// <summary>Instancia un prefab/seed. <paramref name="variant"/> opcional: seed <c>id_nombre_variant</c> (ej. enemy + fast → enemy_fast).</summary>
    public virtual object? instantiate(string prefabName, double x, double y, double rotation = 0, string? variant = null) =>
        ToProxy(_context?.Instantiate(prefabName ?? "", x, y, rotation, variant));

    /// <summary>Objeto con la etiqueta más cercano a (x,y) en casillas, o nil.</summary>
    public virtual object? findNearestByTag(string tag, double x, double y)
    {
        if (_context == null || _createProxy == null) return null;
        GameObject? best = null;
        double bestD2 = double.PositiveInfinity;
        foreach (var go in _context.GetObjectsByTag(tag ?? ""))
        {
            double dx = go.Transform.X - x;
            double dy = go.Transform.Y - y;
            double d2 = dx * dx + dy * dy;
            if (d2 < bestD2)
            {
                bestD2 = d2;
                best = go;
            }
        }
        return ToProxy(best);
    }

    public virtual void destroy(object obj)
    {
        if (obj is SelfProxy proxy && _context != null)
            _context.Destroy(proxy.GameObject);
    }

    /// <summary>Mueve un objeto (proxy) a la posición indicada. Expuesto a Lua como world:setPosition(obj, x, y). Si obj es null/nil, no hace nada (evita crash si instantiate devolvió nil).</summary>
    public virtual void setPosition(object? obj, double x, double y)
    {
        if (obj == null || obj is not SelfProxy proxy) return;
        proxy.move(x, y);
    }

    public virtual object? getPlayer()
    {
        if (_context == null || _createProxy == null) return null;
        foreach (var go in _context.GetAllObjects())
        {
            if (string.Equals(go.Name, "Player", StringComparison.OrdinalIgnoreCase))
                return _createProxy(go, ProxyInstanceId(go), "");
        }
        return null;
    }

    /// <summary>
    /// Rayo desde el origen en dirección (dx,dy); (dx,dy) se normaliza. maxDistance en casillas.
    /// ignore: opcional, proxy (<see cref="SelfProxy"/>) cuyo collider se ignora (útil para no golpearse a uno mismo).
    /// Devuelve nil sin golpe, o <see cref="RaycastHitInfo"/> con .hit (SelfProxy), .distance, .x, .y del primer collider sólido.
    /// </summary>
    public virtual object? raycast(double originX, double originY, double dirX, double dirY, double maxDistance, object? ignore = null)
    {
        GameObject? skip = null;
        if (ignore is SelfProxy sp)
            skip = sp.GameObject;
        return _raycastImpl?.Invoke(originX, originY, dirX, dirY, maxDistance, skip);
    }

    /// <summary>
    /// DDA sobre el tilemap en Play: primer celda con colisión (<see cref="TileMap.IsCollisionAt"/>).
    /// Origen y dirección en casillas; (dirX,dirY) se normaliza; maxDistance en casillas. Sin mapa o sin golpe: nil.
    /// </summary>
    public virtual object? raycastTiles(double originX, double originY, double dirX, double dirY, double maxDistance)
    {
        if (_playTileMap == null || maxDistance <= 0) return null;
        var r = TileMapRaycast.Raycast(_playTileMap, originX, originY, dirX, dirY, maxDistance);
        if (!r.Hit) return null;
        return new TileRaycastHitInfo(r.TileX, r.TileY, r.Distance, r.HitX, r.HitY);
    }

    /// <summary>
    /// El impacto más cercano entre <see cref="raycastTiles"/> y <see cref="raycast"/> (objetos). <see cref="CombinedRaycastHitInfo.kind"/> es "tile" u "object".
    /// </summary>
    public virtual object? raycastCombined(double originX, double originY, double dirX, double dirY, double maxDistance, object? ignore = null)
    {
        GameObject? skip = null;
        if (ignore is SelfProxy sp)
            skip = sp.GameObject;

        object? objRes = _raycastImpl?.Invoke(originX, originY, dirX, dirY, maxDistance, skip);
        var objHit = objRes as RaycastHitInfo;

        TileRaycastResult tileR = default;
        bool hasTile = _playTileMap != null && maxDistance > 0;
        if (hasTile)
            tileR = TileMapRaycast.Raycast(_playTileMap!, originX, originY, dirX, dirY, maxDistance);

        if (objHit == null && !tileR.Hit) return null;
        if (objHit == null && tileR.Hit)
            return new CombinedRaycastHitInfo("tile", null, tileR.TileX, tileR.TileY, tileR.Distance, tileR.HitX, tileR.HitY);
        if (objHit != null && !tileR.Hit)
            return new CombinedRaycastHitInfo("object", objHit.hit, -1, -1, objHit.distance, objHit.x, objHit.y);

        if (tileR.Distance <= objHit!.distance)
            return new CombinedRaycastHitInfo("tile", null, tileR.TileX, tileR.TileY, tileR.Distance, tileR.HitX, tileR.HitY);
        return new CombinedRaycastHitInfo("object", objHit.hit, -1, -1, objHit.distance, objHit.x, objHit.y);
    }

    private ProjectInfo? _viewportProject;
    private Func<(double, double)>? _getCameraCenter;
    /// <summary>Último tamaño en píxeles del canvas del tab Juego (debe coincidir con <see cref="GameViewportRenderer"/> para que Lua y el render usen la misma resolución Auto).</summary>
    private double _playViewportSurfaceW;
    private double _playViewportSurfaceH;

    /// <summary>Play: rectángulo de vista lógica (misma lógica que el marco azul del editor / visor embebido).</summary>
    public void ConfigurePlayViewport(ProjectInfo? project, Func<(double, double)>? getCameraCenter)
    {
        _viewportProject = project;
        _getCameraCenter = getCameraCenter;
        _playViewportSurfaceW = 0;
        _playViewportSurfaceH = 0;
    }

    /// <summary>Llama el runner cada tick con el tamaño real del visor de juego (antes de scripts).</summary>
    public void SetPlayViewportSurfacePixels(double width, double height)
    {
        _playViewportSurfaceW = width > 0 ? width : 0;
        _playViewportSurfaceH = height > 0 ? height : 0;
    }

    private void GetPlayViewportWorldRect(out double left, out double top, out double widthTiles, out double heightTiles)
    {
        left = top = widthTiles = heightTiles = 0;
        var p = _viewportProject;
        if (p == null) return;
        var (cx, cy) = _getCameraCenter?.Invoke() ?? (0.0, 0.0);
        double sw = _playViewportSurfaceW > 0 ? _playViewportSurfaceW : 0;
        double sh = _playViewportSurfaceH > 0 ? _playViewportSurfaceH : 0;
        GameViewportMath.GetVisibleWorldRectFromCenter(p, cx, cy, out left, out top, out widthTiles, out heightTiles, sw, sh, 1.0);
    }

    public double getPlayViewportLeft()
    {
        GetPlayViewportWorldRect(out var l, out _, out _, out _);
        return l;
    }

    public double getPlayViewportTop()
    {
        GetPlayViewportWorldRect(out _, out var t, out _, out _);
        return t;
    }

    public double getPlayViewportWidthTiles()
    {
        GetPlayViewportWorldRect(out _, out _, out var w, out _);
        return w;
    }

    public double getPlayViewportHeightTiles()
    {
        GetPlayViewportWorldRect(out _, out _, out _, out var h);
        return h;
    }
}

/// <summary>API input expuesta a Lua.</summary>
[LuaVisible]
public class InputApi
{
    public virtual bool isKeyDown(object key) => false;
    public virtual bool isKeyPressed(object key) => false;
    public virtual bool isMouseDown(object button) => false;
    public virtual double mouseX => 0;
    public virtual double mouseY => 0;
}

/// <summary>API time expuesta a Lua: time.delta, time.frame, time.seconds (tiempo total).</summary>
[LuaVisible]
public class TimeApi
{
    public double delta { get; set; }
    public double time { get; set; }
    /// <summary>Tiempo total en segundos (alias de time).</summary>
    public double seconds => time;
    public long frame { get; set; }
    public double scale { get; set; } = 1.0;
}

/// <summary>API audio expuesta a Lua. Por ID (ej. "sfx/jump"). El host puede inyectar una implementación real.</summary>
[LuaVisible]
public class AudioApi
{
    public virtual void play(string id) { }
    public virtual void play(string id, double volume) { }
    public virtual void playMusic(string id) { }
    /// <summary>Música por id del manifiesto; <paramref name="loop"/> fuerza bucle aunque el clip no lo declare.</summary>
    public virtual void playMusic(string id, bool loop) => playMusic(id);
    public virtual void playSfx(string id) => play(id);
    /// <param name="fadeSeconds">0 = parada inmediata.</param>
    public virtual void stopMusic(double fadeSeconds = 0) { }
    /// <summary><paramref name="bus"/>: master, music o sfx. <paramref name="value"/> en 0..1.</summary>
    public virtual void setVolume(string bus, double value) { }
    public virtual void stop(string id) { }
    public virtual void stopAll() { }
    public virtual void setMasterVolume(double v) { }
}

/// <summary>API physics en Lua: simulación/consultas de colliders. En Play usar <see cref="PlayScenePhysicsApi"/>.</summary>
[LuaVisible]
public class PhysicsApi
{
    /// <summary>Segmento (x1,y1)→(x2,y2) en casillas contra colliders sólidos; nil si no hay golpe.</summary>
    public virtual object? raycast(double x1, double y1, double x2, double y2) => null;

    /// <summary>Círculo en casillas; devuelve lista de <see cref="SelfProxy"/> (incluye triggers).</summary>
    public virtual object? overlapCircle(double centerX, double centerY, double radius) => null;
}

/// <summary>API ui: Show/Hide/SetFocus por Canvas; Get(canvasId, elementId) para binding. Input y render según plan (UI antes que mundo).</summary>
[LuaVisible]
public class UiApi
{
    private UIRuntimeBackend? _backend;

    public void SetBackend(UIRuntimeBackend? backend) => _backend = backend;

    public void show(string id) => _backend?.Show(id ?? "");
    public void hide(string id) => _backend?.Hide(id ?? "");
    public void setFocus(string id) => _backend?.SetFocus(id ?? "");
    /// <summary>Guarda estado visible/focus actual (para menús anidados).</summary>
    public void pushState() => _backend?.PushState();
    /// <summary>Restaura estado previo guardado con pushState().</summary>
    public bool popState() => _backend?.PopState() ?? false;
    /// <summary>Obtiene elemento por Canvas y ID (para scripts). Retorna el elemento o nil.</summary>
    public object? get(string canvasId, string elementId)
    {
        var el = _backend?.GetElement(canvasId ?? "", elementId ?? "");
        return el;
    }
    /// <summary>Enlaza un evento (click, hover, pressed, released) a un elemento. Solo el Canvas con focus recibe input.</summary>
    public void bind(string canvasId, string elementId, string eventName, object? callback)
    {
        _backend?.Bind(canvasId ?? "", elementId ?? "", eventName ?? "", callback);
    }
}

/// <summary>API game en Lua: escenas, salida y RNG opcional.</summary>
[LuaVisible]
public class GameApi
{
    private Random _rng = new();

    /// <summary>Host callback to change scene from scripts (optional).</summary>
    public Action<string>? OnLoadScene { get; set; }
    /// <summary>Host callback to quit simulation/game (optional).</summary>
    public Action? OnQuit { get; set; }

    public void setRandomSeed(int seed) => _rng = new Random(seed);

    public int randomInt(int minInclusive, int maxExclusive) => _rng.Next(minInclusive, maxExclusive);

    public double randomDouble() => _rng.NextDouble();

    public void loadScene(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        OnLoadScene?.Invoke(name);
    }

    public void quit()
    {
        OnQuit?.Invoke();
    }
}

/// <summary>Constantes de teclas para Lua.</summary>
[LuaVisible]
public class KeyConstants
{
    public string W => "W";
    public string A => "A";
    public string S => "S";
    public string D => "D";
    public string Left => "LEFT";
    public string Right => "RIGHT";
    public string Up => "UP";
    public string Down => "DOWN";
    public string Space => "SPACE";
    public string E => "E";
    public string Q => "Q";
    public string F => "F";
    public string Enter => "ENTER";
    public string Shift => "SHIFT";
    public string Ctrl => "CTRL";
    public string Escape => "ESCAPE";
}

/// <summary>Constantes de botones del ratón para Lua.</summary>
[LuaVisible]
public class MouseConstants
{
    public int Left => 0;
    public int Right => 1;
}
