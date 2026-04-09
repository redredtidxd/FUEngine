namespace FUEngine.Editor;

/// <summary>
/// Punto de enganche para que el ejecutable (p. ej. WPF) enrute fallos de deserialización JSON a <see cref="FUEngine.Service.IEditorLog"/>.
/// Sin registrar callback, el comportamiento es solo lanzar excepciones como hasta ahora.
/// </summary>
public static class EditorJsonLoadDiagnostics
{
    /// <summary>Mensaje, categoría opcional, ruta de archivo opcional.</summary>
    public static Action<string, string?, string?>? ReportJsonError;
}
