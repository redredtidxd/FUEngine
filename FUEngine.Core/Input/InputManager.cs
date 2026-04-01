namespace FUEngine.Core;

/// <summary>Gestión de entrada (teclado, ratón). Usado por editor y runtime. Stub para futuro.</summary>
public class InputManager
{
    public bool GetKey(string key) => false;
    public (float x, float y) GetMousePosition() => (0, 0);
    public bool GetMouseButton(int button) => false;
}
