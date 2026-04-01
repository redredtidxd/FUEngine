namespace FUEngine.Service.Scripting;

/// <summary>
/// Sincroniza el registro de scripts (<c>scripts.json</c>) con los archivos .lua
/// que existen en disco. Detecta scripts nuevos, renombrados o eliminados y
/// actualiza el manifiesto del proyecto.
/// </summary>
public interface IScriptRegistryWriter
{
    void SyncFromDisk(string projectDirectory);
    void AddScript(string scriptId, string relativePath);
    void RemoveScript(string scriptId);
}
