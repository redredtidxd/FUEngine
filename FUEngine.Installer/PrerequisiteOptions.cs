namespace FUEngine.Installer;

/// <summary>Opciones de dependencias del sistema antes de copiar el motor.</summary>
internal readonly struct PrerequisiteOptions
{
    internal PrerequisiteOptions(bool installVcRedistX64, bool directXEndUserRuntime, bool dotNet8DesktopDownloadIfMissing)
    {
        InstallVcRedistX64 = installVcRedistX64;
        DirectXEndUserRuntime = directXEndUserRuntime;
        DotNet8DesktopDownloadIfMissing = dotNet8DesktopDownloadIfMissing;
    }

    /// <summary>Visual C++ 2015-2022 x64 (NLua, Vulkan, NAudio).</summary>
    public bool InstallVcRedistX64 { get; }

    /// <summary>DirectX End-User Runtime (web installer; útil en Windows antiguo).</summary>
    public bool DirectXEndUserRuntime { get; }

    /// <summary>Abrir descarga de .NET 8 Desktop si no está (el motor autocontenido no lo requiere).</summary>
    public bool DotNet8DesktopDownloadIfMissing { get; }
}
