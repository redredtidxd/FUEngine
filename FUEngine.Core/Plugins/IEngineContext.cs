namespace FUEngine.Core;

/// <summary>
/// Contexto del motor en Play para plugins: rutas, APIs ya cableadas y configuración de proyecto.
/// Los tipos concretos de API viven en <c>FUEngine.Runtime</c>; aquí se exponen como <see cref="object"/> para no acoplar Core al runtime.
/// </summary>
public interface IEngineContext
{
    /// <summary>Directorio raíz del proyecto (donde está <c>proyecto.json</c> o equivalente).</summary>
    string ProjectDirectory { get; }

    /// <summary>API de mundo en runtime (típicamente <c>WorldApi</c>); null si aún no se ha configurado.</summary>
    object? World { get; }

    /// <summary>Datos del proyecto (<see cref="ProjectInfo"/>) o null.</summary>
    object? ProjectConfiguration { get; }

    /// <summary>
    /// APIs registradas en el runtime por nombre corto (minúsculas): world, input, game, physics, ui, time, audio, ads, debug.
    /// </summary>
    object? GetRuntimeApi(string name);

    /// <summary>Servicios opcionales del host (p. ej. <c>IEditorLog</c>) por tipo exacto.</summary>
    object? GetService(System.Type serviceType);
}
