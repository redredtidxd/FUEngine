// -----------------------------------------------------------------------------
// FUEngine (FUEngine.Core) — Condiciones: LICENSE.md en la raíz del repositorio.
// Copyright (c) Red Redtid. No es licencia MIT/Apache ni "open source" OSI.
// Productos comerciales: revenue share según LICENSE.md. Prohibido integrar
// publicidad de terceros en forks GitHub sin autorización escrita. Plugins
// gratuitos (no venta de extensiones). Build pública sin garantías ni servidores
// de validación del titular.
// -----------------------------------------------------------------------------
using FUEngine.Core.Graphics;

namespace FUEngine.Core;

/// <summary>Renderizado base (editor y runtime). Usa IGraphicsDevice (Vulkan por defecto) cuando está asignado.</summary>
public class Renderer
{
    private IGraphicsDevice? _device;

    /// <summary>Dispositivo gráfico (p. ej. Vulkan). Si es null, BeginFrame/EndFrame/Clear no hacen nada.</summary>
    public IGraphicsDevice? GraphicsDevice
    {
        get => _device;
        set => _device = value;
    }

    /// <summary>Configura el color de limpieza en el dispositivo actual.</summary>
    public void SetClearColor(float r, float g, float b, float a = 1f) =>
        _device?.SetClearColor(r, g, b, a);

    public void BeginFrame()
    {
        if (_device?.IsValid == true)
            _device.BeginFrame();
    }

    public void EndFrame()
    {
        if (_device?.IsValid == true)
            _device.EndFrame();
    }

    public void Clear()
    {
        if (_device?.IsValid == true)
            _device.Clear();
    }
}
