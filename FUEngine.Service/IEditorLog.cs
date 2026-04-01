namespace FUEngine.Service;

/// <summary>
/// Servicio de log del editor. Centraliza mensajes de información, advertencia
/// y error para la consola del editor, reportes de fallo y diagnósticos.
/// Las implementaciones pueden escribir a la consola WPF, a archivo, o ambos.
/// </summary>
public interface IEditorLog
{
    void Info(string message, string? category = null);
    void Warning(string message, string? category = null);
    void Error(string message, string? category = null, string? filePath = null, int? line = null);
}
