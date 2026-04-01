using System;
using System.Collections.Generic;
using FUEngine.Core;

namespace FUEngine.Runtime;

/// <summary>Implementación de <see cref="IEngineContext"/> para modo Play (APIs ya cableadas en <see cref="LuaScriptRuntime"/>).</summary>
public sealed class PlayLuaEngineContext : IEngineContext
{
    private readonly LuaScriptRuntime _runtime;
    private readonly ProjectInfo? _project;
    private readonly IReadOnlyDictionary<Type, object>? _hostServices;

    public PlayLuaEngineContext(
        LuaScriptRuntime runtime,
        ProjectInfo? project = null,
        IReadOnlyDictionary<Type, object>? hostServices = null)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _project = project;
        _hostServices = hostServices;
    }

    public string ProjectDirectory => _runtime.ProjectDirectory;

    public object? World => _runtime.GetWorldApi();

    public object? ProjectConfiguration => _project;

    public object? GetRuntimeApi(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return name.Trim().ToLowerInvariant() switch
        {
            "world" => _runtime.GetWorldApi(),
            "input" => _runtime.GetInputApi(),
            "game" => _runtime.GetGameApi(),
            "physics" => _runtime.GetPhysicsApi(),
            "ui" => _runtime.GetUiApi(),
            "time" => _runtime.GetTimeApi(),
            "audio" => _runtime.GetAudioApi(),
            "ads" => _runtime.GetAdsApi(),
            "debug" => _runtime.GetDebugDrawApi(),
            _ => null
        };
    }

    public object? GetService(Type serviceType)
    {
        if (serviceType == null) return null;
        if (_hostServices != null && _hostServices.TryGetValue(serviceType, out var o))
            return o;
        return null;
    }
}
