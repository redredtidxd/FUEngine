// -----------------------------------------------------------------------------
// FUEngine (FUEngine.Runtime) — Condiciones: LICENSE.md en la raíz del repositorio.
// Copyright (c) Red Redtid. No es licencia MIT/Apache ni "open source" OSI.
// Productos comerciales: revenue share según LICENSE.md. Prohibido integrar
// publicidad de terceros en forks GitHub sin autorización escrita. Plugins
// gratuitos (no venta de extensiones). Build pública sin garantías ni servidores
// de validación del titular.
// -----------------------------------------------------------------------------
using FUEngine.Core;
using FUEngine.Core.Graphics;
using FUEngine.Graphics.Vulkan;

namespace FUEngine.Runtime;

/// <summary>Bucle principal del juego. Usa Core.Renderer con API gráfica Vulkan y Core.Input.InputManager.</summary>
public class GameLoop
{
    public bool IsRunning { get; private set; }
    public Renderer Renderer { get; } = new Renderer();
    public InputManager Input { get; } = new InputManager();
    /// <summary>Runtime de scripting Lua; opcional. Si está asignado, Tick(deltaTime) llama a onUpdate/onLateUpdate.</summary>
    public LuaScriptRuntime? ScriptRuntime { get; set; }

    private long _frameCount;
    /// <summary>Dispositivo gráfico creado por el bucle en Start(); se hace Dispose en Stop().</summary>
    private IGraphicsDevice? _ownedGraphicsDevice;

    /// <summary>Inicia el bucle e intenta inicializar el dispositivo Vulkan. Si falla, el renderer sigue sin dispositivo (no-op).</summary>
    public void Start()
    {
        IsRunning = true;
        if (Renderer.GraphicsDevice == null)
        {
            var vulkan = VulkanGraphicsDevice.Create();
            if (vulkan != null)
            {
                _ownedGraphicsDevice = vulkan;
                Renderer.GraphicsDevice = vulkan;
            }
        }
    }

    /// <summary>Detiene el bucle y libera el dispositivo Vulkan si lo creó el propio bucle.</summary>
    public void Stop()
    {
        IsRunning = false;
        if (_ownedGraphicsDevice != null)
        {
            if (Renderer.GraphicsDevice == _ownedGraphicsDevice)
                Renderer.GraphicsDevice = null;
            _ownedGraphicsDevice.Dispose();
            _ownedGraphicsDevice = null;
        }
    }

    public void Tick()
    {
        Tick(1.0 / 60.0);
    }

    /// <summary>Un frame del bucle. deltaTime en segundos.</summary>
    public void Tick(double deltaTime)
    {
        if (!IsRunning) return;
        Renderer.BeginFrame();
        ScriptRuntime?.Tick(deltaTime, ++_frameCount);
        Renderer.EndFrame();
    }
}
