// -----------------------------------------------------------------------------
// FUEngine (FUEngine.Runtime) — Condiciones: LICENSE.md en la raíz del repositorio.
// Copyright (c) Red Redtid. No es licencia MIT/Apache ni "open source" OSI.
// Productos comerciales: revenue share según LICENSE.md. Prohibido integrar
// publicidad de terceros en forks GitHub sin autorización escrita. Plugins
// gratuitos (no venta de extensiones). Build pública sin garantías ni servidores
// de validación del titular.
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using FUEngine.Core;
using NLua;
using NLua.Exceptions;

namespace FUEngine.Runtime;

/// <summary>
/// Runtime de scripting Lua: inicializa el intérprete, carga scripts, expone API, ejecuta eventos.
/// Cada ScriptInstance tiene su propio entorno para evitar conflictos entre scripts.
/// </summary>
public sealed class LuaScriptRuntime
{
    private readonly LuaEnvironment _environment;
    private readonly ScriptLoader _loader;
    private readonly List<ScriptInstance> _instances = new();
    private readonly List<ScriptInstance> _layerScriptInstances = new();
    private readonly Dictionary<GameObject, List<ScriptInstance>> _instancesByObject = new();
    private readonly TimeApi _timeApi = new();
    private WorldApi? _worldApi;
    private InputApi? _inputApi;
    private UiApi? _uiApi;
    private GameApi? _gameApi;
    private PhysicsApi? _physicsApi;
    private AdsApi? _adsApi;
    private readonly AudioApi? _audioApi;
    private readonly DebugDrawApi _debugDraw = new();
    private readonly Dictionary<string, object> _requireModuleCache = new(StringComparer.OrdinalIgnoreCase);
    private Func<GameObject, string, bool>? _playAnimationClip;
    private Action<GameObject>? _stopAnimation;
    private readonly LuaLogApi _logApi;
    private Action<string, string> _logSink;
    private bool _disposed;

    /// <summary>Cuando el script lanza un error, se notifica aquí (ej: para consola del editor).</summary>
    public Action<string, int, string>? ScriptError;

    /// <summary>Cuando print() se llama desde Lua, se redirige aquí (ej: EditorLog.Info).</summary>
    public Action<string>? PrintOutput;

    /// <summary>
    /// Cuando se recarga un script se eliminan sus instancias; el motor debe recrearlas.
    /// Suscribirse aquí y para cada objeto que tenga ese script: volver a llamar CreateInstance(scriptPath, scriptId, gameObject, ...)
    /// y asignar el resultado a ScriptComponent.ScriptInstanceHandle. Así los GameObjectProxy siguen apuntando al nuevo script (getComponent usa ScriptInstanceHandle).
    /// </summary>
    public Action<string>? ScriptReloaded;

    /// <summary>Se dispara cuando un evento está a punto de ejecutarse (para panel "Eventos ejecutándose").</summary>
    public Action<ScriptInstance, string>? EventExecuting;

    private readonly HashSet<(string path, int line)> _breakpoints = new();
    private bool _breakpointHit;
    private string? _breakpointPath;
    private int _breakpointLine;
    private bool _hookInstalled;

    /// <param name="exposeLuaDebugTraceback">Si es true, los scripts pueden usar <c>debug.traceback</c> (solo traceback, no el depurador completo).</param>
    public LuaScriptRuntime(string projectDirectory, AudioApi? audioApi = null, bool exposeLuaDebugTraceback = true)
    {
        _logSink = DefaultLuaLogSink;
        _logApi = new LuaLogApi((level, msg) => _logSink(level, msg));
        _environment = new LuaEnvironment(msg => PrintOutput?.Invoke(msg ?? ""), exposeLuaDebugTraceback);
        _loader = new ScriptLoader(projectDirectory ?? "");
        _audioApi = audioApi;
    }

    private void DefaultLuaLogSink(string level, string msg) =>
        PrintOutput?.Invoke($"[Lua {level}] {msg}");

    /// <summary>Redirige <c>log.info</c> / <c>warn</c> / <c>error</c> (por defecto van a <see cref="PrintOutput"/> con prefijo).</summary>
    public void SetLuaLogSink(Action<string, string>? sink) => _logSink = sink ?? DefaultLuaLogSink;

    /// <summary>Directorio del proyecto (misma raíz que <see cref="ScriptLoader"/>).</summary>
    public string ProjectDirectory => _loader.ProjectDirectory;

    /// <summary>
    /// Registra plugins Lua. Debe llamarse cuando World, input, game, física, UI y ads ya estén configurados en este runtime.
    /// </summary>
    /// <param name="projectConfiguration"><see cref="ProjectInfo"/> del proyecto en Play, o null.</param>
    /// <param name="hostServices">Servicios opcionales del host (p. ej. <c>IEditorLog</c>) por tipo.</param>
    public void RegisterEnginePlugins(
        ProjectInfo? projectConfiguration = null,
        IReadOnlyDictionary<Type, object>? hostServices = null)
    {
        var ctx = new PlayLuaEngineContext(this, projectConfiguration, hostServices);
        foreach (var plugin in PluginLoader.Loaded)
        {
            if (plugin is ILuaEngineBinding bind)
                bind.RegisterLuaHost(_environment.State, ctx);
        }
    }

    public void SetWorldApi(WorldApi api) => _worldApi = api;

    public WorldApi? GetWorldApi() => _worldApi;
    public InputApi? GetInputApi() => _inputApi;
    public void SetInputApi(InputApi api) => _inputApi = api;
    public void SetUiApi(UiApi api) => _uiApi = api;
    public void SetGameApi(GameApi api) => _gameApi = api;

    public void SetPhysicsApi(PhysicsApi api) => _physicsApi = api;

    public void SetAdsApi(AdsApi api) => _adsApi = api;

    public AdsApi? GetAdsApi() => _adsApi;

    public GameApi? GetGameApi() => _gameApi;

    public UiApi? GetUiApi() => _uiApi;

    public PhysicsApi? GetPhysicsApi() => _physicsApi;

    public AudioApi? GetAudioApi() => _audioApi;

    public TimeApi GetTimeApi() => _timeApi;

    public DebugDrawApi GetDebugDrawApi() => _debugDraw;

    /// <summary>Factory para crear proxies (self y world). El host puede pasarla a WorldApi.SetWorldContext(..., GetProxyFactory()).</summary>
    public Func<GameObject, string?, string?, SelfProxy> GetProxyFactory() => CreateProxy;

    /// <summary>Callbacks opcionales para <see cref="SelfProxy.playAnimation"/> / <see cref="SelfProxy.stopAnimation"/>.</summary>
    public void SetSpriteAnimationCallbacks(Func<GameObject, string, bool>? playClip, Action<GameObject>? stopClip)
    {
        _playAnimationClip = playClip;
        _stopAnimation = stopClip;
    }

    private SelfProxy CreateProxy(GameObject go, string? instanceId, string? unusedLegacyTag = null) =>
        new SelfProxy(go, instanceId ?? go.Name, CreateProxy, _worldApi, _playAnimationClip, _stopAnimation);

    /// <summary>Carga el código del script (path relativo al proyecto).</summary>
    public string LoadScriptSource(string relativePath)
    {
        return _loader.LoadSource(relativePath);
    }

    /// <summary>
    /// Crea una instancia del script. Ejecuta el chunk, inyecta propiedades e invoca <see cref="KnownEvents.OnAwake"/> si existe.
    /// <see cref="KnownEvents.OnStart"/> lo ejecuta el host con <see cref="InvokeOnStarts"/> antes del primer <see cref="KnownEvents.OnUpdate"/>.
    /// </summary>
    public ScriptInstance CreateInstance(
        string scriptPath,
        string scriptId,
        GameObject gameObject,
        string? instanceId = null,
        string? tag = null,
        IReadOnlyList<ScriptPropertyEntry>? scriptProperties = null)
    {
        var source = _loader.LoadSource(scriptPath);
        var env = _environment.CreateInstanceEnvironment();
        var selfProxy = CreateProxy(gameObject, instanceId, tag);
        ScriptBindings.PopulateEnvironment(env, selfProxy, _worldApi, _inputApi, _timeApi, _audioApi, _uiApi, _gameApi, _debugDraw, _physicsApi, _adsApi, _logApi);
        LuaRequireSupport.InjectRequire(_environment.State, env, _loader, _requireModuleCache);

        _environment.State["__scriptSource"] = source;
        _environment.State["__scriptName"] = scriptPath;
        _environment.State["__scriptEnv"] = env;
        try
        {
            _environment.State.DoString(@"
                local src, name, env = __scriptSource, __scriptName, __scriptEnv
                local fn, err = load(src, name, 't', env)
                if not fn then error(err or 'load failed') end
                fn()
            ");
        }
        catch (LuaException ex)
        {
            var msg = ex.ToString();
            ReportError(scriptPath, TryParseLineFromError(msg), msg);
            throw;
        }
        catch (Exception ex)
        {
            var msg = ex.ToString();
            ReportError(scriptPath, TryParseLineFromError(msg), msg);
            throw;
        }

        var instance = new ScriptInstance(_environment.State, env, scriptPath, scriptId, gameObject);

        if (!_instancesByObject.TryGetValue(gameObject, out var list))
        {
            list = new List<ScriptInstance>();
            _instancesByObject[gameObject] = list;
        }
        list.Add(instance);

        if (scriptProperties != null)
        {
            foreach (var p in scriptProperties)
            {
                var val = ResolveInjectedProperty(p);
                instance.Set(p.Key, val);
            }
        }

        _instances.Add(instance);

        if (instance.HasFunction(KnownEvents.OnAwake))
        {
            EventExecuting?.Invoke(instance, KnownEvents.OnAwake);
            if (!instance.TryInvoke(KnownEvents.OnAwake, out _, out var awakeErr) && !string.IsNullOrEmpty(awakeErr))
                ReportError(scriptPath, TryParseLineFromError(awakeErr), awakeErr);
        }
        instance.LifecycleAwakeDone = true;

        return instance;
    }

    /// <summary>
    /// Script de capa del tilemap: entorno con <c>layer</c> (sin <c>self</c>). Eventos: <see cref="KnownEvents.OnAwake"/>, <see cref="KnownEvents.OnStart"/>, <see cref="KnownEvents.OnLayerUpdate"/>, <see cref="KnownEvents.OnDestroy"/>.
    /// No se mezcla con <see cref="_instances"/> de objetos.
    /// </summary>
    public ScriptInstance CreateLayerScriptInstance(
        string scriptPath,
        string scriptId,
        MapLayerDescriptor layerDescriptor,
        int layerIndex,
        IReadOnlyList<ScriptPropertyEntry>? scriptProperties = null)
    {
        var source = _loader.LoadSource(scriptPath);
        var env = _environment.CreateInstanceEnvironment();
        var layerProxy = new LayerProxy(layerDescriptor, layerIndex);
        ScriptBindings.PopulateLayerEnvironment(env, layerProxy, _worldApi, _inputApi, _timeApi, _audioApi, _uiApi, _gameApi, _debugDraw, _physicsApi, _adsApi, _logApi);
        LuaRequireSupport.InjectRequire(_environment.State, env, _loader, _requireModuleCache);

        _environment.State["__scriptSource"] = source;
        _environment.State["__scriptName"] = scriptPath;
        _environment.State["__scriptEnv"] = env;
        try
        {
            _environment.State.DoString(@"
                local src, name, env = __scriptSource, __scriptName, __scriptEnv
                local fn, err = load(src, name, 't', env)
                if not fn then error(err or 'load failed') end
                fn()
            ");
        }
        catch (LuaException ex)
        {
            var msg = ex.ToString();
            ReportError(scriptPath, TryParseLineFromError(msg), msg);
            throw;
        }
        catch (Exception ex)
        {
            var msg = ex.ToString();
            ReportError(scriptPath, TryParseLineFromError(msg), msg);
            throw;
        }

        var instance = new ScriptInstance(_environment.State, env, scriptPath, scriptId, null);
        _layerScriptInstances.Add(instance);

        if (scriptProperties != null)
        {
            foreach (var p in scriptProperties)
            {
                var val = ResolveInjectedProperty(p);
                instance.Set(p.Key, val);
            }
        }

        if (instance.HasFunction(KnownEvents.OnAwake))
        {
            EventExecuting?.Invoke(instance, KnownEvents.OnAwake);
            if (!instance.TryInvoke(KnownEvents.OnAwake, out _, out var awakeErr) && !string.IsNullOrEmpty(awakeErr))
                ReportError(scriptPath, TryParseLineFromError(awakeErr), awakeErr);
        }
        instance.LifecycleAwakeDone = true;

        return instance;
    }

    /// <summary>Elimina instancias de script de capa (invoca <see cref="KnownEvents.OnDestroy"/> si existe).</summary>
    public void ClearLayerScriptInstances()
    {
        if (_disposed) return;
        foreach (var inst in _layerScriptInstances)
        {
            EventExecuting?.Invoke(inst, KnownEvents.OnDestroy);
            if (inst.HasFunction(KnownEvents.OnDestroy))
            {
                if (!inst.TryInvoke(KnownEvents.OnDestroy, out _, out var err) && !string.IsNullOrEmpty(err))
                    ReportError(inst.ScriptPath, TryParseLineFromError(err), err);
            }
            inst.Dispose();
        }
        _layerScriptInstances.Clear();
    }

    /// <summary>Antes del primer <see cref="KnownEvents.OnLayerUpdate"/>: <see cref="KnownEvents.OnStart"/> una vez por script de capa.</summary>
    public void InvokeLayerOnStarts()
    {
        if (_disposed) return;
        foreach (var inst in _layerScriptInstances)
        {
            if (inst.LifecycleStartDone) continue;
            if (!inst.LifecycleAwakeDone) continue;
            EventExecuting?.Invoke(inst, KnownEvents.OnStart);
            if (inst.HasFunction(KnownEvents.OnStart))
            {
                if (!inst.TryInvoke(KnownEvents.OnStart, out _, out var err) && !string.IsNullOrEmpty(err))
                    ReportError(inst.ScriptPath, TryParseLineFromError(err), err);
            }
            inst.LifecycleStartDone = true;
        }
    }

    /// <summary>Una sola instancia de capa (p. ej. tras hot reload).</summary>
    public void InvokeLayerOnStartFor(ScriptInstance instance)
    {
        if (_disposed || instance.LifecycleStartDone) return;
        if (!instance.LifecycleAwakeDone) return;
        if (!_layerScriptInstances.Contains(instance)) return;
        EventExecuting?.Invoke(instance, KnownEvents.OnStart);
        if (instance.HasFunction(KnownEvents.OnStart))
        {
            if (!instance.TryInvoke(KnownEvents.OnStart, out _, out var err) && !string.IsNullOrEmpty(err))
                ReportError(instance.ScriptPath, TryParseLineFromError(err), err);
        }
        instance.LifecycleStartDone = true;
    }

    /// <summary><see cref="KnownEvents.OnLayerUpdate"/> cada frame con <c>dt</c> (usa <see cref="TimeApi.delta"/>).</summary>
    public void InvokeLayerScripts()
    {
        if (_disposed) return;
        double dt = _timeApi.delta;
        foreach (var inst in _layerScriptInstances)
        {
            EventExecuting?.Invoke(inst, KnownEvents.OnLayerUpdate);
            if (inst.HasFunction(KnownEvents.OnLayerUpdate))
            {
                if (!inst.TryInvoke(KnownEvents.OnLayerUpdate, out _, out var err, dt) && !string.IsNullOrEmpty(err))
                    ReportError(inst.ScriptPath, TryParseLineFromError(err), err);
            }
        }
    }

    /// <summary>Ejecuta <see cref="KnownEvents.OnStart"/> una vez (p. ej. inmediatamente tras hot reload).</summary>
    public void InvokeOnStartFor(ScriptInstance instance)
    {
        if (_disposed || instance.LifecycleStartDone) return;
        if (!instance.LifecycleAwakeDone) return;
        var go = instance.GameObjectRef;
        if (go == null || go.PendingDestroy || !go.RuntimeActive) return;
        EventExecuting?.Invoke(instance, KnownEvents.OnStart);
        if (instance.HasFunction(KnownEvents.OnStart))
        {
            if (!instance.TryInvoke(KnownEvents.OnStart, out _, out var err) && !string.IsNullOrEmpty(err))
                ReportError(instance.ScriptPath, TryParseLineFromError(err), err);
        }
        instance.LifecycleStartDone = true;
    }

    /// <summary>Antes del primer <see cref="KnownEvents.OnUpdate"/> del frame: <see cref="KnownEvents.OnStart"/> una vez por instancia.</summary>
    public void InvokeOnStarts()
    {
        if (_disposed) return;
        foreach (var inst in _instances)
        {
            if (inst.LifecycleStartDone) continue;
            if (!inst.LifecycleAwakeDone) continue;
            var go = inst.GameObjectRef;
            if (go == null || go.PendingDestroy || !go.RuntimeActive) continue;
            EventExecuting?.Invoke(inst, KnownEvents.OnStart);
            if (inst.HasFunction(KnownEvents.OnStart))
            {
                if (!inst.TryInvoke(KnownEvents.OnStart, out _, out var err) && !string.IsNullOrEmpty(err))
                    ReportError(inst.ScriptPath, TryParseLineFromError(err), err);
            }
            inst.LifecycleStartDone = true;
        }
    }

    /// <summary>Antes de quitar el objeto del mundo: <see cref="KnownEvents.OnDestroy"/> en cada script del GO.</summary>
    public void InvokeOnDestroyPhase(GameObject gameObject)
    {
        if (_disposed || gameObject == null) return;
        if (!_instancesByObject.TryGetValue(gameObject, out var list)) return;
        var copy = new List<ScriptInstance>(list);
        foreach (var inst in copy)
        {
            EventExecuting?.Invoke(inst, KnownEvents.OnDestroy);
            if (inst.HasFunction(KnownEvents.OnDestroy))
            {
                if (!inst.TryInvoke(KnownEvents.OnDestroy, out _, out var err) && !string.IsNullOrEmpty(err))
                    ReportError(inst.ScriptPath, TryParseLineFromError(err), err);
            }
        }
    }

    /// <summary>Actualiza todos los scripts: time y onUpdate(dt) / onLateUpdate(dt).</summary>
    public void Tick(double deltaTime, long frame) => Tick(deltaTime, frame, activeGameObjects: null);

    /// <summary>Actualiza scripts; si activeGameObjects no es null, solo se ejecuta onUpdate/onLateUpdate en entidades dentro de ese set (ChunkEntitySleep).</summary>
    public void Tick(double deltaTime, long frame, IReadOnlySet<GameObject>? activeGameObjects)
    {
        BeginTick(deltaTime, frame);
        InvokeOnStarts();
        InvokeOnUpdates(activeGameObjects);
        InvokeOnLateUpdates(activeGameObjects);
        EndTick();
    }

    /// <summary>API de depuración compartida (scripts + motor, p. ej. contornos de colliders).</summary>
    public DebugDrawApi SharedDebugDraw => _debugDraw;

    /// <summary>Inicio de frame de simulación: delta, tiempo acumulado y número de frame.</summary>
    public void BeginTick(double deltaTime, long frame)
    {
        _timeApi.delta = deltaTime;
        _timeApi.time += deltaTime;
        _timeApi.frame = frame;
    }

    /// <summary>Ejecuta onUpdate(dt) en todas las instancias (respetando ChunkEntitySleep si aplica).</summary>
    public void InvokeOnUpdates(IReadOnlySet<GameObject>? activeGameObjects)
    {
        double dt = _timeApi.delta;
        var activeSet = activeGameObjects;
        bool filter = activeSet != null;
        foreach (var inst in _instances)
        {
            var goRef = inst.GameObjectRef;
            if (goRef != null && (goRef.PendingDestroy || !goRef.RuntimeActive)) continue;
            if (filter && (goRef == null || !activeSet!.Contains(goRef)))
                continue;
            EventExecuting?.Invoke(inst, KnownEvents.OnUpdate);
            if (!inst.TryInvoke(KnownEvents.OnUpdate, out _, out var err, dt) && !string.IsNullOrEmpty(err))
                ReportError(inst.ScriptPath, TryParseLineFromError(err), err);
        }
    }

    /// <summary>Ejecuta onLateUpdate(dt).</summary>
    public void InvokeOnLateUpdates(IReadOnlySet<GameObject>? activeGameObjects)
    {
        double dt = _timeApi.delta;
        var activeSet = activeGameObjects;
        bool filter = activeSet != null;
        foreach (var inst in _instances)
        {
            var goRef = inst.GameObjectRef;
            if (goRef != null && (goRef.PendingDestroy || !goRef.RuntimeActive)) continue;
            if (filter && (goRef == null || !activeSet!.Contains(goRef)))
                continue;
            EventExecuting?.Invoke(inst, KnownEvents.OnLateUpdate);
            if (!inst.TryInvoke(KnownEvents.OnLateUpdate, out _, out var err, dt) && !string.IsNullOrEmpty(err))
                ReportError(inst.ScriptPath, TryParseLineFromError(err), err);
        }
    }

    /// <summary>Copia el buffer de Debug al snapshot del frame (llamar al final del tick).</summary>
    public void EndTick() => _debugDraw.FinalizeFrame();

    /// <summary>Últimos comandos Debug del frame de simulación (para el viewport del tab Juego).</summary>
    public IReadOnlyList<DebugDrawItem> GetDebugDrawSnapshot() => _debugDraw.GetLastFrameSnapshot();

    /// <summary>Cantidad de instancias de script activas (para Runtime Info).</summary>
    public int GetActiveScriptCount() => _instances.Count + _layerScriptInstances.Count;

    /// <summary>Memoria Lua en KB (collectgarbage("count")).</summary>
    public double GetLuaMemoryKb()
    {
        if (_disposed) return 0;
        try
        {
            var r = _environment.State.DoString("return collectgarbage('count')");
            if (r?.Length > 0 && r[0] is double d) return d;
            if (r?.Length > 0 && r[0] is int i) return i;
        }
        catch { /* ignore */ }
        return 0;
    }

    /// <summary>Indica si se alcanzó un breakpoint (el runner debe pausar).</summary>
    public bool IsBreakpointHit => _breakpointHit;

    /// <summary>Ubicación del breakpoint alcanzado.</summary>
    public (string? path, int line) GetBreakpointLocation() => (_breakpointPath, _breakpointLine);

    /// <summary>Limpia el estado de breakpoint alcanzado (llamar al reanudar).</summary>
    public void ClearBreakpointHit()
    {
        _breakpointHit = false;
        _breakpointPath = null;
        _breakpointLine = 0;
    }

    /// <summary>Establece los breakpoints (path relativo, línea 1-based). Al pasar a vacío se quita el hook.</summary>
    public void SetBreakpoints(IEnumerable<(string path, int line)> breakpoints)
    {
        _breakpoints.Clear();
        if (breakpoints != null)
        {
            foreach (var bp in breakpoints)
                if (!string.IsNullOrEmpty(bp.path) && bp.line > 0)
                    _breakpoints.Add((NormalizePath(bp.path), bp.line));
        }
        UpdateBreakpointHook();
    }

    private static string NormalizePath(string source)
    {
        if (string.IsNullOrEmpty(source)) return source;
        var s = source.TrimStart('@').Replace('\\', '/');
        return s;
    }

    private void UpdateBreakpointHook()
    {
        if (_disposed) return;
        try
        {
            var state = _environment.State;
            if (_breakpoints.Count > 0)
            {
                if (!_hookInstalled)
                {
                    state["__breakpointCheck"] = (Action<string, int>)((source, line) =>
                    {
                        var path = NormalizePath(source ?? "");
                        if (_breakpoints.Contains((path, line)))
                        {
                            _breakpointHit = true;
                            _breakpointPath = path;
                            _breakpointLine = line;
                        }
                    });
                    state.DoString(@"
                        debug.sethook(function()
                            local i = debug.getinfo(2, 'Sl')
                            if i and i.source and i.currentline then
                                __breakpointCheck(i.source, i.currentline)
                            end
                        end, 'l', 0)
                    ");
                    _hookInstalled = true;
                }
            }
            else
            {
                if (_hookInstalled)
                {
                    state.DoString("debug.sethook()");
                    _hookInstalled = false;
                }
            }
        }
        catch { /* hook no disponible o error */ }
    }

    /// <summary>
    /// Recarga el script: invalida caché, elimina todas las instancias que usan este path
    /// y notifica por ScriptReloaded para que el motor las recree (Opción A: recrear instancias).
    /// </summary>
    public void ReloadScript(string relativePath)
    {
        var norm = ScriptLoader.NormalizeRelativePath(relativePath);
        if (string.IsNullOrEmpty(norm)) return;
        _loader.InvalidateCache(norm);
        _requireModuleCache.Clear();
        var toRemove = _instances.Where(i =>
            string.Equals(ScriptLoader.NormalizeRelativePath(i.ScriptPath), norm, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var inst in toRemove)
        {
            if (inst.GameObjectRef != null && _instancesByObject.TryGetValue(inst.GameObjectRef, out var list))
            {
                list.Remove(inst);
                if (list.Count == 0) _instancesByObject.Remove(inst.GameObjectRef);
            }
            inst.Dispose();
        }
        _instances.RemoveAll(i => toRemove.Contains(i));

        var toRemoveLayer = _layerScriptInstances.Where(i =>
            string.Equals(ScriptLoader.NormalizeRelativePath(i.ScriptPath), norm, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var inst in toRemoveLayer)
        {
            EventExecuting?.Invoke(inst, KnownEvents.OnDestroy);
            if (inst.HasFunction(KnownEvents.OnDestroy))
            {
                if (!inst.TryInvoke(KnownEvents.OnDestroy, out _, out var derr) && !string.IsNullOrEmpty(derr))
                    ReportError(inst.ScriptPath, TryParseLineFromError(derr), derr);
            }
            inst.Dispose();
        }
        _layerScriptInstances.RemoveAll(i => toRemoveLayer.Contains(i));

        if (toRemove.Count > 0 || toRemoveLayer.Count > 0)
            ScriptReloaded?.Invoke(norm);
    }

    public void RemoveInstance(ScriptInstance instance)
    {
        if (instance.GameObjectRef != null && _instancesByObject.TryGetValue(instance.GameObjectRef, out var list))
        {
            list.Remove(instance);
            if (list.Count == 0) _instancesByObject.Remove(instance.GameObjectRef);
        }
        _instances.Remove(instance);
        instance.Dispose();
    }

    /// <summary>Notifica a todos los scripts del objeto un evento (ej: onParentChanged, onChildAdded). El host lo llama al cambiar jerarquía.</summary>
    public void NotifyScripts(GameObject gameObject, string eventName, params object[] args)
    {
        if (gameObject.PendingDestroy || !gameObject.RuntimeActive) return;
        if (!_instancesByObject.TryGetValue(gameObject, out var list)) return;
        foreach (var inst in list)
        {
            if (!inst.TryInvoke(eventName, out _, out var err, args ?? Array.Empty<object>()) && !string.IsNullOrEmpty(err))
                ReportError(inst.ScriptPath, TryParseLineFromError(err), err);
        }
    }

    /// <summary>Instancias de script asociadas al GameObject (para panel Debug).</summary>
    public IReadOnlyList<ScriptInstance> GetScriptInstancesFor(GameObject gameObject)
    {
        if (gameObject == null || _disposed) return Array.Empty<ScriptInstance>();
        return _instancesByObject.TryGetValue(gameObject, out var list) ? list : Array.Empty<ScriptInstance>();
    }

    public void Dispose()
    {
        if (_disposed) return;
        try
        {
            if (_hookInstalled)
                _environment.State.DoString("debug.sethook()");
        }
        catch { /* ignore */ }
        _hookInstalled = false;
        _breakpoints.Clear();
        _instances.Clear();
        _instancesByObject.Clear();
        foreach (var inst in _layerScriptInstances)
            inst.Dispose();
        _layerScriptInstances.Clear();
        _environment.Dispose();
        _disposed = true;
    }

    /// <summary>Convierte texto del Inspector / JSON de propiedades al valor que recibe Lua.</summary>
    public static object? ParseScriptPropertyValue(string? type, string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        return type?.ToLowerInvariant() switch
        {
            "int" => int.TryParse(value, out var i) ? i : 0,
            "float" => float.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var f) ? f : 0f,
            "bool" => bool.TryParse(value, out var b) && b,
            _ => value
        };
    }

    private object? ResolveInjectedProperty(ScriptPropertyEntry p)
    {
        if (string.Equals(p.Type, "object", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(p.Value)) return null;
            var go = _worldApi?.ResolveGameObjectByInstanceId(p.Value.Trim());
            if (go != null)
                return CreateProxy(go, go.InstanceId ?? go.Name, null);
            return null;
        }

        return ParseScriptPropertyValue(p.Type, p.Value);
    }

    private object? ResolveValueForLiveEdit(string? type, string? value)
    {
        if (string.Equals(type, "object", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var go = _worldApi?.ResolveGameObjectByInstanceId(value.Trim());
            if (go != null)
                return CreateProxy(go, go.InstanceId ?? go.Name, null);
            return null;
        }

        return ParseScriptPropertyValue(type, value);
    }

    /// <summary>Inspector en vivo: tipos primitivos y <c>object</c> (InstanceId → <see cref="SelfProxy"/>).</summary>
    public object? ResolveInspectorPropertyValue(string? type, string? value) => ResolveValueForLiveEdit(type, value);

    private void ReportError(string path, int line, string message)
    {
        var formatted = LuaErrorFormatter.Format(path, line, message);
        ScriptError?.Invoke(path, line, formatted);
    }

    /// <summary>Extrae la línea del mensaje de error Lua (ej: "player.lua:42: attempt to index nil") para mostrar en consola.</summary>
    private static int TryParseLineFromError(string? message) => LuaErrorLineParser.TryParseLine(message);
}
