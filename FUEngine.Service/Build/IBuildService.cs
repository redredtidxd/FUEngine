using FUEngine.Core;

namespace FUEngine.Service.Build;

/// <summary>
/// Empaqueta el motor + proyecto para distribución: copia del ejecutable, bundle
/// de datos, rename del .exe, escritura de <c>proyecto.json</c> exportado y
/// opcionalmente <c>dotnet publish</c> self-contained.
/// </summary>
public interface IBuildService
{
    void Build(
        ProjectInfo project,
        string projectRootDirectory,
        string outputDirectory,
        string executableBaseName,
        int sceneIndex,
        bool useDotnetPublish,
        Action<string>? log);

    string SanitizeExecutableBaseName(string? raw);
}
