using FUEngine.Service;

namespace FUEngine;

/// <summary>Implementa <see cref="IEditorLog"/> delegando en el <see cref="EditorLog"/> estático del ejecutable.</summary>
internal sealed class EditorLogServiceAdapter : IEditorLog
{
    public void Info(string message, string? category = null) => EditorLog.Info(message, category);

    public void Warning(string message, string? category = null) => EditorLog.Warning(message, category);

    public void Error(string message, string? category = null, string? filePath = null, int? line = null) =>
        EditorLog.Error(message, category, filePath, line);
}
