using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;
using FUEngine.Runtime;
using FUEngine.Core;
using FUEngine.Editor;
using FUEngine.Input;
using FUEngine.Service;

namespace FUEngine;

/// <summary>
/// Ejecuta el modo Play: convierte ObjectLayer a GameObjects, crea LuaScriptRuntime,
/// WorldApi, ciclo onAwake → onStart (1×) → onUpdate/onLateUpdate, cola de spawn de seeds y destrucción al final del frame.
/// </summary>
public sealed class PlayModeRunner
{
    private readonly ProjectInfo _project;
    private readonly ObjectLayer _objectLayer;
    private readonly ScriptRegistry _scriptRegistry;
    private readonly Action _openConsole;
    private LuaScriptRuntime? _runtime;
    private WorldContextFromList? _worldContext;
    private UIRuntimeBackend? _uiBackend;
    private LocalizationRuntime? _localization;
    private ObjectLayer? _activeLayer;
    private readonly List<GameObject> _sceneObjects = new();
    private readonly Dictionary<GameObject, ObjectInstance> _goToInstance = new();
    private readonly List<(GameObject go, ScriptComponent sc, string scriptPath, string scriptId, string instanceId, string? tag, IReadOnlyList<ScriptPropertyEntry>? props)> _scriptBindings = new();
    /// <summary>Seeds instanciados en Lua: binding de scripts al inicio del siguiente tick (o el mismo si anidan Awake).</summary>
    private readonly List<(GameObject go, ObjectInstance inst)> _pendingSpawnBinds = new();
    /// <summary>Pares dirigidos (id instancia del trigger, id del otro) para detectar enter/exit.</summary>
    private readonly HashSet<(string triggerId, string otherId)> _triggerDirectedPairsLastFrame = new();
    private DispatcherTimer? _timer;
    private bool _paused;
    private DateTime _lastTick;
    private long _frameCount;
    private int _fpsFrames;
    private double _fpsAccumTime;
    private double _gameTimeSeconds;
    private double _lastDeltaTime;
    private double _currentFps;
    private bool _pausedForBreakpoint;
    private ScriptHotReloadWatcher? _luaFileWatcher;
    private readonly TileMap? _editorTileMapSnapshot;
    private TileMap? _playTileMap;
    private readonly PlayKeyboardSnapshot _keyboardSnap;
    /// <summary>Devuelve el tamaño en píxeles del canvas de play del tab Juego (para alinear <c>world:getPlayViewport*</c> con el render).</summary>
    public Func<(double Width, double Height)>? GetPlayViewportSurfacePixels { get; set; }
    private double _cameraWorldX;
    private double _cameraWorldY;
    private bool _warnedNativeInputNoMap;
    private readonly HashSet<string> _warnedNativeInputColliderByInstance = new();
    private readonly List<AnimationDefinition> _runtimeAnimations = new();
    private TextureAssetCache? _nativeAnimTextureProbe;
    private PlayNaudioAudioEngine? _playAudioEngine;
    private List<SeedDefinition> _runtimeSeeds = new();
    private readonly PhysicsWorld _physicsWorld = new();
    private List<TriggerZone> _mapTriggerZones = new();
    private readonly HashSet<string> _mapZonesPlayerInside = new();
    private readonly Dictionary<string, GameObject> _mapZoneTickHosts = new();
    private UiAccessibilityTts? _accessibilityTts;

    public PlayModeRunner(ProjectInfo project, ObjectLayer objectLayer, ScriptRegistry scriptRegistry, Action openConsole, UIRoot? uiRoot = null, TileMap? editorTileMapSnapshot = null, PlayKeyboardSnapshot? playKeyboard = null)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _objectLayer = objectLayer ?? throw new ArgumentNullException(nameof(objectLayer));
        _scriptRegistry = scriptRegistry ?? throw new ArgumentNullException(nameof(scriptRegistry));
        _openConsole = openConsole ?? (() => { });
        _uiRoot = uiRoot;
        _editorTileMapSnapshot = editorTileMapSnapshot;
        _keyboardSnap = playKeyboard ?? new PlayKeyboardSnapshot();
    }

    private readonly UIRoot? _uiRoot;

    /// <summary>Mapa de tiles en memoria durante Play (copia del editor o carga desde disco).</summary>
    public TileMap? GetPlayTileMap() => _playTileMap;

    public bool IsRunning => _timer != null && _timer.IsEnabled;
    public bool IsPaused => _paused;
    public long FrameCount => _frameCount;
    public double GameTimeSeconds => _gameTimeSeconds;
    public double LastDeltaTimeSeconds => _lastDeltaTime;
    public double CurrentFps => _currentFps;

    /// <summary>Máquina de escribir UI durante Play.</summary>
    public UiTypewriterRuntime UiTypewriter { get; } = new();

    /// <summary>TTS opcional (Windows) cuando <see cref="EngineSettings.UiAccessibilityTtsEnabled"/> está activo.</summary>
    public UiAccessibilityTts? AccessibilityTts => _accessibilityTts;

    /// <summary>Tablas de <c>Data/localization.json</c> en Play (null si no se inició).</summary>
    public LocalizationRuntime? GetLocalizationRuntime() => _localization;

    /// <summary>
    /// Centro de cámara en casillas mundo para el visor WPF: seguimiento nativo al protagonista, o centro del marco del editor,
    /// salvo cuando la pausa o la UI modal bloquean la sincronización con el editor.
    /// </summary>
    public bool TryGetCameraCenterOverride(out double worldX, out double worldY)
    {
        if (_project.UseNativeCameraFollow)
        {
            worldX = _cameraWorldX;
            worldY = _cameraWorldY;
            return true;
        }
        if (ShouldApplyEditorViewportToPlayCamera())
        {
            worldX = _project.EditorViewportCenterWorldX;
            worldY = _project.EditorViewportCenterWorldY;
            return true;
        }
        worldX = 0;
        worldY = 0;
        return false;
    }

    /// <summary>Play usa el centro del marco azul del mapa salvo pausa o stack de UI (menús modales).</summary>
    private bool ShouldApplyEditorViewportToPlayCamera()
    {
        if (_paused) return false;
        if (_uiBackend != null && _uiBackend.StateStackDepth > 0) return false;
        return true;
    }

    /// <summary>Nombres de los objetos de la escena en ejecución (para panel de jerarquía en Play).</summary>
    public IReadOnlyList<string> GetSceneObjectNames()
    {
        var list = new List<string>(_sceneObjects.Count);
        foreach (var go in _sceneObjects)
            list.Add(go.Name ?? "(sin nombre)");
        return list;
    }

    /// <summary>GameObjects de la escena en ejecución (p. ej. jerarquía del tab Juego).</summary>
    public IReadOnlyList<GameObject> GetSceneObjects() => _sceneObjects;

    /// <summary>Inicia el juego. useMainScene: true = carga objetos desde disco (escena principal); false = escena actual del editor.</summary>
    public void Start(bool useMainScene = false)
    {
        if (_runtime != null) return;
        _openConsole();
        _sceneObjects.Clear();
        _scriptBindings.Clear();
        _goToInstance.Clear();
        _triggerDirectedPairsLastFrame.Clear();
        _pendingSpawnBinds.Clear();

        if (useMainScene && !string.IsNullOrEmpty(_project.MainSceneObjectsPath) && File.Exists(_project.MainSceneObjectsPath))
        {
            try
            {
                _activeLayer = ObjectsSerialization.Load(_project.MainSceneObjectsPath);
                EditorLog.Info($"Play: escena principal cargada desde {_project.MainSceneObjectsPath}.", "Play");
            }
            catch (Exception ex)
            {
                EditorLog.Warning($"No se pudo cargar escena principal: {ex.Message}. Se usa escena actual.", "Play");
                _activeLayer = _objectLayer;
            }
        }
        else
            _activeLayer = _objectLayer;

        var projectDir = _project.ProjectDirectory ?? "";
        var dispatcher = System.Windows.Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        _playAudioEngine = new PlayNaudioAudioEngine(projectDir, dispatcher);
        _playAudioEngine.LoadManifest(_project.AudioManifestAbsolutePath);
        _playAudioEngine.SetVolumes(_project.MasterVolume, _project.MusicVolume, _project.SfxVolume);
        if (!File.Exists(_project.AudioManifestAbsolutePath))
            EditorLog.Info($"Play: sin manifiesto de audio ({_project.AudioManifestPath ?? "audio.json"}). Crea el archivo o ajusta la ruta en Configuración del proyecto.", "Play");

        _runtime = new LuaScriptRuntime(projectDir, new WpfPlayAudioApi(_playAudioEngine));
        PluginLoader.DiagnosticLog = msg => EditorLog.Info(msg ?? "", "Plugins");
        PluginLoader.LoadFromDirectory(Path.Combine(projectDir, "Plugins"));
        _runtime.SetLuaLogSink((level, msg) =>
        {
            var m = msg ?? "";
            if (string.Equals(level, "warn", StringComparison.OrdinalIgnoreCase))
                EditorLog.Warning(m, "Lua");
            else if (string.Equals(level, "error", StringComparison.OrdinalIgnoreCase))
                EditorLog.Error(m, "Lua");
            else
                EditorLog.Info(m, "Lua");
        });
        _worldContext = new WorldContextFromList { DeferDestroy = true };
        _runtimeSeeds = TryLoadSeedsForPlay();
        _mapTriggerZones = TryLoadMapTriggerZones();
        _worldContext.TryExpandPrefab = (n, px, py, rot, v) => TryInstantiateSeedPrefab(n, px, py, rot, v);

        _runtimeAnimations.Clear();
        try
        {
            if (!string.IsNullOrEmpty(_project.AnimacionesPath) && File.Exists(_project.AnimacionesPath))
                _runtimeAnimations.AddRange(AnimationSerialization.Load(_project.AnimacionesPath));
        }
        catch (Exception ex)
        {
            EditorLog.Warning($"Play: no se pudieron cargar animaciones.json: {ex.Message}", "Play");
        }
        _nativeAnimTextureProbe = string.IsNullOrEmpty(projectDir) ? null : new TextureAssetCache(projectDir);
        _runtime.SetSpriteAnimationCallbacks(
            (go, clip) => NativeAutoAnimationApplier.TryApplyClipForGameObject(_project, go, clip, _runtimeAnimations, _nativeAnimTextureProbe),
            go =>
            {
                var s = go.GetComponent<SpriteComponent>();
                if (s == null) return;
                s.AnimationFramesPerSecond = 0;
                s.NativeAutoAnimationKey = null;
                s.AnimationTimeAccum = 0;
            });

        foreach (var inst in _activeLayer.Instances)
        {
            var go = ObjectInstanceToGameObject(inst);
            _sceneObjects.Add(go);
            _goToInstance[go] = inst;
            _worldContext.Objects.Add(go);
        }

        var worldApi = new WorldApi();
        worldApi.SetWorldContext(_worldContext, _runtime.GetProxyFactory());
        worldApi.SetRaycastImpl((ox, oy, dx, dy, maxD, skip) => RaycastScene(ox, oy, dx, dy, maxD, skip));
        _playTileMap = _editorTileMapSnapshot != null
            ? _editorTileMapSnapshot.Clone()
            : LoadPlayTileMapFromDisk();
        worldApi.ConfigurePlayTilemap(_playTileMap, projectDir, _project.DefaultTilesetPath);
        worldApi.ConfigurePlayViewport(_project, () =>
        {
            if (TryGetCameraCenterOverride(out var wx, out var wy))
                return (wx, wy);
            return (_cameraWorldX, _cameraWorldY);
        });
        _runtime.SetWorldApi(worldApi);
        _runtime.SetInputApi(new WpfPlayInputApi(_keyboardSnap));
        var gameApi = new GameApi();
        if (_project.RuntimeRandomSeed is int seedFixed)
            gameApi.setRandomSeed(seedFixed);
        _runtime.SetGameApi(gameApi);
        _runtime.SetPhysicsApi(new PlayScenePhysicsApi(() => _sceneObjects, CreateProxyFor));

        _uiBackend = new UIRuntimeBackend(_uiRoot);
        var uiApi = new UiApi();
        uiApi.SetBackend(_uiBackend);
        _localization = new LocalizationRuntime();
        _localization.LoadFromProject(projectDir);
        _localization.ApplySystemLocale();
        uiApi.SetLocaleProvider(_localization);
        _runtime.SetUiApi(uiApi);

        var adsApi = new SimulatedAdsApi(
            a => dispatcher.BeginInvoke(a),
            msg => EditorLog.Info(msg ?? "", "Ads"));
        _runtime.SetAdsApi(adsApi);

        IReadOnlyDictionary<Type, object>? pluginHostServices = null;
        if (ServiceLocator.TryGet<IEditorLog>() is { } editorLog)
            pluginHostServices = new Dictionary<Type, object> { [typeof(IEditorLog)] = editorLog };
        _runtime.RegisterEnginePlugins(_project, pluginHostServices);

        _runtime.ScriptReloaded += OnScriptReloaded;
        _runtime.ScriptError = (path, line, msg) =>
            EditorLog.Error(line > 0 ? $"{path}:{line} {msg}" : $"{path} {msg}", "Lua", path, line > 0 ? line : null);
        _runtime.PrintOutput = msg => EditorLog.Log(msg ?? "", LogLevel.Lua, "Lua");

        foreach (var go in _sceneObjects)
        {
            _goToInstance.TryGetValue(go, out var oi);
            TryBindScriptsForGameObject(go, oi);
        }

        BindLayerScripts();

        if (!string.IsNullOrWhiteSpace(_project.StartupMusicPath))
            _playAudioEngine.PlayMusicFromPath(_project.StartupMusicPath, loop: true);

        var hero0 = NativeProtagonistController.FindProtagonist(_sceneObjects, _project, _goToInstance);
        _cameraWorldX = hero0?.Transform.X ?? 0;
        _cameraWorldY = hero0?.Transform.Y ?? 0;

        _lastTick = DateTime.UtcNow;
        _frameCount = 0;
        _fpsFrames = 0;
        _fpsAccumTime = 0;
        _gameTimeSeconds = 0;
        UiTypewriter.Clear();
        UiTypewriter.TypewriterLineComplete -= OnTypewriterLineCompleteForTts;
        _accessibilityTts?.Dispose();
        _accessibilityTts = null;
        if (EngineSettings.Load().UiAccessibilityTtsEnabled)
        {
            _accessibilityTts = new UiAccessibilityTts { IsEnabled = true };
            _accessibilityTts.EnsureSynthesizer();
            UiTypewriter.TypewriterLineComplete += OnTypewriterLineCompleteForTts;
        }
        _warnedNativeInputNoMap = false;
        _warnedNativeInputColliderByInstance.Clear();
        _paused = false;
        _timer = new DispatcherTimer(DispatcherPriority.Send) { Interval = TimeSpan.FromMilliseconds(1000.0 / Math.Max(1, _project.Fps)) };
        _timer.Tick += Timer_Tick;
        _timer.Start();
        var sceneLabel = useMainScene ? "escena principal" : "escena actual";
        EditorLog.Info($"Modo Play iniciado ({sceneLabel}) · {_sceneObjects.Count} objetos, {_scriptBindings.Count} scripts, {_mapTriggerZones.Count} zonas (triggerZones.json).", "Play");

        StartScriptHotReloadWatcher();
    }

    private void StartScriptHotReloadWatcher()
    {
        _luaFileWatcher?.Dispose();
        _luaFileWatcher = null;
        var dir = _project.ProjectDirectory;
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) return;
        try
        {
            _luaFileWatcher = new ScriptHotReloadWatcher(dir, OnScriptSavedFromDisk);
            _luaFileWatcher.Start();
            EditorLog.Info("Hot reload: vigilando cambios en archivos .lua del proyecto.", "Lua");
        }
        catch (Exception ex)
        {
            EditorLog.Warning($"Hot reload por disco no disponible: {ex.Message}", "Lua");
        }
    }

    /// <summary>Recarga por FileSystemWatcher (editor externo, etc.). Mismo pipeline que guardar en el IDE.</summary>
    private void OnScriptSavedFromDisk(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;
        EditorLog.Info($"Cambio en disco · recargando {relativePath}", "Lua");
        OnScriptSaved(relativePath);
    }

    private static IReadOnlyList<ScriptPropertyEntry>? GetScriptPropertiesFor(ObjectInstance? inst, string scriptId)
    {
        if (inst?.ScriptProperties == null) return null;
        var set = inst.ScriptProperties.FirstOrDefault(s => string.Equals(s.ScriptId, scriptId, StringComparison.OrdinalIgnoreCase));
        return set?.Properties;
    }

    private List<SeedDefinition> TryLoadSeedsForPlay()
    {
        try
        {
            var path = _project.SeedsPath;
            if (File.Exists(path))
                return SeedSerialization.Load(path);
        }
        catch (Exception ex)
        {
            EditorLog.Warning($"Play: no se pudieron cargar seeds ({_project.SeedsPath}): {ex.Message}", "Play");
        }
        return new List<SeedDefinition>();
    }

    private List<TriggerZone> TryLoadMapTriggerZones()
    {
        try
        {
            if (File.Exists(_project.TriggerZonesPath))
                return TriggerZoneSerialization.Load(_project.TriggerZonesPath);
        }
        catch (Exception ex)
        {
            EditorLog.Warning($"Play: no se pudieron cargar zonas trigger ({_project.TriggerZonesPath}): {ex.Message}", "Play");
        }

        return new List<TriggerZone>();
    }

    private bool TryResolveRegisteredScriptPath(string? scriptId, out string path)
    {
        path = "";
        if (string.IsNullOrWhiteSpace(scriptId)) return false;
        var id = scriptId.Trim();
        var def = _scriptRegistry.Get(id);
        var raw = def?.Path?.Trim();
        if (string.IsNullOrEmpty(raw))
            raw = $"Scripts/{id}.lua";
        path = ScriptLoader.NormalizeRelativePath(raw);
        return !string.IsNullOrEmpty(path);
    }

    private void RunMapZoneOneShotScript(TriggerZone zone, string? scriptId, string eventName, object playerProxy)
    {
        if (_runtime == null || string.IsNullOrWhiteSpace(scriptId)) return;
        if (!TryResolveRegisteredScriptPath(scriptId, out var path)) return;
        var rid = scriptId.Trim();
        var go = new GameObject
        {
            Name = $"__mapZoneEvt_{zone.Id}_{eventName}_{Guid.NewGuid():N}",
            InstanceId = zone.Id,
            RuntimeActive = true,
        };
        go.Transform.X = (float)(zone.X + zone.Width * 0.5);
        go.Transform.Y = (float)(zone.Y + zone.Height * 0.5);
        try
        {
            var inst = _runtime.CreateInstance(path, rid, go, zone.Id, null, null);
            _runtime.InvokeOnStartFor(inst);
            _runtime.NotifyScripts(go, eventName, playerProxy);
            _runtime.RemoveInstance(inst);
        }
        catch (Exception ex)
        {
            var line = TryParseLineFromLuaMessage(ex.Message);
            EditorLog.Error($"Play: zona «{zone.Nombre}» ({eventName}): {ex.Message}", "Lua", path, line > 0 ? line : null);
        }
    }

    private void EnsureMapZoneTickHost(TriggerZone zone)
    {
        if (_runtime == null || _worldContext == null) return;
        if (_mapZoneTickHosts.ContainsKey(zone.Id)) return;
        if (string.IsNullOrWhiteSpace(zone.ScriptIdOnTick) || !TryResolveRegisteredScriptPath(zone.ScriptIdOnTick, out var path)) return;
        var rid = zone.ScriptIdOnTick.Trim();
        var go = new GameObject
        {
            Name = $"__mapZoneTick_{zone.Nombre}_{zone.Id}",
            InstanceId = zone.Id + "_tick",
            RuntimeActive = true,
        };
        go.Transform.X = (float)(zone.X + zone.Width * 0.5);
        go.Transform.Y = (float)(zone.Y + zone.Height * 0.5);
        _sceneObjects.Add(go);
        _worldContext.Objects.Add(go);
        try
        {
            var inst = _runtime.CreateInstance(path, rid, go, zone.Id + "_tick", null, null);
            _runtime.InvokeOnStartFor(inst);
            _mapZoneTickHosts[zone.Id] = go;
        }
        catch (Exception ex)
        {
            _sceneObjects.Remove(go);
            _worldContext.Objects.Remove(go);
            var line = TryParseLineFromLuaMessage(ex.Message);
            EditorLog.Error($"Play: script tick de zona «{zone.Nombre}»: {ex.Message}", "Lua", path, line > 0 ? line : null);
        }
    }

    private void RemoveMapZoneTickHost(string zoneId)
    {
        if (_runtime == null || _worldContext == null) return;
        if (!_mapZoneTickHosts.TryGetValue(zoneId, out var go)) return;
        _mapZoneTickHosts.Remove(zoneId);
        _runtime.InvokeOnDestroyPhase(go);
        foreach (var si in _runtime.GetScriptInstancesFor(go).ToList())
            _runtime.RemoveInstance(si);
        _sceneObjects.Remove(go);
        _worldContext.Objects.Remove(go);
    }

    private void RunMapZoneEnter(TriggerZone zone, object playerProxy)
    {
        if (!string.IsNullOrWhiteSpace(zone.ScriptIdOnEnter))
            RunMapZoneOneShotScript(zone, zone.ScriptIdOnEnter, KnownEvents.OnZoneEnter, playerProxy);
        if (!string.IsNullOrWhiteSpace(zone.ScriptIdOnTick))
            EnsureMapZoneTickHost(zone);
    }

    private void RunMapZoneExit(TriggerZone zone, object playerProxy)
    {
        RemoveMapZoneTickHost(zone.Id);
        if (!string.IsNullOrWhiteSpace(zone.ScriptIdOnExit))
            RunMapZoneOneShotScript(zone, zone.ScriptIdOnExit, KnownEvents.OnZoneExit, playerProxy);
    }

    private void ProcessMapTriggerZones(GameObject? hero)
    {
        if (_runtime == null || _mapTriggerZones.Count == 0 || hero == null) return;

        int px = (int)Math.Floor(hero.Transform.X);
        int py = (int)Math.Floor(hero.Transform.Y);
        var playerProxy = CreateProxyFor(hero);

        foreach (var zone in _mapTriggerZones)
        {
            bool inside = zone.Contains(px, py);
            bool wasInside = _mapZonesPlayerInside.Contains(zone.Id);

            if (inside && !wasInside)
            {
                _mapZonesPlayerInside.Add(zone.Id);
                RunMapZoneEnter(zone, playerProxy);
            }
            else if (!inside && wasInside)
            {
                _mapZonesPlayerInside.Remove(zone.Id);
                RunMapZoneExit(zone, playerProxy);
            }
        }

        foreach (var zone in _mapTriggerZones)
        {
            if (!_mapZonesPlayerInside.Contains(zone.Id)) continue;
            if (string.IsNullOrWhiteSpace(zone.ScriptIdOnTick)) continue;
            if (!_mapZoneTickHosts.ContainsKey(zone.Id))
                EnsureMapZoneTickHost(zone);
        }
    }

    private void InvalidateMapZoneTickScriptsForPath(string normalizedRelativePath)
    {
        foreach (var zone in _mapTriggerZones)
        {
            if (string.IsNullOrWhiteSpace(zone.ScriptIdOnTick)) continue;
            if (!TryResolveRegisteredScriptPath(zone.ScriptIdOnTick, out var path)) continue;
            if (!string.Equals(ScriptLoader.NormalizeRelativePath(path), normalizedRelativePath, StringComparison.OrdinalIgnoreCase))
                continue;
            if (_mapZonesPlayerInside.Contains(zone.Id))
                RemoveMapZoneTickHost(zone.Id);
        }
    }

    private SeedDefinition? FindSeedByName(string name, string? variant)
    {
        if (!string.IsNullOrWhiteSpace(variant))
        {
            var composite = $"{name}_{variant.Trim()}";
            foreach (var s in _runtimeSeeds)
            {
                if (string.Equals(s.Id, composite, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(s.Nombre, composite, StringComparison.OrdinalIgnoreCase))
                    return s;
            }
        }
        foreach (var s in _runtimeSeeds)
        {
            if (string.Equals(s.Id, name, StringComparison.OrdinalIgnoreCase)) return s;
        }
        foreach (var s in _runtimeSeeds)
        {
            if (string.Equals(s.Nombre, name, StringComparison.OrdinalIgnoreCase)) return s;
        }
        return null;
    }

    private GameObject? TryInstantiateSeedPrefab(string prefabName, double x, double y, double rotation, string? variant)
    {
        if (_runtime == null || _worldContext == null || _activeLayer == null) return null;
        var seed = FindSeedByName(prefabName, variant);
        if (seed?.Objects == null || seed.Objects.Count == 0) return null;

        GameObject? first = null;
        double rad = rotation * (Math.PI / 180.0);
        double cos = Math.Cos(rad), sin = Math.Sin(rad);
        foreach (var entry in seed.Objects)
        {
            if (string.IsNullOrWhiteSpace(entry.DefinitionId)) continue;
            var defId = entry.DefinitionId.Trim();
            var ox = entry.OffsetX;
            var oy = entry.OffsetY;
            double rx = ox * cos - oy * sin;
            double ry = ox * sin + oy * cos;
            double wx = x + rx;
            double wy = y + ry;
            double rotDeg = rotation + entry.Rotation;

            var def = _activeLayer.GetDefinition(defId);
            var mergedTags = new List<string>();
            if (seed.Tags != null && seed.Tags.Count > 0)
                mergedTags.AddRange(seed.Tags);

            var synth = new ObjectInstance
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                DefinitionId = defId,
                X = wx,
                Y = wy,
                Rotation = rotDeg,
                Nombre = string.IsNullOrEmpty(entry.Nombre) ? (def?.Nombre ?? defId) : entry.Nombre!,
                Tags = mergedTags,
            };

            var go = ObjectInstanceToGameObject(synth);
            _sceneObjects.Add(go);
            _worldContext.Objects.Add(go);
            _goToInstance[go] = synth;
            _pendingSpawnBinds.Add((go, synth));
            first ??= go;
        }

        return first;
    }

    private void TryBindScriptsForGameObject(GameObject go, ObjectInstance? inst)
    {
        if (_runtime == null) return;
        foreach (var sc in go.Components.OfType<ScriptComponent>())
        {
            var path = sc.ScriptPath ?? _scriptRegistry.Get(sc.ScriptId ?? "")?.Path;
            if (string.IsNullOrEmpty(path)) continue;
            var scriptId = sc.ScriptId ?? "";
            var instanceId = inst?.InstanceId ?? go.Name;
            var tag = inst?.Tags?.FirstOrDefault() ?? go.Tags?.FirstOrDefault();
            var props = GetScriptPropertiesFor(inst, scriptId);
            try
            {
                var scriptInstance = _runtime.CreateInstance(path, scriptId, go, instanceId, tag, props);
                sc.ScriptInstanceHandle = scriptInstance;
                _scriptBindings.Add((go, sc, path, scriptId, instanceId, tag, props));
            }
            catch (Exception ex)
            {
                var line = TryParseLineFromLuaMessage(ex.Message);
                EditorLog.Error($"Play: no se pudo crear instancia de script '{scriptId}' en {go.Name}: {ex.Message}", "Lua", path, line > 0 ? line : null);
            }
        }
    }

    private GameObject ObjectInstanceToGameObject(ObjectInstance inst)
    {
        var def = _activeLayer?.GetDefinition(inst.DefinitionId);
        var go = new GameObject
        {
            Name = string.IsNullOrEmpty(inst.Nombre) ? inst.InstanceId : inst.Nombre,
            InstanceId = inst.InstanceId,
            RenderOrder = inst.LayerOrder,
            RuntimeActive = inst.Enabled,
        };
        if (inst.Tags != null && inst.Tags.Count > 0)
            go.Tags = new List<string>(inst.Tags);
        else if (def?.Tags != null && def.Tags.Count > 0)
            go.Tags = new List<string>(def.Tags);
        go.Transform.X = (float)inst.X;
        go.Transform.Y = (float)inst.Y;
        go.Transform.RotationDegrees = (float)inst.Rotation;
        go.Transform.ScaleX = (float)inst.ScaleX;
        go.Transform.ScaleY = (float)inst.ScaleY;

        TryAddSpriteFromInstance(go, inst, def);
        ApplySpriteRendererInstanceData(go, inst);
        TryApplyDefaultAnimationIfAny(go, inst);

        var scriptIds = inst.GetScriptIds(def);
        foreach (var scriptId in scriptIds)
        {
            var scriptDef = _scriptRegistry.Get(scriptId);
            var path = scriptDef?.Path ?? $"Scripts/{scriptId}.lua";
            go.Components.Add(new ScriptComponent { ScriptId = scriptId, ScriptPath = path, Enabled = true });
        }

        TryAddColliderFromInstance(go, inst, def);
        if (inst.PointLightEnabled)
        {
            go.AddComponent(new LightComponent
            {
                Radius = inst.PointLightRadius > 0 ? inst.PointLightRadius : 5f,
                Intensity = inst.PointLightIntensity > 0 ? inst.PointLightIntensity : 1f,
                ColorHex = string.IsNullOrWhiteSpace(inst.PointLightColorHex) ? "#ffffff" : inst.PointLightColorHex,
            });
        }

        if (inst.RigidbodyEnabled)
        {
            go.AddComponent(new RigidbodyComponent
            {
                Mass = inst.RigidbodyMass > 0 ? inst.RigidbodyMass : 1f,
                GravityScale = inst.RigidbodyGravityScale,
                Drag = inst.RigidbodyDrag,
                FreezeRotation = inst.RigidbodyFreezeRotation,
            });
        }

        if (inst.HealthEnabled)
        {
            var hm = inst.HealthMax > 0 ? inst.HealthMax : 100f;
            var hc = inst.HealthCurrent > 0 ? Math.Min(inst.HealthCurrent, hm) : hm;
            go.AddComponent(new HealthComponent { MaxHealth = hm, CurrentHealth = hc, IsInvulnerable = inst.HealthInvulnerable });
        }

        if (inst.ProximitySensorEnabled)
        {
            go.AddComponent(new ProximitySensorComponent
            {
                DetectionRangeTiles = inst.ProximityDetectionRangeTiles > 0 ? inst.ProximityDetectionRangeTiles : 1f,
                TargetTag = string.IsNullOrWhiteSpace(inst.ProximityTargetTag) ? "player" : inst.ProximityTargetTag.Trim(),
            });
        }

        if (inst.CameraTargetEnabled)
            go.AddComponent(new CameraTargetComponent());

        if (inst.AudioSourceEnabled)
        {
            go.AddComponent(new AudioSourceComponent
            {
                AudioClipId = inst.AudioClipId,
                Volume = inst.AudioVolume > 0 ? inst.AudioVolume : 1f,
                Pitch = inst.AudioPitch > 0 ? inst.AudioPitch : 1f,
                Loop = inst.AudioLoop,
                SpatialBlend = inst.AudioSpatialBlend,
            });
        }

        if (inst.ParticleEmitterEnabled)
        {
            go.AddComponent(new ParticleEmitterComponent
            {
                ParticleTexturePath = inst.ParticleTexturePath,
                EmissionRate = inst.ParticleEmissionRate > 0 ? inst.ParticleEmissionRate : 10f,
                LifeTime = inst.ParticleLifeTime > 0 ? inst.ParticleLifeTime : 1f,
                GravityScale = inst.ParticleGravityScale,
            });
        }

        return go;
    }

    private void TryApplyDefaultAnimationIfAny(GameObject go, ObjectInstance inst)
    {
        if (string.IsNullOrWhiteSpace(inst.DefaultAnimationClipId) || !inst.AnimationAutoPlay) return;
        NativeAutoAnimationApplier.TryApplyClipForGameObject(_project, go, inst.DefaultAnimationClipId.Trim(), _runtimeAnimations, _nativeAnimTextureProbe);
    }

    private static void ApplySpriteRendererInstanceData(GameObject go, ObjectInstance inst)
    {
        var sprite = go.GetComponent<SpriteComponent>();
        if (sprite == null) return;
        ParseTintHex(inst.SpriteColorTintHex, out var r, out var g, out var b);
        sprite.ColorTintR = r;
        sprite.ColorTintG = g;
        sprite.ColorTintB = b;
        sprite.FlipX = inst.SpriteFlipX;
        sprite.FlipY = inst.SpriteFlipY;
        sprite.SortOffset = inst.SpriteSortOffset;
        sprite.AnimationSpeedMultiplier = inst.AnimationSpeedMultiplier > 0 ? inst.AnimationSpeedMultiplier : 1f;
    }

    private static void ParseTintHex(string? hex, out float r, out float g, out float b)
    {
        r = g = b = 1f;
        if (string.IsNullOrWhiteSpace(hex)) return;
        var s = hex.Trim();
        if (s.StartsWith('#')) s = s.Substring(1);
        if (s.Length == 6 && uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var u))
        {
            r = ((u >> 16) & 0xff) / 255f;
            g = ((u >> 8) & 0xff) / 255f;
            b = (u & 0xff) / 255f;
        }
    }

    private static void TryAddSpriteFromInstance(GameObject go, ObjectInstance inst, ObjectDefinition? def)
    {
        var path = def?.SpritePath;
        if (string.IsNullOrWhiteSpace(path)) return;
        int w = def?.Width ?? 1;
        int h = def?.Height ?? 1;
        if (w < 1) w = 1;
        if (h < 1) h = 1;
        var sprite = new SpriteComponent
        {
            TexturePath = path.Trim().Replace('\\', '/'),
            DisplayWidthTiles = w,
            DisplayHeightTiles = h,
        };
        go.AddComponent(sprite);
    }

    private static void AdvanceSpriteAnimations(IEnumerable<GameObject> objects, double deltaSeconds)
    {
        foreach (var go in objects)
        {
            var s = go.GetComponent<SpriteComponent>();
            if (s == null || s.AnimationFramesPerSecond <= 0) continue;
            int n = s.FrameRegions.Count;
            if (n <= 1) continue;

            float mult = s.AnimationSpeedMultiplier > 0 ? s.AnimationSpeedMultiplier : 1f;
            s.AnimationTimeAccum += (float)(deltaSeconds * s.AnimationFramesPerSecond * mult);
            while (s.AnimationTimeAccum >= 1f)
            {
                s.AnimationTimeAccum -= 1f;
                s.CurrentFrameIndex = (s.CurrentFrameIndex + 1) % n;
            }
        }
    }

    private static bool InstanceHasDynamicTag(ObjectInstance inst)
    {
        if (inst.Tags == null) return false;
        foreach (var t in inst.Tags)
        {
            if (string.IsNullOrEmpty(t)) continue;
            if (string.Equals(t, "player", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(t, "dynamic", StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    /// <summary>Añade AABB si la instancia tiene colisión sólida o tipo Trigger.</summary>
    private static void TryAddColliderFromInstance(GameObject go, ObjectInstance inst, ObjectDefinition? def)
    {
        var colType = (inst.CollisionType ?? "").Trim();
        bool hasCol = inst.GetColision(def ?? new ObjectDefinition());
        bool isTrigger = string.Equals(colType, "Trigger", StringComparison.OrdinalIgnoreCase);
        if (!hasCol && !isTrigger)
            return;

        bool circle = string.Equals(inst.ColliderShape?.Trim(), "Circle", StringComparison.OrdinalIgnoreCase);
        float hw, hh;
        if (circle)
        {
            float rad = inst.ColliderCircleRadiusTiles > 0 ? inst.ColliderCircleRadiusTiles : 0.5f;
            hw = rad;
            hh = rad;
        }
        else
        {
            float wTiles = inst.ColliderBoxWidthTiles > 0 ? inst.ColliderBoxWidthTiles : (def?.Width ?? 1);
            float hTiles = inst.ColliderBoxHeightTiles > 0 ? inst.ColliderBoxHeightTiles : (def?.Height ?? 1);
            if (wTiles < 0.01f) wTiles = 1f;
            if (hTiles < 0.01f) hTiles = 1f;
            hw = wTiles * 0.5f;
            hh = hTiles * 0.5f;
        }

        var collider = new ColliderComponent
        {
            Shape = circle ? ColliderShapeKind.Circle : ColliderShapeKind.Box,
            TileHalfWidth = hw,
            TileHalfHeight = hh,
            OffsetX = inst.ColliderOffsetX,
            OffsetY = inst.ColliderOffsetY,
            IsTrigger = isTrigger,
            BlocksMovement = !isTrigger && hasCol,
            IsStatic = !InstanceHasDynamicTag(inst),
            Mass = inst.RigidbodyEnabled && inst.RigidbodyMass > 0 ? inst.RigidbodyMass : 1f,
        };
        go.AddComponent(collider);
    }

    /// <summary>Procesa seeds encolados; si <see cref="TryBindScriptsForGameObject"/> dispara más spawns en <c>onAwake</c>, drena hasta vaciar.</summary>
    private void FlushPendingSpawnBinds()
    {
        if (_runtime == null) return;
        while (_pendingSpawnBinds.Count > 0)
        {
            var batch = _pendingSpawnBinds.ToArray();
            _pendingSpawnBinds.Clear();
            foreach (var (go, inst) in batch)
            {
                if (go.PendingDestroy) continue;
                TryBindScriptsForGameObject(go, inst);
            }
        }
    }

    /// <summary><see cref="GameObject.PendingDestroy"/>: <c>onDestroy</c>, quita scripts y listas al cerrar el frame.</summary>
    private void FlushDestroyQueue()
    {
        if (_runtime == null || _worldContext == null) return;
        for (var i = _sceneObjects.Count - 1; i >= 0; i--)
        {
            var go = _sceneObjects[i];
            if (!go.PendingDestroy) continue;

            if (_goToInstance.TryGetValue(go, out var removed) && !string.IsNullOrEmpty(removed.InstanceId))
            {
                var rid = removed.InstanceId;
                _triggerDirectedPairsLastFrame.RemoveWhere(p =>
                    string.Equals(p.triggerId, rid, StringComparison.Ordinal) ||
                    string.Equals(p.otherId, rid, StringComparison.Ordinal));
            }

            _runtime.InvokeOnDestroyPhase(go);
            foreach (var sc in go.Components.OfType<ScriptComponent>().ToList())
            {
                if (sc.ScriptInstanceHandle is ScriptInstance si)
                    _runtime.RemoveInstance(si);
                sc.ScriptInstanceHandle = null;
            }
            _scriptBindings.RemoveAll(t => t.go == go);
            _goToInstance.Remove(go);
            _worldContext.Objects.Remove(go);
            _sceneObjects.RemoveAt(i);
            go.SetParent(null);
            go.PendingDestroy = false;
        }
    }

    private void OnScriptReloaded(string relativePath)
    {
        if (_runtime == null) return;
        var norm = ScriptLoader.NormalizeRelativePath(relativePath);
        foreach (var (go, sc, scriptPath, scriptId, instanceId, tag, props) in _scriptBindings.ToList())
        {
            if (!string.Equals(ScriptLoader.NormalizeRelativePath(sc.ScriptPath), norm, StringComparison.OrdinalIgnoreCase)) continue;
            try
            {
                var scriptInstance = _runtime.CreateInstance(scriptPath, scriptId, go, instanceId, tag, props);
                sc.ScriptInstanceHandle = scriptInstance;
                _runtime.InvokeOnStartFor(scriptInstance);
            }
            catch (Exception ex)
            {
                var line = TryParseLineFromLuaMessage(ex.Message);
                EditorLog.Error($"Hot reload '{relativePath}' en {go.Name}: {ex.Message}", "Lua", scriptPath, line > 0 ? line : null);
            }
        }

        RebindLayerScriptsForPath(norm);
        InvalidateMapZoneTickScriptsForPath(norm);
    }

    private void BindLayerScripts()
    {
        if (_runtime == null || _playTileMap == null) return;
        _runtime.ClearLayerScriptInstances();
        for (int i = 0; i < _playTileMap.Layers.Count; i++)
        {
            var d = _playTileMap.Layers[i];
            if (!d.LayerScriptEnabled || string.IsNullOrWhiteSpace(d.LayerScriptId)) continue;
            var rel = d.LayerScriptId.Trim();
            try
            {
                _runtime.CreateLayerScriptInstance(rel, rel, d, i, d.LayerScriptProperties);
            }
            catch (Exception ex)
            {
                EditorLog.Error($"Play: script de capa «{rel}» (capa «{d.Name}»): {ex.Message}", "Lua", rel, null);
            }
        }
    }

    /// <summary>Tras <see cref="LuaScriptRuntime.ReloadScript"/>: vuelve a crear scripts de capa que usaban ese archivo.</summary>
    private void RebindLayerScriptsForPath(string normalizedRelativePath)
    {
        if (_runtime == null || _playTileMap == null) return;
        for (int i = 0; i < _playTileMap.Layers.Count; i++)
        {
            var d = _playTileMap.Layers[i];
            if (!d.LayerScriptEnabled || string.IsNullOrWhiteSpace(d.LayerScriptId)) continue;
            if (!string.Equals(ScriptLoader.NormalizeRelativePath(d.LayerScriptId.Trim()), normalizedRelativePath, StringComparison.OrdinalIgnoreCase))
                continue;
            var rel = d.LayerScriptId.Trim();
            try
            {
                var inst = _runtime.CreateLayerScriptInstance(rel, rel, d, i, d.LayerScriptProperties);
                _runtime.InvokeLayerOnStartFor(inst);
            }
            catch (Exception ex)
            {
                var line = TryParseLineFromLuaMessage(ex.Message);
                EditorLog.Error($"Hot reload capa «{d.Name}» ({rel}): {ex.Message}", "Lua", rel, line > 0 ? line : null);
            }
        }
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (_runtime == null || _paused) return;
        if (_pausedForBreakpoint) return;
        var wapi = _runtime.GetWorldApi();
        if (wapi != null)
        {
            var surf = GetPlayViewportSurfacePixels?.Invoke() ?? (800.0, 600.0);
            wapi.SetPlayViewportSurfacePixels(surf.Item1, surf.Item2);
        }
        if (_playTileMap != null && _project.ChunkStreaming)
            PreloadChunksFromPlayCache();
        var now = DateTime.UtcNow;
        var delta = (now - _lastTick).TotalSeconds;
        _lastTick = now;
        _lastDeltaTime = delta;
        _frameCount++;
        _gameTimeSeconds += delta;
        _fpsFrames++;
        _fpsAccumTime += delta;
        HashSet<GameObject>? activeForUpdate = null;
        if (_project.ChunkEntitySleep && _project.ChunkLoadRadius >= 0 && _project.ChunkSize > 0 && _playTileMap != null)
        {
            int cs = Math.Max(1, _project.ChunkSize);
            int radius = Math.Max(0, _project.ChunkLoadRadius);
            var (camTx, camTy) = GetCameraTileCenterForStreaming();
            var (ccx, ccy) = _playTileMap.WorldTileToChunk(camTx, camTy);
            activeForUpdate = new HashSet<GameObject>();
            foreach (var go in _sceneObjects)
            {
                if (go.PendingDestroy) continue;
                int goCx = (int)Math.Floor((double)go.Transform.X / cs);
                int goCy = (int)Math.Floor((double)go.Transform.Y / cs);
                if (Math.Max(Math.Abs(goCx - ccx), Math.Abs(goCy - ccy)) <= radius)
                    activeForUpdate.Add(go);
            }
        }
        _runtime.BeginTick(delta, _frameCount);
        FlushPendingSpawnBinds();
        _runtime.InvokeOnStarts();
        _runtime.InvokeLayerOnStarts();
        _runtime.InvokeLayerScripts();
        var hero = NativeProtagonistController.FindProtagonist(_sceneObjects, _project, _goToInstance);
        ValidateNativeInputDependencies(hero);
        NativeProtagonistController.ApplyNativeInputBeforeLua(_project, hero, _keyboardSnap, delta, this, _runtimeAnimations, _nativeAnimTextureProbe);
        ProcessMapTriggerZones(hero);
        if (activeForUpdate != null)
        {
            foreach (var go in _mapZoneTickHosts.Values)
            {
                if (!go.PendingDestroy)
                    activeForUpdate.Add(go);
            }
        }

        _runtime.InvokeOnUpdates(activeForUpdate);
        ApplyRigidbodyVelocityStep(delta, hero);
        _physicsWorld.StepPlayScene(
            _sceneObjects,
            _playTileMap,
            go =>
            {
                if (_goToInstance.TryGetValue(go, out var inst) && !string.IsNullOrEmpty(inst.InstanceId))
                    return inst.InstanceId;
                return go.Name;
            },
            _triggerDirectedPairsLastFrame,
            (tgo, ogo) => _runtime.NotifyScripts(tgo, KnownEvents.OnTriggerEnter, CreateProxyFor(ogo)),
            (tgo, ogo) => _runtime.NotifyScripts(tgo, KnownEvents.OnTriggerExit, CreateProxyFor(ogo)));
        UpdateProximitySensors();
        AppendColliderDebugOverlay();
        _runtime.InvokeOnLateUpdates(activeForUpdate);
        UpdateSmoothedCameraFollow(delta);
        AdvanceSpriteAnimations(_sceneObjects, delta);
        FlushDestroyQueue();
        _runtime.EndTick();

        UiTypewriter.Tick(delta, _uiRoot, _uiBackend, _gameTimeSeconds, _playAudioEngine, _project.ProjectDirectory, _localization);

        if (_playTileMap != null && _project.ChunkStreaming && _project.ChunkUnloadFar && _project.ChunkSize > 0 && _project.ChunkLoadRadius >= 0)
        {
            var (camTx, camTy) = GetCameraTileCenterForStreaming();
            var (ccx, ccy) = _playTileMap.WorldTileToChunk(camTx, camTy);
            int keepChebyshev = Math.Max(0, _project.ChunkLoadRadius) + Math.Max(0, _project.ChunkStreamEvictMargin);
            if (_project.ChunkStreamSpillRuntimeEmpty)
                _playTileMap.EvictEmptyChunksBeyond(ccx, ccy, keepChebyshev, skipRuntimeTouched: false, SpillEmptyRuntimeChunkIfPossible);
            else
                _playTileMap.EvictEmptyChunksBeyond(ccx, ccy, keepChebyshev, skipRuntimeTouched: true);
        }

        if (_runtime.IsBreakpointHit)
        {
            _paused = true;
            _pausedForBreakpoint = true;
            EditorLog.Info($"Breakpoint: {_runtime.GetBreakpointLocation().path}:{_runtime.GetBreakpointLocation().line}", "Play");
        }
        if (_fpsAccumTime >= 1.0)
        {
            _currentFps = _fpsFrames / _fpsAccumTime;
            _fpsFrames = 0;
            _fpsAccumTime = 0;
        }
    }

    public void Pause()
    {
        _paused = true;
        EditorLog.Info("Juego pausado.", "Play");
    }

    public void Resume()
    {
        _paused = false;
        _pausedForBreakpoint = false;
        _runtime?.ClearBreakpointHit();
        _lastTick = DateTime.UtcNow;
        EditorLog.Info("Juego reanudado.", "Play");
    }

    public bool IsPausedForBreakpoint => _pausedForBreakpoint;
    public (string? path, int line) GetBreakpointLocation() => _runtime?.GetBreakpointLocation() ?? (null, 0);

    public void ResumeFromBreakpoint()
    {
        _pausedForBreakpoint = false;
        _paused = false;
        _runtime?.ClearBreakpointHit();
        _lastTick = DateTime.UtcNow;
    }

    public int GetActiveScriptCount() => _runtime?.GetActiveScriptCount() ?? 0;
    public double GetLuaMemoryKb() => _runtime?.GetLuaMemoryKb() ?? 0;

    public void SetBreakpoints(IEnumerable<(string path, int line)> breakpoints) => _runtime?.SetBreakpoints(breakpoints);

    public void Stop()
    {
        _timer?.Stop();
        _timer = null;
        _luaFileWatcher?.Dispose();
        _luaFileWatcher = null;
        if (_runtime != null)
        {
            PluginLoader.UnloadAll();
            _runtime.GetWorldApi()?.ConfigurePlayTilemap(null, null, null);
            _runtime.ScriptReloaded -= OnScriptReloaded;
            foreach (var zid in _mapZoneTickHosts.Keys.ToList())
                RemoveMapZoneTickHost(zid);
            _mapZoneTickHosts.Clear();
            _mapZonesPlayerInside.Clear();
            _mapTriggerZones.Clear();
            _runtime.ClearLayerScriptInstances();
            _runtime.Dispose();
            _runtime = null;
        }
        _playAudioEngine?.Dispose();
        _playAudioEngine = null;
        _playTileMap = null;
        _runtimeAnimations.Clear();
        _nativeAnimTextureProbe = null;
        _worldContext = null;
        _uiBackend = null;
        _localization = null;
        UiTypewriter.TypewriterLineComplete -= OnTypewriterLineCompleteForTts;
        _accessibilityTts?.Stop();
        _accessibilityTts?.Dispose();
        _accessibilityTts = null;
        UiTypewriter.Clear();
        _sceneObjects.Clear();
        _scriptBindings.Clear();
        _goToInstance.Clear();
        _triggerDirectedPairsLastFrame.Clear();
        _runtimeSeeds.Clear();
        _pendingSpawnBinds.Clear();
        EditorLog.Info("Modo Play detenido.", "Play");
    }

    /// <summary>Llamar cuando el editor guarda un script .lua: recarga el script y recrea instancias (hot reload).</summary>
    public void OnScriptSaved(string relativePath)
    {
        _runtime?.ReloadScript(relativePath ?? "");
    }

    /// <summary>Devuelve el runtime actual (para conectar errores/print a la consola).</summary>
    public LuaScriptRuntime? GetRuntime() => _runtime;

    /// <summary>Backend UI activo del runtime (visibilidad, focus, bindings e input).</summary>
    public UIRuntimeBackend? GetUiBackend() => _uiBackend;

    /// <summary>Comandos Debug del último tick (viewport tab Juego).</summary>
    public IReadOnlyList<DebugDrawItem> GetDebugDrawSnapshot() => _runtime?.GetDebugDrawSnapshot() ?? Array.Empty<DebugDrawItem>();

    /// <summary>Reproduce un sonido por ID en el runtime del juego (para "Play in Game" del tab Audio). Stub hasta conectar backend de audio.</summary>
    public void PlayAudioInGame(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        _playAudioEngine?.PlaySfxById(id.Trim());
    }

    /// <summary>Estado actual de los objetos (para "Guardar estado en escena" del tab Juego).</summary>
    public IReadOnlyList<RuntimeObjectState> GetObjectStates()
    {
        var list = new List<RuntimeObjectState>();
        foreach (var go in _sceneObjects)
        {
            if (go.PendingDestroy) continue;
            if (!_goToInstance.TryGetValue(go, out var inst)) continue;
            var t = go.Transform;
            list.Add(new RuntimeObjectState(inst.InstanceId, t?.X ?? 0, t?.Y ?? 0, t?.RotationDegrees ?? 0, t?.ScaleX ?? 1, t?.ScaleY ?? 1));
        }
        return list;
    }

    /// <summary>Variables Lua actuales por instancia/script para persistir en <c>objetos.json</c> al guardar estado.</summary>
    public IReadOnlyList<RuntimeScriptPropertySnapshot> GetScriptPropertySnapshotsForScene()
    {
        var list = new List<RuntimeScriptPropertySnapshot>();
        foreach (var (go, sc, _, scriptId, _, _, _) in _scriptBindings)
        {
            if (go.PendingDestroy) continue;
            if (string.IsNullOrEmpty(scriptId)) continue;
            if (sc.ScriptInstanceHandle is not ScriptInstance si) continue;
            if (!_goToInstance.TryGetValue(go, out var oi)) continue;
            var instanceId = oi.InstanceId ?? "";
            var snap = si.GetVariableSnapshot();
            if (snap == null) continue;
            foreach (var kv in snap)
            {
                if (KnownEvents.IsReservedScriptVariableName(kv.Key)) continue;
                if (kv.Value.StartsWith("table:", StringComparison.OrdinalIgnoreCase)) continue;
                var type = InferPropertyTypeFromLuaDisplay(kv.Value);
                var value = FormatPropertyValueForSave(kv.Value, type);
                list.Add(new RuntimeScriptPropertySnapshot(instanceId, scriptId, kv.Key, type, value));
            }
        }

        return list;
    }

    /// <summary>Variables Lua en vivo (mismo criterio que <see cref="ScriptInstance.GetVariableSnapshot"/>) para el Inspector durante Play.</summary>
    public bool TryGetLiveScriptVariables(string instanceId, string scriptId, out IReadOnlyDictionary<string, string>? snapshot)
    {
        snapshot = null;
        if (_runtime == null || !IsRunning) return false;
        foreach (var (go, sc, _, sid, iid, _, _) in _scriptBindings)
        {
            if (go.PendingDestroy) continue;
            if (!string.Equals(iid ?? "", instanceId ?? "", StringComparison.OrdinalIgnoreCase)) continue;
            if (!string.Equals(sid, scriptId, StringComparison.OrdinalIgnoreCase)) continue;
            if (sc.ScriptInstanceHandle is not ScriptInstance si) continue;
            snapshot = si.GetVariableSnapshot();
            return snapshot != null;
        }
        return false;
    }

    /// <summary>Escribe una propiedad en el entorno Lua del script en ejecución (Inspector en caliente).</summary>
    public bool TrySetLiveScriptVariable(string instanceId, string scriptId, string key, string? type, string? value)
    {
        if (_runtime == null || !IsRunning || string.IsNullOrEmpty(key)) return false;
        foreach (var (go, sc, _, sid, iid, _, _) in _scriptBindings)
        {
            if (go.PendingDestroy) continue;
            if (!string.Equals(iid ?? "", instanceId ?? "", StringComparison.OrdinalIgnoreCase)) continue;
            if (!string.Equals(sid, scriptId, StringComparison.OrdinalIgnoreCase)) continue;
            if (sc.ScriptInstanceHandle is not ScriptInstance si) continue;
            var obj = _runtime.ResolveInspectorPropertyValue(type, value);
            si.Set(key, obj);
            return true;
        }
        return false;
    }

    private static string InferPropertyTypeFromLuaDisplay(string v)
    {
        if (string.Equals(v, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(v, "false", StringComparison.OrdinalIgnoreCase))
            return "bool";
        if (string.Equals(v, "nil", StringComparison.OrdinalIgnoreCase))
            return "string";
        if (long.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) && v.IndexOf('.') < 0 &&
            v.IndexOf('e', StringComparison.OrdinalIgnoreCase) < 0)
            return "int";
        if (double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            return "float";
        return "string";
    }

    private static string FormatPropertyValueForSave(string display, string type)
    {
        if (string.Equals(display, "nil", StringComparison.OrdinalIgnoreCase)) return "";
        if (type == "bool")
            return string.Equals(display, "true", StringComparison.OrdinalIgnoreCase) ? "true" : "false";
        return display;
    }

    /// <summary>Destruye un objeto en el runtime (tab Juego): mismo pipeline que <c>self.destroy()</c> pero aplicado al instante.</summary>
    public void DestroyObject(GameObject gameObject)
    {
        if (gameObject == null || _runtime == null || _worldContext == null) return;
        if (!_sceneObjects.Contains(gameObject)) return;
        gameObject.PendingDestroy = true;
        gameObject.RuntimeActive = false;
        FlushDestroyQueue();
    }

    private SelfProxy CreateProxyFor(GameObject go)
    {
        _goToInstance.TryGetValue(go, out var inst);
        var id = inst?.InstanceId ?? go.Name;
        return _runtime!.GetProxyFactory()(go, id, null);
    }

    private TileMap LoadPlayTileMapFromDisk()
    {
        var path = _project.GetSceneMapPath(0);
        try
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                return MapSerialization.Load(path);
        }
        catch (Exception ex)
        {
            EditorLog.Warning($"Play: no se pudo cargar el mapa: {ex.Message}", "Play");
        }
        return new TileMap(Math.Max(8, _project.ChunkSize));
    }

    private string? GetPlayChunkCacheDirectory()
    {
        var pd = _project.ProjectDirectory;
        if (string.IsNullOrWhiteSpace(pd)) return null;
        string mapKey = Path.GetFileNameWithoutExtension(_project.MapPath);
        if (string.IsNullOrEmpty(mapKey)) mapKey = "map";
        foreach (var c in Path.GetInvalidFileNameChars())
            mapKey = mapKey.Replace(c, '_');
        return Path.Combine(pd, ".fue_play_chunk_cache", mapKey);
    }

    private static string ChunkCacheFileName(int layerIndex, int cx, int cy) =>
        $"layer{layerIndex}_{cx}_{cy}.json";

    /// <summary>Restaura chunks volcados antes del tick (misma zona Chebyshev que la evicción).</summary>
    private void PreloadChunksFromPlayCache()
    {
        if (_playTileMap == null) return;
        var root = GetPlayChunkCacheDirectory();
        if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return;
        var (camTx, camTy) = GetCameraTileCenterForStreaming();
        var (ccx, ccy) = _playTileMap.WorldTileToChunk(camTx, camTy);
        int r = Math.Max(0, _project.ChunkLoadRadius) + Math.Max(0, _project.ChunkStreamEvictMargin);
        for (int dx = -r; dx <= r; dx++)
        for (int dy = -r; dy <= r; dy++)
        {
            if (Math.Max(Math.Abs(dx), Math.Abs(dy)) > r) continue;
            int cx = ccx + dx, cy = ccy + dy;
            for (int li = 0; li < _playTileMap.Layers.Count; li++)
            {
                if (_playTileMap.GetChunk(li, cx, cy) != null) continue;
                var path = Path.Combine(root, ChunkCacheFileName(li, cx, cy));
                if (!File.Exists(path)) continue;
                if (MapSerialization.TryMergeChunkFromFile(_playTileMap, path))
                {
                    try { File.Delete(path); } catch { /* ok */ }
                }
            }
        }
    }

    private bool SpillEmptyRuntimeChunkIfPossible(int layerIndex, int cx, int cy)
    {
        if (_playTileMap == null) return false;
        var dto = MapSerialization.ToChunkDto(_playTileMap, layerIndex, cx, cy);
        if (dto == null) return false;
        var root = GetPlayChunkCacheDirectory();
        if (string.IsNullOrEmpty(root)) return false;
        try
        {
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, ChunkCacheFileName(layerIndex, cx, cy));
            MapSerialization.SaveChunkDtoToFile(dto, path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private GameObject? FindCameraFollowTarget()
    {
        foreach (var go in _sceneObjects)
        {
            if (go.PendingDestroy) continue;
            if (go.GetComponent<CameraTargetComponent>() != null)
                return go;
        }
        return NativeProtagonistController.FindProtagonist(_sceneObjects, _project, _goToInstance);
    }

    private (int tx, int ty) GetCameraTileCenterForStreaming()
    {
        var go = FindCameraFollowTarget();
        if (go != null)
            return ((int)Math.Floor(go.Transform.X), (int)Math.Floor(go.Transform.Y));
        return (0, 0);
    }

    private void ApplyRigidbodyVelocityStep(double deltaSeconds, GameObject? nativeHero)
    {
        if (deltaSeconds <= 0) return;
        float dt = (float)deltaSeconds;
        double g = _project.PhysicsGravity;
        foreach (var go in _sceneObjects)
        {
            if (go.PendingDestroy || !go.RuntimeActive) continue;
            var rb = go.GetComponent<RigidbodyComponent>();
            if (rb == null) continue;
            if (_project.UseNativeInput && nativeHero != null && ReferenceEquals(go, nativeHero)) continue;

            rb.VelocityY += (float)(g * rb.GravityScale * dt);
            if (rb.Drag > 0)
            {
                float damp = MathF.Exp(-rb.Drag * dt);
                rb.VelocityX *= damp;
                rb.VelocityY *= damp;
            }
            go.Transform.X += rb.VelocityX * dt;
            go.Transform.Y += rb.VelocityY * dt;
        }
    }

    private void UpdateProximitySensors()
    {
        if (_runtime == null) return;
        foreach (var go in _sceneObjects)
        {
            var prox = go.GetComponent<ProximitySensorComponent>();
            if (prox == null || !go.RuntimeActive || go.PendingDestroy) continue;
            var target = FindFirstWithTag(_sceneObjects, prox.TargetTag);
            if (target == null)
            {
                prox.WasInside = false;
                continue;
            }
            double dx = target.Transform.X - go.Transform.X;
            double dy = target.Transform.Y - go.Transform.Y;
            double r = prox.DetectionRangeTiles;
            bool inside = dx * dx + dy * dy <= r * r;
            if (inside && !prox.WasInside)
                _runtime.NotifyScripts(go, KnownEvents.OnTriggerEnter, CreateProxyFor(target));
            else if (!inside && prox.WasInside)
                _runtime.NotifyScripts(go, KnownEvents.OnTriggerExit, CreateProxyFor(target));
            prox.WasInside = inside;
        }
    }

    private static GameObject? FindFirstWithTag(IReadOnlyList<GameObject> scene, string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return null;
        foreach (var go in scene)
        {
            if (go.PendingDestroy || !go.RuntimeActive) continue;
            if (go.Tags == null) continue;
            foreach (var t in go.Tags)
            {
                if (string.Equals(t, tag, StringComparison.OrdinalIgnoreCase))
                    return go;
            }
        }
        return null;
    }

    private void UpdateSmoothedCameraFollow(double dt)
    {
        var h = FindCameraFollowTarget();
        if (h == null) return;
        double tx = h.Transform.X, ty = h.Transform.Y;
        if (!_project.UseNativeCameraFollow)
        {
            _cameraWorldX = tx;
            _cameraWorldY = ty;
            return;
        }
        float k = _project.NativeCameraSmoothing;
        if (k <= 0)
        {
            _cameraWorldX = tx;
            _cameraWorldY = ty;
            return;
        }
        double a = 1.0 - Math.Exp(-k * dt);
        _cameraWorldX += (tx - _cameraWorldX) * a;
        _cameraWorldY += (ty - _cameraWorldY) * a;
    }

    internal void TryMoveDynamicAgainstTilemap(GameObject dyn, double dx, double dy)
    {
        if (_playTileMap == null)
        {
            dyn.Transform.X += (float)dx;
            dyn.Transform.Y += (float)dy;
            return;
        }
        var d = dyn.GetComponent<ColliderComponent>();
        if (d == null || d.IsTrigger || !d.BlocksMovement)
        {
            dyn.Transform.X += (float)dx;
            dyn.Transform.Y += (float)dy;
            return;
        }
        dyn.Transform.X += (float)dx;
        PhysicsWorld.ResolveDynamicAgainstTiles(dyn, d, _playTileMap);
        dyn.Transform.Y += (float)dy;
        PhysicsWorld.ResolveDynamicAgainstTiles(dyn, d, _playTileMap);
    }

    private void ValidateNativeInputDependencies(GameObject? hero)
    {
        if (!_project.UseNativeInput || hero == null) return;

        if (_playTileMap == null && !_warnedNativeInputNoMap)
        {
            _warnedNativeInputNoMap = true;
            EditorLog.Warning("NativeInput activo pero no hay TileMap en Play; el protagonista se moverá sin colisión contra tiles.", "Play");
        }

        if (!_goToInstance.TryGetValue(hero, out var inst)) return;
        var instanceId = string.IsNullOrEmpty(inst.InstanceId) ? hero.Name : inst.InstanceId;
        if (string.IsNullOrEmpty(instanceId) || _warnedNativeInputColliderByInstance.Contains(instanceId)) return;

        var col = hero.GetComponent<ColliderComponent>();
        if (col == null)
        {
            _warnedNativeInputColliderByInstance.Add(instanceId);
            EditorLog.Warning($"NativeInput: el protagonista '{hero.Name}' ({instanceId}) no tiene ColliderComponent; movimiento libre sin colisión.", "Play");
            return;
        }
        if (col.IsTrigger || !col.BlocksMovement)
        {
            _warnedNativeInputColliderByInstance.Add(instanceId);
            EditorLog.Warning($"NativeInput: el protagonista '{hero.Name}' ({instanceId}) no bloquea movimiento (IsTrigger={col.IsTrigger}, BlocksMovement={col.BlocksMovement}); movimiento libre sin colisión.", "Play");
        }
    }

    private object? RaycastScene(double originX, double originY, double dirX, double dirY, double maxDistance, GameObject? ignoreColliderOwner)
    {
        if (maxDistance <= 0 || _runtime == null) return null;
        double len = Math.Sqrt(dirX * dirX + dirY * dirY);
        if (len < 1e-12) return null;
        double ux = dirX / len, uy = dirY / len;
        if (!ScenePhysicsQueries.RaycastSolids(_sceneObjects, originX, originY, ux, uy, maxDistance, ignoreColliderOwner, out var bestT, out var bestGo) || bestGo == null)
            return null;
        double hitX = originX + ux * bestT;
        double hitY = originY + uy * bestT;
        return new RaycastHitInfo(CreateProxyFor(bestGo), bestT, hitX, hitY);
    }

    private void AppendColliderDebugOverlay()
    {
        if (_runtime == null) return;
        var dbg = _runtime.SharedDebugDraw;
        foreach (var go in _sceneObjects)
        {
            if (go.PendingDestroy) continue;
            var c = go.GetComponent<ColliderComponent>();
            if (c == null) continue;
            ScenePhysicsQueries.GetWorldAabb(go, c, out var cx, out var cy, out var hx, out var hy);
            double x0 = cx - hx, y0 = cy - hy, x1 = cx + hx, y1 = cy + hy;
            double r, g, b, a;
            if (c.IsTrigger)
            {
                r = 40; g = 220; b = 90; a = 200;
            }
            else
            {
                r = 235; g = 50; b = 55; a = 200;
            }

            dbg.drawLine(x0, y0, x1, y0, r, g, b, a);
            dbg.drawLine(x1, y0, x1, y1, r, g, b, a);
            dbg.drawLine(x1, y1, x0, y1, r, g, b, a);
            dbg.drawLine(x0, y1, x0, y0, r, g, b, a);
        }
    }

    private static int TryParseLineFromLuaMessage(string? message)
    {
        if (string.IsNullOrEmpty(message)) return 0;
        var match = Regex.Match(message, @":(\d+):");
        return match.Success && int.TryParse(match.Groups[1].Value, out var line) ? line : 0;
    }

    private void OnTypewriterLineCompleteForTts(string canvasId, FUEngine.Core.UIElement el, string plain)
    {
        _accessibilityTts?.Speak(plain);
    }
}

/// <summary>Estado de un objeto en runtime para aplicar de vuelta a la escena del editor.</summary>
public sealed record RuntimeObjectState(string InstanceId, double X, double Y, double Rotation, double ScaleX, double ScaleY);

/// <summary>Una variable de script en runtime para fusionar en <see cref="ObjectInstance.ScriptProperties"/>.</summary>
public sealed record RuntimeScriptPropertySnapshot(string InstanceId, string ScriptId, string Key, string Type, string Value);
