using FUEngine.Core;

namespace FUEngine.Runtime;

/// <summary>Gestión de escenas/niveles en tiempo de ejecución. Usa Core.Scene.</summary>
public class SceneManager
{
    public Scene? CurrentScene { get; private set; }
    public void LoadScene(Scene scene) => CurrentScene = scene;
    public void LoadScene(string name) => CurrentScene = new Scene { Id = name, Name = name };
}
