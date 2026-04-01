namespace FUEngine.Core;

/// <summary>Interfaz que debe implementar un plugin del motor. Permite extender sin tocar el core.</summary>
public interface IPlugin
{
    string Id { get; }
    string Name { get; }
    string Version { get; }
    void OnLoad();
    void OnUnload();
}
